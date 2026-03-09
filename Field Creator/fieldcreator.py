"""
Parse an AGD file, bin existing elevations to 2ft x 2ft in-memory,
fill missing bins by interpolation, render elevation + haul arrows,
and write a SQLite database describing field state and per-bin haul paths.
"""

from __future__ import annotations

import argparse
import csv
import math
import random
import sqlite3
import sys
import time
import xml.etree.ElementTree as ET
from pathlib import Path
from typing import Dict, List, Tuple

import cv2
import numpy as np
from PIL import Image, ImageDraw

BIN_SIZE_M = 0.6096  # 2 ft
EARTH_RADIUS_M = 6_378_137.0
BIN_AREA_M2 = BIN_SIZE_M * BIN_SIZE_M

# Haul-arrow rendering constants (adapted from render_existing_heights.py)
ARROW_LENGTH_PX = 12
ARROW_BASE_HALF_WIDTH_PX = 4
SOURCE_ARROW_SCALE = 2.0
SOURCE_ARROW_COLOR = (255, 0, 0)


# ---------------------------------------------------------------------------
# UTM (WGS‑84) helpers – ported from Application/Data/UTM.cs
# ---------------------------------------------------------------------------

class UTMCoordinate(Tuple[int, bool, float, float]):
    __slots__ = ()

    @property
    def zone(self) -> int:
        return self[0]

    @property
    def is_northern(self) -> bool:
        return self[1]

    @property
    def easting(self) -> float:
        return self[2]

    @property
    def northing(self) -> float:
        return self[3]


_UTM_A = 6378137.0
_UTM_F = 1.0 / 298.257223563
_UTM_K0 = 0.9996
_UTM_B = _UTM_A * (1.0 - _UTM_F)
_UTM_E2 = 1.0 - (_UTM_B * _UTM_B) / (_UTM_A * _UTM_A)
_UTM_EPRIME2 = _UTM_E2 / (1.0 - _UTM_E2)


def _deg_to_rad(deg: float) -> float:
    return deg * math.pi / 180.0


def _rad_to_deg(rad: float) -> float:
    return rad * 180.0 / math.pi


def utm_from_latlon(latitude_deg: float, longitude_deg: float) -> UTMCoordinate:
    """
    Match AgGrade.Data.UTM.FromLatLon (WGS‑84, no Norway/Svalbard special cases).
    Returns (zone, is_northern, easting_m, northing_m).
    """
    if latitude_deg < -80.0 or latitude_deg > 84.0:
        raise ValueError("Latitude out of UTM bounds (-80 to 84 degrees).")

    # Normalize longitude to [-180, 180)
    lon_norm = ((longitude_deg + 180.0) % 360.0 + 360.0) % 360.0 - 180.0

    zone = int(math.floor((lon_norm + 180.0) / 6.0) + 1)
    is_northern = latitude_deg >= 0.0

    lat_rad = _deg_to_rad(latitude_deg)
    lon_rad = _deg_to_rad(lon_norm)

    lon_origin = (zone - 1) * 6 - 180 + 3  # central meridian of zone
    lon_origin_rad = _deg_to_rad(lon_origin)

    sin_lat = math.sin(lat_rad)
    cos_lat = math.cos(lat_rad)

    N = _UTM_A / math.sqrt(1.0 - _UTM_E2 * sin_lat * sin_lat)
    T = math.tan(lat_rad) ** 2
    C = _UTM_EPRIME2 * cos_lat * cos_lat
    A = cos_lat * (lon_rad - lon_origin_rad)

    M = _UTM_A * (
        (1.0 - _UTM_E2 / 4.0 - 3.0 * _UTM_E2 * _UTM_E2 / 64.0 - 5.0 * _UTM_E2 * _UTM_E2 * _UTM_E2 / 256.0) * lat_rad
        - (3.0 * _UTM_E2 / 8.0 + 3.0 * _UTM_E2 * _UTM_E2 / 32.0 + 45.0 * _UTM_E2 * _UTM_E2 * _UTM_E2 / 1024.0)
        * math.sin(2.0 * lat_rad)
        + (15.0 * _UTM_E2 * _UTM_E2 / 256.0 + 45.0 * _UTM_E2 * _UTM_E2 * _UTM_E2 / 1024.0)
        * math.sin(4.0 * lat_rad)
        - (35.0 * _UTM_E2 * _UTM_E2 * _UTM_E2 / 3072.0) * math.sin(6.0 * lat_rad)
    )

    x = _UTM_K0 * N * (
        A
        + (1.0 - T + C) * A**3 / 6.0
        + (5.0 - 18.0 * T + T * T + 72.0 * C - 58.0 * _UTM_EPRIME2) * A**5 / 120.0
    )

    y = _UTM_K0 * (
        M
        + N * math.tan(lat_rad)
        * (
            A * A / 2.0
            + (5.0 - T + 9.0 * C + 4.0 * C * C) * A**4 / 24.0
            + (61.0 - 58.0 * T + T * T + 600.0 * C - 330.0 * _UTM_EPRIME2) * A**6 / 720.0
        )
    )

    easting = x + 500000.0
    northing = y + (0.0 if is_northern else 10000000.0)

    return UTMCoordinate((zone, is_northern, easting, northing))


def utm_to_latlon(
    zone: int,
    is_northern: bool,
    easting: float,
    northing: float,
) -> Tuple[float, float]:
    """
    Match AgGrade.Data.UTM.ToLatLon.
    """
    if zone < 1 or zone > 60:
        raise ValueError("UTM zone must be between 1 and 60.")

    x = easting - 500000.0
    y = northing - (0.0 if is_northern else 10000000.0)

    lon_origin = (zone - 1) * 6 - 180 + 3
    lon_origin_rad = _deg_to_rad(lon_origin)

    M = y / _UTM_K0
    mu = M / (
        _UTM_A
        * (
            1.0
            - _UTM_E2 / 4.0
            - 3.0 * _UTM_E2 * _UTM_E2 / 64.0
            - 5.0 * _UTM_E2 * _UTM_E2 * _UTM_E2 / 256.0
        )
    )

    e1 = (1.0 - math.sqrt(1.0 - _UTM_E2)) / (1.0 + math.sqrt(1.0 - _UTM_E2))

    J1 = 3.0 * e1 / 2.0 - 27.0 * e1**3 / 32.0
    J2 = 21.0 * e1 * e1 / 16.0 - 55.0 * e1**4 / 32.0
    J3 = 151.0 * e1**3 / 96.0
    J4 = 1097.0 * e1**4 / 512.0

    fp = (
        mu
        + J1 * math.sin(2.0 * mu)
        + J2 * math.sin(4.0 * mu)
        + J3 * math.sin(6.0 * mu)
        + J4 * math.sin(8.0 * mu)
    )

    sin_fp = math.sin(fp)
    cos_fp = math.cos(fp)

    C1 = _UTM_EPRIME2 * cos_fp * cos_fp
    T1 = math.tan(fp) ** 2
    N1 = _UTM_A / math.sqrt(1.0 - _UTM_E2 * sin_fp * sin_fp)
    R1 = N1 * (1.0 - _UTM_E2) / (1.0 - _UTM_E2 * sin_fp * sin_fp)
    D = x / (N1 * _UTM_K0)

    lat_rad = fp - (N1 * math.tan(fp) / R1) * (
        D * D / 2.0
        - (5.0 + 3.0 * T1 + 10.0 * C1 - 4.0 * C1 * C1 - 9.0 * _UTM_EPRIME2) * D**4 / 24.0
        + (
            61.0
            + 90.0 * T1
            + 298.0 * C1
            + 45.0 * T1 * T1
            - 252.0 * _UTM_EPRIME2
            - 3.0 * C1 * C1
        )
        * D**6
        / 720.0
    )

    lon_rad = lon_origin_rad + (
        D
        - (1.0 + 2.0 * T1 + C1) * D**3 / 6.0
        + (5.0 - 2.0 * C1 + 28.0 * T1 - 3.0 * C1 * C1 + 8.0 * _UTM_EPRIME2 + 24.0 * T1 * T1)
        * D**5
        / 120.0
    ) / cos_fp

    return _rad_to_deg(lat_rad), _rad_to_deg(lon_rad)


# ---------------------------------------------------------------------------
# Palette + elevation helpers (from render_existing_heights.py, inlined here)
# ---------------------------------------------------------------------------


def hsv_to_rgb(h: float, s: float, v: float) -> tuple[float, float, float]:
    """Convert HSV to RGB. H in [0, 360), S and V in [0, 1]. Returns (R, G, B) in [0, 1]."""
    h = h % 360.0
    if h < 0:
        h += 360.0
    c = v * s
    x = c * (1 - abs((h / 60.0) % 2 - 1))
    m = v - c
    if 0 <= h < 60:
        r, g, b = c, x, 0
    elif 60 <= h < 120:
        r, g, b = x, c, 0
    elif 120 <= h < 180:
        r, g, b = 0, c, x
    elif 180 <= h < 240:
        r, g, b = 0, x, c
    elif 240 <= h < 300:
        r, g, b = x, 0, c
    else:
        r, g, b = c, 0, x
    return r + m, g + m, b + m


def generate_palette() -> list[tuple[int, int, int]]:
    """256 colors: hue from 240 (blue) to 0 (red), S=1, V=1."""
    palette = []
    for i in range(256):
        hue = 240.0 - (i / 255.0) * 240.0
        r, g, b = hsv_to_rgb(hue, 1.0, 1.0)
        palette.append((int(r * 255 + 0.5), int(g * 255 + 0.5), int(b * 255 + 0.5)))
    return palette


def fill_missing_bins(
    values: Dict[Tuple[int, int], float],
    grid_width: int,
    grid_height: int,
) -> Dict[Tuple[int, int], float]:
    """
    Fill only gaps *inside* the field: a missing bin is filled only if it has
    at least two neighbors in the original data. This fixes sparse rows
    without filling regions outside the field (bounding-box corners).
    """
    max_bin_x = grid_width - 1
    max_bin_y = grid_height - 1
    original = set(values.keys())
    out: Dict[Tuple[int, int], float] = dict(values)
    if not original:
        return out

    for by in range(max_bin_y + 1):
        for bx in range(max_bin_x + 1):
            if (bx, by) in original:
                continue
            # Count neighbors that are in the original data (not just filled)
            orig_neighbors = [
                (nx, ny)
                for nx, ny in [(bx, by - 1), (bx, by + 1), (bx - 1, by), (bx + 1, by)]
                if 0 <= nx <= max_bin_x and 0 <= ny <= max_bin_y and (nx, ny) in original
            ]
            if len(orig_neighbors) < 2:
                # Outside field or on edge: do not fill
                continue
            # Average over all neighbors that we have values for (original or already filled)
            neighbor_vals = [
                out[(nx, ny)]
                for nx, ny in [(bx, by - 1), (bx, by + 1), (bx - 1, by), (bx + 1, by)]
                if 0 <= nx <= max_bin_x and 0 <= ny <= max_bin_y and (nx, ny) in out
            ]
            if neighbor_vals:
                out[(bx, by)] = sum(neighbor_vals) / len(neighbor_vals)

    return out


