using System;
using System.Collections.Generic;

namespace AgGrade.Controller
{
    /// <summary>
    /// High-level GNSS quality state for a single receiver stream.
    /// </summary>
    public enum GnssQualityState
    {
        /// <summary>No fix has been received yet.</summary>
        NoData,

        /// <summary>Fix is missing, stale, or otherwise not <see cref="GNSSFix.IsValid"/>.</summary>
        NoFix,

        /// <summary>Fix is valid but not RTK fixed.</summary>
        NoRtk,

        /// <summary>RTK fixed but continuous RTK dwell has not yet reached <see cref="GnssQualityThresholds.MinRtkFixedSeconds"/>.</summary>
        Unstable,

        /// <summary>
        /// RTK has been fixed long enough for normal operations (surveying, leveling, field work).
        /// Does not require low horizontal jitter or being parked.
        /// </summary>
        Stable,

        /// <summary>
        /// Parked with low horizontal jitter for long enough; safe for benchmarks and calibration capture.
        /// </summary>
        HighQuality,
    }

    /// <summary>
    /// Thresholds for <see cref="GnssQualityMonitor"/>. Tune per receiver or environment as needed.
    /// </summary>
    public sealed class GnssQualityThresholds
    {
        /// <summary>Minimum continuous RTK-fixed time before <see cref="GnssQualityState.Stable"/> can be reached (seconds).</summary>
        public double MinRtkFixedSeconds { get; set; } = 45.0;

        /// <summary>Minimum time speed must remain at or below <see cref="MaxSpeedMph"/> before <see cref="GnssQualityState.HighQuality"/> (seconds).</summary>
        public double MinParkedSeconds { get; set; } = 5.0;

        /// <summary>
        /// Minimum time horizontal jitter must remain at or below <see cref="MaxHorizontalJitterMm"/>
        /// before <see cref="GnssQualityState.HighQuality"/> (seconds).
        /// </summary>
        public double MinStableSeconds { get; set; } = 15.0;

        /// <summary>Maximum ground speed while considered parked (mph).</summary>
        public double MaxSpeedMph { get; set; } = 0.2;

        /// <summary>Maximum horizontal scatter from the window mean (mm).</summary>
        public double MaxHorizontalJitterMm { get; set; } = 25.0;

        /// <summary>Rolling history length used for jitter (seconds).</summary>
        public double SampleWindowSeconds { get; set; } = 60.0;

        /// <summary>
        /// Gap between valid fixes longer than this resets continuity timers (milliseconds).
        /// Defaults to <see cref="GNSSFix.FIX_STALE_TIME_MS"/>.
        /// </summary>
        public int MaxFixGapMs { get; set; } = GNSSFix.FIX_STALE_TIME_MS;
    }

    /// <summary>
    /// Tracks RTK convergence, operational stability, and parked precision quality for one GNSS receiver.
    /// <see cref="GnssQualityState.Stable"/> is sufficient for surveying and leveling;
    /// <see cref="GnssQualityState.HighQuality"/> is required for benchmarks and calibration.
    /// Create one instance per receiver (tractor, front pan, rear pan). Does not use GNSS heading.
    /// </summary>
    public sealed class GnssQualityMonitor
    {
        private readonly GnssQualityThresholds _thresholds;
        private readonly List<PositionSample> _samples = new List<PositionSample>();

        private DateTime? _lastValidFixTime;
        private DateTime? _rtkFixedSince;
        private DateTime? _parkedSince;
        private DateTime? _stableSince;
        private bool _hasReceivedFix;

        // WGS84 ellipsoid (same as TractorAntennaFinder)
        private const double Wgs84A = 6378137.0;
        private const double Wgs84F = 1.0 / 298.257223563;
        private static readonly double Wgs84E2 = 2 * Wgs84F - Wgs84F * Wgs84F;

