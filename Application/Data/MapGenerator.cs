using AgGrade.Controller;
using AgGrade.Controls;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using static AgGrade.Data.FlowMapGenerator;
using static System.Net.Mime.MediaTypeNames;

namespace AgGrade.Data
{
    public class MapGenerator
    {
        private const int TILE_SIZE = 128;

        /// <summary>Pixels of source overlap when extracting flow/ponding tiles to avoid visible seams.</summary>
        private const int OVERLAY_TILE_SOURCE_OVERLAP = 1;
        /// <summary>Pixels of source overlap for ponding tiles.</summary>
        private const int PONDING_TILE_SOURCE_OVERLAP = 1;
        private const int TILE_OVERLAP = 5;
        private const int PROJECTED_PATH_LENGTH_M = 80;
        private const double HEADING_BUCKET_DEGREES = 0.5;

        private class MapTile
        {
            public Bitmap Bitmap;
            public Bitmap RotatedBitmap;
            public int TileGridX;
            public int TileGridY;
            public int StartX;
            public int StartY;
            public int Width;
            public int Height;
            public int MinBinX;
            public int MaxBinX;
            public int MinBinY;
            public int MaxBinY;
            public List<Bin> AssociatedBins;
            public int RotationBucket = int.MinValue;

            public MapTile
                (
                int TileGridX,
                int TileGridY,
                int StartX,
                int StartY,
                int Width,
                int Height
                )
            {
                this.TileGridX = TileGridX;
                this.TileGridY = TileGridY;
                this.StartX = StartX;
                this.StartY = StartY;
                this.Width = Width;
                this.Height = Height;
                this.AssociatedBins = new List<Bin>();
            }

            /// <summary>
            /// Checks if any of the bins in this tile are dirty
            /// If they are then marks the bins as not dirty and returns true
            /// otherwise returns false
            /// </summary>
            /// <param name="field">The field containing the bins to check (unused, kept for compatibility)</param>
            /// <returns>true if any bins in the tile were dirty, false otherwise</returns>
            public bool IsDirty
                (
                Field field
                )
            {
                if (AssociatedBins == null || AssociatedBins.Count == 0)
                {
                    return false;
                }

                // Only check the bins associated with this tile
                bool anyDirty = false;
                foreach (Bin bin in AssociatedBins)
                {
                    if (bin.Dirty)
                    {
                        anyDirty = true;
                    }
                }

                return anyDirty;
            }

            /// <summary>
            /// Resets all dirty flags
            /// </summary>
            public void ClearDirty
                (
                )
            {
                foreach (Bin bin in AssociatedBins)
                {
                    bin.Dirty = false;
                }
            }
        }

        private class TileCache
        {
            public Dictionary<long, MapTile> Tiles = new Dictionary<long, MapTile>();
            public double ScaleFactor;
            public int ImageWidthpx;
            public int ImageHeightpx;
        }

        private Color Background = Color.FromArgb(0xD4, 0xF9, 0xD8);
        // these are close to the official John Deere, CaseIH, New Holland and Catapillar colors
        private Color TractorYellow = Color.FromArgb(0xFF, 0xC4, 0x00);
        private Color TractorBlue = Color.FromArgb(0x00, 0x3F, 0x7D);
        private Color TractorRed = Color.FromArgb(0xC3, 0x1F, 0x17);
        private Color TractorGreen = Color.FromArgb(0x36, 0x7C, 0x2B);

        private TileCache Cache = new TileCache();

        /// <summary>
        /// Supported tractor colors
        /// </summary>
        public enum TractorColors
        {
            Red,
            Green,
            Blue,
            Yellow
        }

        public enum TractorStyles
        {
            Arrow,
            Dot
        }

        public enum MapTypes
        {
            Elevation,
            CutFill,
            Completion
        }

        /// <summary>
        /// The current scaling in pixels per meter
        /// </summary>
        public double CurrentScaleFactor { get; private set; }
        public string? BasemapDataFolderPath { get; set; }
        public Field? CurrentField { get; private set; }
        public Survey? CurrentSurvey { get; private set; }

        /// <summary>
        /// Offset of tractor Y position from top of screen in range 0 -> 10 where 5 is middle of screen and 10 is the bottom
        /// </summary>
        public int TractorYOffset = 7;

        /// <summary>
        /// The color of the tractor symbol
        /// </summary>
        public TractorColors TractorColor = TractorColors.Red;

        /// <summary>
        /// The style of the tractor symbol
        /// </summary>
        public TractorStyles TractorStyle = TractorStyles.Arrow;

        // Transformation parameters for mapping unrotated map pixels to final image pixels
        private int _unrotatedMapWidthpx;
        private int _unrotatedMapHeightpx;
        private double _tractorHeading;
        private int _mapLeftpx;
        private int _mapToppx;
        private Point _mapOffsetInImage;
        private GNSSFix TractorFix;
        private int _tractorXpx;
        private int _tractorYpx;
        private GNSSFix FrontScraperFix;
        private GNSSFix RearScraperFix;
        private EquipmentSettings CurrentEquipmentSettings;
        private AppSettings CurrentAppSettings;
        private bool ShowHaulArrows;
        private MapTypes MapType;
        private List<Coordinate> HaulPath;
        private int _rotatedMapWidthpx;
        private int _rotatedMapHeightpx;
        private double _rotationCosNeg;
        private double _rotationSinNeg;
        private double _unrotatedCenterX;
        private double _unrotatedCenterY;
        private double _rotatedCenterX;
        private double _rotatedCenterY;
        private double _invScaleFactor;
        private double _pixelsPerBin;
        private static readonly int[,] ElevationPalette = BuildColorPalette();
        private static readonly Color[] CutFillBandColors = BuildCutFillBandColors();
        private static readonly Color CutFillFallbackColor = Color.FromArgb(0x80, 0x80, 0x80);
        private bool ShowSurfaceFlow;
        private FlowMapGenerator.ElevationTypes SurfaceFlowElevationType;
        private Coordinate SurfaceFlowSWCorner = new Coordinate();
        private Coordinate SurfaceFlowNECorner = new Coordinate();
        private string? SurfaceFlowImage = null;
        private bool ShowPonding;
        private FlowMapGenerator.ElevationTypes PondingElevationType;
        private Coordinate PondingSWCorner = new Coordinate();
        private Coordinate PondingNECorner = new Coordinate();
        private string? PondingImage = null;
        private bool ShowBenchmarks;
        private bool ShowSurveyCoverage;
        private bool ShowSatelliteBasemap;
        private BruTileBasemapLayer? _bruTileBasemapLayer = null;
        private bool _enableBruTileBasemap = false;
        private bool _wasBruTileEnabled = false;

        private readonly Dictionary<long, Bitmap> _surfaceFlowTileCache = new Dictionary<long, Bitmap>();
        private readonly Dictionary<(long, int), Bitmap> _surfaceFlowRotatedTileCache = new Dictionary<(long, int), Bitmap>();
        private int _surfaceFlowRotationBucket = int.MinValue;
        private double? _surfaceFlowCacheScaleFactor = null;
        private Bitmap? _surfaceFlowImageBitmap = null;
        private string? _surfaceFlowImagePath = null;