def elevation_range(values: Dict[Tuple[int, int], float]) -> Tuple[float, float]:
    """Min and max elevation. If all same, return center ± 0.01 to avoid div by zero."""
    elevations = list(values.values())
    if not elevations:
        raise ValueError("No elevations")
    elev_min = min(elevations)
    elev_max = max(elevations)
    if elev_max - elev_min < 1e-9:
        center = (elev_min + elev_max) / 2
        elev_min = center - 0.01
        elev_max = center + 0.01
    return elev_min, elev_max


# ---------------------------------------------------------------------------
# Haul arrow drawing helpers (from render_existing_heights.py, inlined)
# ---------------------------------------------------------------------------


def _direction_to_image_vector(deg: float) -> Tuple[float, float]:
    """Compass degrees (0=north, 90=east) -> unit vector (dx, dy) in image coords (x right, y down)."""
    rad = math.radians(deg)
    return math.sin(rad), -math.cos(rad)


def _draw_arrow_hollow_triangle(
    draw: ImageDraw.ImageDraw,
    px: float,
    py: float,
    direction_deg: float,
    color: Tuple[int, int, int] = (0, 0, 0),
    length_px: float = ARROW_LENGTH_PX,
    base_half_width_px: float = ARROW_BASE_HALF_WIDTH_PX,
) -> None:
    """Draw one arrow at (px, py) as a hollow triangle (outline only, no fill)."""
    dx, dy = _direction_to_image_vector(direction_deg)
    end_x = px + length_px * dx
    end_y = py + length_px * dy
    base_x = px - 0.35 * length_px * dx
    base_y = py - 0.35 * length_px * dy
    perp_x, perp_y = -dy, dx
    tip = (round(end_x), round(end_y))
    left = (
        round(base_x + base_half_width_px * perp_x),
        round(base_y + base_half_width_px * perp_y),
    )
    right = (
        round(base_x - base_half_width_px * perp_x),
        round(base_y - base_half_width_px * perp_y),
    )
    draw.polygon([tip, left, right], outline=color, fill=None)


# ---------------------------------------------------------------------------
# Haul image (PNG+KML) processing, adapted from HaulImageProcessor.py
# ---------------------------------------------------------------------------


def haversine_meters(lat1: float, lon1: float, lat2: float, lon2: float) -> float:
    """Distance between two (lat, lon) points in meters."""
    phi1, phi2 = math.radians(lat1), math.radians(lat2)
    dphi = math.radians(lat2 - lat1)
    dlam = math.radians(lon2 - lon1)
    a = math.sin(dphi / 2) ** 2 + math.cos(phi1) * math.cos(phi2) * math.sin(dlam / 2) ** 2
    c = 2 * math.atan2(math.sqrt(a), math.sqrt(1 - a))
    return EARTH_RADIUS_M * c


def drop_duplicate_dots(rows: list[list], min_separation_ft: float = 1.0) -> list[list]:
    """
    Remove duplicate dots: keep first of any pair closer than min_separation_ft.
    Each row is [lat, lon, direction_degrees, source].
    """
    min_separation_m = min_separation_ft * 0.3048
    kept: list[list] = []
    for row in rows:
        lat, lon = float(row[0]), float(row[1])
        is_dup = False
        for k in kept:
            if haversine_meters(lat, lon, float(k[0]), float(k[1])) < min_separation_m:
                is_dup = True
                break
        if not is_dup:
            kept.append(row)
    return kept


def get_kml_path(image_path: Path) -> Path | None:
    """Path to KML file with same stem as image (e.g. image.png -> image.kml), or None."""
    kml = image_path.parent / f"{image_path.stem}.kml"
    return kml if kml.is_file() else None


def extract_bbox_from_kml(kml_path: Path) -> dict[str, float] | None:
    """
    Extract bounding box from this project's KML: GroundOverlay with <coordinates>
    containing lon,lat,alt,lon,lat,alt,... (comma-separated). Returns north, south, east, west.
    """
    try:
        tree = ET.parse(kml_path)
    except (ET.ParseError, OSError):
        return None
    root = tree.getroot()
    for el in root.iter():
        if "coordinates" not in el.tag or not (el.text and el.text.strip()):
            continue
        parts = [p.strip() for p in el.text.replace("\n", " ").replace(",", " ").split() if p.strip()]
        lons: list[float] = []
        lats: list[float] = []
        for i in range(0, len(parts) - 1, 3):
            if i + 1 >= len(parts):
                break
            try:
                lons.append(float(parts[i]))
                lats.append(float(parts[i + 1]))
            except ValueError:
                pass
        if len(lons) >= 2 and len(lats) >= 2:
            return {
                "north": max(lats),
                "south": min(lats),
                "east": max(lons),
                "west": min(lons),
            }
    return None


def pixel_to_lat_lon(
    pixel_x: float,
    pixel_y: float,
    image_width: int,
    image_height: int,
    bbox: dict[str, float],
) -> tuple[float, float]:
    """
    Convert pixel (x, y) to (latitude, longitude) using the KML bounding box.
    Image origin is top-left; top = north, bottom = south, left = west, right = east.
    """
    if image_width <= 0 or image_height <= 0:
        raise ValueError("image_width and image_height must be positive")
    north, south = bbox["north"], bbox["south"]
    east, west = bbox["east"], bbox["west"]
    lon = west + (east - west) * (pixel_x / image_width)
    lat = north - (north - south) * (pixel_y / image_height)
    return lat, lon


def direction_degrees_north_up(dx: float, dy: float) -> float:
    """Direction of (dx, dy) in degrees; 0 = North/up, 90 = East, in [0, 360)."""
    deg = math.degrees(math.atan2(dx, -dy))
    return deg + 360.0 if deg < 0 else deg


def load_image(image_path: Path) -> np.ndarray:
    img = cv2.imread(str(image_path))
    if img is None:
        raise FileNotFoundError(f"Could not load image: {image_path}")
    return img


def build_outline_mask(img: np.ndarray, black_threshold: int) -> np.ndarray:
    """Binary mask where black (outline) pixels are 255. Color of fill is ignored."""
    b, g, r = cv2.split(img)
    black = (b <= black_threshold) & (g <= black_threshold) & (r <= black_threshold)
    mask = np.uint8(np.where(black, 255, 0))
    # Close tiny gaps in the outline so one arrow = one contour
    kernel = np.ones((2, 2), np.uint8)
    mask = cv2.morphologyEx(mask, cv2.MORPH_CLOSE, kernel)
    return mask


def _angle_at_vertex(prev_pt: np.ndarray, pt: np.ndarray, next_pt: np.ndarray) -> float:
    """Angle in radians at pt formed by prev_pt -> pt -> next_pt (interior angle)."""
    a = prev_pt - pt
    b = next_pt - pt
    na = np.linalg.norm(a)
    nb = np.linalg.norm(b)
    if na < 1e-9 or nb < 1e-9:
        return math.pi
    cos_angle = float(np.dot(a, b) / (na * nb))
    cos_angle = float(np.clip(cos_angle, -1.0, 1.0))
    return float(math.acos(cos_angle))


def _acute_vertex_index(hull_pts: np.ndarray) -> int:
    """Index (0, 1, or 2) of the hull vertex with the smallest (acute) angle = tip."""
    n = len(hull_pts)
    if n != 3:
        return 0
    angles = [
        _angle_at_vertex(hull_pts[(i - 1) % n], hull_pts[i], hull_pts[(i + 1) % n])
        for i in range(3)
    ]
    return int(np.argmin(angles))


def _triangle_area(a: np.ndarray, b: np.ndarray, c: np.ndarray) -> float:
    """Signed area of triangle * 2 (for comparison)."""
    return float(abs((b[0] - a[0]) * (c[1] - a[1]) - (c[0] - a[0]) * (b[1] - a[1])))


def _max_area_triangle_three_indices(pts: np.ndarray) -> np.ndarray:
    """From convex hull pts (n>=3), return 3 points that form the largest area triangle."""
    n = len(pts)
    best_area = 0.0
    best = (0, 1, 2)
    for i in range(n):
        for j in range(i + 1, n):
            for k in range(j + 1, n):
                area = _triangle_area(pts[i], pts[j], pts[k])
                if area > best_area:
                    best_area = area
                    best = (i, j, k)
    return np.array([pts[best[0]], pts[best[1]], pts[best[2]]], dtype=np.float64)


def _approx_triangle(hull_pts: np.ndarray) -> np.ndarray | None:
    """If hull has 3+ points, return 3 vertices (best triangle); else None."""
    n = len(hull_pts)
    if n < 3:
        return None
    if n == 3:
        return hull_pts.astype(np.float64)
    # Try approxPolyDP with increasing epsilon until we get 3 vertices
    contour = hull_pts.reshape(-1, 1, 2).astype(np.int32)
    peri = cv2.arcLength(contour, True)
    for frac in (0.03, 0.06, 0.10, 0.15, 0.22, 0.30):
        approx = cv2.approxPolyDP(contour, frac * peri, True)
        ap = approx.reshape(-1, 2).astype(np.float64)
        if len(ap) == 3:
            return ap
    # Fallback: pick the 3 hull vertices that form the largest area triangle (best fit arrow)
    return _max_area_triangle_three_indices(hull_pts.astype(np.float64))


def find_arrows_from_outline(
    outline_mask: np.ndarray,
    min_area: int,
    max_area: int,
) -> list[tuple[float, float, float, float]]:
    """
    Find arrow center and direction from each black-outline contour.
    Returns list of (cx, cy, dx, dy) with (dx, dy) unit vector toward tip (acute vertex).
    """
    contours, _ = cv2.findContours(outline_mask, cv2.RETR_EXTERNAL, cv2.CHAIN_APPROX_SIMPLE)
    result: list[tuple[float, float, float, float]] = []
    for c in contours:
        area = cv2.contourArea(c)
        if area < min_area:
            continue
        if max_area > 0 and area > max_area:
            continue
        hull = cv2.convexHull(c)
        hull_pts = hull.reshape(-1, 2)
        tri = _approx_triangle(hull_pts)
        if tri is None:
            continue
        hull_pts = tri
        M = cv2.moments(c)
        if M["m00"] == 0:
            continue
        cx = M["m10"] / M["m00"]
        cy = M["m01"] / M["m00"]
        tip_idx = _acute_vertex_index(hull_pts)
        tx, ty = hull_pts[tip_idx][0], hull_pts[tip_idx][1]
        dx = tx - cx
        dy = ty - cy
        length = float(np.sqrt(dx * dx + dy * dy))
        if length < 1e-6:
            continue
        dx /= length
        dy /= length
        result.append((float(cx), float(cy), float(dx), float(dy)))
    return result