        /// <summary>
        /// One RTK-fixed position observation stored for jitter and median capture.
        /// </summary>
        private struct PositionSample
        {
            /// <summary>Local time when the sample was recorded.</summary>
            public DateTime Time;

            /// <summary>Latitude in decimal degrees (WGS-84).</summary>
            public double Latitude;

            /// <summary>Longitude in decimal degrees (WGS-84).</summary>
            public double Longitude;
        }

        /// <summary>
        /// Creates a monitor with default <see cref="GnssQualityThresholds"/>.
        /// </summary>
        public GnssQualityMonitor() : this(null)
        {
        }

        /// <summary>
        /// Creates a monitor with the given thresholds, or defaults when <paramref name="thresholds"/> is null.
        /// </summary>
        /// <param name="thresholds">Optional per-receiver or per-use-case thresholds.</param>
        public GnssQualityMonitor(GnssQualityThresholds? thresholds)
        {
            _thresholds = thresholds ?? new GnssQualityThresholds();
        }

        /// <summary>Configured thresholds (read-only reference).</summary>
        public GnssQualityThresholds Thresholds => _thresholds;

        /// <summary>Current quality state from the most recent <see cref="Update"/>.</summary>
        public GnssQualityState State { get; private set; } = GnssQualityState.NoData;

        /// <summary>
        /// Raised when <see cref="State"/> changes. Arguments are (previousState, newState).
        /// </summary>
        public event Action<GnssQualityState, GnssQualityState>? StateChanged;

        /// <summary>True when <see cref="State"/> is <see cref="GnssQualityState.HighQuality"/>.</summary>
        public bool IsReadyToCapture => State == GnssQualityState.HighQuality;

        /// <summary>
        /// True when <see cref="State"/> is <see cref="GnssQualityState.Stable"/> or
        /// <see cref="GnssQualityState.HighQuality"/> (RTK converged for field operations).
        /// </summary>
        public bool IsStableForOperations =>
            State == GnssQualityState.Stable || State == GnssQualityState.HighQuality;

        /// <summary>Ground speed from the last update (mph); not derived from heading.</summary>
        public double CurrentSpeedMph { get; private set; }

        /// <summary>
        /// Maximum horizontal distance (mm) from the mean of RTK-fixed samples in the rolling window.
        /// Zero when fewer than two RTK-fixed samples are in the window.
        /// </summary>
        public double HorizontalJitterMm { get; private set; }

        /// <summary>Seconds RTK has been continuously fixed with valid fixes arriving on time.</summary>
        public double RtkFixedDwellSeconds { get; private set; }

        /// <summary>Seconds speed has been at or below <see cref="GnssQualityThresholds.MaxSpeedMph"/>.</summary>
        public double ParkedDwellSeconds { get; private set; }

        /// <summary>Seconds horizontal jitter has been at or below the jitter threshold (counts toward <see cref="GnssQualityState.HighQuality"/>).</summary>
        public double StableDwellSeconds { get; private set; }

        /// <summary>Number of RTK-fixed position samples in the rolling window.</summary>
        public int RtkSampleCount { get; private set; }

        /// <summary>
        /// Clears history and dwell timers. State becomes <see cref="GnssQualityState.NoData"/>.
        /// </summary>
        public void Reset()
        {
            _samples.Clear();
            _lastValidFixTime = null;
            _rtkFixedSince = null;
            _parkedSince = null;
            _stableSince = null;
            _hasReceivedFix = false;

            CurrentSpeedMph = 0.0;
            HorizontalJitterMm = 0.0;
            RtkFixedDwellSeconds = 0.0;
            ParkedDwellSeconds = 0.0;
            StableDwellSeconds = 0.0;
            RtkSampleCount = 0;
            SetState(GnssQualityState.NoData);
        }

