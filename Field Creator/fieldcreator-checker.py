"""
Visual checker for fieldcreator.py SQLite output.

Loads a FieldState/HaulPaths SQLite DB, reconstructs the elevation grid,
and renders a 4K PNG with existing elevation plus 50 random haul paths
drawn as black polylines over the top, using the same image generation
approach as interpolate.py.
"""

from __future__ import annotations

import argparse
import random
import sqlite3
import sys
from pathlib import Path
from typing import Dict, List, Tuple

from PIL import Image, ImageDraw

from render_existing_heights import (
    fill_missing_bins,
    elevation_range,
    generate_palette,
    _draw_arrow_hollow_triangle,
    ARROW_BASE_HALF_WIDTH_PX,
    ARROW_LENGTH_PX,
    SOURCE_ARROW_COLOR,
)
from fieldcreator import BIN_SIZE_M, EARTH_RADIUS_M


PIXELS_PER_BIN = 16  # 4K-style output, matching interpolate.py 4x setting


def _load_fieldstate(
    db_path: Path,
) -> Tuple[Dict[Tuple[int, int], float], Dict[Tuple[int, int], float], int, int]:
    """
    Load FieldState from SQLite DB.

    Returns:
      existing_values[(bx, by)] = HeightM
      target_values[(bx, by)] = TargetHeightM
      grid_width, grid_height
    """
    con = sqlite3.connect(db_path)
    cur = con.cursor()
    cur.execute("SELECT X, Y, InitialHeightM, TargetHeightM FROM FieldState")

    existing_values: Dict[Tuple[int, int], float] = {}
    target_values: Dict[Tuple[int, int], float] = {}
    max_x = -1
    max_y = -1

    for x, y, height_m, target_m in cur.fetchall():
        bx = int(x)
        by = int(y)
        if height_m is not None:
            existing_values[(bx, by)] = float(height_m)
        if target_m is not None:
            target_values[(bx, by)] = float(target_m)
        if bx > max_x:
            max_x = bx
        if by > max_y:
            max_y = by

    if max_x < 0 or max_y < 0:
        con.close()
        raise RuntimeError(f"No FieldState rows found in DB: {db_path}")

    grid_width = max_x + 1
    grid_height = max_y + 1

    # Also load projection parameters from Data table if present.
    cur.execute("SELECT Name, Value FROM Data")
    meta_rows = cur.fetchall()
    con.close()

    meta = {name: float(value) for name, value in meta_rows}
    globals().update(
        {
            "_FC_MEAN_LAT": meta.get("MeanLat"),
            "_FC_MEAN_LON": meta.get("MeanLon"),
            "_FC_MIN_X": meta.get("MinX"),
            "_FC_MIN_Y": meta.get("MinY"),
        }
    )

    return existing_values, target_values, grid_width, grid_height


def _pick_random_cut_bins_with_paths(
    db_path: Path,
    count: int,
) -> List[Tuple[int, int, int]]:
    """
    Pick up to `count` random bins that need cutting and have a non-zero HaulPath.
    Returns a list of (bx, by, haul_path_id).
    """
    con = sqlite3.connect(db_path)
    cur = con.cursor()
    cur.execute(
        "SELECT X, Y, HaulPath FROM FieldState "
        "WHERE InitialHeightM > TargetHeightM AND HaulPath IS NOT NULL AND HaulPath > 0"
    )
    rows = cur.fetchall()
    con.close()

    if not rows:
        return []

    random.shuffle(rows)
    rows = rows[: min(count, len(rows))]
    return [(int(x), int(y), int(hp)) for (x, y, hp) in rows]


def _render_elevation_image(
    existing_values: Dict[Tuple[int, int], float],
    grid_width: int,
    grid_height: int,
) -> Tuple[Image.Image, ImageDraw.ImageDraw]:
    """
    Render existing elevation to a 4K-style image using the same bin-loop
    approach as interpolate.py / render_existing_heights.py.
    """
    values = fill_missing_bins(existing_values, grid_width, grid_height)
    palette = generate_palette()
    min_elev, max_elev = elevation_range(values)

    img_width = grid_width * PIXELS_PER_BIN
    img_height = grid_height * PIXELS_PER_BIN
    image = Image.new("RGB", (img_width, img_height), (0, 0, 0))
    pixels = image.load()
    max_bin_y = grid_height - 1

    for (bx, by), elev in values.items():
        normalized = (elev - min_elev) / (max_elev - min_elev) if max_elev > min_elev else 0.0
        normalized = max(0.0, min(1.0, normalized))
        idx = int(round(normalized * 255))
        color = palette[idx]
        x0 = bx * PIXELS_PER_BIN
        y0 = (max_bin_y - by) * PIXELS_PER_BIN
        for dy in range(PIXELS_PER_BIN):
            for dx in range(PIXELS_PER_BIN):
                px = x0 + dx
                py = y0 + dy
                if 0 <= px < img_width and 0 <= py < img_height:
                    pixels[px, py] = color

    draw = ImageDraw.Draw(image)
    return image, draw