        private readonly Dictionary<long, Bitmap> _pondingTileCache = new Dictionary<long, Bitmap>();
        private readonly Dictionary<(long, int), Bitmap> _pondingRotatedTileCache = new Dictionary<(long, int), Bitmap>();
        private int _pondingRotationBucket = int.MinValue;
        private double? _pondingCacheScaleFactor = null;
        private Bitmap? _pondingImageBitmap = null;
        private string? _pondingImagePath = null;
        private Bitmap? _pondingProjectedMapBitmap = null;
        private double? _pondingProjectedScaleFactor = null;
        private string? _pondingProjectedImagePath = null;
        private int _pondingProjectedMapWidthpx = 0;
        private int _pondingProjectedMapHeightpx = 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static long GetTileKey(int tileX, int tileY)
        {
            return ((long)tileX << 32) | (uint)tileY;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static double QuantizeHeading(double headingDegrees)
        {
            return Math.Round(headingDegrees / HEADING_BUCKET_DEGREES) * HEADING_BUCKET_DEGREES;
        }

        private static void DisposeTileBitmaps(MapTile tile)
        {
            if ((tile.RotatedBitmap != null) && !ReferenceEquals(tile.RotatedBitmap, tile.Bitmap))
            {
                tile.RotatedBitmap.Dispose();
                tile.RotatedBitmap = null!;
            }

            if (tile.Bitmap != null)
            {
                tile.Bitmap.Dispose();
                tile.Bitmap = null!;
            }
        }

        /// <summary>
        /// Calculates the scale factor (pixels per meter) to fit the entire map inside the image
        /// Takes into account that the field may be wider than it is tall, or taller than it is wide
        /// </summary>
        /// <param name="Field">The field to fit</param>
        /// <param name="ImageWidthpx">Width of the image in pixels</param>
        /// <param name="ImageHeightpx">Height of the image in pixels</param>
        /// <param name="TractorHeading">The tractor heading in degrees</param>
        /// <returns>Scale factor in pixels per meter that will fit the map in the image</returns>
        public static double CalculateScaleFactorToFit
            (
            Field Field,
            int ImageWidthpx,
            int ImageHeightpx,
            double TractorHeading
            )
        {
            if (ImageWidthpx <= 0 || ImageHeightpx <= 0)
            {
                throw new ArgumentException("Image dimensions must be greater than zero");
            }

            if (Field != null)
            {
                // Calculate field dimensions in meters
                double MapWidthM = Field.FieldMaxX - Field.FieldMinX;
                double MapHeightM = Field.FieldMaxY - Field.FieldMinY;

                if (MapWidthM <= 0 || MapHeightM <= 0)
                {
                    throw new ArgumentException("Field dimensions must be greater than zero");
                }

                PointD RotatedSizeM = GetRotatedSizeD(MapWidthM, MapHeightM, TractorHeading);

                // Calculate scale factors for both dimensions
                // Use the smaller scale factor to ensure the map fits in both dimensions
                double ScaleFactorX = ImageWidthpx / RotatedSizeM.X;
                double ScaleFactorY = ImageHeightpx / RotatedSizeM.Y;

                // Return the smaller scale factor to ensure the entire map fits
                return Math.Min(ScaleFactorX, ScaleFactorY);
            }

            return 1;
        }

        /// <summary>
        /// Generates the map as a bitmap
        /// </summary>
        /// <param name="Field">Field to display or null for no field</param>
        /// <param name="Survey">Survey to display or null for no survey</param>
        /// <param name="ImageWidthpx">Width of bitmap</param>
        /// <param name="ImageHeightpx">Height of bitmap</param>
        /// <param name="ShowGrid">true to show the bin grid</param>
        /// <param name="ScaleFactor">Zoom level for map in pixels per m</param>
        /// <param name="TractorFix">Current tractor fix</param>
        /// <param name="FrontScraperFix">Current front scraper fix</param>
        /// <param name="RearScraperFix">Current rear scraper fix</param>
        /// <param name="Benchmarks">Benchmark points to show</param>
        /// <param name="TractorLocationHistory">Locations where the tractor has been</param>
        /// <param name="CurrentEquipmentSettings">The current equipment settings</param>
        /// <param name="CurrentAppSettings">The current application settings</param>
        /// <param name="ShowHaulArrows">true to show the haul arrows</param>
        /// <param name="MapType">The type of map to generate</param>
        /// <param name="TractorStyle">Style of tractor icon</param>
        /// <param name="HaulPath">List of points for current haul path or empty/null for no path</param>
        /// <param name="ShowSurfaceFlow">true to show the surface water flow</param>
        /// <param name="ShowPonding">true to show the ponding overlay</param>
        /// <param name="ShowBenchmarks">true to show benchmarks</param>
        /// <param name="ShowSatelliteBasemap">true to show satellite basemap</param>
        /// <param name="ShowSurveyCoverage">true to show the survey coverage</param>
        /// <returns>Generated bitmap</returns>
        public Bitmap Generate
            (
            Field? Field,
            Survey? Survey,
            int ImageWidthpx,
            int ImageHeightpx,
            bool ShowGrid,
            double ScaleFactor,
            GNSSFix TractorFix,
            GNSSFix FrontScraperFix,
            GNSSFix RearScraperFix,
            List<Coordinate> TractorLocationHistory,
            EquipmentSettings CurrentEquipmentSettings,
            AppSettings CurrentAppSettings,
            bool ShowHaulArrows,
            MapTypes MapType,
            TractorStyles TractorStyle,
            List<Coordinate> HaulPath,
            bool ShowSurfaceFlow,
            bool ShowPonding,
            bool ShowBenchmarks,
            bool ShowSatelliteBasemap,
            bool ShowSurveyCoverage
            )
        {
            CurrentField = Field;
            CurrentSurvey = Survey;
            CurrentScaleFactor = ScaleFactor;

            this.TractorFix = TractorFix;
            this.FrontScraperFix = FrontScraperFix;
            this.RearScraperFix = RearScraperFix;

            this.CurrentEquipmentSettings = CurrentEquipmentSettings;
            this.CurrentAppSettings = CurrentAppSettings;
            _enableBruTileBasemap = ShowSatelliteBasemap;
            if (!_enableBruTileBasemap && _wasBruTileEnabled && _bruTileBasemapLayer != null)
            {
                _bruTileBasemapLayer.Dispose();
                _bruTileBasemapLayer = null;
            }
            _wasBruTileEnabled = _enableBruTileBasemap;

            this.ShowHaulArrows = ShowHaulArrows;
            this.ShowBenchmarks = ShowBenchmarks;
            this.ShowSatelliteBasemap = ShowSatelliteBasemap;
            this.ShowSurveyCoverage = ShowSurveyCoverage;

            this.HaulPath = HaulPath;

            // has the type of map changed?
            bool MapTypeChanged = false;
            if (MapType != this.MapType)
            {
                MapTypeChanged = true;
            }

            this.MapType = MapType;
            this.TractorStyle = TractorStyle;

            this.ShowSurfaceFlow = ShowSurfaceFlow;
            this.ShowPonding = ShowPonding;

            // get size of display in meters
            double ImageWidthM = ImageWidthpx * ScaleFactor;
            double ImageHeightM = ImageHeightpx * ScaleFactor;

            // calculate location of tractor in pixels
            int TractorXpx = ImageWidthpx / 2;
            int TractorYpx = (int)(ImageHeightpx * TractorYOffset / 10.0);

            // Store tractor pixel position for LatLonToWorld
            _tractorXpx = TractorXpx;
            _tractorYpx = TractorYpx;

            _tractorHeading = QuantizeHeading(
                TractorFix.Vector.GetTrueHeading(
                    CurrentAppSettings.MagneticDeclinationDegrees,
                    CurrentAppSettings.MagneticDeclinationMinutes));

            // if tile cache is empty then set up
            if (Cache.Tiles.Count == 0)
            {
                Cache.ScaleFactor = ScaleFactor;
                Cache.ImageHeightpx = ImageHeightpx;
                Cache.ImageWidthpx = ImageWidthpx;
            }
            // cache is not empty, check if we need to empty it
            else
            {
                // if anything that affects the cache has changed then empty it and set up
                if ((ScaleFactor != Cache.ScaleFactor) || (ImageHeightpx != Cache.ImageHeightpx) || (ImageWidthpx != Cache.ImageWidthpx) || MapTypeChanged)
                {
                    foreach (MapTile tile in Cache.Tiles.Values)
                    {
                        DisposeTileBitmaps(tile);
                    }
                    Cache.Tiles.Clear();
                    Cache.ScaleFactor = ScaleFactor;
                    Cache.ImageHeightpx = ImageHeightpx;
                    Cache.ImageWidthpx = ImageWidthpx;
                    ClearSurfaceFlowTileCache();
                    ClearPondingTileCache();
                }
            }

            // Create bitmap directly in memory with transparency support
            Bitmap bitmap = new Bitmap(ImageWidthpx, ImageHeightpx, PixelFormat.Format32bppArgb);
            using (Graphics g = Graphics.FromImage(bitmap))
            {
                g.FillRectangle(new SolidBrush(Background), 0, 0, ImageWidthpx, ImageHeightpx);
            }

            if (!TractorFix.IsValid)
            {
                using (Graphics g = Graphics.FromImage(bitmap))
                {
                    Bitmap NoLocation = Properties.Resources.nolocation_800px;
                    int Width = NoLocation.Width;
                    int Height = NoLocation.Height;

                    if (ImageWidthpx < Width)
                    {
                        Width = ImageWidthpx - 100;
                        Height = Width / ImageWidthpx * Height;
                    }

                    g.DrawImage(NoLocation, (ImageWidthpx / 2) - (Width / 2), (ImageHeightpx / 2) - (Height / 2), Width, Height);
                }

                return bitmap;
            }

            if (_enableBruTileBasemap)
            {
                _bruTileBasemapLayer ??= new BruTileBasemapLayer();
                try
                {
                    using (Graphics g = Graphics.FromImage(bitmap))
                    {
                        _bruTileBasemapLayer.Render(
                            g,
                            ImageWidthpx,
                            ImageHeightpx,
                            TractorFix,
                            TractorXpx,
                            TractorYpx,
                            _tractorHeading,
                            CurrentScaleFactor,
                            BruTileBasemapLayer.BasemapProviders.SatelliteEsri,
                            (coord) => LatLonToWorldF(coord),
                            BasemapDataFolderPath);
                    }
                }
                catch
                {
                    // Basemap add-on must never break the existing renderer.
                }
            }

            // if a field is defined then render it
            if ((Field != null) && (Field.Bins.Count > 0))
            {
                // get size of map in meters
                double MapWidthM = Field.FieldMaxX - Field.FieldMinX;
                double MapHeightM = Field.FieldMaxY - Field.FieldMinY;

                // get size of map in pixels
                int MapWidthpx = (int)Math.Round(MapWidthM * ScaleFactor);
                int MapHeightpx = (int)Math.Round(MapHeightM * ScaleFactor);

                int gridWidth = Field.GridWidth;
                int gridHeight = Field.GridHeight;
                //Field.CalculateBinGridSize(out gridWidth, out gridHeight);
                var BinGrid = Field.BinGrid;

                // The bin origin is the field coordinate that corresponds to bin grid index 0
                // Bins are created with: BinX = Floor((Point.X - MinX) / BinSizeM)
                // where MinX is the minimum X of topology points, which should equal Field.FieldMinX
                // So bin grid index 0 corresponds to field coordinate Field.FieldMinX
                double binOriginX = Field.FieldMinX;
                double binOriginY = Field.FieldMinY;

                double[] ElevationRange = CalculateInitialElevationRange(Field);
                double minElevation = ElevationRange[0];
                double maxElevation = ElevationRange[1];

                if (minElevation == Field.BIN_NO_DATA_SENTINEL && maxElevation == Field.BIN_NO_DATA_SENTINEL)
                {
                    throw new InvalidOperationException("No valid elevation data found in initial elevation range");
                }

                // If elevation range is too small, add some artificial range for visualization
                if (maxElevation - minElevation < 0.01) // Less than 1cm difference
                {
                    var centerElevation = (minElevation + maxElevation) / 2;
                    minElevation = centerElevation - 0.1; // 10cm below center
                    maxElevation = centerElevation + 0.1; // 10cm above center
                }

                // Store transformation parameters for helper function
                _unrotatedMapWidthpx = MapWidthpx;
                _unrotatedMapHeightpx = MapHeightpx;

                // get top left corner of map inside image
                Point MapTopLeft = GetMapOffset(TractorFix.Latitude, TractorFix.Longitude, TractorXpx, TractorYpx, _tractorHeading);

                _mapOffsetInImage = new Point(MapTopLeft.X, MapTopLeft.Y);

                // Adjust for tile overlap - tiles are drawn with -TILE_OVERLAP offset, so we need to compensate
                int MapLeftpx = MapTopLeft.X + TILE_OVERLAP;
                int MapToppx = MapTopLeft.Y + TILE_OVERLAP;

                // Store translation parameters
                _mapLeftpx = MapLeftpx;
                _mapToppx = MapToppx;

                // Precompute transform values reused by visibility checks and coordinate transforms.
                if (_tractorHeading == 0.0)
                {
                    _rotatedMapWidthpx = MapWidthpx;
                    _rotatedMapHeightpx = MapHeightpx;
                    _rotationCosNeg = 1.0;
                    _rotationSinNeg = 0.0;
                }
                else
                {
                    Point rotatedSize = GetRotatedSize(_unrotatedMapWidthpx, _unrotatedMapHeightpx, _tractorHeading);
                    _rotatedMapWidthpx = rotatedSize.X;
                    _rotatedMapHeightpx = rotatedSize.Y;
                    double radians = _tractorHeading * Math.PI / 180.0;
                    _rotationCosNeg = Math.Cos(-radians);
                    _rotationSinNeg = Math.Sin(-radians);
                }

                _unrotatedCenterX = _unrotatedMapWidthpx / 2.0;
                _unrotatedCenterY = _unrotatedMapHeightpx / 2.0;
                _rotatedCenterX = _rotatedMapWidthpx / 2.0;
                _rotatedCenterY = _rotatedMapHeightpx / 2.0;
                _invScaleFactor = 1.0 / CurrentScaleFactor;
                _pixelsPerBin = CurrentScaleFactor * Field.BIN_SIZE_M;

                // Use static palette to avoid per-frame regeneration.
                var colorPalette = ElevationPalette;

                // generate a list of visible tiles
                List<MapTile> Tiles = new List<MapTile>();

                // Only render if there's a visible portion
                if (MapWidthpx > 0 && MapHeightpx > 0)
                {
                    // Render map using tiles with overlap
                    int tilesX = (int)Math.Ceiling((double)MapWidthpx / TILE_SIZE);
                    int tilesY = (int)Math.Ceiling((double)MapHeightpx / TILE_SIZE);

                    for (int tileY = 0; tileY < tilesY; tileY++)
                    {
                        for (int tileX = 0; tileX < tilesX; tileX++)
                        {
                            // Calculate tile bounds (core tile area)
                            int tileStartX = tileX * TILE_SIZE;
                            int tileStartY = tileY * TILE_SIZE;
                            int tileWidth = Math.Min(TILE_SIZE, MapWidthpx - tileStartX);
                            int tileHeight = Math.Min(TILE_SIZE, MapHeightpx - tileStartY);

                            // Calculate extended bounds with overlap
                            int extendedStartX = Math.Max(0, tileStartX - TILE_OVERLAP);
                            int extendedStartY = Math.Max(0, tileStartY - TILE_OVERLAP);
                            int extendedEndX = Math.Min(MapWidthpx, tileStartX + tileWidth + TILE_OVERLAP);
                            int extendedEndY = Math.Min(MapHeightpx, tileStartY + tileHeight + TILE_OVERLAP);
                            int extendedWidth = extendedEndX - extendedStartX;
                            int extendedHeight = extendedEndY - extendedStartY;

                            // if this tile can be seen then retrieve from cache or render and add to list of tiles to show
                            bool tileInView = IsTileInView(tileStartX, tileStartY, tileWidth, tileHeight, ImageWidthpx, ImageHeightpx);
                            if (tileInView)
                            {
                                // O(1) tile cache lookup by grid coordinate.
                                long tileKey = GetTileKey(tileX, tileY);
                                bool InCache = Cache.Tiles.TryGetValue(tileKey, out MapTile? CachedTile);

                                // check if any of the bins in the cached tile are marked as dirty
                                // if they are then remove tile from cache
                                if (InCache && CachedTile!.IsDirty(CurrentField))
                                {
                                    // remove tile from cache to force a re-render
                                    Cache.Tiles.Remove(tileKey);
                                    DisposeTileBitmaps(CachedTile!);
                                    InCache = false;
                                }

                                if (!InCache)
                                {
                                    MapTile NewTile = new MapTile(tileX, tileY, tileStartX, tileStartY, tileWidth, tileHeight);

                                    // Calculate and store the bin coordinate range for this tile
                                    CalculateTileBinRange(NewTile);

                                    // Populate the list of bins associated with this tile
                                    PopulateTileBins(NewTile, CurrentField);

                                    switch (MapType)
                                    {
                                        case MapTypes.Elevation:
                                            // Render this tile with extended bounds to include overlap
                                            NewTile.Bitmap = RenderElevationTile(
                                                BinGrid,
                                                extendedStartX, extendedStartY, extendedWidth, extendedHeight,
                                                MapWidthpx, MapHeightpx,
                                                0, 0, gridWidth, gridHeight,
                                                minElevation, maxElevation,
                                                colorPalette, ShowGrid, ScaleFactor);
                                            break;

                                        case MapTypes.CutFill:
                                            NewTile.Bitmap = RenderCutFillTile(
                                                BinGrid,
                                                extendedStartX, extendedStartY, extendedWidth, extendedHeight,
                                                MapWidthpx, MapHeightpx,
                                                0, 0, gridWidth, gridHeight,
                                                ShowGrid, ScaleFactor);
                                            break;

                                        case MapTypes.Completion:
                                            NewTile.Bitmap = RenderCompletionTile(
                                                BinGrid,
                                                extendedStartX, extendedStartY, extendedWidth, extendedHeight,
                                                MapWidthpx, MapHeightpx,
                                                0, 0, gridWidth, gridHeight,
                                                ShowGrid, ScaleFactor);
                                            break;
                                    }

                                    // cache it
                                    Cache.Tiles[tileKey] = NewTile;
                                    // show it
                                    Tiles.Add(NewTile);
                                }
                                else
                                {
                                    Tiles.Add(CachedTile!);
                                }
                            }
                        }
                    }

                    // clear all dirty flags
                    foreach (MapTile Tile in Cache.Tiles.Values)
                    {
                        Tile.ClearDirty();
                    }
                }

                // rotate tiles
                int rotationBucket = (int)Math.Round(_tractorHeading / HEADING_BUCKET_DEGREES);
                double rotationHeading = rotationBucket * HEADING_BUCKET_DEGREES;
                Parallel.ForEach(Tiles, Tile =>
                {
                    if (_tractorHeading != 0)
                    {
                        if ((Tile.RotatedBitmap == null) || (Tile.RotationBucket != rotationBucket))
                        {
                            if ((Tile.RotatedBitmap != null) && !ReferenceEquals(Tile.RotatedBitmap, Tile.Bitmap))
                            {
                                Tile.RotatedBitmap.Dispose();
                            }
                            Tile.RotatedBitmap = Rotate(Tile.Bitmap, rotationHeading);
                            Tile.RotationBucket = rotationBucket;
                        }
                    }
                    else
                    {
                        Tile.RotatedBitmap = Tile.Bitmap;
                        Tile.RotationBucket = int.MinValue;
                    }
                });

                // render tiles
                using (Graphics g = Graphics.FromImage(bitmap))
                {
                    // Configure Graphics for high-quality rendering with transparency
                    g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;
                    g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                    g.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
                    g.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceOver;
                    // Keep previously rendered basemap pixels when enabled.
                    if (!_enableBruTileBasemap)
                    {
                        g.FillRectangle(new SolidBrush(Background), 0, 0, bitmap.Width, bitmap.Height);
                    }

                    // add tiles
                    foreach (MapTile Tile in Tiles)
                    {
                        int StartX;
                        int StartY;

                        // Recalculate StartX and StartY for rotated tiles
                        if (_tractorHeading != 0)
                        {
                            // Get original unrotated tile dimensions
                            int originalWidth = Tile.Width;
                            int originalHeight = Tile.Height;

                            // Calculate the center of the unrotated tile in unrotated map coordinates
                            int unrotatedCenterX = Tile.StartX + originalWidth / 2;
                            int unrotatedCenterY = Tile.StartY + originalHeight / 2;

                            // Transform the center point to final image coordinates
                            Point rotatedCenter = UnrotatedMapPixelToFinalImagePixel(unrotatedCenterX, unrotatedCenterY);

                            // Get the rotated tile dimensions
                            int rotatedWidth = Tile.RotatedBitmap.Width;
                            int rotatedHeight = Tile.RotatedBitmap.Height;

                            // Calculate top-left position by subtracting half the rotated dimensions from the center
                            StartX = rotatedCenter.X - rotatedWidth / 2;
                            StartY = rotatedCenter.Y - rotatedHeight / 2;
                        }
                        else
                        {
                            // For unrotated tiles, translate from unrotated map coordinates to final image coordinates
                            // StartX/StartY are in unrotated map pixel coordinates (0 to MapWidthpx-1)
                            // Need to add map offset to get final image coordinates
                            StartX = Tile.StartX + _mapLeftpx;
                            StartY = Tile.StartY + _mapToppx;
                        }

                        // Draw tile - the bitmap already includes 5px overlap on all sides
                        // For unrotated tiles: StartX/StartY is now in final image coordinates, shift by -5px to account for overlap in bitmap
                        // For rotated tiles: StartX/StartY is already calculated for the rotated position in final image coordinates
                        int drawX = StartX - TILE_OVERLAP;
                        int drawY = StartY - TILE_OVERLAP;

                        // Calculate source rectangle offset if drawing position is outside bounds
                        int srcX = 0;
                        int srcY = 0;
                        int srcWidth = Tile.RotatedBitmap.Width;
                        int srcHeight = Tile.RotatedBitmap.Height;

                        // Adjust source rectangle if drawing position is outside bitmap bounds
                        if (drawX < 0)
                        {
                            srcX = -drawX;
                            srcWidth -= srcX;
                            drawX = 0;
                        }
                        if (drawY < 0)
                        {
                            srcY = -drawY;
                            srcHeight -= srcY;
                            drawY = 0;
                        }

                        // Clip destination to bitmap bounds
                        int drawWidth = Math.Min(srcWidth, bitmap.Width - drawX);
                        int drawHeight = Math.Min(srcHeight, bitmap.Height - drawY);
                        srcWidth = drawWidth;
                        srcHeight = drawHeight;

                        // Only draw if we have valid dimensions
                        if (srcWidth > 0 && srcHeight > 0 && drawWidth > 0 && drawHeight > 0)
                        {
                            Rectangle srcRect = new Rectangle(srcX, srcY, srcWidth, srcHeight);
                            Rectangle destRect = new Rectangle(drawX, drawY, drawWidth, drawHeight);
                            g.DrawImage(Tile.RotatedBitmap, destRect, srcRect, GraphicsUnit.Pixel);
                        }
                    }
                }
            }

            List<Benchmark> Benchmarks = new List<Benchmark>();
            if (CurrentField != null) Benchmarks = CurrentField.Benchmarks;
            else if (CurrentSurvey != null) Benchmarks = CurrentSurvey.Benchmarks;

            Decorate(bitmap, Benchmarks, TractorLocationHistory, CurrentEquipmentSettings, HaulPath);

            return bitmap;
        }

        /// <summary>
        /// Calculates and stores the bin coordinate range for a tile
        /// </summary>
        /// <param name="tile">The tile to calculate bin range for</param>
        private void CalculateTileBinRange
            (
            MapTile tile
            )
        {
            if (CurrentField == null)
            {
                return;
            }

            // Tile coordinates are already in unrotated map space; use direct conversion.
            Point topLeftBin = PixelToBinFastUnrotated(tile.StartX, tile.StartY);
            Point topRightBin = PixelToBinFastUnrotated(tile.StartX + tile.Width, tile.StartY);
            Point bottomLeftBin = PixelToBinFastUnrotated(tile.StartX, tile.StartY + tile.Height);
            Point bottomRightBin = PixelToBinFastUnrotated(tile.StartX + tile.Width, tile.StartY + tile.Height);

            // Find the range of bin coordinates that could be in this tile
            tile.MinBinX = Math.Min(Math.Min(topLeftBin.X, topRightBin.X), Math.Min(bottomLeftBin.X, bottomRightBin.X));
            tile.MaxBinX = Math.Max(Math.Max(topLeftBin.X, topRightBin.X), Math.Max(bottomLeftBin.X, bottomRightBin.X));
            tile.MinBinY = Math.Min(Math.Min(topLeftBin.Y, topRightBin.Y), Math.Min(bottomLeftBin.Y, bottomRightBin.Y));
            tile.MaxBinY = Math.Max(Math.Max(topLeftBin.Y, topRightBin.Y), Math.Max(bottomLeftBin.Y, bottomRightBin.Y));
        }

        /// <summary>
        /// Populates the list of bins associated with a tile based on the tile's bin coordinate range
        /// </summary>
        /// <param name="tile">The tile to populate bins for</param>
        /// <param name="field">The field containing the bins</param>
        private void PopulateTileBins
            (
            MapTile tile,
            Field field
            )
        {
            if (field == null || field.Bins == null || field.Bins.Count == 0)
            {
                return;
            }

            // Clear any existing bins
            tile.AssociatedBins.Clear();

            // Pull bins directly from BinGrid window instead of scanning all field bins.
            int minX = Math.Max(0, tile.MinBinX);
            int maxX = Math.Min(field.GridWidth - 1, tile.MaxBinX);
            int minY = Math.Max(0, tile.MinBinY);
            int maxY = Math.Min(field.GridHeight - 1, tile.MaxBinY);

            if ((minX > maxX) || (minY > maxY))
            {
                return;
            }

            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    Bin? bin = field.BinGrid[y, x];
                    if (bin != null)
                    {
                        tile.AssociatedBins.Add(bin);
                    }
                }
            }
        }