        /// <summary>
        /// Ingests the latest fix. Call whenever a receiver publishes a new <see cref="GNSSFix"/>.
        /// Updates <see cref="State"/>, dwell metrics, and the rolling sample buffer.
        /// </summary>
        /// <param name="fix">Current fix from one GNSS receiver.</param>
        /// <exception cref="ArgumentNullException"><paramref name="fix"/> is null.</exception>
        public void Update(GNSSFix fix)
        {
            if (fix == null)
                throw new ArgumentNullException(nameof(fix));

            _hasReceivedFix = true;
            CurrentSpeedMph = fix.Vector.SpeedMph;
            DateTime now = DateTime.Now;

            if (!fix.IsValid)
            {
                ClearContinuityTimers();
                _samples.Clear();
                RecomputeJitter();
                UpdateDwellSeconds(now);
                SetState(GnssQualityState.NoFix);
                return;
            }

            if (HasFixGap(now))
            {
                ClearContinuityTimers();
            }

            _lastValidFixTime = fix.LastFixTime ?? now;

            if (!fix.HasRTK)
            {
                _rtkFixedSince = null;
                _stableSince = null;
                PruneSamples(now);
                RecomputeJitter();
                UpdateDwellSeconds(now);
                SetState(GnssQualityState.NoRtk);
                return;
            }

            if (_rtkFixedSince == null)
                _rtkFixedSince = now;

            _samples.Add(new PositionSample
            {
                Time = now,
                Latitude = fix.Latitude,
                Longitude = fix.Longitude,
            });

            PruneSamples(now);
            RecomputeJitter();
            UpdateParkedDwell(now);
            UpdateStableDwell(now);
            UpdateDwellSeconds(now);

            SetState(EvaluateState());
        }

        /// <summary>
        /// Assigns <see cref="State"/> and raises <see cref="StateChanged"/> when the value changes.
        /// </summary>
        /// <param name="newState">New quality state.</param>
        private void SetState(GnssQualityState newState)
        {
            if (State == newState)
                return;

            GnssQualityState previousState = State;
            State = newState;
            StateChanged?.Invoke(previousState, newState);
        }

        /// <summary>
        /// Median latitude/longitude of RTK-fixed samples in the rolling window.
        /// </summary>
        /// <param name="latitude">Median latitude when the method returns true.</param>
        /// <param name="longitude">Median longitude when the method returns true.</param>
        /// <returns>
        /// True when <see cref="IsReadyToCapture"/> is true and at least one sample exists; otherwise false.
        /// </returns>
        public bool TryGetHighQualityPosition(out double latitude, out double longitude)
        {
            latitude = 0.0;
            longitude = 0.0;

            if (!IsReadyToCapture || RtkSampleCount == 0)
                return false;

            List<double> lats = new List<double>(RtkSampleCount);
            List<double> lons = new List<double>(RtkSampleCount);
            foreach (PositionSample sample in _samples)
            {
                lats.Add(sample.Latitude);
                lons.Add(sample.Longitude);
            }

            latitude = Median(lats);
            longitude = Median(lons);
            return true;
        }

        /// <summary>
        /// Returns true if the time since the last valid fix exceeds <see cref="GnssQualityThresholds.MaxFixGapMs"/>.
        /// </summary>
        /// <param name="now">Current local time.</param>
        /// <returns>True when a fix-gap should reset continuity timers.</returns>
        private bool HasFixGap(DateTime now)
        {
            if (_lastValidFixTime == null)
                return false;

            return (now - _lastValidFixTime.Value).TotalMilliseconds > _thresholds.MaxFixGapMs;
        }

        /// <summary>
        /// Resets RTK, parked, and stable dwell anchors after invalid data or a fix gap.
        /// </summary>
        private void ClearContinuityTimers()
        {
            _rtkFixedSince = null;
            _parkedSince = null;
            _stableSince = null;
        }