def _load_haul_arrows_from_db(
    db_path: Path,
    grid_width: int,
    grid_height: int,
) -> List[Tuple[float, float, float]]:
    """
    Load high-level haul arrows from HaulArrows table and map them into
    image pixel coordinates using a simple local projection and scaling.

    Returns a list of (px, py, heading_deg).
    """
    con = sqlite3.connect(db_path)
    cur = con.cursor()
    cur.execute("SELECT Latitude, Longitude, Heading FROM HaulArrows")
    rows = cur.fetchall()
    con.close()

    if not rows:
        return []

    import math

    mean_lat = globals().get("_FC_MEAN_LAT")
    mean_lon = globals().get("_FC_MEAN_LON")
    min_x = globals().get("_FC_MIN_X")
    min_y = globals().get("_FC_MIN_Y")
    if any(v is None for v in (mean_lat, mean_lon, min_x, min_y)):
        # Fallback: cannot reliably project without metadata.
        return []

    lat0_rad = math.radians(mean_lat)
    max_bin_y = grid_height - 1

    arrows_px: List[Tuple[float, float, float]] = []
    for lat, lon, heading in rows:
        lat = float(lat)
        lon = float(lon)
        heading = float(heading)

        dlat = math.radians(lat - mean_lat)
        dlon = math.radians(lon - mean_lon)
        y_local = EARTH_RADIUS_M * dlat
        x_local = EARTH_RADIUS_M * dlon * math.cos(lat0_rad)

        bx_f = (x_local - min_x) / BIN_SIZE_M
        by_f = (y_local - min_y) / BIN_SIZE_M
        if bx_f < 0.0 or bx_f >= grid_width or by_f < 0.0 or by_f >= grid_height:
            continue

        px = bx_f * PIXELS_PER_BIN
        py = (max_bin_y - by_f) * PIXELS_PER_BIN
        arrows_px.append((px, py, heading))

    return arrows_px


def main() -> int:
    parser = argparse.ArgumentParser(
        description=(
            "Visual checker for fieldcreator SQLite output: renders a 4K PNG of existing "
            "elevation and 50 random haul paths as black polylines."
        )
    )
    parser.add_argument(
        "--db",
        type=Path,
        required=True,
        help="Path to SQLite DB produced by fieldcreator.py",
    )
    parser.add_argument(
        "--output",
        type=Path,
        default=Path("fieldcreator_checker_4k.png"),
        help="Output PNG path (default: fieldcreator_checker_4k.png)",
    )
    parser.add_argument(
        "--path-count",
        type=int,
        default=50,
        help="Number of random haul paths to draw (default: 50)",
    )
    args = parser.parse_args()

    if not args.db.is_file():
        print(f"ERROR: DB file not found: {args.db}", file=sys.stderr)
        return 1
    if args.path_count <= 0:
        print("ERROR: --path-count must be > 0", file=sys.stderr)
        return 1

    existing_values, target_values, grid_width, grid_height = _load_fieldstate(args.db)

    image, draw = _render_elevation_image(existing_values, grid_width, grid_height)

    # Draw high-level haul arrows (from HaulArrows) above elevation but below haul paths.
    arrows = _load_haul_arrows_from_db(args.db, grid_width, grid_height)
    if arrows:
        for px, py, direction_deg in arrows:
            _draw_arrow_hollow_triangle(
                draw,
                px,
                py,
                direction_deg,
                color=SOURCE_ARROW_COLOR,
                length_px=ARROW_LENGTH_PX * 2.0,
                base_half_width_px=ARROW_BASE_HALF_WIDTH_PX * 2.0,
            )

    # Choose random cut bins with associated haul paths directly from the DB.
    selected = _pick_random_cut_bins_with_paths(
        db_path=args.db,
        count=args.path_count,
    )
    if not selected:
        print(
            "WARNING: no cut bins with HaulPath > 0 found; only elevation will be rendered.",
            file=sys.stderr,
        )
    else:
        import math

        con = sqlite3.connect(args.db)
        cur = con.cursor()
        max_bin_y = grid_height - 1
        path_count = 0
        for bx, by, haul_path_id in selected:
            cur.execute(
                "SELECT PointNumber, Latitude, Longitude "
                "FROM HaulPaths WHERE HaulPath = ? ORDER BY PointNumber",
                (haul_path_id,),
            )
            rows = cur.fetchall()
            if len(rows) < 2:
                continue

            # Use the first point as local origin for this path.
            _, lat0, lon0 = rows[0]
            lat0 = float(lat0)
            lon0 = float(lon0)
            lat0_rad = math.radians(lat0)
            x0_bin = bx + 0.5
            y0_bin = by + 0.5

            bin_points: List[Tuple[float, float]] = []
            for _, lat, lon in rows:
                lat = float(lat)
                lon = float(lon)
                dlat = math.radians(lat - lat0)
                dlon = math.radians(lon - lon0)
                y_m = EARTH_RADIUS_M * dlat
                x_m = EARTH_RADIUS_M * dlon * math.cos(lat0_rad)
                dx_bin = x_m / BIN_SIZE_M
                dy_bin = y_m / BIN_SIZE_M
                x_center = x0_bin + dx_bin
                y_center = y0_bin + dy_bin
                bin_points.append((x_center, y_center))

            if len(bin_points) < 2:
                continue

            path_pixels = [
                (
                    x_center * PIXELS_PER_BIN,
                    (max_bin_y - y_center) * PIXELS_PER_BIN,
                )
                for (x_center, y_center) in bin_points
            ]
            draw.line(
                path_pixels,
                fill=(0, 0, 0),
                width=max(2, PIXELS_PER_BIN // 4),
            )
            path_count += 1

        con.close()

        print(
            f"INFO: drew {path_count} haul paths from {len(selected)} random cut-bin starts",
            file=sys.stderr,
        )

    image.save(args.output)
    print(f"Wrote {args.output}", file=sys.stderr)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())