        private void Decorate
            (
            Bitmap Map,
            List<Benchmark> Benchmarks,
            List<Coordinate> TractorLocationHistory,
            EquipmentSettings CurrentEquipmentSettings,
            List<Coordinate> HaulPath
            )
        {
            Pen TractorPen = new Pen(Color.FromArgb(0x80, 0x00, 0x00, 0x00), 2);
            Pen HaulArrowPen = new Pen(Color.FromArgb(0x80, 0x00, 0x00, 0x00), 2);
            Pen HaulArrowTipPen = new Pen(Color.FromArgb(0x80, 0x50, 0x50, 0x50), 2);
            Pen HaulPathPen = new Pen(Color.FromArgb(0xA0, 0x00, 0x00, 0x00), 4);

            var BenchmarkFontFamily = new FontFamily("Arial");
            var BenchmarkFont = new System.Drawing.Font(BenchmarkFontFamily, 14, FontStyle.Regular, GraphicsUnit.Pixel);

            using (Graphics g = Graphics.FromImage(Map))
            {
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

                // show the flow of surface water
                if (ShowSurfaceFlow)
                {
                    RenderSurfaceFlow(g);
                }

                if (ShowPonding)
                {
                    RenderPonding(g);
                }

                // draw survey points
                if (CurrentSurvey != null)
                {
                    bool hasSurveyElevationRange = TryGetSurveyElevationRange(CurrentSurvey, out double minSurveyElevation, out double maxSurveyElevation);
                    int defaultSurveyColorIndex = CutFillBandColors.Length / 2;
                    SolidBrush[] surveyPointBrushes = new SolidBrush[CutFillBandColors.Length];
                    Pen BoundaryPen = new Pen(Color.Gray, 1);

                    int PointWidthMm = 1200;
                    int PointWidthpx = (int)(PointWidthMm / 1000.0 * CurrentScaleFactor);
                    if (PointWidthpx < 2) PointWidthpx = 2;

                    int BoundaryWidthMm = 6000;
                    int BoundaryWidthpx = (int)(BoundaryWidthMm / 1000.0 * CurrentScaleFactor);

                    try
                    {
                        foreach (TopologyPoint tp in CurrentSurvey.InteriorPoints)
                        {
                            int colorIndex = hasSurveyElevationRange
                                ? GetCutFillBandIndexForSurveyElevation(tp.ExistingElevation, minSurveyElevation, maxSurveyElevation)
                                : defaultSurveyColorIndex;

                            surveyPointBrushes[colorIndex] ??= new SolidBrush(CutFillBandColors[colorIndex]);
                            SolidBrush pointBrush = surveyPointBrushes[colorIndex];

                            Point pt = LatLonToWorld(new Coordinate(tp.Latitude, tp.Longitude));
                            if (ShowSurveyCoverage)
                            {
                                g.DrawEllipse(BoundaryPen, pt.X - (BoundaryWidthpx / 2), pt.Y - (BoundaryWidthpx / 2), BoundaryWidthpx, BoundaryWidthpx);
                            }
                            g.FillEllipse(pointBrush, pt.X - (PointWidthpx / 2), pt.Y - (PointWidthpx / 2), PointWidthpx, PointWidthpx);
                        }

                        foreach (TopologyPoint tp in CurrentSurvey.BoundaryPoints)
                        {
                            int colorIndex = hasSurveyElevationRange
                                ? GetCutFillBandIndexForSurveyElevation(tp.ExistingElevation, minSurveyElevation, maxSurveyElevation)
                                : defaultSurveyColorIndex;

                            surveyPointBrushes[colorIndex] ??= new SolidBrush(CutFillBandColors[colorIndex]);
                            SolidBrush pointBrush = surveyPointBrushes[colorIndex];

                            Point pt = LatLonToWorld(new Coordinate(tp.Latitude, tp.Longitude));
                            g.FillEllipse(pointBrush, pt.X - (PointWidthpx / 2), pt.Y - (PointWidthpx / 2), PointWidthpx, PointWidthpx);
                        }
                    }
                    finally
                    {
                        foreach (SolidBrush? brush in surveyPointBrushes)
                        {
                            brush?.Dispose();
                        }

                        BoundaryPen.Dispose();
                    }
                }

                if (ShowBenchmarks)
                {
                    // draw benchmarks
                    foreach (Benchmark bmark in Benchmarks)
                    {
                        Point Pix = LatLonToWorld(bmark.Location);

                        g.FillPolygon(new SolidBrush(Color.Gray), new Point[] { new Point(Pix.X - 10, Pix.Y + 10), new Point(Pix.X + 10, Pix.Y + 10),
                        new Point(Pix.X, Pix.Y - 10) });
                        g.FillPolygon(new SolidBrush(Color.Orange), new Point[] { new Point(Pix.X - 9, Pix.Y + 9), new Point(Pix.X + 9, Pix.Y + 9),
                        new Point(Pix.X, Pix.Y - 9) });

                        g.DrawString(bmark.Name, BenchmarkFont, new SolidBrush(Color.Black), Pix.X + 12, Pix.Y - 6);
                    }
                }

                /*// draw tractor trail
                Point? LastLocpx = null;
                foreach (Coordinate TractorLocation in TractorLocationHistory.ToList())
                {
                    Point Pix = LatLonToWorld(TractorLocation);

                    if (LastLocpx != null)
                    {
                        g.DrawLine(TractorPen, LastLocpx.Value.X, LastLocpx.Value.Y, Pix.X, Pix.Y);
                    }

                    LastLocpx = Pix;
                }*/

                if (ShowHaulArrows && (CurrentField != null))
                {
                    // draw haul arrows
                    foreach (HaulDirection HaulDir in CurrentField.HaulDirections)
                    {
                        Point ArrowPix = LatLonToWorld(HaulDir.Location);
                        Point[] Vertices = ArrowOutlineTriangle(ArrowPix.X, ArrowPix.Y, HaulDir.DirectionDeg - _tractorHeading, CurrentScaleFactor);
                        g.DrawPolygon(HaulArrowPen, Vertices);
                        double lineDirDeg = HaulDir.DirectionDeg - _tractorHeading;
                        (double ldx, double ldy) = DirectionDegreesToVector(lineDirDeg);
                        int TipLength = (int)(40 * (CurrentScaleFactor / 13.0));
                        Point lineEnd = new Point(Vertices[0].X + (int)(TipLength * ldx), Vertices[0].Y + (int)(TipLength * ldy));
                        g.DrawLine(HaulArrowTipPen, Vertices[0], lineEnd);
                    }
                }

                // draw haul path
                if (HaulPath != null && HaulPath.Count > 0)
                {
                    List<Coordinate> haulPoints = HaulPath.ToList();
                    List<PointF> haulPixelPoints = haulPoints.Select(LatLonToWorldF).ToList();

                    for (int i = 1; i < haulPixelPoints.Count; i++)
                    {
                        g.DrawLine(HaulPathPen, haulPixelPoints[i - 1].X, haulPixelPoints[i - 1].Y, haulPixelPoints[i].X, haulPixelPoints[i].Y);
                    }

                    if (haulPoints.Count > 1)
                    {
                        // Arrow geometry and spacing in real-world feet.
                        const double FEET_TO_METERS = 0.3048;
                        double arrowLengthM = 20.0 * FEET_TO_METERS;
                        double arrowWidthM = 10.0 * FEET_TO_METERS;
                        double arrowSpacingM = 70.0 * FEET_TO_METERS;

                        List<double> cumulativeDistanceM = new List<double>(haulPoints.Count) { 0.0 };
                        for (int i = 1; i < haulPoints.Count; i++)
                        {
                            double segmentDistanceM = Haversine.Distance(
                                haulPoints[i - 1].Latitude,
                                haulPoints[i - 1].Longitude,
                                haulPoints[i].Latitude,
                                haulPoints[i].Longitude);
                            cumulativeDistanceM.Add(cumulativeDistanceM[i - 1] + segmentDistanceM);
                        }

                        double totalDistanceM = cumulativeDistanceM[haulPoints.Count - 1];
                        List<double> arrowDistancesM = new List<double>();
                        for (double d = totalDistanceM; d >= 0.0; d -= arrowSpacingM)
                        {
                            arrowDistancesM.Add(d);
                        }
                        if (arrowDistancesM.Count == 0)
                        {
                            arrowDistancesM.Add(totalDistanceM);
                        }

                        float arrowLengthPx = (float)(arrowLengthM * CurrentScaleFactor);
                        float arrowHalfWidthPx = (float)(arrowWidthM * CurrentScaleFactor / 2.0);

                        using (SolidBrush arrowBrush = new SolidBrush(HaulPathPen.Color))
                        {
                            foreach (double arrowDistanceM in arrowDistancesM)
                            {
                                if (TryGetPathPointAndDirection(haulPixelPoints, cumulativeDistanceM, arrowDistanceM, out PointF tip, out PointF direction))
                                {
                                    DrawArrowhead(g, arrowBrush, tip, direction, arrowLengthPx, arrowHalfWidthPx);
                                }
                            }
                        }
                    }
                }

                // draw tractor heading
                double Lat = TractorFix.Latitude;
                double Lon = TractorFix.Longitude;
                Haversine.MoveDistanceBearing(ref Lat, ref Lon, _tractorHeading, PROJECTED_PATH_LENGTH_M);
                Point DestPix = LatLonToWorld(new Coordinate(Lat, Lon));
                float[] dashValues = { 4, 4 };
                TractorPen.DashPattern = dashValues;
                g.DrawLine(TractorPen, _tractorXpx, _tractorYpx, DestPix.X, DestPix.Y);

                // get tractor pixel location - always fixed
                int TractorXpx = Map.Width / 2;
                int TractorYpx = (int)(Map.Height * TractorYOffset / 10.0);

                if (CurrentSurvey == null)
                {
                    if (CurrentEquipmentSettings.FrontPan.Equipped)
                    {
                        // get front scraper location
                        Point FrontScraperCenter = LatLonToWorld(new Coordinate(FrontScraperFix.Latitude, FrontScraperFix.Longitude));

                        // draw connector between tractor and front scraper
                        g.DrawLine(new Pen(new SolidBrush(Color.Black), 2), TractorXpx, TractorYpx, FrontScraperCenter.X, FrontScraperCenter.Y);

                        if (CurrentEquipmentSettings.RearPan.Equipped)
                        {
                            // get rear scraper location
                            Point RearScraperCenter = LatLonToWorld(new Coordinate(RearScraperFix.Latitude, RearScraperFix.Longitude));
                            // draw connector between front scraper to rear scraper
                            g.DrawLine(new Pen(new SolidBrush(Color.Black), 2), FrontScraperCenter.X, FrontScraperCenter.Y, RearScraperCenter.X, RearScraperCenter.Y);
                        }
                    }

                    if (CurrentEquipmentSettings.FrontPan.Equipped)
                    {
                        // draw front scraper
                        double PerpAngle = (FrontScraperFix.Vector.GetTrueHeading(CurrentAppSettings.MagneticDeclinationDegrees, CurrentAppSettings.MagneticDeclinationMinutes) + 90) % 360;
                        double BladeEndALat = FrontScraperFix.Latitude;
                        double BladeEndALon = FrontScraperFix.Longitude;
                        Haversine.MoveDistanceBearing(ref BladeEndALat, ref BladeEndALon, PerpAngle, CurrentEquipmentSettings.FrontPan.WidthMm / 1000.0 / 2.0);
                        Point BladeEndA = LatLonToWorld(new Coordinate(BladeEndALat, BladeEndALon));
                        double BladeEndBLat = FrontScraperFix.Latitude;
                        double BladeEndBLon = FrontScraperFix.Longitude;
                        Haversine.MoveDistanceBearing(ref BladeEndBLat, ref BladeEndBLon, PerpAngle, -(CurrentEquipmentSettings.FrontPan.WidthMm / 1000.0 / 2.0));
                        Point BladeEndB = LatLonToWorld(new Coordinate(BladeEndBLat, BladeEndBLon));
                        g.DrawLine(new Pen(new SolidBrush(Color.Black), 8), BladeEndA.X, BladeEndA.Y, BladeEndB.X, BladeEndB.Y);
                        g.DrawLine(new Pen(TractorYellow, 4), BladeEndA.X, BladeEndA.Y, BladeEndB.X, BladeEndB.Y);

                        if (CurrentEquipmentSettings.RearPan.Equipped)
                        {
                            // draw rear scraper
                            double RearPerpAngle = (RearScraperFix.Vector.GetTrueHeading(CurrentAppSettings.MagneticDeclinationDegrees, CurrentAppSettings.MagneticDeclinationMinutes) + 90) % 360;
                            double RearBladeEndALat = RearScraperFix.Latitude;
                            double RearBladeEndALon = RearScraperFix.Longitude;
                            Haversine.MoveDistanceBearing(ref RearBladeEndALat, ref RearBladeEndALon, RearPerpAngle, CurrentEquipmentSettings.RearPan.WidthMm / 1000.0 / 2.0);
                            Point RearBladeEndA = LatLonToWorld(new Coordinate(RearBladeEndALat, RearBladeEndALon));
                            double RearBladeEndBLat = RearScraperFix.Latitude;
                            double RearBladeEndBLon = RearScraperFix.Longitude;
                            Haversine.MoveDistanceBearing(ref RearBladeEndBLat, ref RearBladeEndBLon, RearPerpAngle, -(CurrentEquipmentSettings.RearPan.WidthMm / 1000.0 / 2.0));
                            Point RearBladeEndB = LatLonToWorld(new Coordinate(RearBladeEndBLat, RearBladeEndBLon));
                            g.DrawLine(new Pen(new SolidBrush(Color.Black), 8), RearBladeEndA.X, RearBladeEndA.Y, RearBladeEndB.X, RearBladeEndB.Y);
                            g.DrawLine(new Pen(TractorYellow, 4), RearBladeEndA.X, RearBladeEndA.Y, RearBladeEndB.X, RearBladeEndB.Y);
                        }
                    }
                }

                // draw tractor
                if (TractorStyle == TractorStyles.Arrow)
                {
                    // get tractor color
                    Bitmap TractorImage;
                    switch (TractorColor)
                    {
                        default:
                        case TractorColors.Green: TractorImage = Properties.Resources.navarrow_green_256px; break;
                        case TractorColors.Red: TractorImage = Properties.Resources.navarrow_red_256px; break;
                        case TractorColors.Blue: TractorImage = Properties.Resources.navarrow_blue_256px; break;
                        case TractorColors.Yellow: TractorImage = Properties.Resources.navarrow_yellow_256px; break;
                    }

                    // scale tractor so the width of the symbol matches the tractor width
                    int TractorWidthpx = (int)(CurrentEquipmentSettings.TractorWidthMm / 1000.0 * CurrentScaleFactor);
                    if (TractorWidthpx < 16) TractorWidthpx = 16;
                    // draw
                    g.DrawImage(TractorImage, TractorXpx - (TractorWidthpx / 2), TractorYpx - (TractorWidthpx / 2), TractorWidthpx, TractorWidthpx);
                }
                else if (TractorStyle == TractorStyles.Dot)
                {
                    Brush TractorBrush;
                    
                    switch (TractorColor)
                    {
                        default:
                        case TractorColors.Green: TractorBrush = new SolidBrush(Color.FromArgb(0x36, 0x7C, 0x2B)); break;
                        case TractorColors.Red: TractorBrush = new SolidBrush(Color.FromArgb(0xC3, 0x1F, 0x17)); break;
                        case TractorColors.Blue: TractorBrush = new SolidBrush(Color.FromArgb(0x00, 0x3F, 0x7D)); break;
                        case TractorColors.Yellow: TractorBrush = new SolidBrush(Color.FromArgb(0xFF, 0xC4, 0x00)); break;
                    }

                    g.FillEllipse(TractorBrush, TractorXpx - 10, TractorYpx - 10, 20, 20);
                }
            }
        }