def arrows_to_rows(
    arrows: list[tuple[float, float, float, float]],
    image_width: int,
    image_height: int,
    bbox: dict[str, float],
    source: str | None = None,
) -> list[list]:
    """Convert arrows to CSV-like rows (lat, lon, direction_degrees [, source])."""
    rows: list[list] = []
    for cx, cy, dx, dy in arrows:
        lat, lon = pixel_to_lat_lon(cx, cy, image_width, image_height, bbox)
        direction = direction_degrees_north_up(dx, dy)
        row: list = [lat, lon, direction]
        if source is not None:
            row.append(source)
        rows.append(row)
    return rows


def haul_samples_from_png_kml_dir(
    input_dir: Path,
    min_separation_ft: float = 1.0,
    min_area: int = 15,
    max_area: int = 2500,
    black_threshold: int = 120,
) -> list[tuple[float, float, float]]:
    """
    Detect haul arrows from all PNG+KML pairs in input_dir.
    Returns list of (lat, lon, direction_degrees).
    """
    if not input_dir.is_dir():
        raise FileNotFoundError(f"haul image directory not found: {input_dir}")

    pngs = sorted(input_dir.glob("*.png"))
    pairs: list[tuple[Path, Path, dict[str, float]]] = []
    for png in pngs:
        kml = get_kml_path(png)
        if kml is None:
            print(f"WARNING: skip {png.name}: no matching KML", file=sys.stderr)
            continue
        bbox = extract_bbox_from_kml(kml)
        if bbox is None:
            print(f"WARNING: skip {png.name}: no bbox in {kml.name}", file=sys.stderr)
            continue
        pairs.append((png, kml, bbox))

    if not pairs:
        raise RuntimeError(f"no PNG+KML pairs found in haul image directory: {input_dir}")

    all_rows: list[list] = []
    for png_path, kml_path, bbox in pairs:
        try:
            image = load_image(png_path)
        except FileNotFoundError as exc:
            print(exc, file=sys.stderr)
            continue
        outline_mask = build_outline_mask(image, black_threshold)
        arrows = find_arrows_from_outline(outline_mask, min_area, max_area)
        h, w = image.shape[:2]
        rows = arrows_to_rows(arrows, w, h, bbox, source=png_path.name)
        all_rows.extend(rows)
        print(f"INFO: {png_path.name}: detected {len(arrows)} arrow(s)", file=sys.stderr)

    if not all_rows:
        raise RuntimeError(f"no arrows detected in any haul image in {input_dir}")

    n_before = len(all_rows)
    all_rows = drop_duplicate_dots(all_rows, min_separation_ft=min_separation_ft)
    n_removed = n_before - len(all_rows)
    if n_removed:
        print(
            f"INFO: haul image dedup removed {n_removed} near-duplicate dots "
            f"(within {min_separation_ft} ft)",
            file=sys.stderr,
        )

    samples: list[tuple[float, float, float]] = []
    for row in all_rows:
        lat = float(row[0])
        lon = float(row[1])
        direction = float(row[2])
        samples.append((lat, lon, direction))
    print(f"INFO: total haul samples after dedup: {len(samples)}", file=sys.stderr)
    return samples


# ---------------------------------------------------------------------------
# Haul vector field over bins (adapted from render_existing_heights._load_haul_arrows)
# ---------------------------------------------------------------------------


def _blend_vector_field(
    field_vectors: Dict[Tuple[int, int], Tuple[float, float]],
    bins_to_draw: set[Tuple[int, int]],
    known_vectors: Dict[Tuple[int, int], Tuple[float, float]],
    passes: int = 5,
) -> Dict[Tuple[int, int], Tuple[float, float]]:
    """
    Smooth direction transitions by iterative neighbor blending.
    Keeps source bins more stable while allowing surrounding interpolation
    to transition gradually between directions.
    """
    if not field_vectors:
        return field_vectors

    neighbors = [
        (-1, -1), (0, -1), (1, -1),
        (-1, 0),           (1, 0),
        (-1, 1),  (0, 1),  (1, 1),
    ]
    blended: Dict[Tuple[int, int], Tuple[float, float]] = dict(field_vectors)

    for pass_i in range(max(0, passes)):
        if passes > 0:
            print(
                f"INFO: haul_heading: blend pass {pass_i + 1}/{passes}...",
                file=sys.stderr,
                flush=True,
            )
        next_vectors: Dict[Tuple[int, int], Tuple[float, float]] = {}
        for bx, by in bins_to_draw:
            base = blended.get((bx, by))
            if base is None:
                continue

            if (bx, by) in known_vectors:
                self_weight = 6.0
                neighbor_weight = 0.35
            else:
                self_weight = 1.0
                neighbor_weight = 0.8

            sx = base[0] * self_weight
            sy = base[1] * self_weight
            w = self_weight

            for ox, oy in neighbors:
                vec = blended.get((bx + ox, by + oy))
                if vec is None:
                    continue
                sx += vec[0] * neighbor_weight
                sy += vec[1] * neighbor_weight
                w += neighbor_weight

            if w <= 0.0:
                next_vectors[(bx, by)] = base
                continue

            sx /= w
            sy /= w
            norm = math.hypot(sx, sy)
            if norm <= 0.0:
                next_vectors[(bx, by)] = base
            else:
                next_vectors[(bx, by)] = (sx / norm, sy / norm)

        blended = next_vectors

    return blended


def build_haul_heading_by_bin(
    samples: list[tuple[float, float, float]],
    min_x: float,
    min_y: float,
    grid_width: int,
    grid_height: int,
    bins_to_draw: set[Tuple[int, int]],
    utm_zone: int,
    utm_is_northern: bool,
) -> Dict[Tuple[int, int], float]:
    """
    From raw haul samples (lat, lon, heading_deg), build a smooth heading
    field over the binned grid: (bin_x, bin_y) -> heading_deg.
    Uses the same UTM (min_x, min_y) as the grid so bin indices match.
    """
    if not samples:
        return {}

    # 1) Accumulate unit vectors per bin from samples. Convert lat/lon to UTM
    #    so bin indices match the grid (min_x, min_y are UTM easting/northing).
    accum: Dict[Tuple[int, int], List[Tuple[float, float]]] = {}
    n_samples = len(samples)
    for i, (lat, lon, direction_deg) in enumerate(samples):
        if (i + 1) % 2000 == 0 or i == 0:
            print(f"INFO: haul_heading: assigning samples {i + 1}/{n_samples}...", file=sys.stderr, flush=True)
        try:
            utm = utm_from_latlon(lat, lon)
        except ValueError:
            continue
        if utm.zone != utm_zone or utm.is_northern != utm_is_northern:
            continue
        bx_f = (utm.easting - min_x) / BIN_SIZE_M
        by_f = (utm.northing - min_y) / BIN_SIZE_M
        bx = int(math.floor(bx_f))
        by = int(math.floor(by_f))
        if bx < 0 or bx >= grid_width or by < 0 or by >= grid_height:
            continue

        rad = math.radians(direction_deg)
        dx = math.sin(rad)
        dy = math.cos(rad)
        accum.setdefault((bx, by), []).append((dx, dy))

    print(f"INFO: haul_heading: {len(accum)} bins from samples", file=sys.stderr, flush=True)
    if not accum:
        return {}

    known: Dict[Tuple[int, int], Tuple[float, float]] = {}
    for (bx, by), vecs in accum.items():
        sx = sum(v[0] for v in vecs)
        sy = sum(v[1] for v in vecs)
        norm = math.hypot(sx, sy)
        if norm <= 0.0:
            continue
        known[(bx, by)] = (sx / norm, sy / norm)

    if not known:
        return {}

    # 2) Interpolate vectors so every requested bin gets an arrow.
    field_vectors: Dict[Tuple[int, int], Tuple[float, float]] = dict(known)
    pending: set[Tuple[int, int]] = set(bins_to_draw) - set(field_vectors.keys())
    print(
        f"INFO: haul_heading: interpolating to {len(pending)} bins (from {len(known)} sample bins)...",
        file=sys.stderr,
        flush=True,
    )
    neighbors = [
        (-1, -1), (0, -1), (1, -1),
        (-1, 0),           (1, 0),
        (-1, 1),  (0, 1),  (1, 1),
    ]
    iter_num = 0
    while pending:
        iter_num += 1
        if iter_num % 5 == 1 or iter_num <= 2:
            print(
                f"INFO: haul_heading: interpolation pass {iter_num}, {len(pending)} pending...",
                file=sys.stderr,
                flush=True,
            )
        progressed = False
        still_pending: set[Tuple[int, int]] = set()
        for bx, by in pending:
            sx = 0.0
            sy = 0.0
            count = 0
            for ox, oy in neighbors:
                key = (bx + ox, by + oy)
                vec = field_vectors.get(key)
                if vec is None:
                    continue
                sx += vec[0]
                sy += vec[1]
                count += 1
            if count < 2:
                still_pending.add((bx, by))
                continue
            norm = math.hypot(sx, sy)
            if norm <= 0.0:
                still_pending.add((bx, by))
                continue
            field_vectors[(bx, by)] = (sx / norm, sy / norm)
            progressed = True
        if not progressed:
            break
        pending = still_pending

    print(
        f"INFO: haul_heading: interpolation done; {len(pending)} isolated bins remaining",
        file=sys.stderr,
        flush=True,
    )
    # 3) Fallback for isolated bins: use nearest known vector.
    if pending and known:
        known_items = list(known.items())
        n_pending = len(pending)
        print(
            f"INFO: haul_heading: nearest-vector fallback for {n_pending} isolated bins...",
            file=sys.stderr,
            flush=True,
        )
        done = 0
        for bx, by in pending:
            done += 1
            if n_pending >= 10000 and (done % 50000 == 0 or done == n_pending):
                print(
                    f"INFO: haul_heading: fallback {done}/{n_pending}...",
                    file=sys.stderr,
                    flush=True,
                )
            best_vec = None
            best_d2 = None
            for (kx, ky), vec in known_items:
                d2 = (kx - bx) * (kx - bx) + (ky - by) * (ky - by)
                if best_d2 is None or d2 < best_d2:
                    best_d2 = d2
                    best_vec = vec
            if best_vec is not None:
                field_vectors[(bx, by)] = best_vec

    # 4) Blend interpolated vectors to reduce abrupt block boundaries.
    print("INFO: haul_heading: blending (5 passes)...", file=sys.stderr, flush=True)
    field_vectors = _blend_vector_field(
        field_vectors=field_vectors,
        bins_to_draw=bins_to_draw,
        known_vectors=known,
        passes=5,
    )

    # 5) Convert unit vectors back to compass headings.
    heading_by_bin: Dict[Tuple[int, int], float] = {}
    for (bx, by), (dx_avg, dy_avg) in field_vectors.items():
        direction_deg = math.degrees(math.atan2(dx_avg, dy_avg)) % 360.0
        heading_by_bin[(bx, by)] = direction_deg
    return heading_by_bin