        /// <summary>
        /// Removes samples older than <see cref="GnssQualityThresholds.SampleWindowSeconds"/>.
        /// </summary>
        /// <param name="now">Current local time used as the window end.</param>
        private void PruneSamples(DateTime now)
        {
            double windowSeconds = _thresholds.SampleWindowSeconds;
            if (windowSeconds <= 0.0)
            {
                _samples.Clear();
                return;
            }

            DateTime cutoff = now.AddSeconds(-windowSeconds);
            int writeIndex = 0;
            for (int readIndex = 0; readIndex < _samples.Count; readIndex++)
            {
                if (_samples[readIndex].Time >= cutoff)
                {
                    if (writeIndex != readIndex)
                        _samples[writeIndex] = _samples[readIndex];
                    writeIndex++;
                }
            }

            if (writeIndex < _samples.Count)
                _samples.RemoveRange(writeIndex, _samples.Count - writeIndex);
        }

        /// <summary>
        /// Recomputes <see cref="HorizontalJitterMm"/> and <see cref="RtkSampleCount"/> from the rolling buffer.
        /// Jitter is the maximum horizontal distance (mm) from the sample mean in a local East/North plane.
        /// </summary>
        private void RecomputeJitter()
        {
            RtkSampleCount = _samples.Count;
            if (_samples.Count < 2)
            {
                HorizontalJitterMm = 0.0;
                return;
            }

            double lat0 = 0.0;
            double lon0 = 0.0;
            foreach (PositionSample sample in _samples)
            {
                lat0 += sample.Latitude;
                lon0 += sample.Longitude;
            }

            lat0 /= _samples.Count;
            lon0 /= _samples.Count;

            double meanEastMm = 0.0;
            double meanNorthMm = 0.0;
            double[] eastMm = new double[_samples.Count];
            double[] northMm = new double[_samples.Count];

            for (int i = 0; i < _samples.Count; i++)
            {
                LatLonToLocalMm(
                    _samples[i].Latitude,
                    _samples[i].Longitude,
                    lat0,
                    lon0,
                    out eastMm[i],
                    out northMm[i]);
                meanEastMm += eastMm[i];
                meanNorthMm += northMm[i];
            }

            meanEastMm /= _samples.Count;
            meanNorthMm /= _samples.Count;

            double maxRadiusMm = 0.0;
            for (int i = 0; i < _samples.Count; i++)
            {
                double dEast = eastMm[i] - meanEastMm;
                double dNorth = northMm[i] - meanNorthMm;
                double radiusMm = Math.Sqrt(dEast * dEast + dNorth * dNorth);
                if (radiusMm > maxRadiusMm)
                    maxRadiusMm = radiusMm;
            }

            HorizontalJitterMm = maxRadiusMm;
        }

        /// <summary>
        /// Starts or clears the parked dwell anchor from <see cref="CurrentSpeedMph"/>.
        /// </summary>
        /// <param name="now">Current local time.</param>
        private void UpdateParkedDwell(DateTime now)
        {
            if (CurrentSpeedMph <= _thresholds.MaxSpeedMph)
            {
                if (_parkedSince == null)
                    _parkedSince = now;
            }
            else
            {
                _parkedSince = null;
            }
        }

        /// <summary>
        /// Starts or clears the low-jitter dwell anchor from <see cref="HorizontalJitterMm"/>
        /// (required for <see cref="GnssQualityState.HighQuality"/>, not for <see cref="GnssQualityState.Stable"/>).
        /// </summary>
        /// <param name="now">Current local time.</param>
        private void UpdateStableDwell(DateTime now)
        {
            bool jitterOk = RtkSampleCount >= 2 && HorizontalJitterMm <= _thresholds.MaxHorizontalJitterMm;
            if (jitterOk)
            {
                if (_stableSince == null)
                    _stableSince = now;
            }
            else
            {
                _stableSince = null;
            }
        }

        /// <summary>
        /// Refreshes public dwell-second properties from internal anchors and <paramref name="now"/>.
        /// </summary>
        /// <param name="now">Current local time.</param>
        private void UpdateDwellSeconds(DateTime now)
        {
            RtkFixedDwellSeconds = ElapsedSeconds(_rtkFixedSince, now);
            ParkedDwellSeconds = ElapsedSeconds(_parkedSince, now);
            StableDwellSeconds = ElapsedSeconds(_stableSince, now);
        }