        /// <summary>Convert direction (0 = North/up) to unit (dx, dy) in image coords (y down).</summary>
        private (double dx, double dy) DirectionDegreesToVector(double degrees)
        {
            double rad = degrees * Math.PI / 180.0;
            double dx = Math.Sin(rad);
            double dy = -Math.Cos(rad);
            return (dx, dy);
        }

        /// <summary>
        /// Gets a point and direction vector at a distance along a polyline.
        /// </summary>
        private bool TryGetPathPointAndDirection
            (
            List<PointF> polylinePoints,
            List<double> cumulativeDistanceM,
            double targetDistanceM,
            out PointF point,
            out PointF direction
            )
        {
            point = PointF.Empty;
            direction = PointF.Empty;

            if (polylinePoints == null || cumulativeDistanceM == null || polylinePoints.Count < 2 || cumulativeDistanceM.Count != polylinePoints.Count)
            {
                return false;
            }

            int segmentIndex = -1;
            for (int i = 1; i < cumulativeDistanceM.Count; i++)
            {
                if (targetDistanceM <= cumulativeDistanceM[i])
                {
                    segmentIndex = i;
                    break;
                }
            }

            if (segmentIndex < 0)
            {
                segmentIndex = cumulativeDistanceM.Count - 1;
                targetDistanceM = cumulativeDistanceM[segmentIndex];
            }

            PointF start = polylinePoints[segmentIndex - 1];
            PointF end = polylinePoints[segmentIndex];
            double startDistanceM = cumulativeDistanceM[segmentIndex - 1];
            double segmentDistanceM = cumulativeDistanceM[segmentIndex] - startDistanceM;

            double t = (segmentDistanceM > 1e-9) ? ((targetDistanceM - startDistanceM) / segmentDistanceM) : 1.0;
            if (t < 0.0) t = 0.0;
            if (t > 1.0) t = 1.0;

            float px = (float)(start.X + (end.X - start.X) * t);
            float py = (float)(start.Y + (end.Y - start.Y) * t);
            point = new PointF(px, py);

            float dx = end.X - start.X;
            float dy = end.Y - start.Y;
            float length = (float)Math.Sqrt(dx * dx + dy * dy);
            if (length <= 0.001f)
            {
                return false;
            }

            direction = new PointF(dx / length, dy / length);
            return true;
        }

        /// <summary>
        /// Draws a filled arrowhead at the given tip using unit direction vector.
        /// </summary>
        private void DrawArrowhead
            (
            Graphics g,
            Brush brush,
            PointF tip,
            PointF directionUnit,
            float lengthPx,
            float halfWidthPx
            )
        {
            PointF baseCenter = new PointF(
                tip.X - directionUnit.X * lengthPx,
                tip.Y - directionUnit.Y * lengthPx);

            PointF perp = new PointF(-directionUnit.Y, directionUnit.X);
            PointF left = new PointF(
                baseCenter.X + perp.X * halfWidthPx,
                baseCenter.Y + perp.Y * halfWidthPx);
            PointF right = new PointF(
                baseCenter.X - perp.X * halfWidthPx,
                baseCenter.Y - perp.Y * halfWidthPx);

            g.FillPolygon(brush, new[] { tip, left, right });
        }

        /// <summary>
        /// Calculates and generates the tiles for showing the surface water flow
        /// </summary>
        /// <param name="SurfaceFlowElevationType">Elevation type to use</param>
        /// <param name="Opacity">Opacity to use</param>
        public void CalculateSurfaceFlow
            (
            FlowMapGenerator.ElevationTypes SurfaceFlowElevationType,
            int Opacity = 50
            )
        {
            // Release the cached flow image and tiles (dispose bitmaps) so the temp file is no longer locked, then delete it
            ClearSurfaceFlowTileCache();
            if ((SurfaceFlowImage != null) && File.Exists(SurfaceFlowImage))
            {
                try { File.Delete(SurfaceFlowImage); } catch { /* ignore if already deleted or still in use */ }
                SurfaceFlowImage = null;
            }

            if (CurrentField == null) return;

            this.SurfaceFlowElevationType = SurfaceFlowElevationType;

            FlowMapGenerator FlowGen = new FlowMapGenerator();

            string TempDEM = Path.GetTempFileName() + ".tiff";
            string TempFlowDEM = Path.GetTempFileName() + ".tiff";
            string TempPNG = Path.GetTempFileName() + ".png";

            try
            {
                CurrentField.GenerateElevationDEM(FlowMapGenerator.ElevationTypes.Current, TempDEM, out SurfaceFlowSWCorner, out SurfaceFlowNECorner, true);
                FlowGen.GenerateFlowDEM(TempDEM, TempFlowDEM);
                FlowGen.ConvertFlowDEMtoPNG(TempFlowDEM, TempPNG, true, Opacity);

                SurfaceFlowImage = TempPNG;
            }
            catch
            {
                // on any error remove image
                if (File.Exists(TempPNG)) { File.Delete(TempPNG); }
                SurfaceFlowImage = null;
            }
            finally
            {
                if (File.Exists(TempFlowDEM)) { File.Delete(TempFlowDEM); }
                if (File.Exists(TempDEM)) { File.Delete(TempDEM); }
            }
        }

        /// <summary>
        /// Calculates and generates the ponding overlay (SCS runoff + D8 + fill-spill). Uses Curve Number and rainfall event depth.
        /// </summary>
        /// <param name="PondingElevationType">Elevation type to use</param>
        /// <param name="CurveNumber">SCS Curve Number (e.g. 78, 85)</param>
        /// <param name="RainfallMm">Event rainfall depth in mm</param>
        /// <param name="Opacity">PNG transparency: 0=fully opaque, 100=white fully transparent</param>
        public void CalculatePonding
            (
            FlowMapGenerator.ElevationTypes PondingElevationType,
            double CurveNumber,
            double RainfallMm,
            int Opacity = 50
            )
        {
            ClearPondingTileCache();
            if ((PondingImage != null) && File.Exists(PondingImage))
            {
                try { File.Delete(PondingImage); } catch { }
                PondingImage = null;
            }

            if (CurrentField == null) return;

            this.PondingElevationType = PondingElevationType;

            var pondGen = new PondingMapGenerator();
            string TempPNG = Path.GetTempFileName() + ".png";

            try
            {
                pondGen.GeneratePondingPNG(CurrentField, PondingElevationType, TempPNG,
                    out PondingSWCorner, out PondingNECorner,
                    RainfallMm, CurveNumber, Opacity, false);
                PondingImage = TempPNG;
            }
            catch
            {
                if (File.Exists(TempPNG)) { try { File.Delete(TempPNG); } catch { } }
                PondingImage = null;
            }
        }

        /// <summary>
        /// Clears all cached map tiles and associated overlay tile caches.
        /// </summary>
        public void ClearTileCache()
        {
            foreach (MapTile tile in Cache.Tiles.Values)
            {
                DisposeTileBitmaps(tile);
            }
            Cache.Tiles.Clear();

            ClearSurfaceFlowTileCache();
            ClearPondingTileCache();
        }

        /// <summary>
        /// Clears the surface flow tile cache and releases the cached flow image bitmap.
        /// Call when SurfaceFlowImage is null or when zoom/scale changes.
        /// </summary>
        private void ClearSurfaceFlowTileCache()
        {
            foreach (Bitmap bmp in _surfaceFlowTileCache.Values)
            {
                bmp?.Dispose();
            }
            _surfaceFlowTileCache.Clear();
            foreach (Bitmap bmp in _surfaceFlowRotatedTileCache.Values)
            {
                bmp?.Dispose();
            }
            _surfaceFlowRotatedTileCache.Clear();
            _surfaceFlowRotationBucket = int.MinValue;
            _surfaceFlowCacheScaleFactor = null;
            _surfaceFlowImageBitmap?.Dispose();
            _surfaceFlowImageBitmap = null;
            _surfaceFlowImagePath = null;
        }

        /// <summary>
        /// Clears the ponding tile cache and releases the cached ponding image bitmap.
        /// </summary>
        private void ClearPondingTileCache()
        {
            foreach (Bitmap bmp in _pondingTileCache.Values)
                bmp?.Dispose();
            _pondingTileCache.Clear();
            foreach (Bitmap bmp in _pondingRotatedTileCache.Values)
                bmp?.Dispose();
            _pondingRotatedTileCache.Clear();
            _pondingRotationBucket = int.MinValue;
            _pondingCacheScaleFactor = null;
            _pondingImageBitmap?.Dispose();
            _pondingImageBitmap = null;
            _pondingImagePath = null;
            _pondingProjectedMapBitmap?.Dispose();
            _pondingProjectedMapBitmap = null;
            _pondingProjectedScaleFactor = null;
            _pondingProjectedImagePath = null;
            _pondingProjectedMapWidthpx = 0;
            _pondingProjectedMapHeightpx = 0;
        }

        /// <summary>
        /// Renders the ponding overlay on the current map (same tile/cache pattern as surface flow).
        /// </summary>
        private void RenderPonding(Graphics g)
        {
            if (PondingImage == null)
            {
                ClearPondingTileCache();
                return;
            }
            if (CurrentField == null) return;

            if (_pondingCacheScaleFactor.HasValue && Math.Abs(CurrentScaleFactor - _pondingCacheScaleFactor.Value) > 1e-9)
            {
                foreach (Bitmap bmp in _pondingTileCache.Values)
                    bmp?.Dispose();
                _pondingTileCache.Clear();
                _pondingProjectedMapBitmap?.Dispose();
                _pondingProjectedMapBitmap = null;
                _pondingProjectedScaleFactor = null;
                _pondingProjectedMapWidthpx = 0;
                _pondingProjectedMapHeightpx = 0;
            }
            _pondingCacheScaleFactor = CurrentScaleFactor;

            if (_pondingImagePath != PondingImage)
            {
                _pondingImageBitmap?.Dispose();
                _pondingImageBitmap = null;
                _pondingProjectedMapBitmap?.Dispose();
                _pondingProjectedMapBitmap = null;
                _pondingProjectedScaleFactor = null;
                _pondingProjectedImagePath = null;
                _pondingProjectedMapWidthpx = 0;
                _pondingProjectedMapHeightpx = 0;
                try
                {
                    if (File.Exists(PondingImage))
                    {
                        _pondingImageBitmap = new Bitmap(PondingImage);
                        _pondingImagePath = PondingImage;
                    }
                    else
                    {
                        _pondingImagePath = null;
                        return;
                    }
                }
                catch
                {
                    _pondingImagePath = null;
                    return;
                }
            }

            if (_pondingImageBitmap == null) return;

            UTM.UTMCoordinate swUtm = UTM.FromLatLon(PondingSWCorner.Latitude, PondingSWCorner.Longitude);
            UTM.UTMCoordinate neUtm = UTM.FromLatLon(PondingNECorner.Latitude, PondingNECorner.Longitude);
            double swE = swUtm.Easting;
            double swN = swUtm.Northing;
            double neE = neUtm.Easting;
            double neN = neUtm.Northing;
            double spanE = neE - swE;
            double spanN = neN - swN;
            if (spanE <= 0 || spanN <= 0) return;

            int MapWidthpx = _unrotatedMapWidthpx;
            int MapHeightpx = _unrotatedMapHeightpx;
            int Wf = _pondingImageBitmap.Width;
            int Hf = _pondingImageBitmap.Height;
            double ScaleFactor = CurrentScaleFactor;
            double FieldMinX = CurrentField.FieldMinX;
            double FieldMaxY = CurrentField.FieldMaxY;
            int ImageWidthpx = Cache.ImageWidthpx;
            int ImageHeightpx = Cache.ImageHeightpx;

            bool projectedInvalid =
                _pondingProjectedMapBitmap == null ||
                !_pondingProjectedScaleFactor.HasValue ||
                Math.Abs(_pondingProjectedScaleFactor.Value - ScaleFactor) > 1e-9 ||
                _pondingProjectedImagePath != _pondingImagePath ||
                _pondingProjectedMapWidthpx != MapWidthpx ||
                _pondingProjectedMapHeightpx != MapHeightpx;

            if (projectedInvalid)
            {
                _pondingProjectedMapBitmap?.Dispose();
                _pondingProjectedMapBitmap = new Bitmap(MapWidthpx, MapHeightpx, PixelFormat.Format32bppArgb);
                using (Graphics pg = Graphics.FromImage(_pondingProjectedMapBitmap))
                {
                    pg.Clear(Color.Transparent);
                    pg.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceCopy;
                    pg.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                    pg.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;

                    float destX = (float)((swE - FieldMinX) * ScaleFactor);
                    float destY = (float)((FieldMaxY - neN) * ScaleFactor);
                    float destW = (float)(spanE * ScaleFactor);
                    float destH = (float)(spanN * ScaleFactor);
                    if (destW > 0 && destH > 0)
                    {
                        pg.DrawImage(
                            _pondingImageBitmap,
                            new RectangleF(destX, destY, destW, destH),
                            new RectangleF(0, 0, Wf, Hf),
                            GraphicsUnit.Pixel);
                    }
                }

                _pondingProjectedScaleFactor = ScaleFactor;
                _pondingProjectedImagePath = _pondingImagePath;
                _pondingProjectedMapWidthpx = MapWidthpx;
                _pondingProjectedMapHeightpx = MapHeightpx;
            }

            g.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceOver;
            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
            g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;

            int tilesX = (int)Math.Ceiling((double)MapWidthpx / TILE_SIZE);
            int tilesY = (int)Math.Ceiling((double)MapHeightpx / TILE_SIZE);

            int rotationBucket = (int)Math.Round(_tractorHeading / HEADING_BUCKET_DEGREES);
            double rotationHeading = rotationBucket * HEADING_BUCKET_DEGREES;
            if (_tractorHeading != 0 && rotationBucket != _pondingRotationBucket)
            {
                foreach (Bitmap bmp in _pondingRotatedTileCache.Values)
                    bmp?.Dispose();
                _pondingRotatedTileCache.Clear();
                _pondingRotationBucket = rotationBucket;
            }

            for (int tileY = 0; tileY < tilesY; tileY++)
            {
                for (int tileX = 0; tileX < tilesX; tileX++)
                {
                    int tileStartX = tileX * TILE_SIZE;
                    int tileStartY = tileY * TILE_SIZE;
                    int tileWidth = Math.Min(TILE_SIZE, MapWidthpx - tileStartX);
                    int tileHeight = Math.Min(TILE_SIZE, MapHeightpx - tileStartY);

                    if (!IsTileInView(tileStartX, tileStartY, tileWidth, tileHeight, ImageWidthpx, ImageHeightpx))
                        continue;

                    long tileKey = GetTileKey(tileX, tileY);
                    if (!_pondingTileCache.TryGetValue(tileKey, out Bitmap? pondTile))
                    {
                        pondTile = new Bitmap(tileWidth, tileHeight, PixelFormat.Format32bppArgb);
                        using (Graphics tg = Graphics.FromImage(pondTile))
                        {
                            tg.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceCopy;
                            tg.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
                            tg.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.None;
                            tg.DrawImage(_pondingProjectedMapBitmap!,
                                new Rectangle(0, 0, tileWidth, tileHeight),
                                new Rectangle(tileStartX, tileStartY, tileWidth, tileHeight),
                                GraphicsUnit.Pixel);
                        }
                        _pondingTileCache[tileKey] = pondTile;
                    }

                    Bitmap tileToDraw = pondTile;
                    if (_tractorHeading != 0)
                    {
                        var rotatedKey = (tileKey, rotationBucket);
                        if (!_pondingRotatedTileCache.TryGetValue(rotatedKey, out Bitmap? rotatedTile))
                        {
                            rotatedTile = Rotate(pondTile, rotationHeading);
                            _pondingRotatedTileCache[rotatedKey] = rotatedTile;
                        }
                        tileToDraw = rotatedTile;
                    }

                    int drawX, drawY;
                    if (_tractorHeading != 0)
                    {
                        int centerX = tileStartX + tileWidth / 2;
                        int centerY = tileStartY + tileHeight / 2;
                        Point rotatedCenter = UnrotatedMapPixelToFinalImagePixel(centerX, centerY);
                        drawX = rotatedCenter.X - tileToDraw.Width / 2;
                        drawY = rotatedCenter.Y - tileToDraw.Height / 2;
                    }
                    else
                    {
                        drawX = tileStartX + _mapLeftpx;
                        drawY = tileStartY + _mapToppx;
                    }

                    int srcRectX = 0, srcRectY = 0, srcRectW = tileToDraw.Width, srcRectH = tileToDraw.Height;
                    if (drawX < 0) { srcRectX = -drawX; srcRectW -= srcRectX; drawX = 0; }
                    if (drawY < 0) { srcRectY = -drawY; srcRectH -= srcRectY; drawY = 0; }
                    int drawW = Math.Min(srcRectW, ImageWidthpx - drawX);
                    int drawH = Math.Min(srcRectH, ImageHeightpx - drawY);
                    srcRectW = drawW;
                    srcRectH = drawH;
                    if (drawW > 0 && drawH > 0)
                    {
                        g.DrawImage(tileToDraw,
                            new Rectangle(drawX, drawY, drawW, drawH),
                            new Rectangle(srcRectX, srcRectY, srcRectW, srcRectH),
                            GraphicsUnit.Pixel);
                    }
                }
            }
        }