def _local_xy_to_latlon(x: float, y: float, lat0: float, lon0: float) -> Tuple[float, float]:
    """
    Legacy helper kept for compatibility in other modules.
    NOTE: In this script, core binning now uses UTM; this equirectangular
    helper is only used where we still need an approximate local XY
    for visualization unrelated to bin indexing.
    """
    lat0_rad = math.radians(lat0)
    lat = math.degrees(y / EARTH_RADIUS_M + lat0_rad)
    lon = math.degrees(x / (EARTH_RADIUS_M * math.cos(lat0_rad))) + lon0
    return lat, lon


def parse_agd_points(
    agd_path: Path,
) -> Tuple[List[Tuple[float, float, float, float]], List[Tuple[float, float, str, float]]]:
    """
    Parse AGD lines using extract_agd_heights.py rules and return:
    - points: (lat, lon, existing_elevation, target_elevation)
    - benchmarks: (lat, lon, point_name, elevation_m) where code/name contains BM or MB
    """
    try:
        text = agd_path.read_text(encoding="utf-8")
    except UnicodeDecodeError:
        text = agd_path.read_text(errors="ignore")

    lines = text.splitlines()
    if not lines:
        raise ValueError(f"AGD file is empty: {agd_path}")

    points: List[Tuple[float, float, float, float]] = []
    benchmarks: List[Tuple[float, float, str, float]] = []
    # Skip AGD header row
    for line_no, line in enumerate(lines[1:], start=2):
        raw = line.strip()
        if not raw:
            continue
        parts = [p.strip() for p in raw.split(",")]
        if len(parts) < 4:
            continue
        lat_s, lon_s, existing_s, target_s = parts[:4]
        point_name = parts[4].strip() if len(parts) > 4 else ""
        code_s = parts[5].strip() if len(parts) > 5 else ""
        benchmark_token = f"{point_name} {code_s}".upper()
        if "MB" in benchmark_token or "BM" in benchmark_token:
            try:
                lat = float(lat_s)
                lon = float(lon_s)
                elevation_m = float(existing_s)
            except ValueError:
                continue
            name = point_name or code_s
            benchmarks.append((lat, lon, name, elevation_m))
            continue
        if not existing_s or not target_s:
            continue
        try:
            lat = float(lat_s)
            lon = float(lon_s)
            existing = float(existing_s)
            target = float(target_s)
        except ValueError:
            continue
        # Mirror extract/bin logic: require both non-zero
        if existing == 0.0 or target == 0.0:
            continue
        points.append((lat, lon, existing, target))

    if not points:
        raise ValueError(f"No valid elevation points parsed from AGD: {agd_path}")
    return points, benchmarks


def bin_agd_points_2ft(
    points: List[Tuple[float, float, float, float]],
) -> Tuple[
    Dict[Tuple[int, int], float],
    Dict[Tuple[int, int], float],
    Dict[Tuple[int, int], Tuple[float, float]],
    Dict[Tuple[int, int], Tuple[float, float, float, float]],
    float,
    float,
    float,
    float,
    float,
    float,
    int,
    int,
    int,
    bool,
]:
    """
    In-memory equivalent of CalculateCutFillVolumesWithBinning in AGDLoader.cs
    for existing + target elevations, using the same UTM-based binning.
    Returns:
    - existing_values (bin -> avg existing elev for original AGD points, no fill)
    - target_values (bin -> avg target elev for original AGD points, no fill)
    - centroids (bin -> centroid_lat, centroid_lon) for every bin in the full grid
    - bins_raw (bin -> centroid_lat, centroid_lon, existing_elev, area_m2) for image helpers
    - mean_lat, mean_lon (for optional visualization)
    - min_x, min_y (UTM easting/northing min over points; matches AGDLoader FieldMinX/FieldMinY)
    - max_x, max_y (UTM easting/northing max over points; matches AGDLoader FieldMaxX/FieldMaxY)
    - grid_width, grid_height (bin dimensions)
    - utm_zone, utm_is_northern (zone parameters used for all conversions)
    """
    if not points:
        raise ValueError("No AGD points to bin")

    # Field centroid for reporting/visualization (not used for binning).
    mean_lat = sum(p[0] for p in points) / len(points)
    mean_lon = sum(p[1] for p in points) / len(points)

    # Match AGDLoader.ConvertToLocalCoordinates: use SW corner (min lat, min lon)
    # to determine UTM zone / hemisphere, then convert every point to UTM.
    min_lat = min(p[0] for p in points)
    min_lon = min(p[1] for p in points)
    sw_utm = utm_from_latlon(min_lat, min_lon)
    utm_zone = sw_utm.zone
    utm_is_northern = sw_utm.is_northern

    utm_points: List[Tuple[float, float, float, float]] = []
    for lat, lon, existing, target in points:
        coord = utm_from_latlon(lat, lon)
        if coord.zone != utm_zone:
            # AGDLoader does not support fields that cross UTM zones; we mirror that
            raise ValueError(
                f"AGD field crosses UTM zones (got zone {coord.zone}, expected {utm_zone})"
            )
        utm_points.append((coord.easting, coord.northing, existing, target))

    min_x = min(p[0] for p in utm_points)
    max_x = max(p[0] for p in utm_points)
    min_y = min(p[1] for p in utm_points)
    max_y = max(p[1] for p in utm_points)

    grid_width = int(math.ceil((max_x - min_x) / BIN_SIZE_M))
    grid_height = int(math.ceil((max_y - min_y) / BIN_SIZE_M))
    if grid_width <= 0 or grid_height <= 0:
        raise ValueError("Computed non-positive grid dimensions from AGD data")

    # Aggregate existing/target elevations per bin, mirroring the C# logic that
    # only bins points with non-zero cut/fill (existing != target).
    grouped: Dict[Tuple[int, int], List[Tuple[float, float]]] = {}
    for easting, northing, existing, target in utm_points:
        cut_fill = existing - target
        if abs(cut_fill) <= 0.0:
            continue
        bx = int(math.floor((easting - min_x) / BIN_SIZE_M))
        by = int(math.floor((northing - min_y) / BIN_SIZE_M))
        if bx < 0 or bx >= grid_width or by < 0 or by >= grid_height:
            continue
        grouped.setdefault((bx, by), []).append((existing, target))

    existing_values: Dict[Tuple[int, int], float] = {}
    target_values: Dict[Tuple[int, int], float] = {}
    centroids: Dict[Tuple[int, int], Tuple[float, float]] = {}
    bins_raw: Dict[Tuple[int, int], Tuple[float, float, float, float]] = {}

    # Precompute averages for bins that received AGD points.
    for (bx, by), pairs in grouped.items():
        avg_existing = sum(p[0] for p in pairs) / len(pairs)
        avg_target = sum(p[1] for p in pairs) / len(pairs)
        existing_values[(bx, by)] = avg_existing
        target_values[(bx, by)] = avg_target

    # Now mirror AGDLoader: materialize the full rectangular grid of bins and
    # compute centroids for every bin using UTM -> lat/lon.
    for by in range(grid_height):
        for bx in range(grid_width):
            bin_min_x = min_x + bx * BIN_SIZE_M
            bin_min_y = min_y + by * BIN_SIZE_M
            bin_max_x = bin_min_x + BIN_SIZE_M
            bin_max_y = bin_min_y + BIN_SIZE_M
            cx = 0.5 * (bin_min_x + bin_max_x)
            cy = 0.5 * (bin_min_y + bin_max_y)
            centroid_lat, centroid_lon = utm_to_latlon(
                utm_zone,
                utm_is_northern,
                cx,
                cy,
            )
            centroids[(bx, by)] = (centroid_lat, centroid_lon)

            avg_existing = existing_values.get((bx, by), 0.0)
            bins_raw[(bx, by)] = (centroid_lat, centroid_lon, avg_existing, BIN_AREA_M2)

    return (
        existing_values,
        target_values,
        centroids,
        bins_raw,
        mean_lat,
        mean_lon,
        min_x,
        min_y,
        max_x,
        max_y,
        grid_width,
        grid_height,
        utm_zone,
        utm_is_northern,
    )


def subset_center_bottom_eighth(
    existing_values: Dict[Tuple[int, int], float],
    target_values: Dict[Tuple[int, int], float],
    centroids: Dict[Tuple[int, int], Tuple[float, float]],
    bins_raw: Dict[Tuple[int, int], Tuple[float, float, float, float]],
    min_x: float,
    min_y: float,
    max_x: float,
    max_y: float,
    grid_width: int,
    grid_height: int,
    utm_zone: int,
    utm_is_northern: bool,
) -> tuple[
    Dict[Tuple[int, int], float],
    Dict[Tuple[int, int], float],
    Dict[Tuple[int, int], Tuple[float, float]],
    Dict[Tuple[int, int], Tuple[float, float, float, float]],
    float,
    float,
    float,
    float,
    int,
    int,
    int,
    bool,
]:
    """
    Keep only center-bottom (south) one-eighth of field:
      width fraction = 1/2 (centered), height fraction = 1/4 (bottom),
      total area fraction = 1/8.
    Reindex bins so subset starts at (0,0), and adjust min_x/min_y/max_x/max_y accordingly.
    """
    x0 = int(grid_width * 0.25)
    x1 = int(grid_width * 0.75)
    y0 = 0
    y1 = max(1, int(grid_height * 0.25))

    def in_subset(bx: int, by: int) -> bool:
        return x0 <= bx < x1 and y0 <= by < y1

    ex2: Dict[Tuple[int, int], float] = {}
    tg2: Dict[Tuple[int, int], float] = {}
    ct2: Dict[Tuple[int, int], Tuple[float, float]] = {}
    br2: Dict[Tuple[int, int], Tuple[float, float, float, float]] = {}

    for (bx, by), v in existing_values.items():
        if in_subset(bx, by):
            ex2[(bx - x0, by - y0)] = v
    for (bx, by), v in target_values.items():
        if in_subset(bx, by):
            tg2[(bx - x0, by - y0)] = v
    for (bx, by), v in centroids.items():
        if in_subset(bx, by):
            ct2[(bx - x0, by - y0)] = v
    for (bx, by), v in bins_raw.items():
        if in_subset(bx, by):
            br2[(bx - x0, by - y0)] = v

    new_w = max(1, x1 - x0)
    new_h = max(1, y1 - y0)
    new_min_x = min_x + x0 * BIN_SIZE_M
    new_min_y = min_y + y0 * BIN_SIZE_M
    new_max_x = new_min_x + new_w * BIN_SIZE_M
    new_max_y = new_min_y + new_h * BIN_SIZE_M
    return ex2, tg2, ct2, br2, new_min_x, new_min_y, new_max_x, new_max_y, new_w, new_h, utm_zone, utm_is_northern


