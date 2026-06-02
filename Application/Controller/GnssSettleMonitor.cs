using System;
using System.Collections.Generic;

namespace AgGrade.Controller
{
    /// <summary>
    /// High-level readiness of a GNSS stream for precision capture (e.g. antenna calibration).
    /// </summary>
    public enum GnssSettleState
    {
        /// <summary>No fix has been received yet.</summary>
        NoData,

        /// <summary>Fix is missing, stale, or otherwise not <see cref="GNSSFix.IsValid"/>.</summary>
        NoFix,

        /// <summary>Fix is valid but not RTK fixed.</summary>
        NoRtk,

        /// <summary>RTK fixed but dwell, speed, or horizontal jitter requirements are not met.</summary>
        Unstable,

        /// <summary>All settle criteria satisfied; safe to capture a position sample.</summary>
        Settled,
    }

    /// <summary>
    /// Thresholds for <see cref="GnssSettleMonitor"/>. Tune per receiver or environment as needed.
    /// </summary>
    public sealed class GnssSettleThresholds
    {
        /// <summary>Minimum continuous RTK-fixed time before settle can complete (seconds).</summary>
        public double MinRtkFixedSeconds { get; set; } = 45.0;

        /// <summary>Minimum time speed must remain at or below <see cref="MaxSpeedMph"/> (seconds).</summary>
        public double MinParkedSeconds { get; set; } = 5.0;

        /// <summary>
        /// Minimum time horizontal jitter must remain at or below <see cref="MaxHorizontalJitterMm"/> (seconds).
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
    /// Tracks RTK-fixed dwell, parked speed, and horizontal position stability for one GNSS receiver.
    /// Create one instance per receiver (tractor, front pan, rear pan). Does not use GNSS heading.
    /// </summary>
    public sealed class GnssSettleMonitor
    {
        private readonly GnssSettleThresholds _thresholds;
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

        private struct PositionSample
        {
            public DateTime Time;
            public double Latitude;
            public double Longitude;
        }

        public GnssSettleMonitor() : this(null)
        {
        }

        public GnssSettleMonitor(GnssSettleThresholds? thresholds)
        {
            _thresholds = thresholds ?? new GnssSettleThresholds();
        }

        /// <summary>Configured thresholds (read-only reference).</summary>
        public GnssSettleThresholds Thresholds => _thresholds;

        /// <summary>Current settle state from the most recent <see cref="Update"/>.</summary>
        public GnssSettleState State { get; private set; } = GnssSettleState.NoData;

        /// <summary>True when <see cref="State"/> is <see cref="GnssSettleState.Settled"/>.</summary>
        public bool IsReadyToCapture => State == GnssSettleState.Settled;

        /// <summary>Ground speed from the last update (mph); not derived from heading.</summary>
        public double CurrentSpeedMph { get; private set; }

        /// <summary>
        /// Maximum horizontal distance (mm) from the mean of RTK-fixed samples in the rolling window.
        /// Zero when fewer than two RTK-fixed samples are in the window.
        /// </summary>
        public double HorizontalJitterMm { get; private set; }

        /// <summary>Seconds RTK has been continuously fixed with valid fixes arriving on time.</summary>
        public double RtkFixedDwellSeconds { get; private set; }

        /// <summary>Seconds speed has been at or below <see cref="GnssSettleThresholds.MaxSpeedMph"/>.</summary>
        public double ParkedDwellSeconds { get; private set; }

        /// <summary>Seconds horizontal jitter has been at or below the jitter threshold.</summary>
        public double StableDwellSeconds { get; private set; }

        /// <summary>Number of RTK-fixed position samples in the rolling window.</summary>
        public int RtkSampleCount { get; private set; }

        /// <summary>Clears history and dwell timers. State becomes <see cref="GnssSettleState.NoData"/>.</summary>
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
            State = GnssSettleState.NoData;
        }

        /// <summary>
        /// Ingests the latest fix. Call whenever a receiver publishes a new <see cref="GNSSFix"/>.
        /// </summary>
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
                State = GnssSettleState.NoFix;
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
                State = GnssSettleState.NoRtk;
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

            State = EvaluateState(now);
        }

        /// <summary>
        /// Median latitude/longitude of RTK-fixed samples in the rolling window.
        /// Returns false unless <see cref="IsReadyToCapture"/> is true and at least one sample exists.
        /// </summary>
        public bool TryGetSettledPosition(out double latitude, out double longitude)
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

        private bool HasFixGap(DateTime now)
        {
            if (_lastValidFixTime == null)
                return false;

            return (now - _lastValidFixTime.Value).TotalMilliseconds > _thresholds.MaxFixGapMs;
        }

        private void ClearContinuityTimers()
        {
            _rtkFixedSince = null;
            _parkedSince = null;
            _stableSince = null;
        }

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

        private void UpdateDwellSeconds(DateTime now)
        {
            RtkFixedDwellSeconds = ElapsedSeconds(_rtkFixedSince, now);
            ParkedDwellSeconds = ElapsedSeconds(_parkedSince, now);
            StableDwellSeconds = ElapsedSeconds(_stableSince, now);
        }

        private GnssSettleState EvaluateState(DateTime now)
        {
            if (!_hasReceivedFix)
                return GnssSettleState.NoData;

            if (_rtkFixedSince == null)
                return GnssSettleState.NoRtk;

            bool rtkLongEnough = RtkFixedDwellSeconds >= _thresholds.MinRtkFixedSeconds;
            bool parkedLongEnough = ParkedDwellSeconds >= _thresholds.MinParkedSeconds;
            bool stableLongEnough = StableDwellSeconds >= _thresholds.MinStableSeconds;

            if (rtkLongEnough && parkedLongEnough && stableLongEnough)
                return GnssSettleState.Settled;

            return GnssSettleState.Unstable;
        }

        private static double ElapsedSeconds(DateTime? since, DateTime now)
        {
            if (since == null)
                return 0.0;

            double seconds = (now - since.Value).TotalSeconds;
            return seconds < 0.0 ? 0.0 : seconds;
        }

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