        /// <summary>
        /// Renders the surface water flow on the current map using the current tractor location and heading.
        /// Splits SurfaceFlowImage into tiles aligned with the field tile grid; uses cache unless zoom or image changes.
        /// </summary>
        private void RenderSurfaceFlow(Graphics g)
        {
            if (SurfaceFlowImage == null)
            {
                ClearSurfaceFlowTileCache();
                return;
            }

            if (CurrentField == null) return;

            if (_surfaceFlowCacheScaleFactor.HasValue && Math.Abs(CurrentScaleFactor - _surfaceFlowCacheScaleFactor.Value) > 1e-9)
            {
                foreach (Bitmap bmp in _surfaceFlowTileCache.Values)
                    bmp?.Dispose();
                _surfaceFlowTileCache.Clear();
            }
            _surfaceFlowCacheScaleFactor = CurrentScaleFactor;

            if (_surfaceFlowImagePath != SurfaceFlowImage)
            {
                _surfaceFlowImageBitmap?.Dispose();
                _surfaceFlowImageBitmap = null;
                try
                {
                    if (File.Exists(SurfaceFlowImage))
                    {
                        _surfaceFlowImageBitmap = new Bitmap(SurfaceFlowImage);
                        _surfaceFlowImagePath = SurfaceFlowImage;
                    }
                    else
                    {
                        _surfaceFlowImagePath = null;
                        return;
                    }
                }
                catch
                {
                    _surfaceFlowImagePath = null;
                    return;
                }
            }

            if (_surfaceFlowImageBitmap == null) return;

            UTM.UTMCoordinate swUtm = UTM.FromLatLon(SurfaceFlowSWCorner.Latitude, SurfaceFlowSWCorner.Longitude);
            UTM.UTMCoordinate neUtm = UTM.FromLatLon(SurfaceFlowNECorner.Latitude, SurfaceFlowNECorner.Longitude);
            double swE = swUtm.Easting;
            double swN = swUtm.Northing;
            double neE = neUtm.Easting;
            double neN = neUtm.Northing;
            double flowEastingSpan = neE - swE;
            double flowNorthingSpan = neN - swN;
            if (flowEastingSpan <= 0 || flowNorthingSpan <= 0) return;

            int MapWidthpx = _unrotatedMapWidthpx;
            int MapHeightpx = _unrotatedMapHeightpx;
            int Wf = _surfaceFlowImageBitmap.Width;
            int Hf = _surfaceFlowImageBitmap.Height;
            double ScaleFactor = CurrentScaleFactor;
            double FieldMinX = CurrentField.FieldMinX;
            double FieldMaxY = CurrentField.FieldMaxY;
            int ImageWidthpx = Cache.ImageWidthpx;
            int ImageHeightpx = Cache.ImageHeightpx;

            g.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceOver;
            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
            g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;

            int tilesX = (int)Math.Ceiling((double)MapWidthpx / TILE_SIZE);
            int tilesY = (int)Math.Ceiling((double)MapHeightpx / TILE_SIZE);

            int rotationBucket = (int)Math.Round(_tractorHeading / HEADING_BUCKET_DEGREES);
            double rotationHeading = rotationBucket * HEADING_BUCKET_DEGREES;
            if (_tractorHeading != 0 && rotationBucket != _surfaceFlowRotationBucket)
            {
                foreach (Bitmap bmp in _surfaceFlowRotatedTileCache.Values)
                    bmp?.Dispose();
                _surfaceFlowRotatedTileCache.Clear();
                _surfaceFlowRotationBucket = rotationBucket;
            }

            for (int tileY = 0; tileY < tilesY; tileY++)
            {
                for (int tileX = 0; tileX < tilesX; tileX++)
                {
                    int tileStartX = tileX * TILE_SIZE;
                    int tileStartY = tileY * TILE_SIZE;
                    int tileWidth = Math.Min(TILE_SIZE, MapWidthpx - tileStartX);
                    int tileHeight = Math.Min(TILE_SIZE, MapHeightpx - tileStartY);

                    if (!IsTileInView(tileStartX, tileStartY, tileWidth, tileHeight, ImageWidthpx, ImageHeightpx))
                        continue;

                    long tileKey = GetTileKey(tileX, tileY);
                    if (!_surfaceFlowTileCache.TryGetValue(tileKey, out Bitmap? flowTile))
                    {
                        double leftUtm = FieldMinX + tileStartX / ScaleFactor;
                        double rightUtm = FieldMinX + (tileStartX + tileWidth) / ScaleFactor;
                        double topUtm = FieldMaxY - tileStartY / ScaleFactor;
                        double bottomUtm = FieldMaxY - (tileStartY + tileHeight) / ScaleFactor;
                        float srcX = (float)((leftUtm - swE) / flowEastingSpan * Wf);
                        float srcY = (float)((neN - topUtm) / flowNorthingSpan * Hf);
                        float srcW = (float)((rightUtm - leftUtm) / flowEastingSpan * Wf);
                        float srcH = (float)((topUtm - bottomUtm) / flowNorthingSpan * Hf);
                        float srcX0 = Math.Max(0, srcX - OVERLAY_TILE_SOURCE_OVERLAP);
                        float srcY0 = Math.Max(0, srcY - OVERLAY_TILE_SOURCE_OVERLAP);
                        float srcW0 = Math.Min(Wf - srcX0, srcW + 2 * OVERLAY_TILE_SOURCE_OVERLAP);
                        float srcH0 = Math.Min(Hf - srcY0, srcH + 2 * OVERLAY_TILE_SOURCE_OVERLAP);
                        srcX0 = Math.Min(srcX0, Wf - 1);
                        srcY0 = Math.Min(srcY0, Hf - 1);
                        srcW0 = Math.Max(1, srcW0);
                        srcH0 = Math.Max(1, srcH0);

                        flowTile = new Bitmap(tileWidth, tileHeight, PixelFormat.Format32bppArgb);
                        using (Graphics tg = Graphics.FromImage(flowTile))
                        {
                            tg.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                            tg.DrawImage(_surfaceFlowImageBitmap,
                                new Rectangle(0, 0, tileWidth, tileHeight),
                                new RectangleF(srcX0, srcY0, srcW0, srcH0),
                                GraphicsUnit.Pixel);
                        }
                        _surfaceFlowTileCache[tileKey] = flowTile;
                    }

                    Bitmap tileToDraw = flowTile;
                    if (_tractorHeading != 0)
                    {
                        var rotatedKey = (tileKey, rotationBucket);
                        if (!_surfaceFlowRotatedTileCache.TryGetValue(rotatedKey, out Bitmap? rotatedTile))
                        {
                            rotatedTile = Rotate(flowTile, rotationHeading);
                            _surfaceFlowRotatedTileCache[rotatedKey] = rotatedTile;
                        }
                        tileToDraw = rotatedTile;
                    }

                    int drawX, drawY;
                    if (_tractorHeading != 0)
                    {
                        int centerX = tileStartX + tileWidth / 2;
                        int centerY = tileStartY + tileHeight / 2;
                        Point rotatedCenter = UnrotatedMapPixelToFinalImagePixel(centerX, centerY);
                        drawX = rotatedCenter.X - tileToDraw.Width / 2;
                        drawY = rotatedCenter.Y - tileToDraw.Height / 2;
                    }
                    else
                    {
                        drawX = tileStartX + _mapLeftpx;
                        drawY = tileStartY + _mapToppx;
                    }

                    int srcRectX = 0, srcRectY = 0, srcRectW = tileToDraw.Width, srcRectH = tileToDraw.Height;
                    if (drawX < 0) { srcRectX = -drawX; srcRectW -= srcRectX; drawX = 0; }
                    if (drawY < 0) { srcRectY = -drawY; srcRectH -= srcRectY; drawY = 0; }
                    int drawW = Math.Min(srcRectW, ImageWidthpx - drawX);
                    int drawH = Math.Min(srcRectH, ImageHeightpx - drawY);
                    srcRectW = drawW;
                    srcRectH = drawH;
                    if (drawW > 0 && drawH > 0)
                    {
                        g.DrawImage(tileToDraw,
                            new Rectangle(drawX, drawY, drawW, drawH),
                            new Rectangle(srcRectX, srcRectY, srcRectW, srcRectH),
                            GraphicsUnit.Pixel);
                    }
                }
            }
        }

        /// <summary>
        /// Returns three vertices for an arrow outline triangle
        /// Tip points in directionDegrees (0 = North/up). Scale is distance from center to tip in pixels.
        /// </summary>
        /// <param name="centerX">Center X in pixel coordinates.</param>
        /// <param name="centerY">Center Y in pixel coordinates.</param>
        /// <param name="directionDegrees">Compass direction in degrees (0 = North).</param>
        /// <param name="scale">Distance from center to tip in pixels.</param>
        /// <param name="baseFraction">How far back the base is from center (relative to scale).</param>
        /// <param name="baseWidthFraction">Half-width of base relative to scale.</param>
        private Point[] ArrowOutlineTriangle
            (
            double centerX,
            double centerY,
            double directionDegrees,
            double scale,
            double baseFraction = 0.6,
            double baseWidthFraction = 0.5
            )
        {
            (double dx, double dy) = DirectionDegreesToVector(directionDegrees);

            double tipX = centerX + scale * dx;
            double tipY = centerY + scale * dy;
            double backX = centerX - scale * baseFraction * dx;
            double backY = centerY - scale * baseFraction * dy;
            double half = scale * baseWidthFraction;
            double leftX = backX - half * dy;
            double leftY = backY + half * dx;
            double rightX = backX + half * dy;
            double rightY = backY - half * dx;

            return new Point[]
            {
                new Point((int)tipX, (int)tipY),
                new Point((int)leftX, (int)leftY),
                new Point((int)rightX, (int)rightY)
            };
        }

        /// <summary>
        /// Converts latitude and longitude to a pixel in the world
        /// This method is independent of Field data and uses only:
        /// - Tractor location (in pixels and lat/lon)
        /// - Tractor heading (rotation of the map)
        /// - Scale factor (pixels per meter)
        /// </summary>
        /// <param name="Location">Location to convert</param>
        /// <returns>Pixel location</returns>
        private Point LatLonToWorld
            (
            Coordinate Location
            )
        {
            PointF world = LatLonToWorldF(Location);
            return new Point((int)Math.Round(world.X), (int)Math.Round(world.Y));
        }

        /// <summary>
        /// Converts latitude and longitude to a pixel in the world with sub-pixel precision.
        /// </summary>
        /// <param name="Location">Location to convert</param>
        /// <returns>Pixel location with floating-point precision</returns>
        private PointF LatLonToWorldF
            (
            Coordinate Location
            )
        {
            // Convert input location to UTM coordinates
            UTM.UTMCoordinate Pos = UTM.FromLatLon(Location.Latitude, Location.Longitude);

            // Convert tractor location to UTM coordinates
            UTM.UTMCoordinate TractorUTM = UTM.FromLatLon(TractorFix.Latitude, TractorFix.Longitude);

            // Calculate the difference in meters (in UTM coordinate system)
            double deltaXM = Pos.Easting - TractorUTM.Easting;
            double deltaYM = Pos.Northing - TractorUTM.Northing;

            // Convert meters to pixels using the scale factor
            // Note: In the map coordinate system, X increases east (same as UTM Easting)
            // but Y increases south (opposite of UTM Northing, which increases north)
            double deltaXPx = deltaXM * CurrentScaleFactor;
            double deltaYPx = -deltaYM * CurrentScaleFactor; // Negative because UTM northing increases north, but map Y increases south

            // Apply rotation around the tractor position
            // The map is rotated so that the tractor heading is up (north)
            // We need to rotate the delta vector by -_tractorHeading degrees (counter-clockwise)
            // to align with the rotated map coordinate system (same as FieldMToPixel)
            double rotatedX;
            double rotatedY;
            if (_tractorHeading == 0.0)
            {
                rotatedX = deltaXPx;
                rotatedY = deltaYPx;
            }
            else
            {
                double radians = -_tractorHeading * Math.PI / 180.0; // Negative because we rotate counter-clockwise (same as FieldMToPixel)
                double cosAngle = Math.Cos(radians);
                double sinAngle = Math.Sin(radians);
                
                // Rotate the delta vector
                rotatedX = deltaXPx * cosAngle - deltaYPx * sinAngle;
                rotatedY = deltaXPx * sinAngle + deltaYPx * cosAngle;
            }

            // Add the tractor pixel position to get the final pixel location
            return new PointF(
                (float)(_tractorXpx + rotatedX),
                (float)(_tractorYpx + rotatedY));
        }