def write_binned_elevation_csv(
    out_path: Path,
    elevations: Dict[Tuple[int, int], float],
    centroids: Dict[Tuple[int, int], Tuple[float, float]],
) -> None:
    with out_path.open("w", newline="", encoding="utf-8") as f:
        writer = csv.writer(f, lineterminator="\n")
        writer.writerow(["bin_x", "bin_y", "centroid_lat", "centroid_lon", "elevation_m"])
        for bx, by in sorted(elevations.keys()):
            lat, lon = centroids[(bx, by)]
            writer.writerow([bx, by, f"{lat:.9f}", f"{lon:.9f}", f"{elevations[(bx, by)]:.4f}"])


def write_haul_directions_binned_csv(
    out_path: Path,
    arrows: List[Tuple[float, float, float]],
    centroids: Dict[Tuple[int, int], Tuple[float, float]],
    grid_height: int,
    pixels_per_bin: int,
) -> None:
    with out_path.open("w", newline="", encoding="utf-8") as f:
        writer = csv.writer(f, lineterminator="\n")
        writer.writerow(["lat", "lon", "binx", "biny", "heading"])
        for px, py, heading in arrows:
            bx = int(round(px / pixels_per_bin - 0.5))
            by = int(round(grid_height - 0.5 - (py / pixels_per_bin)))
            lat, lon = centroids.get((bx, by), ("", ""))
            writer.writerow([f"{lat:.9f}", f"{lon:.9f}", bx, by, f"{heading:.6f}"])


def load_source_haul_arrows(
    haul_path: Path,
    utm_zone: int,
    utm_is_northern: bool,
    min_x: float,
    min_y: float,
    grid_width: int,
    grid_height: int,
) -> List[Tuple[float, float, float]]:
    """
    Load high-level haul arrows directly from HaulDirections.csv and keep only
    those that fall within the current binned grid extents. Returns
    (latitude, longitude, heading_deg) tuples.
    """
    arrows: List[Tuple[float, float, float]] = []
    if not haul_path.is_file():
        return arrows

    with haul_path.open(newline="", encoding="utf-8") as f:
        reader = csv.DictReader(f)
        for row in reader:
            try:
                lat = float(row["lat"])
                lon = float(row["lon"])
                heading = float(row["direction_degrees"])
            except (KeyError, ValueError):
                continue
            coord = utm_from_latlon(lat, lon)
            if coord.zone != utm_zone:
                continue
            x_local = coord.easting
            y_local = coord.northing

            bx_f = (x_local - min_x) / BIN_SIZE_M
            by_f = (y_local - min_y) / BIN_SIZE_M
            if bx_f < 0.0 or bx_f >= grid_width or by_f < 0.0 or by_f >= grid_height:
                continue

            arrows.append((lat, lon, heading))
    return arrows


def write_combined_binned_csv(
    out_path: Path,
    centroids: Dict[Tuple[int, int], Tuple[float, float]],
    existing_values: Dict[Tuple[int, int], float],
    target_values: Dict[Tuple[int, int], float],
    haul_heading_by_bin: Dict[Tuple[int, int], float],
) -> None:
    """
    Write a single merged binned CSV containing elevation + haul heading.
    """
    all_bins = (
        set(centroids.keys())
        | set(existing_values.keys())
        | set(target_values.keys())
        | set(haul_heading_by_bin.keys())
    )
    with out_path.open("w", newline="", encoding="utf-8") as f:
        writer = csv.writer(f, lineterminator="\n")
        writer.writerow(
            [
                "binx",
                "biny",
                "centroid lat",
                "centroid lon",
                "existingheight M",
                "target height M",
                "haul heading deg",
            ]
        )
        for bx, by in sorted(all_bins):
            lat, lon = centroids.get((bx, by), ("", ""))
            existing = existing_values.get((bx, by), "")
            target = target_values.get((bx, by), "")
            heading = haul_heading_by_bin.get((bx, by), "")
            writer.writerow(
                [
                    bx,
                    by,
                    f"{lat:.9f}" if isinstance(lat, float) else "",
                    f"{lon:.9f}" if isinstance(lon, float) else "",
                    f"{existing:.4f}" if isinstance(existing, float) else "",
                    f"{target:.4f}" if isinstance(target, float) else "",
                    f"{heading:.6f}" if isinstance(heading, float) else "",
                ]
            )


def init_sqlite_db(db_path: Path) -> sqlite3.Connection:
    """
    Initialize the SQLite database for field state and haul paths.
    Always called after deleting any existing DB file for a clean schema.
    """
    con = sqlite3.connect(db_path)
    # Tag this schema/version so the consumer can detect compatibility.
    con.executescript(
        """
        PRAGMA user_version = 1;

        CREATE TABLE IF NOT EXISTS FieldState (
            BinID INTEGER PRIMARY KEY AUTOINCREMENT,
            X INTEGER,
            Y INTEGER,
            InitialHeightM REAL,
            CurrentHeightM REAL,
            TargetHeightM REAL,
            CentroidLat REAL,
            CentroidLon REAL,
            HaulPath INTEGER
        );

        CREATE UNIQUE INDEX IF NOT EXISTS idx_fieldstate_xy
            ON FieldState (X, Y);

        CREATE TABLE IF NOT EXISTS BinHistory (
            BinHistoryID INTEGER PRIMARY KEY AUTOINCREMENT,
            X INTEGER,
            Y INTEGER,
            HeightChangeM REAL,
            Timestamp INTEGER
        );

        CREATE TABLE IF NOT EXISTS Events (
            EventID INTEGER PRIMARY KEY AUTOINCREMENT,
            Type TEXT,
            Details TEXT,
            Timestamp INTEGER
        );

        CREATE TABLE IF NOT EXISTS Data (
            DataID INTEGER PRIMARY KEY AUTOINCREMENT,
            Name TEXT,
            Value REAL
        );

        CREATE TABLE IF NOT EXISTS HaulPaths (
            HaulPathID INTEGER PRIMARY KEY AUTOINCREMENT,
            HaulPath INTEGER,
            PointNumber INTEGER,
            Latitude REAL,
            Longitude REAL
        );

        CREATE INDEX IF NOT EXISTS idx_haulpaths_haul_point
            ON HaulPaths (HaulPath, PointNumber);

        CREATE TABLE IF NOT EXISTS HaulArrows (
            ArrowID INTEGER PRIMARY KEY AUTOINCREMENT,
            Latitude REAL,
            Longitude REAL,
            Heading REAL
        );

        CREATE TABLE IF NOT EXISTS Benchmarks (
            BenchMarkID INTEGER PRIMARY KEY AUTOINCREMENT,
            Latitude REAL,
            Longitude REAL,
            Name TEXT,
            ElevationM REAL
        );
        """
    )
    return con


def _heading_to_vec(heading_deg: float) -> Tuple[float, float]:
    """Compass heading (0=north, 90=east) -> unit vector in bin coordinates."""
    rad = math.radians(heading_deg)
    return math.sin(rad), math.cos(rad)


def _turn_angle_deg(a_deg: float, b_deg: float) -> float:
    """Smallest absolute turn angle between two headings in degrees."""
    return abs((b_deg - a_deg + 180.0) % 360.0 - 180.0)


def _signed_turn_delta_deg(from_deg: float, to_deg: float) -> float:
    """Signed smallest turn from from_deg -> to_deg in [-180, 180)."""
    return (to_deg - from_deg + 180.0) % 360.0 - 180.0


def _circular_mean_deg(angles_deg: list[float]) -> float:
    """Circular mean of angles in degrees."""
    if not angles_deg:
        return 0.0
    sx = 0.0
    sy = 0.0
    for a in angles_deg:
        r = math.radians(a)
        sx += math.cos(r)
        sy += math.sin(r)
    if abs(sx) < 1e-12 and abs(sy) < 1e-12:
        return angles_deg[-1]
    return math.degrees(math.atan2(sy, sx)) % 360.0


def _blend_angle_deg(base_deg: float, target_deg: float, alpha: float) -> float:
    """Blend base->target on circle with alpha in [0,1]."""
    alpha = max(0.0, min(1.0, alpha))
    delta = _signed_turn_delta_deg(base_deg, target_deg)
    return (base_deg + alpha * delta) % 360.0


def _find_next_bin_on_heading(
    pos_x: float,
    pos_y: float,
    heading_deg: float,
    bins: set[Tuple[int, int]],
    threshold_bins: float,
    current_bin: Tuple[int, int],
) -> Tuple[int, int] | None:
    """
    Along the current heading ray, find the nearest bin center whose circle
    (radius=threshold_bins) is intersected.
    """
    dx, dy = _heading_to_vec(heading_deg)
    perp_x, perp_y = -dy, dx
    best_t = None
    best_bin = None
    r2 = threshold_bins * threshold_bins

    # Fast local candidate generation (keeps runtime bounded on dense grids).
    candidate_bins: set[Tuple[int, int]] = set()
    for u in range(1, 9):  # forward bins
        for v in range(-4, 5):  # side bins
            px = pos_x + u * dx + v * perp_x
            py = pos_y + u * dy + v * perp_y
            bx = int(round(px - 0.5))
            by = int(round(py - 0.5))
            cand = (bx, by)
            if cand in bins and cand != current_bin:
                candidate_bins.add(cand)

    if not candidate_bins:
        # Fallback near current bin, still bounded.
        cx, cy = current_bin
        for ox in range(-10, 11):
            for oy in range(-10, 11):
                cand = (cx + ox, cy + oy)
                if cand in bins and cand != current_bin:
                    candidate_bins.add(cand)

    for bx, by in candidate_bins:
        cx = bx + 0.5
        cy = by + 0.5
        rx = cx - pos_x
        ry = cy - pos_y

        t_center = rx * dx + ry * dy
        if t_center <= 1e-9:
            continue

        perp2 = rx * rx + ry * ry - t_center * t_center
        if perp2 < 0.0:
            perp2 = 0.0
        if perp2 > r2:
            continue

        dt = math.sqrt(r2 - perp2)
        t_enter = t_center - dt
        if t_enter <= 1e-9:
            t_enter = t_center + dt
        if t_enter <= 1e-9:
            continue

        if best_t is None or t_enter < best_t:
            best_t = t_enter
            best_bin = (bx, by)

    return best_bin


def _build_discrete_bin_route(
    start_bin: Tuple[int, int],
    heading_by_bin: Dict[Tuple[int, int], float],
    hit_threshold_inches: float,
    max_turn_deg: float,
    max_steps: int,
) -> list[Tuple[int, int]]:
    """Build bin-to-bin route using current stop-on-large-turn behavior."""
    if start_bin not in heading_by_bin:
        return []
    threshold_bins = max(0.01, hit_threshold_inches / 24.0)
    bins = set(heading_by_bin.keys())
    current_bin = start_bin
    current_heading = heading_by_bin[current_bin]
    route = [current_bin]
    visited = {current_bin}

    for _ in range(max_steps):
        pos_x = current_bin[0] + 0.5
        pos_y = current_bin[1] + 0.5
        next_bin = _find_next_bin_on_heading(
            pos_x=pos_x,
            pos_y=pos_y,
            heading_deg=current_heading,
            bins=bins,
            threshold_bins=threshold_bins,
            current_bin=current_bin,
        )
        if next_bin is None or next_bin not in heading_by_bin:
            break
        next_heading = heading_by_bin[next_bin]
        if _turn_angle_deg(current_heading, next_heading) > max_turn_deg:
            break
        if next_bin in visited:
            break
        route.append(next_bin)
        visited.add(next_bin)
        current_bin = next_bin
        current_heading = next_heading

    return route