        /// <summary>
        /// Maps current dwell metrics and anchors to <see cref="GnssQualityState"/>.
        /// </summary>
        /// <returns>The quality state after the latest <see cref="Update"/>.</returns>
        private GnssQualityState EvaluateState()
        {
            if (!_hasReceivedFix)
                return GnssQualityState.NoData;

            if (_rtkFixedSince == null)
                return GnssQualityState.NoRtk;

            if (RtkFixedDwellSeconds < _thresholds.MinRtkFixedSeconds)
                return GnssQualityState.Unstable;

            bool parkedLongEnough = ParkedDwellSeconds >= _thresholds.MinParkedSeconds;
            bool lowJitterLongEnough = StableDwellSeconds >= _thresholds.MinStableSeconds;

            if (parkedLongEnough && lowJitterLongEnough)
                return GnssQualityState.HighQuality;

            return GnssQualityState.Stable;
        }

        /// <summary>
        /// Elapsed seconds from <paramref name="since"/> to <paramref name="now"/>, or zero when <paramref name="since"/> is null.
        /// </summary>
        /// <param name="since">Start of an interval, or null if the interval is not active.</param>
        /// <param name="now">End of the interval.</param>
        /// <returns>Non-negative elapsed seconds.</returns>
        private static double ElapsedSeconds(DateTime? since, DateTime now)
        {
            if (since == null)
                return 0.0;

            double seconds = (now - since.Value).TotalSeconds;
            return seconds < 0.0 ? 0.0 : seconds;
        }

        /// <summary>
        /// Computes the median of a list of values (average of two middle values when count is even).
        /// </summary>
        /// <param name="values">Values to sort in place and summarize.</param>
        /// <returns>Median value, or 0 when the list is empty.</returns>
        private static double Median(List<double> values)
        {
            values.Sort();
            int count = values.Count;
            if (count == 0)
                return 0.0;

            int mid = count / 2;
            if ((count & 1) == 1)
                return values[mid];

            return (values[mid - 1] + values[mid]) / 2.0;
        }

        /// <summary>
        /// Converts WGS-84 lat/lon to local East/North offsets in millimeters about a reference point.
        /// Uses the same ellipsoid model as <c>TractorAntennaFinder</c>.
        /// </summary>
        /// <param name="latDeg">Point latitude (degrees).</param>
        /// <param name="lonDeg">Point longitude (degrees).</param>
        /// <param name="lat0Deg">Reference latitude (degrees).</param>
        /// <param name="lon0Deg">Reference longitude (degrees).</param>
        /// <param name="eastMm">East offset from the reference (mm).</param>
        /// <param name="northMm">North offset from the reference (mm).</param>
        private static void LatLonToLocalMm
            (
            double latDeg,
            double lonDeg,
            double lat0Deg,
            double lon0Deg,
            out double eastMm,
            out double northMm
            )
        {
            double lat0Rad = lat0Deg * Math.PI / 180.0;
            double lon0Rad = lon0Deg * Math.PI / 180.0;
            double latRad = latDeg * Math.PI / 180.0;
            double lonRad = lonDeg * Math.PI / 180.0;

            double sinLat0 = Math.Sin(lat0Rad);
            double n = Wgs84A / Math.Sqrt(1.0 - Wgs84E2 * sinLat0 * sinLat0);
            double m = n * (1.0 - Wgs84E2) / (1.0 - Wgs84E2 * sinLat0 * sinLat0);

            double northM = m * (latRad - lat0Rad);
            double eastM = n * Math.Cos(lat0Rad) * (lonRad - lon0Rad);

            eastMm = eastM * 1000.0;
            northMm = northM * 1000.0;
        }
    }
}