        /// <summary>
        /// Checks if a tile is inside or partially inside the image after rotation and translation
        /// </summary>
        /// <param name="tileStartX">X coordinate of tile start in unrotated map pixels</param>
        /// <param name="tileStartY">Y coordinate of tile start in unrotated map pixels</param>
        /// <param name="tileWidth">Width of tile in pixels</param>
        /// <param name="tileHeight">Height of tile in pixels</param>
        /// <param name="ImageWidthpx">Width of image</param>
        /// <param name="ImageHeightpx">Height of image</param>
        /// <returns>true if any part of the tile is visible</returns>
        private bool IsTileInView
            (
            int tileStartX,
            int tileStartY,
            int tileWidth,
            int tileHeight,
            int ImageWidthpx,
            int ImageHeightpx
            )
        {
            // Transform all four corners of the tile to final image coordinates
            Point topLeft = UnrotatedMapPixelToFinalImagePixel(tileStartX, tileStartY);
            Point topRight = UnrotatedMapPixelToFinalImagePixel(tileStartX + tileWidth, tileStartY);
            Point bottomLeft = UnrotatedMapPixelToFinalImagePixel(tileStartX, tileStartY + tileHeight);
            Point bottomRight = UnrotatedMapPixelToFinalImagePixel(tileStartX + tileWidth, tileStartY + tileHeight);

            // Define the image bounds as a rectangle
            Rectangle imageBounds = new Rectangle(0, 0, ImageWidthpx, ImageHeightpx);

            // Check if any corner of the tile is inside the image bounds
            if (imageBounds.Contains(topLeft) || imageBounds.Contains(topRight) ||
                imageBounds.Contains(bottomLeft) || imageBounds.Contains(bottomRight))
            {
                return true;
            }

            // Check if any corner of the image is inside the rotated tile (using point-in-polygon)
            Point[] imageCorners = new Point[]
            {
                new Point(0, 0),
                new Point(ImageWidthpx, 0),
                new Point(ImageWidthpx, ImageHeightpx),
                new Point(0, ImageHeightpx)
            };

            Point[] tileCorners = new Point[] { topLeft, topRight, bottomRight, bottomLeft };
            foreach (Point corner in imageCorners)
            {
                if (IsPointInPolygon(corner, tileCorners))
                {
                    return true;
                }
            }

            // Check if any edge of the tile intersects any edge of the image
            // Tile edges
            Point[][] tileEdges = new Point[][]
            {
                new Point[] { topLeft, topRight },
                new Point[] { topRight, bottomRight },
                new Point[] { bottomRight, bottomLeft },
                new Point[] { bottomLeft, topLeft }
            };

            // Image edges
            Point[][] imageEdges = new Point[][]
            {
                new Point[] { new Point(0, 0), new Point(ImageWidthpx, 0) },
                new Point[] { new Point(ImageWidthpx, 0), new Point(ImageWidthpx, ImageHeightpx) },
                new Point[] { new Point(ImageWidthpx, ImageHeightpx), new Point(0, ImageHeightpx) },
                new Point[] { new Point(0, ImageHeightpx), new Point(0, 0) }
            };

            // Check for edge intersections
            foreach (Point[] tileEdge in tileEdges)
            {
                foreach (Point[] imageEdge in imageEdges)
                {
                    if (DoLineSegmentsIntersect(tileEdge[0], tileEdge[1], imageEdge[0], imageEdge[1]))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Checks if a point is inside a polygon using the ray casting algorithm
        /// </summary>
        private bool IsPointInPolygon(Point point, Point[] polygon)
        {
            int intersections = 0;
            int j = polygon.Length - 1;

            for (int i = 0; i < polygon.Length; i++)
            {
                if (((polygon[i].Y > point.Y) != (polygon[j].Y > point.Y)) &&
                    (point.X < (polygon[j].X - polygon[i].X) * (point.Y - polygon[i].Y) / (double)(polygon[j].Y - polygon[i].Y) + polygon[i].X))
                {
                    intersections++;
                }
                j = i;
            }

            return (intersections % 2) == 1;
        }

        /// <summary>
        /// Checks if two line segments intersect using the orientation method
        /// </summary>
        private bool DoLineSegmentsIntersect(Point p1, Point p2, Point p3, Point p4)
        {
            int o1 = Orientation(p1, p2, p3);
            int o2 = Orientation(p1, p2, p4);
            int o3 = Orientation(p3, p4, p1);
            int o4 = Orientation(p3, p4, p2);

            // General case: segments intersect if orientations are different
            if (o1 != o2 && o3 != o4)
                return true;

            // Special cases: collinear points
            if (o1 == 0 && OnSegment(p1, p3, p2)) return true;
            if (o2 == 0 && OnSegment(p1, p4, p2)) return true;
            if (o3 == 0 && OnSegment(p3, p1, p4)) return true;
            if (o4 == 0 && OnSegment(p3, p2, p4)) return true;

            return false;
        }

        /// <summary>
        /// Calculates the orientation of three points (0 = collinear, 1 = clockwise, 2 = counterclockwise)
        /// </summary>
        private int Orientation(Point a, Point b, Point c)
        {
            int val = (b.Y - a.Y) * (c.X - b.X) - (b.X - a.X) * (c.Y - b.Y);
            if (val == 0) return 0; // Collinear
            return (val > 0) ? 1 : 2; // Clockwise or counterclockwise
        }

        /// <summary>
        /// Checks if point q lies on segment pr
        /// </summary>
        private bool OnSegment(Point p, Point q, Point r)
        {
            if (q.X <= Math.Max(p.X, r.X) && q.X >= Math.Min(p.X, r.X) &&
                q.Y <= Math.Max(p.Y, r.Y) && q.Y >= Math.Min(p.Y, r.Y))
                return true;
            return false;
        }

        /// <summary>
        /// Gets the size of an image after rotation
        /// </summary>
        /// <param name="Width">Width of image</param>
        /// <param name="Height">Height of image</param>
        /// <param name="Angle">Angle of rotation in degrees</param>
        /// <returns>New image size to completely hold image</returns>
        private static Point GetRotatedSize
            (
            int Width,
            int Height,
            double Angle
            )
        {
            double radians = Angle * Math.PI / 180;
            int newWidth = (int)Math.Ceiling(Math.Abs(Width * Math.Cos(radians)) + Math.Abs(Height * Math.Sin(radians)));
            int newHeight = (int)Math.Ceiling(Math.Abs(Width * Math.Sin(radians)) + Math.Abs(Height * Math.Cos(radians)));

            return new Point(newWidth, newHeight);
        }

        /// <summary>
        /// Gets the size of an image after rotation
        /// </summary>
        /// <param name="Width">Width of image</param>
        /// <param name="Height">Height of image</param>
        /// <param name="Angle">Angle of rotation in degrees</param>
        /// <returns>New image size to completely hold image</returns>
        private static PointD GetRotatedSizeD
            (
            double Width,
            double Height,
            double Angle
            )
        {
            double radians = Angle * Math.PI / 180;
            double newWidth = (int)Math.Ceiling(Math.Abs(Width * Math.Cos(radians)) + Math.Abs(Height * Math.Sin(radians)));
            double newHeight = (int)Math.Ceiling(Math.Abs(Width * Math.Sin(radians)) + Math.Abs(Height * Math.Cos(radians)));

            return new PointD(newWidth, newHeight);
        }

        /// <summary>
        /// Rotates the map so heading is up
        /// </summary>
        /// <param name="Map">Bitmap to rotate</param>
        /// <param name="Heading">Heading</param>
        /// <returns>Rotated bitmap</returns>
        private Bitmap Rotate
            (
            Bitmap Map,
            double Heading
            )
        {
            Point RotatedSize = GetRotatedSize(Map.Width, Map.Height, Heading);

            Bitmap rotatedBitmap = new Bitmap(RotatedSize.X, RotatedSize.Y, PixelFormat.Format32bppArgb);

            using (Graphics g = Graphics.FromImage(rotatedBitmap))
            {
                // Clear to transparent
                g.Clear(Color.Transparent);
                
                // Configure for high-quality rotation with transparency
                g.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceOver;
                g.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;

                g.TranslateTransform((float)RotatedSize.X / 2, (float)RotatedSize.Y / 2); // Move origin to center of new bitmap
                g.RotateTransform((float)-Heading); // Apply rotation
                g.TranslateTransform(-(float)Map.Width / 2, -(float)Map.Height / 2); // Move origin back to original bitmap's center
                g.DrawImage(Map, new Point(0, 0)); // Draw the original bitmap
            }
            
            return rotatedBitmap;
        }

        /// <summary>
        /// Rotates the bitmap so the heading is up
        /// Optimized version using lockbits for direct memory access
        /// </summary>
        /// <param name="Map">Bitmap to rotate</param>
        /// <param name="Heading">Heading</param>
        /// <returns>Rotated bitmap</returns>
        private Bitmap FastRotate
            (
            Bitmap Map,
            double Heading
            )
        {
            if (Heading == 0.0)
            {
                // No rotation needed, return a copy
                return new Bitmap(Map);
            }

            Point RotatedSize = GetRotatedSize(Map.Width, Map.Height, Heading);
            Bitmap rotatedBitmap = new Bitmap(RotatedSize.X, RotatedSize.Y, PixelFormat.Format32bppArgb);

            // Pre-calculate rotation values
            double radians = -Heading * Math.PI / 180.0; // Negative for counter-clockwise rotation
            double cosAngle = Math.Cos(radians);
            double sinAngle = Math.Sin(radians);

            // Center points
            double srcCenterX = Map.Width / 2.0;
            double srcCenterY = Map.Height / 2.0;
            double dstCenterX = RotatedSize.X / 2.0;
            double dstCenterY = RotatedSize.Y / 2.0;

            // Lock source bitmap
            BitmapData srcData = Map.LockBits(
                new Rectangle(0, 0, Map.Width, Map.Height),
                ImageLockMode.ReadOnly,
                PixelFormat.Format32bppArgb);

            // Lock destination bitmap
            BitmapData dstData = rotatedBitmap.LockBits(
                new Rectangle(0, 0, RotatedSize.X, RotatedSize.Y),
                ImageLockMode.WriteOnly,
                PixelFormat.Format32bppArgb);

            try
            {
                int srcStride = srcData.Stride;
                int dstStride = dstData.Stride;
                int bytesPerPixel = 4; // BGRA

                // Get pointers to pixel data
                IntPtr srcPtr = srcData.Scan0;
                IntPtr dstPtr = dstData.Scan0;

                // Read entire source bitmap into memory for fast access
                int srcBytesTotal = srcStride * Map.Height;
                byte[] srcBuffer = new byte[srcBytesTotal];
                Marshal.Copy(srcPtr, srcBuffer, 0, srcBytesTotal);

                // Pre-allocate destination row buffer
                byte[] dstRow = new byte[RotatedSize.X * bytesPerPixel];

                // Process each destination pixel
                for (int dstY = 0; dstY < RotatedSize.Y; dstY++)
                {
                    IntPtr dstRowPtr = new IntPtr(dstPtr.ToInt64() + dstY * dstStride);

                    for (int dstX = 0; dstX < RotatedSize.X; dstX++)
                    {
                        // Calculate source coordinates using inverse rotation
                        // Translate destination point relative to destination center
                        double relX = dstX - dstCenterX;
                        double relY = dstY - dstCenterY;

                        // Apply inverse rotation (opposite direction)
                        double srcRelX = relX * cosAngle + relY * sinAngle;
                        double srcRelY = -relX * sinAngle + relY * cosAngle;

                        // Translate relative to source center
                        double srcX = srcRelX + srcCenterX;
                        double srcY = srcRelY + srcCenterY;

                        // Bilinear interpolation for better quality
                        int x0 = (int)Math.Floor(srcX);
                        int y0 = (int)Math.Floor(srcY);
                        int x1 = x0 + 1;
                        int y1 = y0 + 1;

                        double fx = srcX - x0;
                        double fy = srcY - y0;

                        // Bounds checking
                        bool valid00 = (x0 >= 0 && x0 < Map.Width && y0 >= 0 && y0 < Map.Height);
                        bool valid01 = (x0 >= 0 && x0 < Map.Width && y1 >= 0 && y1 < Map.Height);
                        bool valid10 = (x1 >= 0 && x1 < Map.Width && y0 >= 0 && y0 < Map.Height);
                        bool valid11 = (x1 >= 0 && x1 < Map.Width && y1 >= 0 && y1 < Map.Height);

                        byte b = 0, g = 0, r = 0, a = 0;

                        if (valid00 || valid01 || valid10 || valid11)
                        {
                            // Bilinear interpolation weights
                            double w00 = (1.0 - fx) * (1.0 - fy);
                            double w01 = (1.0 - fx) * fy;
                            double w10 = fx * (1.0 - fy);
                            double w11 = fx * fy;

                            double sumB = 0, sumG = 0, sumR = 0, sumA = 0, sumWeight = 0;

                            // Access pixels directly from source buffer
                            if (valid00)
                            {
                                int offset = y0 * srcStride + x0 * bytesPerPixel;
                                double weight = w00;
                                sumB += srcBuffer[offset + 0] * weight;
                                sumG += srcBuffer[offset + 1] * weight;
                                sumR += srcBuffer[offset + 2] * weight;
                                sumA += srcBuffer[offset + 3] * weight;
                                sumWeight += weight;
                            }

                            if (valid01)
                            {
                                int offset = y1 * srcStride + x0 * bytesPerPixel;
                                double weight = w01;
                                sumB += srcBuffer[offset + 0] * weight;
                                sumG += srcBuffer[offset + 1] * weight;
                                sumR += srcBuffer[offset + 2] * weight;
                                sumA += srcBuffer[offset + 3] * weight;
                                sumWeight += weight;
                            }

                            if (valid10)
                            {
                                int offset = y0 * srcStride + x1 * bytesPerPixel;
                                double weight = w10;
                                sumB += srcBuffer[offset + 0] * weight;
                                sumG += srcBuffer[offset + 1] * weight;
                                sumR += srcBuffer[offset + 2] * weight;
                                sumA += srcBuffer[offset + 3] * weight;
                                sumWeight += weight;
                            }

                            if (valid11)
                            {
                                int offset = y1 * srcStride + x1 * bytesPerPixel;
                                double weight = w11;
                                sumB += srcBuffer[offset + 0] * weight;
                                sumG += srcBuffer[offset + 1] * weight;
                                sumR += srcBuffer[offset + 2] * weight;
                                sumA += srcBuffer[offset + 3] * weight;
                                sumWeight += weight;
                            }

                            if (sumWeight > 0)
                            {
                                b = (byte)Math.Round(sumB / sumWeight);
                                g = (byte)Math.Round(sumG / sumWeight);
                                r = (byte)Math.Round(sumR / sumWeight);
                                a = (byte)Math.Round(sumA / sumWeight);
                            }
                        }

                        // Write BGRA pixel to destination buffer
                        int pixelOffset = dstX * bytesPerPixel;
                        dstRow[pixelOffset + 0] = b; // Blue
                        dstRow[pixelOffset + 1] = g; // Green
                        dstRow[pixelOffset + 2] = r; // Red
                        dstRow[pixelOffset + 3] = a; // Alpha
                    }

                    // Copy entire destination row to bitmap at once
                    Marshal.Copy(dstRow, 0, dstRowPtr, RotatedSize.X * bytesPerPixel);
                }
            }
            finally
            {
                Map.UnlockBits(srcData);
                rotatedBitmap.UnlockBits(dstData);
            }

            return rotatedBitmap;
        }

        /// <summary>
        /// Gets the top left location of the map inside the image
        /// </summary>
        /// <param name="TractorLat">Current tractor latitude</param>
        /// <param name="TractorLon">Current tractor longitude</param>
        /// <param name="TractorXpx">Current tractor X location in image in pixels</param>
        /// <param name="TractorYpx">Current tractor Y location in image in pixels</param>
        /// <param name="TractorHeading">The heading of the tractor in degrees</param>
        /// <returns>Top-left corner of the map in image pixel coordinates</returns>
        private Point GetMapOffset
            (
            double TractorLat,
            double TractorLon,
            int TractorXpx,
            int TractorYpx,
            double TractorHeading
            )
        {
            // Convert tractor's lat/lon to UTM coordinates (field coordinates in meters)
            UTM.UTMCoordinate TractorUTM = UTM.FromLatLon(TractorLat, TractorLon);
            double TractorFieldX = TractorUTM.Easting;
            double TractorFieldY = TractorUTM.Northing;

            // Convert tractor's field coordinates to pixel coordinates relative to the map (0,0 is top-left of map)
            // Account for rotation if the map has been rotated
            Point TractorFieldPx = FieldMToPixel(TractorFieldX, TractorFieldY, TractorHeading);

            // Calculate the offset so that the tractor's field position aligns with the fixed pixel position
            // MapTopLeft = TractorImagePosition - TractorFieldPosition
            int MapLeftpx = TractorXpx - TractorFieldPx.X;
            int MapToppx = TractorYpx - TractorFieldPx.Y;

            return new Point(MapLeftpx, MapToppx);
        }

        /// <summary>
        /// Converts a pixel coordinate from the unrotated map to the corresponding pixel coordinate
        /// in the final rotated and translated image.
        /// </summary>
        /// <param name="unrotatedMapX">X coordinate in the unrotated map (0 to MapWidthpx-1)</param>
        /// <param name="unrotatedMapY">Y coordinate in the unrotated map (0 to MapHeightpx-1)</param>
        /// <returns>Pixel coordinate in the final image</returns>
        private Point UnrotatedMapPixelToFinalImagePixel(int unrotatedMapX, int unrotatedMapY)
        {
            int rotatedX, rotatedY;

            if (_tractorHeading == 0.0)
            {
                // No rotation, coordinates stay the same
                rotatedX = unrotatedMapX;
                rotatedY = unrotatedMapY;
            }
            else
            {
                // Apply precomputed rotation transform around map center.
                double relX = unrotatedMapX - _unrotatedCenterX;
                double relY = unrotatedMapY - _unrotatedCenterY;

                // Apply rotation (counter-clockwise by -Heading, which is clockwise by Heading)
                // Graphics.RotateTransform(-Heading) rotates counter-clockwise by -Heading
                double rotatedRelX = relX * _rotationCosNeg - relY * _rotationSinNeg;
                double rotatedRelY = relX * _rotationSinNeg + relY * _rotationCosNeg;

                // Translate back relative to rotated map center
                rotatedX = (int)Math.Round(rotatedRelX + _rotatedCenterX);
                rotatedY = (int)Math.Round(rotatedRelY + _rotatedCenterY);
            }

            // Apply translation to get final image coordinates
            int finalX = rotatedX + _mapLeftpx;
            int finalY = rotatedY + _mapToppx;

            return new Point(finalX, finalY);
        }

        /// <summary>
        /// Convert pixel coordinates to field coordinate in meters
        /// </summary>
        /// <param name="PixelX">Pixel X coordinate</param>
        /// <param name="PixelY">Pixel Y coordinate</param>
        /// <param name="Heading">Tractor heading in degrees</param>
        /// <returns>Field coordinates in meters</returns>
        private PointD PixelToFieldM
            (
            int PixelX,
            int PixelY,
            double Heading = 0.0
            )
        {
            int unrotatedPixelX, unrotatedPixelY;

            // If no rotation, use the pixel coordinates directly
            if (Heading == 0.0)
            {
                unrotatedPixelX = PixelX;
                unrotatedPixelY = PixelY;
            }
            else
            {
                // Calculate unrotated map dimensions
                double MapWidthM = CurrentField.FieldMaxX - CurrentField.FieldMinX;
                double MapHeightM = CurrentField.FieldMaxY - CurrentField.FieldMinY;
                int MapWidthpx = (int)Math.Round(MapWidthM * CurrentScaleFactor);
                int MapHeightpx = (int)Math.Round(MapHeightM * CurrentScaleFactor);

                // Calculate rotated map dimensions (same as in Rotate function and FieldMToPixel)
                double radians = Heading * Math.PI / 180.0;
                int rotatedWidth = (int)Math.Ceiling(Math.Abs(MapWidthpx * Math.Cos(radians)) + Math.Abs(MapHeightpx * Math.Sin(radians)));
                int rotatedHeight = (int)Math.Ceiling(Math.Abs(MapWidthpx * Math.Sin(radians)) + Math.Abs(MapHeightpx * Math.Cos(radians)));

                // Rotation center in rotated map
                double rotatedCenterX = rotatedWidth / 2.0;
                double rotatedCenterY = rotatedHeight / 2.0;

                // Translate point to be relative to rotated map center
                double relX = PixelX - rotatedCenterX;
                double relY = PixelY - rotatedCenterY;

                // Apply inverse rotation (rotate back by positive heading, opposite of FieldMToPixel)
                double cosAngle = Math.Cos(radians);  // Positive angle (inverse of -radians)
                double sinAngle = Math.Sin(radians);
                double unrotatedX = relX * cosAngle - relY * sinAngle;
                double unrotatedY = relX * sinAngle + relY * cosAngle;

                // Translate back relative to unrotated map center
                double centerX = MapWidthpx / 2.0;
                double centerY = MapHeightpx / 2.0;
                unrotatedPixelX = (int)Math.Round(unrotatedX + centerX);
                unrotatedPixelY = (int)Math.Round(unrotatedY + centerY);
            }

            // Convert unrotated pixel coordinates to field coordinates
            double FieldX = CurrentField.FieldMinX + (unrotatedPixelX / CurrentScaleFactor);
            // Y: PixelY=0 maps to FieldMaxY, PixelY=MapHeightpx maps to FieldMinY (inverted for image coordinates)
            double FieldY = CurrentField.FieldMaxY - (unrotatedPixelY / CurrentScaleFactor);

            return new PointD(FieldX, FieldY);
        }

        /// <summary>
        /// Converts pixel coordinates to bin grid indices
        /// </summary>
        /// <param name="PixelX">Pixel X coordinate</param>
        /// <param name="PixelY">Pixel Y coordinate</param>
        /// <param name="Heading">Tractor heading in degrees</param>
        /// <returns>Bin grid X and Y</returns>
        private Point PixelToBin
            (
            int PixelX,
            int PixelY,
            double Heading = 0.0
            )
        {
            PointD FieldPoint = PixelToFieldM(PixelX, PixelY, Heading);
            return CurrentField.FieldMToBin(FieldPoint.X, FieldPoint.Y);
        }

        /// <summary>
        /// Convert field coordinate in meters to pixel coordinates
        /// </summary>
        /// <param name="FieldX">Field X coordinate in meters</param>
        /// <param name="FieldY">Field Y coordinate in meters</param>
        /// <returns>Pixel X and Y coordinates</returns>
        private Point FieldMToPixel
            (
            double FieldX,
            double FieldY,
            double Heading = 0.0
            )
        {
            // Convert to pixel coordinates in the unrotated map
            // X: FieldMinX maps to 0, FieldMaxX maps to MapWidthpx (left to right)
            int PixelX = (int)Math.Round((FieldX - CurrentField.FieldMinX) * CurrentScaleFactor);
            // Y: FieldMaxY maps to 0, FieldMinY maps to MapHeightpx (north to south, inverted for image coordinates)
            int PixelY = (int)Math.Round((CurrentField.FieldMaxY - FieldY) * CurrentScaleFactor);

            // If no rotation, return the unrotated coordinates
            if (Heading == 0.0)
            {
                return new Point(PixelX, PixelY);
            }

            // Calculate unrotated map dimensions
            double MapWidthM = CurrentField.FieldMaxX - CurrentField.FieldMinX;
            double MapHeightM = CurrentField.FieldMaxY - CurrentField.FieldMinY;
            int MapWidthpx = (int)Math.Round(MapWidthM * CurrentScaleFactor);
            int MapHeightpx = (int)Math.Round(MapHeightM * CurrentScaleFactor);

            // Calculate rotated map dimensions (same as in Rotate function)
            double radians = Heading * Math.PI / 180.0;
            int rotatedWidth = (int)Math.Ceiling(Math.Abs(MapWidthpx * Math.Cos(radians)) + Math.Abs(MapHeightpx * Math.Sin(radians)));
            int rotatedHeight = (int)Math.Ceiling(Math.Abs(MapWidthpx * Math.Sin(radians)) + Math.Abs(MapHeightpx * Math.Cos(radians)));

            // Rotation center in unrotated map
            double centerX = MapWidthpx / 2.0;
            double centerY = MapHeightpx / 2.0;

            // Translate point to be relative to rotation center
            double relX = PixelX - centerX;
            double relY = PixelY - centerY;

            // Rotate around center (counter-clockwise, so negative heading)
            double cosAngle = Math.Cos(-radians);
            double sinAngle = Math.Sin(-radians);
            double rotatedX = relX * cosAngle - relY * sinAngle;
            double rotatedY = relX * sinAngle + relY * cosAngle;

            // Translate back relative to rotated map center
            double rotatedCenterX = rotatedWidth / 2.0;
            double rotatedCenterY = rotatedHeight / 2.0;
            int finalX = (int)Math.Round(rotatedX + rotatedCenterX);
            int finalY = (int)Math.Round(rotatedY + rotatedCenterY);

            return new Point(finalX, finalY);
        }

        // Calculate and set the initial elevation range (ignoring "no data" elevations)
        private double[] CalculateInitialElevationRange
            (
            Field Field
            )
        {
            bool found = false;
            double minElevation = double.MaxValue;
            double maxElevation = double.MinValue;

            foreach (Bin bin in Field.Bins)
            {
                double elevation = bin.CurrentElevationM;
                if (elevation == Field.BIN_NO_DATA_SENTINEL)
                {
                    continue;
                }

                if (elevation < minElevation) minElevation = elevation;
                if (elevation > maxElevation) maxElevation = elevation;
                found = true;
            }

            if (!found)
            {
                return new double[] { Field.BIN_NO_DATA_SENTINEL, Field.BIN_NO_DATA_SENTINEL };
            }

            return new double[] { minElevation, maxElevation };
        }

        private static int[,] BuildColorPalette()
        {
            var palette = new int[256, 3]; // 256 colors, RGB values

            for (int i = 0; i < 256; i++)
            {
                var hue = 240.0 - (i / 255.0) * 240.0; // Blue (240°) to Red (0°)
                var rgb = HsvToRgb(hue, 1.0, 1.0);

                palette[i, 0] = (int)(rgb[0] * 255); // Red
                palette[i, 1] = (int)(rgb[1] * 255); // Green
                palette[i, 2] = (int)(rgb[2] * 255); // Blue
            }

            return palette;
        }

        private static Color[] BuildCutFillBandColors()
        {
            // Low to high: violet -> dark red
            return new[]
            {
                Color.FromArgb(0x80, 0x00, 0x80), // violet
                Color.FromArgb(0x9A, 0x31, 0xFF), // indigo
                Color.FromArgb(0x00, 0x66, 0xFF), // blue
                Color.FromArgb(0x36, 0xF5, 0xF5), // cyan
                Color.FromArgb(0x00, 0xCD, 0x00), // green
                Color.FromArgb(0xFF, 0xFF, 0x00), // yellow
                Color.FromArgb(0xFF, 0x80, 0x00), // orange
                Color.FromArgb(0xFF, 0x00, 0x00), // red
                Color.FromArgb(0xB4, 0x00, 0x00)  // dark red
            };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int GetCutFillBandIndexForDifference(double differenceM)
        {
            if (differenceM < -0.8) return 0;
            if (differenceM < -0.5) return 1;
            if (differenceM < -0.05) return 2;
            if (differenceM < -0.01) return 3;
            if (differenceM <= 0.01) return 4;
            if (differenceM <= 0.05) return 5;
            if (differenceM <= 0.5) return 6;
            if (differenceM <= 0.8) return 7;
            if (differenceM > 0.8) return 8;

            return -1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Color GetCutFillColorForDifference(double differenceM)
        {
            int colorIndex = GetCutFillBandIndexForDifference(differenceM);
            return colorIndex >= 0 ? CutFillBandColors[colorIndex] : CutFillFallbackColor;
        }

        private static bool TryGetSurveyElevationRange
            (
            Survey survey,
            out double minElevation,
            out double maxElevation
            )
        {
            minElevation = double.MaxValue;
            maxElevation = double.MinValue;
            bool found = false;

            // Treat interior and boundary points as a single dataset.
            foreach (TopologyPoint point in survey.InteriorPoints)
            {
                double elevation = point.ExistingElevation;
                if (double.IsNaN(elevation) || double.IsInfinity(elevation))
                {
                    continue;
                }

                if (elevation < minElevation) minElevation = elevation;
                if (elevation > maxElevation) maxElevation = elevation;
                found = true;
            }

            foreach (TopologyPoint point in survey.BoundaryPoints)
            {
                double elevation = point.ExistingElevation;
                if (double.IsNaN(elevation) || double.IsInfinity(elevation))
                {
                    continue;
                }

                if (elevation < minElevation) minElevation = elevation;
                if (elevation > maxElevation) maxElevation = elevation;
                found = true;
            }

            if (!found)
            {
                minElevation = 0;
                maxElevation = 0;
            }

            return found;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int GetCutFillBandIndexForSurveyElevation(double elevation, double minElevation, double maxElevation)
        {
            if (double.IsNaN(elevation) || double.IsInfinity(elevation))
            {
                return CutFillBandColors.Length / 2;
            }

            if (maxElevation <= minElevation)
            {
                return CutFillBandColors.Length / 2;
            }

            double normalized = (elevation - minElevation) / (maxElevation - minElevation);
            normalized = Math.Max(0.0, Math.Min(1.0, normalized));

            int index = (int)Math.Floor(normalized * CutFillBandColors.Length);
            if (index >= CutFillBandColors.Length)
            {
                index = CutFillBandColors.Length - 1;
            }

            return index;
        }

        private static double[] HsvToRgb(double h, double s, double v)
        {
            h = h % 360.0;
            if (h < 0) h += 360.0;

            var c = v * s;
            var x = c * (1 - Math.Abs((h / 60.0) % 2 - 1));
            var m = v - c;

            double r, g, b;

            if (h >= 0 && h < 60)
            {
                r = c; g = x; b = 0;
            }
            else if (h >= 60 && h < 120)
            {
                r = x; g = c; b = 0;
            }
            else if (h >= 120 && h < 180)
            {
                r = 0; g = c; b = x;
            }
            else if (h >= 180 && h < 240)
            {
                r = 0; g = x; b = c;
            }
            else if (h >= 240 && h < 300)
            {
                r = x; g = 0; b = c;
            }
            else
            {
                r = c; g = 0; b = x;
            }

            return new double[] { r + m, g + m, b + m };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Point PixelToBinFastUnrotated(int pixelX, int pixelY)
        {
            int binX = (int)Math.Floor(pixelX / _pixelsPerBin);
            double fieldY = CurrentField.FieldMaxY - (pixelY * _invScaleFactor);
            int binY = (int)Math.Floor((fieldY - CurrentField.FieldMinY) / Field.BIN_SIZE_M);
            return new Point(binX, binY);
        }

        /// <summary>
        /// Renders a single tile of the completion map
        /// </summary>
        /// <param name="BinGrid">2D grid of bins</param>
        /// <param name="tileStartX">X coordinate of tile start in map pixels</param>
        /// <param name="tileStartY">Y coordinate of tile start in map pixels</param>
        /// <param name="tileWidth">Width of tile in pixels</param>
        /// <param name="tileHeight">Height of tile in pixels</param>
        /// <param name="mapWidthpx">Total map width in pixels</param>
        /// <param name="mapHeightpx">Total map height in pixels</param>
        /// <param name="minX">Minimum X bin index</param>
        /// <param name="minY">Minimum Y bin index</param>
        /// <param name="gridWidth">Width of elevation grid</param>
        /// <param name="gridHeight">Height of elevation grid</param>
        /// <param name="showGrid">Whether to show grid lines</param>
        /// <param name="scaleFactor">Scale factor in pixels per meter</param>
        /// <returns>Rendered tile bitmap</returns>
        private Bitmap RenderCompletionTile
            (
            Bin?[,] BinGrid,
            int tileStartX,
            int tileStartY,
            int tileWidth,
            int tileHeight,
            int mapWidthpx,
            int mapHeightpx,
            int minX,
            int minY,
            int gridWidth,
            int gridHeight,
            bool showGrid,
            double scaleFactor
            )
        {
            Bitmap tile = new Bitmap(tileWidth, tileHeight, PixelFormat.Format32bppArgb);

            BitmapData bitmapData = tile.LockBits(
                new Rectangle(0, 0, tileWidth, tileHeight),
                ImageLockMode.WriteOnly,
                PixelFormat.Format32bppArgb);

            try
            {
                int stride = bitmapData.Stride;
                int bytesPerRow = tileWidth * 4;

                byte[] rowData = new byte[bytesPerRow];
                double pixelsPerBin = CurrentScaleFactor * Field.BIN_SIZE_M;
                const double heightEpsM = 1e-4;

                for (int y = 0; y < tileHeight; y++)
                {
                    int rowOffset = 0;
                    int bmpY = y;
                    int mapY = tileStartY + y;

                    for (int x = 0; x < tileWidth; x++)
                    {
                        int mapX = tileStartX + x;

                        int binGridX = (int)Math.Floor(mapX / pixelsPerBin);
                        int binGridY = (int)Math.Floor((CurrentField.FieldMaxY - mapY / CurrentScaleFactor - CurrentField.FieldMinY) / Field.BIN_SIZE_M);
                        int gridX = binGridX - minX;
                        int gridY = binGridY - minY;

                        byte r, g, b, a;

                        if (gridX >= 0 && gridX < gridWidth && gridY >= 0 && gridY < gridHeight)
                        {
                            Bin? bin = BinGrid[gridY, gridX];
                            if (bin != null)
                            {
                                double cur = bin.CurrentElevationM;
                                double tgt = bin.TargetElevationM;
                                double init = bin.InitialElevationM;

                                if (cur != Field.BIN_NO_DATA_SENTINEL &&
                                    tgt != Field.BIN_NO_DATA_SENTINEL &&
                                    init != Field.BIN_NO_DATA_SENTINEL)
                                {
                                    bool atTarget = Math.Abs(cur - tgt) <= heightEpsM;
                                    bool atInitial = Math.Abs(cur - init) <= heightEpsM;

                                    if (atTarget)
                                    {
                                        r = TractorGreen.R;
                                        g = TractorGreen.G;
                                        b = TractorGreen.B;
                                        a = 255;
                                    }
                                    else if (atInitial)
                                    {
                                        r = TractorRed.R;
                                        g = TractorRed.G;
                                        b = TractorRed.B;
                                        a = 255;
                                    }
                                    else
                                    {
                                        // Same orange as cut/fill band palette (in-progress work)
                                        r = 0xFF;
                                        g = 0x80;
                                        b = 0x00;
                                        a = 255;
                                    }
                                }
                                else
                                {
                                    r = g = b = 0;
                                    a = 0;
                                }
                            }
                            else
                            {
                                r = g = b = 0;
                                a = 0;
                            }
                        }
                        else
                        {
                            r = g = b = 0;
                            a = 0;
                        }

                        if (showGrid && a > 0)
                        {
                            if ((mapX % scaleFactor == 0) || (mapY % scaleFactor == 0))
                            {
                                r = g = b = 0x40;
                            }
                        }

                        rowData[rowOffset + 0] = b;
                        rowData[rowOffset + 1] = g;
                        rowData[rowOffset + 2] = r;
                        rowData[rowOffset + 3] = a;
                        rowOffset += 4;
                    }

                    IntPtr rowPtr = new IntPtr(bitmapData.Scan0.ToInt64() + (bmpY * stride));
                    Marshal.Copy(rowData, 0, rowPtr, bytesPerRow);
                }
            }
            finally
            {
                tile.UnlockBits(bitmapData);
            }

            return tile;
        }

        /// <summary>
        /// Renders a single tile of the cut/fill map
        /// </summary>
        /// <param name="BinGrid">2D grid of bins</param>
        /// <param name="tileStartX">X coordinate of tile start in map pixels</param>
        /// <param name="tileStartY">Y coordinate of tile start in map pixels</param>
        /// <param name="tileWidth">Width of tile in pixels</param>
        /// <param name="tileHeight">Height of tile in pixels</param>
        /// <param name="mapWidthpx">Total map width in pixels</param>
        /// <param name="mapHeightpx">Total map height in pixels</param>
        /// <param name="minX">Minimum X bin index</param>
        /// <param name="minY">Minimum Y bin index</param>
        /// <param name="gridWidth">Width of elevation grid</param>
        /// <param name="gridHeight">Height of elevation grid</param>
        /// <param name="showGrid">Whether to show grid lines</param>
        /// <param name="scaleFactor">Scale factor in pixels per meter</param>
        /// <returns>Rendered tile bitmap</returns>
        private Bitmap RenderCutFillTile
            (
            Bin?[,] BinGrid,
            int tileStartX,
            int tileStartY,
            int tileWidth,
            int tileHeight,
            int mapWidthpx,
            int mapHeightpx,
            int minX,
            int minY,
            int gridWidth,
            int gridHeight,
            bool showGrid,
            double scaleFactor
            )
        {
            Bitmap tile = new Bitmap(tileWidth, tileHeight, PixelFormat.Format32bppArgb);

            // Lock bitmap data for direct memory access
            BitmapData bitmapData = tile.LockBits(
                new Rectangle(0, 0, tileWidth, tileHeight),
                ImageLockMode.WriteOnly,
                PixelFormat.Format32bppArgb);

            try
            {
                int stride = bitmapData.Stride;
                int bytesPerRow = tileWidth * 4; // Actual bytes needed for one row (BGRA format)

                // Allocate buffer for one row of pixel data (BGRA format)
                byte[] rowData = new byte[bytesPerRow];

                // Precompute pixel-to-bin conversion (unrotated map space, matches PixelToBin with Heading=0)
                double pixelsPerBin = CurrentScaleFactor * Field.BIN_SIZE_M;

                // Write pixel data directly to bitmap memory
                for (int y = 0; y < tileHeight; y++)
                {
                    int rowOffset = 0;
                    int bmpY = y;

                    // Map Y coordinate (relative to map origin)
                    int mapY = tileStartY + y;

                    for (int x = 0; x < tileWidth; x++)
                    {
                        // Map X coordinate (relative to map origin)
                        int mapX = tileStartX + x;

                        // Inlined pixel-to-bin: same as PixelToBin(mapX, mapY, 0) without allocations
                        int binGridX = (int)Math.Floor(mapX / pixelsPerBin);
                        int binGridY = (int)Math.Floor((CurrentField.FieldMaxY - mapY / CurrentScaleFactor - CurrentField.FieldMinY) / Field.BIN_SIZE_M);
                        int gridX = binGridX - minX;
                        int gridY = binGridY - minY;

                        byte r, g, b, a;

                        if (gridX >= 0 && gridX < gridWidth && gridY >= 0 && gridY < gridHeight)
                        {
                            var ExistingElevation = BinGrid[gridY, gridX]?.CurrentElevationM;
                            var TargetElevation = BinGrid[gridY, gridX]?.TargetElevationM;

                            var CutNum = BinGrid[gridY, gridX]?.NumberOfCuts;
                            var FillNum = BinGrid[gridY, gridX]?.NumberofFills;

                            if (CutNum > 0 || FillNum > 0)
                            {
                                r = 0xFF;
                                g = 0xFF;
                                b = 0xFF;
                                a = 255;
                            }
                            else
                            {
                                if (ExistingElevation.HasValue && TargetElevation.HasValue && ((ExistingElevation.Value != Field.BIN_NO_DATA_SENTINEL) && (TargetElevation.Value != Field.BIN_NO_DATA_SENTINEL)))
                                {
                                    double DifferenceM = ExistingElevation.Value - TargetElevation.Value;

                                    Color cutFillColor = GetCutFillColorForDifference(DifferenceM);
                                    r = cutFillColor.R;
                                    g = cutFillColor.G;
                                    b = cutFillColor.B;
                                    a = 255;
                                }
                                else
                                {
                                    // No data - make transparent
                                    r = g = b = 0;
                                    a = 0; // Transparent
                                }
                            }
                        }
                        else
                        {
                            // Outside grid - make transparent
                            r = g = b = 0;
                            a = 0; // Transparent
                        }

                        // Draw grid lines (using map coordinates)
                        if (showGrid && a > 0)
                        {
                            if ((mapX % scaleFactor == 0) || (mapY % scaleFactor == 0))
                            {
                                r = g = b = 0x40; // Dark gray
                            }
                        }

                        // Write BGRA (bitmap format with alpha)
                        rowData[rowOffset + 0] = b; // Blue
                        rowData[rowOffset + 1] = g; // Green
                        rowData[rowOffset + 2] = r; // Red
                        rowData[rowOffset + 3] = a; // Alpha
                        rowOffset += 4;
                    }

                    // Copy row data to bitmap memory
                    IntPtr rowPtr = new IntPtr(bitmapData.Scan0.ToInt64() + (bmpY * stride));
                    Marshal.Copy(rowData, 0, rowPtr, bytesPerRow);
                }
            }
            finally
            {
                tile.UnlockBits(bitmapData);
            }

            return tile;
        }

        /// <summary>
        /// Renders a single tile of the elevation map
        /// </summary>
        /// <param name="BinGrid">2D grid of bins</param>
        /// <param name="tileStartX">X coordinate of tile start in map pixels</param>
        /// <param name="tileStartY">Y coordinate of tile start in map pixels</param>
        /// <param name="tileWidth">Width of tile in pixels</param>
        /// <param name="tileHeight">Height of tile in pixels</param>
        /// <param name="mapWidthpx">Total map width in pixels</param>
        /// <param name="mapHeightpx">Total map height in pixels</param>
        /// <param name="minX">Minimum X bin index</param>
        /// <param name="minY">Minimum Y bin index</param>
        /// <param name="gridWidth">Width of elevation grid</param>
        /// <param name="gridHeight">Height of elevation grid</param>
        /// <param name="minElevation">Minimum elevation value</param>
        /// <param name="maxElevation">Maximum elevation value</param>
        /// <param name="colorPalette">Color palette for elevation mapping</param>
        /// <param name="showGrid">Whether to show grid lines</param>
        /// <param name="scaleFactor">Scale factor in pixels per meter</param>
        /// <returns>Rendered tile bitmap</returns>
        private Bitmap RenderElevationTile
            (
            Bin?[,] BinGrid,
            int tileStartX,
            int tileStartY,
            int tileWidth,
            int tileHeight,
            int mapWidthpx,
            int mapHeightpx,
            int minX,
            int minY,
            int gridWidth,
            int gridHeight,
            double minElevation,
            double maxElevation,
            int[,] colorPalette,
            bool showGrid,
            double scaleFactor
            )
        {
            Bitmap tile = new Bitmap(tileWidth, tileHeight, PixelFormat.Format32bppArgb);

            // Lock bitmap data for direct memory access
            BitmapData bitmapData = tile.LockBits(
                new Rectangle(0, 0, tileWidth, tileHeight),
                ImageLockMode.WriteOnly,
                PixelFormat.Format32bppArgb);

            try
            {
                int stride = bitmapData.Stride;
                int bytesPerRow = tileWidth * 4; // Actual bytes needed for one row (BGRA format)

                // Allocate buffer for one row of pixel data (BGRA format)
                byte[] rowData = new byte[bytesPerRow];

                // Precompute pixel-to-bin conversion (unrotated map space, matches PixelToBin with Heading=0)
                double pixelsPerBin = CurrentScaleFactor * Field.BIN_SIZE_M;

                // Write pixel data directly to bitmap memory
                for (int y = 0; y < tileHeight; y++)
                {
                    int rowOffset = 0;
                    int bmpY = y;

                    // Map Y coordinate (relative to map origin)
                    int mapY = tileStartY + y;

                    for (int x = 0; x < tileWidth; x++)
                    {
                        // Map X coordinate (relative to map origin)
                        int mapX = tileStartX + x;

                        // Inlined pixel-to-bin: same as PixelToBin(mapX, mapY, 0) without allocations
                        int binGridX = (int)Math.Floor(mapX / pixelsPerBin);
                        int binGridY = (int)Math.Floor((CurrentField.FieldMaxY - mapY / CurrentScaleFactor - CurrentField.FieldMinY) / Field.BIN_SIZE_M);
                        int gridX = binGridX - minX;
                        int gridY = binGridY - minY;

                        byte r, g, b, a;

                        if (gridX >= 0 && gridX < gridWidth && gridY >= 0 && gridY < gridHeight)
                        {
                            var elevation = BinGrid[gridY, gridX]?.CurrentElevationM;

                            if (elevation.HasValue && elevation.Value != Field.BIN_NO_DATA_SENTINEL)
                            {
                                // Normalize elevation to 0-255 range
                                var normalizedElevation = (elevation.Value - minElevation) / (maxElevation - minElevation);
                                var colorIndex = Math.Max(0, Math.Min(255, (int)(normalizedElevation * 255)));

                                // Apply color from palette (palette is RGB, bitmap needs BGR)
                                r = (byte)colorPalette[colorIndex, 0]; // Red
                                g = (byte)colorPalette[colorIndex, 1]; // Green
                                b = (byte)colorPalette[colorIndex, 2]; // Blue
                                a = 255; // Fully opaque
                            }
                            else
                            {
                                // No data - make transparent
                                r = g = b = 0;
                                a = 0; // Transparent
                            }
                        }
                        else
                        {
                            // Outside grid - make transparent
                            r = g = b = 0;
                            a = 0; // Transparent
                        }

                        // Draw grid lines (using map coordinates)
                        if (showGrid && a > 0)
                        {
                            if ((mapX % scaleFactor == 0) || (mapY % scaleFactor == 0))
                            {
                                r = g = b = 0x40; // Dark gray
                            }
                        }

                        // Write BGRA (bitmap format with alpha)
                        rowData[rowOffset + 0] = b; // Blue
                        rowData[rowOffset + 1] = g; // Green
                        rowData[rowOffset + 2] = r; // Red
                        rowData[rowOffset + 3] = a; // Alpha
                        rowOffset += 4;
                    }

                    // Copy row data to bitmap memory
                    IntPtr rowPtr = new IntPtr(bitmapData.Scan0.ToInt64() + (bmpY * stride));
                    Marshal.Copy(rowData, 0, rowPtr, bytesPerRow);
                }
            }
            finally
            {
                tile.UnlockBits(bitmapData);
            }

            return tile;
        }
    }
}