def _smooth_route_with_curvature_limit(
    route: list[Tuple[int, int]],
    heading_by_bin: Dict[Tuple[int, int], float],
    look_ahead_bins: int,
    tractor_turning_circle_m: float,
) -> list[Tuple[float, float]]:
    """
    Convert a discrete bin route into a smooth polyline by limiting heading
    change per segment using the tractor turning circle.
    """
    if len(route) < 2:
        return [(route[0][0] + 0.5, route[0][1] + 0.5)] if route else []

    radius_m = max(0.01, tractor_turning_circle_m * 0.5)
    # Max heading change when traveling one bin center-to-center.
    max_delta_deg = math.degrees(BIN_SIZE_M / radius_m)
    n = len(route)

    # 1) Build a low-noise desired heading profile using forward-window circular mean.
    base_headings: list[float] = [heading_by_bin.get(b, 0.0) % 360.0 for b in route]
    desired: list[float] = []
    window = max(2, look_ahead_bins)
    for i in range(n):
        j0 = i
        j1 = min(n, i + window)
        desired.append(_circular_mean_deg(base_headings[j0:j1]))

    # Additional long-wave damping via exponential circular blend.
    desired_lp: list[float] = [desired[0]]
    for i in range(1, n):
        desired_lp.append(_blend_angle_deg(desired_lp[i - 1], desired[i], alpha=0.30))

    # 2) Curvature-limited forward pass.
    smoothed_headings: list[float] = [base_headings[0]]
    for i in range(1, n):
        prev = smoothed_headings[i - 1]
        target = desired_lp[i]
        delta = _signed_turn_delta_deg(prev, target)
        smoothed_headings.append((prev + max(-max_delta_deg, min(max_delta_deg, delta))) % 360.0)

    # 3) Relaxation passes to reduce local oscillations (10-15 bin wavelength),
    # then re-apply curvature constraints to keep turns physically valid.
    for _ in range(2):
        relaxed = smoothed_headings[:]
        for i in range(1, n - 1):
            local_avg = _circular_mean_deg(
                [smoothed_headings[i - 1], smoothed_headings[i], smoothed_headings[i + 1]]
            )
            relaxed[i] = _blend_angle_deg(smoothed_headings[i], local_avg, alpha=0.35)

        # Re-enforce turning-radius limit forward.
        constrained = [relaxed[0]]
        for i in range(1, n):
            prev = constrained[i - 1]
            delta = _signed_turn_delta_deg(prev, relaxed[i])
            constrained.append((prev + max(-max_delta_deg, min(max_delta_deg, delta))) % 360.0)
        smoothed_headings = constrained

    # Hermite-like interpolation between bin centers with overshoot control.
    poly: list[Tuple[float, float]] = []
    for i in range(len(route) - 1):
        p0 = (route[i][0] + 0.5, route[i][1] + 0.5)
        p1 = (route[i + 1][0] + 0.5, route[i + 1][1] + 0.5)
        dist = math.hypot(p1[0] - p0[0], p1[1] - p0[1])
        if dist <= 1e-9:
            continue
        d0 = _heading_to_vec(smoothed_headings[i])
        d1 = _heading_to_vec(smoothed_headings[i + 1])
        # Reduce tangent magnitude on sharper turns to minimize local overshoot.
        turn_here = _turn_angle_deg(smoothed_headings[i], smoothed_headings[i + 1])
        sharpness = max(0.0, min(1.0, turn_here / 180.0))
        tangent_scale = 0.45 * (1.0 - sharpness) + 0.12 * sharpness
        t0 = (d0[0] * dist * tangent_scale, d0[1] * dist * tangent_scale)
        t1 = (d1[0] * dist * tangent_scale, d1[1] * dist * tangent_scale)

        # Keep interpolated points inside a local envelope to suppress bulging.
        pad = 0.22
        min_x = min(p0[0], p1[0]) - pad
        max_x = max(p0[0], p1[0]) + pad
        min_y = min(p0[1], p1[1]) - pad
        max_y = max(p0[1], p1[1]) + pad

        samples = 6
        for s_i in range(samples):
            t = s_i / float(samples)
            t2 = t * t
            t3 = t2 * t
            h00 = 2 * t3 - 3 * t2 + 1
            h10 = t3 - 2 * t2 + t
            h01 = -2 * t3 + 3 * t2
            h11 = t3 - t2
            x = h00 * p0[0] + h10 * t0[0] + h01 * p1[0] + h11 * t1[0]
            y = h00 * p0[1] + h10 * t0[1] + h01 * p1[1] + h11 * t1[1]
            x = max(min_x, min(max_x, x))
            y = max(min_y, min(max_y, y))
            poly.append((x, y))
    poly.append((route[-1][0] + 0.5, route[-1][1] + 0.5))
    return poly


def calculate_soil_path(
    start_bin: Tuple[int, int],
    heading_by_bin: Dict[Tuple[int, int], float],
    hit_threshold_inches: float = 3.0,
    max_turn_deg: float = 90.0,
    look_ahead_enabled: bool = True,
    look_ahead_bins: int = 6,
    tractor_turning_circle_m: float = 2.0,
    max_steps: int = 10000,
) -> list[Tuple[float, float]]:
    """
    Follow the haul-heading flow from start_bin.
    At each step, advance along current heading until within threshold of
    another bin center, then continue using that bin's heading.
    Stop when required turn exceeds max_turn_deg (or no further bin found).
    With look-ahead enabled, the tractor heading starts turning earlier toward
    upcoming heading changes, limited by tractor turning circle.
    Returns path as bin-space points at bin centers: (x_center, y_center).
    """
    if start_bin not in heading_by_bin:
        return []

    route = _build_discrete_bin_route(
        start_bin=start_bin,
        heading_by_bin=heading_by_bin,
        hit_threshold_inches=hit_threshold_inches,
        max_turn_deg=max_turn_deg,
        max_steps=max_steps,
    )
    if not route:
        return []
    if not look_ahead_enabled:
        return [(bx + 0.5, by + 0.5) for bx, by in route]
    return _smooth_route_with_curvature_limit(
        route=route,
        heading_by_bin=heading_by_bin,
        look_ahead_bins=look_ahead_bins,
        tractor_turning_circle_m=tractor_turning_circle_m,
    )


