"""
Write a GeoTIFF from raw float32 raster data using rasterio (same layout as demgenerator.py).
Used by the C# FlowMapGenerator so the output TIFF has identical binary layout to the script.
"""

from __future__ import annotations

import argparse
import json
import sys
from pathlib import Path

import numpy as np
import rasterio
from rasterio.crs import CRS
from rasterio.transform import Affine

BIN_SIZE_M = 0.6096
NODATA_VALUE = -9999.0


def _crs_utm(zone: int, is_northern: bool) -> CRS:
    return CRS.from_epsg(32600 + zone if is_northern else 32700 + zone)


def main() -> None:
    parser = argparse.ArgumentParser(
        description="Write GeoTIFF from raw float32 raster (same layout as demgenerator)."
    )
    parser.add_argument("raw_path", type=Path, help="Raw float32 binary, row-major (nrows*ncols*4 bytes)")
    parser.add_argument("meta_path", type=Path, help="JSON with nrows, ncols, topLeftEasting, topLeftNorthing, utmZone, isNorthernHemisphere, nodata")
    parser.add_argument("output_path", type=Path, help="Output GeoTIFF path")
    args = parser.parse_args()

    if not args.raw_path.is_file():
        print(f"Error: raw file not found: {args.raw_path}", file=sys.stderr)
        sys.exit(1)
    if not args.meta_path.is_file():
        print(f"Error: metadata file not found: {args.meta_path}", file=sys.stderr)
        sys.exit(1)

    with open(args.meta_path) as f:
        meta = json.load(f)
    nrows = int(meta["nrows"])
    ncols = int(meta["ncols"])
    top_left_easting = float(meta["topLeftEasting"])
    top_left_northing = float(meta["topLeftNorthing"])
    utm_zone = int(meta["utmZone"])
    is_northern = bool(meta["isNorthernHemisphere"])
    nodata = float(meta.get("nodata", NODATA_VALUE))

    expected = nrows * ncols * 4
    raw = args.raw_path.read_bytes()
    if len(raw) != expected:
        print(f"Error: raw file size {len(raw)} != expected {expected}", file=sys.stderr)
        sys.exit(1)

    data = np.frombuffer(raw, dtype=np.float32).reshape((nrows, ncols))

    transform = Affine(
        BIN_SIZE_M, 0.0, top_left_easting,
        0.0, -BIN_SIZE_M, top_left_northing,
    )
    crs = _crs_utm(utm_zone, is_northern)

    valid = data[data != nodata]
    if valid.size > 0:
        stats_min, stats_max = float(np.min(valid)), float(np.max(valid))
    else:
        stats_min, stats_max = nodata, nodata

    with rasterio.open(
        args.output_path,
        "w",
        driver="GTiff",
        height=nrows,
        width=ncols,
        count=1,
        dtype=data.dtype,
        crs=crs,
        transform=transform,
        nodata=nodata,
    ) as dst:
        dst.write(data, 1)
        dst.update_tags(1, STATISTICS_MINIMUM=stats_min, STATISTICS_MAXIMUM=stats_max)

    print(f"Wrote: {args.output_path}", file=sys.stderr)


if __name__ == "__main__":
    main()