def main() -> int:
    parser = argparse.ArgumentParser(
        description=(
            "Parse source AGD file, bin to 2ft grid in-memory, detect haul arrows from PNG+KML "
            "or CSV, then render with interpolation and write a SQLite database."
        )
    )
    parser.add_argument(
        "--agd",
        type=Path,
        required=True,
        help="Path to source .agd file",
    )
    parser.add_argument(
        "--haul",
        type=Path,
        default=Path("HaulDirections.csv"),
        help="Optional path to HaulDirections.csv (fallback when no haul-image-dir is provided)",
    )
    parser.add_argument(
        "--haul-image-dir",
        type=Path,
        default=None,
        help="Directory containing PNG+KML haul-arrow images (preferred; no intermediate CSV needed)",
    )
    parser.add_argument(
        "--db",
        "--sqlite",
        dest="db",
        type=Path,
        default=None,
        help="Output SQLite DB path (default: <agd-stem>-base.db in AGD directory)",
    )
    parser.add_argument(
        "--output",
        type=Path,
        default=None,
        help="Output PNG path (default: no image written unless specified)",
    )
    parser.add_argument(
        "--pixels-per-bin",
        type=int,
        default=16,
        help="Pixels used to render each 2ft bin (default: 4)",
    )
    parser.add_argument(
        "--overlay-source-arrows",
        action="store_true",
        help="Overlay source haul arrows in red 2x size over interpolated arrows",
    )
    parser.add_argument(
        "--imgpathgeneration",
        action="store_true",
        help="Enable path generation flow (default: off)",
    )
    parser.add_argument(
        "--path-start-x",
        type=int,
        default=None,
        help="Path start bin X coordinate (used when --imgpathgeneration is enabled)",
    )
    parser.add_argument(
        "--path-start-y",
        type=int,
        default=None,
        help="Path start bin Y coordinate (used when --imgpathgeneration is enabled)",
    )
    parser.add_argument(
        "--path-hit-threshold-in",
        type=float,
        default=3.0,
        help="Distance threshold in inches to consider a bin center reached (default: 3.0)",
    )
    parser.add_argument(
        "--path-max-turn-deg",
        type=float,
        default=90.0,
        help="Stop path when required turn exceeds this many degrees (default: 90.0)",
    )
    parser.set_defaults(look_ahead=True)
    parser.add_argument(
        "--look-ahead",
        dest="look_ahead",
        action="store_true",
        help="Enable look-ahead turning (default: enabled)",
    )
    parser.add_argument(
        "--no-look-ahead",
        dest="look_ahead",
        action="store_false",
        help="Disable look-ahead turning and use current behavior",
    )
    parser.add_argument(
        "--look-ahead-bins",
        type=int,
        default=6,
        help="How many bins ahead to inspect for upcoming heading changes (default: 6)",
    )
    parser.add_argument(
        "--tractor-turning-circle-m",
        type=float,
        default=2.0,
        help="Tractor turning circle in meters used for turn-rate limiting (default: 2.0)",
    )
    parser.add_argument(
        "--path-random-start-count",
        type=int,
        default=15,
        help="Number of random cut-bin start points for path generation (default: 15)",
    )
    parser.add_argument(
        "--test-subset",
        action="store_true",
        help="Process only center-bottom one-eighth subset of field (default: off)",
    )
    parser.add_argument(
        "--disablehaulpaths",
        action="store_true",
        help="Do not calculate haul paths or write to the HaulPaths table",
    )
    args = parser.parse_args()

    if not args.agd.is_file():
        print(f"ERROR: AGD file not found: {args.agd}", file=sys.stderr)
        return 1
    if args.pixels_per_bin <= 0:
        print("ERROR: --pixels-per-bin must be > 0", file=sys.stderr)
        return 1
    if args.imgpathgeneration and (args.path_start_x is None or args.path_start_y is None):
        print(
            "ERROR: --path-start-x and --path-start-y are required when --imgpathgeneration is enabled",
            file=sys.stderr,
        )
        return 1
    if args.path_hit_threshold_in <= 0:
        print("ERROR: --path-hit-threshold-in must be > 0", file=sys.stderr)
        return 1
    if args.path_max_turn_deg < 0 or args.path_max_turn_deg > 180:
        print("ERROR: --path-max-turn-deg must be in [0, 180]", file=sys.stderr)
        return 1
    if args.look_ahead_bins <= 0:
        print("ERROR: --look-ahead-bins must be > 0", file=sys.stderr)
        return 1
    if args.tractor_turning_circle_m <= 0:
        print("ERROR: --tractor-turning-circle-m must be > 0", file=sys.stderr)
        return 1
    if args.path_random_start_count <= 0:
        print("ERROR: --path-random-start-count must be > 0", file=sys.stderr)
        return 1

    # Determine DB path and ensure any existing DB file is removed so that each
    # run starts from a clean database.
    db_path = args.db
    if db_path is None:
        db_path = args.agd.with_name(f"{args.agd.stem}-base.db")
    if db_path.exists():
        try:
            db_path.unlink()
            print(f"INFO: deleted existing DB {db_path}", file=sys.stderr)
        except OSError as exc:
            print(f"ERROR: failed to delete existing DB {db_path}: {exc}", file=sys.stderr)
            return 1

    points, benchmarks = parse_agd_points(args.agd)
    (
        existing_values,
        target_values,
        centroids,
        bins_raw,
        mean_lat,
        mean_lon,
        min_x,
        min_y,
        max_x,
        max_y,
        grid_width,
        grid_height,
        utm_zone,
        utm_is_northern,
    ) = bin_agd_points_2ft(points)

    if args.test_subset:
        (
            existing_values,
            target_values,
            centroids,
            bins_raw,
            min_x,
            min_y,
            max_x,
            max_y,
            grid_width,
            grid_height,
            utm_zone,
            utm_is_northern,
        ) = subset_center_bottom_eighth(
            existing_values=existing_values,
            target_values=target_values,
            centroids=centroids,
            bins_raw=bins_raw,
            min_x=min_x,
            min_y=min_y,
            max_x=max_x,
            max_y=max_y,
            grid_width=grid_width,
            grid_height=grid_height,
            utm_zone=utm_zone,
            utm_is_northern=utm_is_northern,
        )
        print(
            f"INFO: test-subset enabled -> grid {grid_width}x{grid_height}",
            file=sys.stderr,
        )

    print("INFO: filling missing bins (existing elevations)...", file=sys.stderr, flush=True)
    existing_filled = fill_missing_bins(existing_values, grid_width, grid_height)
    print(f"INFO: filled missing bins (existing); {len(existing_filled)} bins", file=sys.stderr, flush=True)
    print("INFO: filling missing bins (target elevations)...", file=sys.stderr, flush=True)
    target_filled = fill_missing_bins(target_values, grid_width, grid_height)
    print(f"INFO: filled missing bins (target); {len(target_filled)} bins", file=sys.stderr, flush=True)
    values = existing_filled
    palette = generate_palette()
    min_elev, max_elev = elevation_range(values)

    img_width = grid_width * args.pixels_per_bin
    img_height = grid_height * args.pixels_per_bin
    # Default behavior: do not create an image unless an explicit output path is provided.
    create_image = args.output is not None
    image = Image.new("RGB", (img_width, img_height), (0, 0, 0)) if create_image else None
    pixels = None if image is None else image.load()
    draw = None if image is None else ImageDraw.Draw(image)
    max_bin_y = grid_height - 1

    if pixels is not None:
        for (bx, by), elev in values.items():
            normalized = (elev - min_elev) / (max_elev - min_elev)
            normalized = max(0.0, min(1.0, normalized))
            idx = int(round(normalized * 255))
            color = palette[idx]
            x0 = bx * args.pixels_per_bin
            y0 = (max_bin_y - by) * args.pixels_per_bin
            for dy in range(args.pixels_per_bin):
                for dx in range(args.pixels_per_bin):
                    px = x0 + dx
                    py = y0 + dy
                    if 0 <= px < img_width and 0 <= py < img_height:
                        pixels[px, py] = color

    # Build haul-heading field either from PNG+KML (preferred) or from HaulDirections.csv.
    bins_to_draw = set(values.keys())
    haul_samples: list[tuple[float, float, float]] = []
    haul_heading_by_bin: Dict[Tuple[int, int], float] = {}

    if args.haul_image_dir is not None:
        try:
            haul_samples = haul_samples_from_png_kml_dir(args.haul_image_dir)
        except Exception as exc:
            print(f"ERROR: failed to read haul data from images in {args.haul_image_dir}: {exc}", file=sys.stderr)
            return 1
    elif args.haul is not None and args.haul.is_file():
        samples: list[tuple[float, float, float]] = []
        with args.haul.open(newline="", encoding="utf-8") as f:
            reader = csv.DictReader(f)
            for row in reader:
                try:
                    lat = float(row["lat"])
                    lon = float(row["lon"])
                    direction = float(row["direction_degrees"])
                except (KeyError, ValueError):
                    continue
                samples.append((lat, lon, direction))
        if samples:
            print(
                f"INFO: loaded {len(samples)} haul samples from CSV {args.haul}; "
                "consider using --haul-image-dir to avoid this intermediate file.",
                file=sys.stderr,
            )
            haul_samples = samples
        else:
            print(
                f"WARNING: haul CSV {args.haul} contained no valid rows; no haul data available",
                file=sys.stderr,
            )
    else:
        print(
            "WARNING: no haul data provided (no --haul-image-dir and HaulDirections.csv not found); "
            "haul paths and arrows will be omitted",
            file=sys.stderr,
        )

    # Build per-bin haul heading only when needed: for HaulPaths, output image arrows,
    # or path-generation overlay. With --disablehaulpaths and no image/pathgen we skip it.
    need_haul_heading = (
        not args.disablehaulpaths
        or args.output is not None
        or args.imgpathgeneration
    )
    if haul_samples and need_haul_heading:
        print(
            f"INFO: building haul heading field from {len(haul_samples)} samples "
            f"(grid {grid_width}x{grid_height}, {len(bins_to_draw)} bins)...",
            file=sys.stderr,
            flush=True,
        )
        haul_heading_by_bin = build_haul_heading_by_bin(
            samples=haul_samples,
            min_x=min_x,
            min_y=min_y,
            grid_width=grid_width,
            grid_height=grid_height,
            bins_to_draw=bins_to_draw,
            utm_zone=utm_zone,
            utm_is_northern=utm_is_northern,
        )
        print(
            f"INFO: haul heading field complete: {len(haul_heading_by_bin)} bins",
            file=sys.stderr,
            flush=True,
        )
    elif haul_samples and not need_haul_heading:
        print(
            "INFO: skipping haul heading field (--disablehaulpaths and no --output or --imgpathgeneration)",
            file=sys.stderr,
            flush=True,
        )

    # Draw interpolated haul arrows (per-bin) and optional source arrows.
    if draw is not None and haul_heading_by_bin:
        arrow_count = 0
        for (bx, by), heading in haul_heading_by_bin.items():
            px = (bx + 0.5) * args.pixels_per_bin
            py = (grid_height - 0.5 - by) * args.pixels_per_bin
            _draw_arrow_hollow_triangle(draw, px, py, heading)
            arrow_count += 1
        print(f"INFO: drew {arrow_count} haul direction arrows (hollow triangles)", file=sys.stderr)

        if args.overlay_source_arrows and haul_samples:
            source_count = 0
            for lat, lon, heading in haul_samples:
                try:
                    utm = utm_from_latlon(lat, lon)
                except ValueError:
                    continue
                if utm.zone != utm_zone or utm.is_northern != utm_is_northern:
                    continue
                bx_f = (utm.easting - min_x) / BIN_SIZE_M
                by_f = (utm.northing - min_y) / BIN_SIZE_M
                if bx_f < 0.0 or bx_f >= grid_width or by_f < 0.0 or by_f >= grid_height:
                    continue
                px = bx_f * args.pixels_per_bin
                py = (grid_height - by_f) * args.pixels_per_bin
                _draw_arrow_hollow_triangle(
                    draw,
                    px,
                    py,
                    heading,
                    color=SOURCE_ARROW_COLOR,
                    length_px=ARROW_LENGTH_PX * SOURCE_ARROW_SCALE,
                    base_half_width_px=ARROW_BASE_HALF_WIDTH_PX * SOURCE_ARROW_SCALE,
                )
                source_count += 1
            print(
                f"INFO: drew {source_count} source haul arrows "
                "(hollow triangles, red, 2x size)",
                file=sys.stderr,
            )

    # Optional path generation overlay for visualization (kept from interpolate.py).
    if args.imgpathgeneration:
        # Random starts must come only from cut bins (existing > target) that also
        # have an interpolated haul heading.
        cut_bins = [
            b for b in haul_heading_by_bin.keys()
            if b in existing_filled and b in target_filled and existing_filled[b] > target_filled[b]
        ]
        if not cut_bins:
            print("WARNING: no cut bins with haul-heading data available for path generation", file=sys.stderr)
        else:
            selected_starts: list[Tuple[int, int]] = []
            requested_start: Tuple[int, int] | None = None
            if args.path_start_x is not None and args.path_start_y is not None:
                requested_start = (args.path_start_x, args.path_start_y)
                if requested_start in cut_bins:
                    selected_starts.append(requested_start)
                else:
                    print(
                        f"WARNING: requested start bin {requested_start} is not a cut bin with heading; "
                        "using random starts only",
                        file=sys.stderr,
                    )

            remaining = [b for b in cut_bins if b not in set(selected_starts)]
            random.shuffle(remaining)
            target_count = min(args.path_random_start_count, len(cut_bins))
            selected_starts.extend(remaining[: max(0, target_count - len(selected_starts))])

            path_count = 0
            path_time_s_total = 0.0
            path_time_count = 0
            for sx, sy in selected_starts:
                # Mark each chosen start in grey.
                if draw is not None:
                    x0 = sx * args.pixels_per_bin
                    y0 = (max_bin_y - sy) * args.pixels_per_bin
                    x1 = x0 + args.pixels_per_bin - 1
                    y1 = y0 + args.pixels_per_bin - 1
                    draw.rectangle([(x0, y0), (x1, y1)], fill=(128, 128, 128), outline=(192, 192, 192), width=1)

                t0 = time.perf_counter()
                soil_path = calculate_soil_path(
                    start_bin=(sx, sy),
                    heading_by_bin=haul_heading_by_bin,
                    hit_threshold_inches=args.path_hit_threshold_in,
                    max_turn_deg=args.path_max_turn_deg,
                    look_ahead_enabled=args.look_ahead,
                    look_ahead_bins=args.look_ahead_bins,
                    tractor_turning_circle_m=args.tractor_turning_circle_m,
                )
                dt = time.perf_counter() - t0
                path_time_s_total += dt
                path_time_count += 1
                if len(soil_path) >= 2:
                    if draw is not None:
                        path_pixels = [
                            (
                                px_center * args.pixels_per_bin,
                                (grid_height - py_center) * args.pixels_per_bin,
                            )
                            for (px_center, py_center) in soil_path
                        ]
                        # Requested: flow path rendered in black.
                        draw.line(path_pixels, fill=(0, 0, 0), width=max(2, args.pixels_per_bin // 4))
                    path_count += 1

            print(
                f"INFO: drew {path_count} soil flow paths from {len(selected_starts)} random starts "
                f"(threshold={args.path_hit_threshold_in}\" max_turn={args.path_max_turn_deg} deg "
                f"look_ahead={args.look_ahead} n={args.look_ahead_bins} "
                f"turn_circle={args.tractor_turning_circle_m}m)",
                file=sys.stderr,
            )
            if path_time_count > 0:
                avg_ms = (path_time_s_total / path_time_count) * 1000.0
                total_ms = path_time_s_total * 1000.0
                print(
                    f"INFO: path_generation_timing count={path_time_count} "
                    f"total_ms={total_ms:.2f} avg_ms={avg_ms:.2f}",
                    file=sys.stderr,
                )

    combined_csv = args.agd.with_name(f"{args.agd.stem}-binned.csv")
    write_combined_binned_csv(
        out_path=combined_csv,
        centroids=centroids,
        existing_values=existing_filled,
        target_values=target_filled,
        haul_heading_by_bin=haul_heading_by_bin,
    )
    print(f"INFO: wrote merged binned CSV {combined_csv}", file=sys.stderr)

    # Initialize and populate SQLite DB with FieldState, HaulArrows, and HaulPaths.
    con = init_sqlite_db(db_path)
    cur = con.cursor()

    # Populate FieldState for every bin in the full rectangular grid (to match AGDLoader).
    all_bins = [(bx, by) for by in range(grid_height) for bx in range(grid_width)]
    field_rows = []
    for bx, by in all_bins:
        existing_height_m = existing_filled.get((bx, by), 0.0)
        target_height_m = target_filled.get((bx, by), 0.0)
        centroid_lat, centroid_lon = centroids.get((bx, by), (0.0, 0.0))
        field_rows.append((bx, by, existing_height_m, existing_height_m, target_height_m, centroid_lat, centroid_lon, 0))

    cur.executemany(
        "INSERT INTO FieldState (X, Y, InitialHeightM, CurrentHeightM, TargetHeightM, CentroidLat, CentroidLon, HaulPath) VALUES (?, ?, ?, ?, ?, ?, ?, ?)",
        field_rows,
    )
    # Store grid and projection parameters in the Data table for downstream consumers.
    # MinX, MinY, MaxX, MaxY: UTM easting/northing (meters), min/max over topology points.
    # MinLat, MinLon, MaxLat, MaxLon: WGS84 bounds from UTM corners (not from centroid extents).
    # Converting (min_x,min_y) and (max_x,max_y) to lat/lon ensures Field.Load gets correct
    # FieldMinX/Y when it does UTM.FromLatLon(MinLat,MinLon). Bin width and height in degrees
    # are not equal, so we must use the actual UTM corners, not min/max of centroid lats/lons.
    data_min_lat, data_min_lon = utm_to_latlon(utm_zone, utm_is_northern, min_x, min_y)
    data_max_lat, data_max_lon = utm_to_latlon(utm_zone, utm_is_northern, max_x, max_y)
    cur.execute(
        "INSERT INTO Data (Name, Value) VALUES (?, ?)",
        ("GridWidth", float(grid_width)),
    )
    cur.execute(
        "INSERT INTO Data (Name, Value) VALUES (?, ?)",
        ("GridHeight", float(grid_height)),
    )
    cur.execute(
        "INSERT INTO Data (Name, Value) VALUES (?, ?)",
        ("MeanLat", float(mean_lat)),
    )
    cur.execute(
        "INSERT INTO Data (Name, Value) VALUES (?, ?)",
        ("MeanLon", float(mean_lon)),
    )
    cur.execute(
        "INSERT INTO Data (Name, Value) VALUES (?, ?)",
        ("MinX", float(min_x)),
    )
    cur.execute(
        "INSERT INTO Data (Name, Value) VALUES (?, ?)",
        ("MinY", float(min_y)),
    )
    cur.execute(
        "INSERT INTO Data (Name, Value) VALUES (?, ?)",
        ("MaxX", float(max_x)),
    )
    cur.execute(
        "INSERT INTO Data (Name, Value) VALUES (?, ?)",
        ("MaxY", float(max_y)),
    )
    cur.execute(
        "INSERT INTO Data (Name, Value) VALUES (?, ?)",
        ("MinLat", float(data_min_lat)),
    )
    cur.execute(
        "INSERT INTO Data (Name, Value) VALUES (?, ?)",
        ("MinLon", float(data_min_lon)),
    )
    cur.execute(
        "INSERT INTO Data (Name, Value) VALUES (?, ?)",
        ("MaxLat", float(data_max_lat)),
    )
    cur.execute(
        "INSERT INTO Data (Name, Value) VALUES (?, ?)",
        ("MaxLon", float(data_max_lon)),
    )
    cur.execute(
        "INSERT INTO Data (Name, Value) VALUES (?, ?)",
        ("CompletedCutCY", 0.0),
    )
    cur.execute(
        "INSERT INTO Data (Name, Value) VALUES (?, ?)",
        ("CompletedFillCY", 0.0),
    )
    con.commit()
    print(f"INFO: wrote {len(field_rows)} bins to FieldState in {db_path}", file=sys.stderr)

    # Populate HaulArrows with high-level source arrows from haul_samples (image or CSV).
    if haul_samples:
        cur.executemany(
            "INSERT INTO HaulArrows (Latitude, Longitude, Heading) VALUES (?, ?, ?)",
            haul_samples,
        )
        con.commit()
        print(
            f"INFO: wrote {len(haul_samples)} haul arrows to HaulArrows in {db_path}",
            file=sys.stderr,
        )
    else:
        print("INFO: no haul samples available; HaulArrows table left empty", file=sys.stderr)

    if benchmarks:
        cur.executemany(
            "INSERT INTO Benchmarks (Latitude, Longitude, Name, ElevationM) VALUES (?, ?, ?, ?)",
            benchmarks,
        )
        con.commit()
        print(
            f"INFO: wrote {len(benchmarks)} benchmarks to Benchmarks in {db_path}",
            file=sys.stderr,
        )
    else:
        print("INFO: no benchmarks found in AGD; Benchmarks table left empty", file=sys.stderr)

    # Generate and store haul paths for every cut bin (InitialHeightM > TargetHeightM).
    cut_bins = [
        (bx, by)
        for (bx, by) in all_bins
        if existing_filled.get((bx, by)) is not None
        and target_filled.get((bx, by)) is not None
        and existing_filled[(bx, by)] > target_filled[(bx, by)]
    ]

    total_cut = len(cut_bins)
    if args.disablehaulpaths:
        if total_cut > 0:
            print(
                "INFO: haul paths disabled (--disablehaulpaths); HaulPaths table left empty",
                file=sys.stderr,
            )
    elif total_cut == 0:
        print("INFO: no cut bins found; HaulPaths table will remain empty", file=sys.stderr)
    else:
        missing_heading = [b for b in cut_bins if b not in haul_heading_by_bin]
        if missing_heading:
            print(
                f"WARNING: {len(missing_heading)} cut bins have no haul heading; "
                "paths for these bins will fall back to a single-point path at the bin centroid.",
                file=sys.stderr,
            )

        start_time = time.perf_counter()
        next_haul_id = 1

        insert_path_sql = (
            "INSERT INTO HaulPaths (HaulPath, PointNumber, Latitude, Longitude) "
            "VALUES (?, ?, ?, ?)"
        )
        update_field_sql = "UPDATE FieldState SET HaulPath = ? WHERE X = ? AND Y = ?"

        for idx, (bx, by) in enumerate(cut_bins, start=1):
            haul_id = next_haul_id
            next_haul_id += 1

            # Compute soil-flow path in bin space.
            if (bx, by) in haul_heading_by_bin:
                soil_path = calculate_soil_path(
                    start_bin=(bx, by),
                    heading_by_bin=haul_heading_by_bin,
                    hit_threshold_inches=args.path_hit_threshold_in,
                    max_turn_deg=args.path_max_turn_deg,
                    look_ahead_enabled=args.look_ahead,
                    look_ahead_bins=args.look_ahead_bins,
                    tractor_turning_circle_m=args.tractor_turning_circle_m,
                )
            else:
                soil_path = []

            # Skip degenerate single-point paths; only store multi-point polylines.
            if len(soil_path) < 2:
                # Leave HaulPath = 0 for this cut bin (no DB polyline).
                continue

            # Remove redundant collinear points along straight segments. For any three
            # consecutive points that lie approximately on a straight line, drop the
            # middle point.
            if len(soil_path) > 2:
                simplified: list[Tuple[float, float]] = [soil_path[0]]
                eps = 1e-4
                for i in range(1, len(soil_path) - 1):
                    x0, y0 = simplified[-1]
                    x1, y1 = soil_path[i]
                    x2, y2 = soil_path[i + 1]
                    vx1 = x1 - x0
                    vy1 = y1 - y0
                    vx2 = x2 - x1
                    vy2 = y2 - y1
                    area = abs(vx1 * vy2 - vy1 * vx2)
                    if area < eps:
                        # Nearly collinear; omit this interior point.
                        continue
                    simplified.append((x1, y1))
                simplified.append(soil_path[-1])
                soil_path = simplified

            if len(soil_path) < 2:
                continue

            path_rows = []
            for point_idx, (x_center, y_center) in enumerate(soil_path):
                cx_local = min_x + x_center * BIN_SIZE_M
                cy_local = min_y + y_center * BIN_SIZE_M
                lat, lon = utm_to_latlon(utm_zone, utm_is_northern, cx_local, cy_local)
                path_rows.append((haul_id, point_idx, lat, lon))

            cur.executemany(insert_path_sql, path_rows)
            cur.execute(update_field_sql, (haul_id, bx, by))

            # Periodic progress + ETA reporting.
            if idx % 10 == 0 or idx == total_cut:
                elapsed = time.perf_counter() - start_time
                done_frac = idx / total_cut
                if done_frac > 0:
                    eta = elapsed * (1.0 - done_frac) / done_frac
                else:
                    eta = 0.0
                print(
                    f"INFO: haul_path_db_progress {idx}/{total_cut} "
                    f"({done_frac * 100.0:.1f}%) elapsed={elapsed:.1f}s eta={eta:.1f}s",
                    file=sys.stderr,
                )

        con.commit()
        print(
            f"INFO: completed haul path generation for {total_cut} cut bins; "
            f"DB written to {db_path}",
            file=sys.stderr,
        )

    con.close()

    if image is not None and args.output is not None:
        image.save(args.output)
        print(f"Wrote {args.output}", file=sys.stderr)

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
