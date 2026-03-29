using Microsoft.Data.Sqlite;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using static AgGrade.Data.Database;
using static OpenCvSharp.FileStorage;

namespace AgGrade.Data
{
    public class Database
    {
        private SqliteConnection? _connection;
        private string? _databaseFilePath;
        private JournalOperationState? _activeJournalOperation;
        private bool _possibleDataLoss;
        private readonly object _operationSync = new object();

        private const int JOURNAL_HEADER_SIZE = 32;
        private const int JOURNAL_RECORD_SIZE = 32;
        private const int JOURNAL_MAGIC = 0x4A4C4F50; // "JLOP"
        private const int JOURNAL_VERSION = 1;

        /// <summary>
        /// Number of journal records appended between mandatory mid-operation flushes.
        /// </summary>
        public int JournalFlushRecords { get; set; } = 32;

        /// <summary>
        /// True when the database detects unresolved/rejected journal state and potential data loss.
        /// </summary>
        public bool PossibleDataLoss => _possibleDataLoss;

        /// <summary>
        /// Raised when PossibleDataLoss transitions from false to true.
        /// </summary>
        public event EventHandler? PossibleDataLossChanged;

        /// <summary>
        /// Supported operation types for leveling history.
        /// </summary>
        public enum LevelingOperationType
        {
            CUT,
            FILL
        }

        /// <summary>
        /// Represents one bin delta in a leveling operation.
        /// </summary>
        public class LevelingOperationBinDelta
        {
            public int X { get; set; }
            public int Y { get; set; }
            public double DeltaHeightM { get; set; }
        }

        /// <summary>
        /// In-memory state for an active journaled leveling operation.
        /// </summary>
        private sealed class JournalOperationState
        {
            public int OperationId { get; set; }
            public LevelingOperationType OperationType { get; set; }
            public long StartedAtMs { get; set; }
            public string JournalPath { get; set; } = string.Empty;
            public FileStream Stream { get; set; } = null!;
            public int Sequence { get; set; }
            public int SinceFlushCount { get; set; }
            public List<LevelingOperationBinDelta> Deltas { get; } = new List<LevelingOperationBinDelta>();
        }

        /// <summary>Row from Events table.</summary>
        public class Event
        {
            public enum EventTypes
            {
                Speedkph,
                Heading,
                FrontBladeHeight,
                RearBladeHeight,
                TractorLat,
                TractorLon,
                FrontScraperLat,
                FrontScraperLon,
                RearScraperLat,
                RearScraperLon
            }

            public int EventID { get; set; }
            public EventTypes Type { get; set; }
            public double Value { get; set; }
            /// <summary>Tenths of seconds since 1970-01-01 00:00:00 UTC.</summary>
            public long Timestamp { get; set; }

            /// <summary>Creates an event with Timestamp set to current time in GMT.</summary>
            public Event
                (
                )
            {
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            }

            /// <summary>Creates an event with the given type and value; Timestamp is set to current time in GMT.</summary>
            public Event
                (
                EventTypes Type,
                double Value
                )
            {
                this.Type = Type;
                this.Value = Value;
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            }
        }

        /// <summary>
        /// Row from the benchmark table
        /// </summary>
        public class BenchMark
        {
            public int BenchMarkID { get; set; }
            public double Latitude { get; set; }
            public double Longitude { get; set; }
            public string Name { get; set; }
            public double ElevationM { get; set; }

            public BenchMark() { }

            public BenchMark
                (
                double Latitude,
                double Longitude,
                string Name,
                double ElevationM
                )
            {
                this.Latitude = Latitude;
                this.Longitude = Longitude;
                this.Name = Name;
                this.ElevationM = ElevationM;
            }
        }

        /// <summary>
        /// Row from HaulArrows table
        /// </summary>
        public class HaulArrow
        {
            public int ArrowID { get; set; }
            public double Latitude { get; set; }
            public double Longitude { get; set; }
            public double Heading { get; set; }

            public HaulArrow() { }

            public HaulArrow
                (
                double Latitude,
                double Longitude,
                double Heading
                )
            {
                this.Latitude = Latitude;
                this.Longitude = Longitude;
                this.Heading = Heading;
            }
        }

        /// <summary>Row from FieldState table (one per bin).</summary>
        public class BinState
        {
            public int BinID { get; set; }
            public int X { get; set; }
            public int Y { get; set; }
            public double InitialHeightM { get; set; }
            public double CurrentHeightM { get; set; }
            public double TargetHeightM { get; set; }
            public double CentroidLat { get; set; }
            public double CentroidLon { get; set; }
            public int HaulPath {  get; set; }

            /// <summary>Creates an empty bin state.</summary>
            public BinState() { }

            /// <summary>Creates a bin state with the given coordinates and height. BinID is assigned by the database on insert.</summary>
            public BinState
                (
                int x,
                int y,
                double initialHeightM,
                double currentHeightM,
                double targetHeightM,
                double centroidLat,
                double centroidLon,
                int haulPath
                )
            {
                X = x;
                Y = y;
                InitialHeightM = initialHeightM;
                CurrentHeightM = currentHeightM;
                TargetHeightM = targetHeightM;
                CentroidLat = centroidLat;
                CentroidLon = centroidLon;
                HaulPath = haulPath;
            }
        }

        /// <summary>
        /// Names of supported data
        /// </summary>
        public enum DataNames
        {
            EastingOffsetM,
            NorthingOffsetM,
            MeanLat,
            MeanLon,
            HeightOffsetM,
            CompletedCutCY,
            CompletedFillCY,
            GridWidth,
            GridHeight,
            MinLat,
            MinLon,
            MaxLat,
            MaxLon,
            Calibrated
        }

        /// <summary>
        /// Opens an existing database file for use. The connection is kept open until Close or Dispose.
        /// </summary>
        /// <param name="FileName">Path and name of database file to open</param>
        public void Open
            (
            string FileName
            )
        {
            _connection?.Dispose();
            _databaseFilePath = FileName;
            _connection = new SqliteConnection($"Data Source={FileName}");
            _connection.Open();
            ApplyHistoryWritePragmas();
            EnsureHaulPathsIndex();
            RecoverFromJournal();
        }

        /// <summary>
        /// Ensures HaulPaths has an index for WHERE HaulPath = ? ORDER BY PointNumber (for existing DBs created before the index existed).
        /// </summary>
        private void EnsureHaulPathsIndex()
        {
            if (_connection == null) return;
            using (var cmd = _connection.CreateCommand())
            {
                cmd.CommandText = "SELECT 1 FROM sqlite_master WHERE type='table' AND name='HaulPaths'";
                if (cmd.ExecuteScalar() == null) return;
                cmd.CommandText = "CREATE INDEX IF NOT EXISTS idx_haulpaths_haul_point ON HaulPaths (HaulPath, PointNumber)";
                cmd.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// Applies SQLite PRAGMA settings used by the leveling history write path.
        /// </summary>
        private void ApplyHistoryWritePragmas
            (
            )
        {
            if (_connection == null) return;
            using (var cmd = _connection.CreateCommand())
            {
                cmd.CommandText = "PRAGMA journal_mode=WAL";
                cmd.ExecuteNonQuery();
                cmd.CommandText = "PRAGMA synchronous=FULL";
                cmd.ExecuteNonQuery();
                cmd.CommandText = "PRAGMA busy_timeout=5000";
                cmd.ExecuteNonQuery();
                cmd.CommandText = "PRAGMA wal_autocheckpoint=2000";
                cmd.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// Releases resources for an unfinished active operation if the database closes unexpectedly.
        /// </summary>
        private void CleanupActiveJournalOperation
            (
            )
        {
            if (_activeJournalOperation == null) return;
            try
            {
                _activeJournalOperation.Stream.Dispose();
            }
            catch
            {
                // best-effort cleanup only
            }
            _activeJournalOperation = null;
        }

        /// <summary>
        /// Sets PossibleDataLoss true and raises its transition event once.
        /// </summary>
        private void SetPossibleDataLossTrue
            (
            )
        {
            if (_possibleDataLoss) return;
            _possibleDataLoss = true;
            PossibleDataLossChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Gets the open connection. Returns null if the database has not been opened.
        /// </summary>
        public SqliteConnection? Connection => _connection;

        /// <summary>
        /// Closes the database connection if open.
        /// </summary>
        public void Close()
        {
            CleanupActiveJournalOperation();
            _connection?.Dispose();
            _connection = null;
        }

        /// <summary>
        /// Inserts an event into the Events table. EventID is assigned by the database.
        /// </summary>
        public void AddEvent(Event evt)
        {
            try
            {
                if (_connection == null) throw new InvalidOperationException("Database is not open.");
                using (var cmd = _connection.CreateCommand())
                {
                    cmd.CommandText = "INSERT INTO Events (Type, Value, Timestamp) VALUES (@Type, @Value, @Timestamp)";
                    cmd.Parameters.AddWithValue("@Type", evt.Type.ToString());
                    cmd.Parameters.AddWithValue("@Value", evt.Value);
                    cmd.Parameters.AddWithValue("@Timestamp", evt.Timestamp);
                    cmd.ExecuteNonQuery();
                }
            }
            catch (IndexOutOfRangeException)
            {
                // ignore
            }
            catch (ArgumentOutOfRangeException)
            {
                // ignore
            }
            catch (InvalidOperationException)
            {
                // ignore
            }
        }

        /// <summary>
        /// Returns the bin state for the given X and Y, or null if no row exists.
        /// </summary>
        public BinState? GetBinState(int x, int y)
        {
            if (_connection == null) throw new InvalidOperationException("Database is not open.");
            using (var cmd = _connection.CreateCommand())
            {
                cmd.CommandText = "SELECT BinID, X, Y, InitialHeightM, CurrentHeightM, TargetHeightM, CentroidLat, CentroidLon, HaulPath FROM FieldState WHERE X = @X AND Y = @Y";
                cmd.Parameters.AddWithValue("@X", x);
                cmd.Parameters.AddWithValue("@Y", y);
                using (var reader = cmd.ExecuteReader())
                {
                    if (!reader.Read()) return null;
                    return new BinState
                    {
                        BinID = reader.GetInt32(0),
                        X = reader.GetInt32(1),
                        Y = reader.GetInt32(2),
                        InitialHeightM = reader.GetDouble(3),
                        CurrentHeightM = reader.GetDouble(3),
                        TargetHeightM = reader.GetDouble(3),
                        CentroidLat = reader.GetDouble(4),
                        CentroidLon = reader.GetDouble(5),
                        HaulPath = reader.GetInt32(6)
                    };
                }
            }
        }

        /// <summary>
        /// Updates the height for the bin at the given X and Y.
        /// </summary>
        /// <param name="x">Bin X coordinate</param>
        /// <param name="y">Bin Y coordinate</param>
        /// <param name="heightM">New height in meters</param>
        public void UpdateBinState(int x, int y, double currentHeightM)
        {
            if (_connection == null) throw new InvalidOperationException("Database is not open.");
            using (var cmd = _connection.CreateCommand())
            {
                cmd.CommandText = "UPDATE FieldState SET CurrentHeightM = @CurrentHeightM WHERE X = @X AND Y = @Y";
                cmd.Parameters.AddWithValue("@X", x);
                cmd.Parameters.AddWithValue("@Y", y);
                cmd.Parameters.AddWithValue("@CurrentHeightM", currentHeightM);
                cmd.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// Inserts a bin row into the FieldState table with the given coordinates and height.
        /// </summary>
        /// <param name="x">Bin X coordinate</param>
        /// <param name="y">Bin Y coordinate</param>
        /// <param name="heightM">Height in meters</param>
        public void AddBinState
            (
            int x,
            int y,
            double initialHeightM,
            double currentHeightM,
            double targetHeightM,
            double centroidLat,
            double centroidLon,
            int haulPath
            )
        {
            if (_connection == null) throw new InvalidOperationException("Database is not open.");
            using (var cmd = _connection.CreateCommand())
            {
                cmd.CommandText = "INSERT INTO FieldState (X, Y, InitialHeightM, CurrentHeightM, TargetHeightM, CentroidLat, CentroidLon, HaulPath) VALUES (@X, @Y, @InitialHeightM, @CurrentHeightM, @TargetHeightM, @CentroidLat, @CentroidLon, @HaulPath)";
                cmd.Parameters.AddWithValue("@X", x);
                cmd.Parameters.AddWithValue("@Y", y);
                cmd.Parameters.AddWithValue("@InitialHeightM", initialHeightM);
                cmd.Parameters.AddWithValue("@CurrentHeightM", currentHeightM);
                cmd.Parameters.AddWithValue("@TargetHeightM", targetHeightM);
                cmd.Parameters.AddWithValue("@CentroidLat", centroidLat);
                cmd.Parameters.AddWithValue("@CentroidLon", centroidLon);
                cmd.Parameters.AddWithValue("@HaulPath", haulPath);
                cmd.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// Inserts a bin row into the FieldState table.
        /// </summary>
        public void AddBinState(BinState binState)
        {
            if (_connection == null) throw new InvalidOperationException("Database is not open.");
            using (var cmd = _connection.CreateCommand())
            {
                cmd.CommandText = "INSERT INTO FieldState (X, Y, InitialHeightM, CurrentHeightM, TargetHeightM, CentroidLat, CentroidLon, HaulPath) VALUES (@X, @Y, @InitialHeightM, @CurrentHeightM, @TargetHeightM, @CentroidLat, @CentroidLon, @HaulPath)";
                cmd.Parameters.AddWithValue("@X", binState.X);
                cmd.Parameters.AddWithValue("@Y", binState.Y);
                cmd.Parameters.AddWithValue("@InitialHeightM", binState.InitialHeightM);
                cmd.Parameters.AddWithValue("@CurrentHeightM", binState.CurrentHeightM);
                cmd.Parameters.AddWithValue("@TargetHeightM", binState.TargetHeightM);
                cmd.Parameters.AddWithValue("@CentroidLat", binState.CentroidLat);
                cmd.Parameters.AddWithValue("@CentroidLon", binState.CentroidLon);
                cmd.Parameters.AddWithValue("@HaulPath", binState.HaulPath);
                cmd.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// Gets the height of every bin in the FieldState table.
        /// </summary>
        /// <returns>Array of bin states</returns>
        public BinState[] GetBinStates()
        {
            if (_connection == null) throw new InvalidOperationException("Database is not open.");

            List<BinState> States = new List<BinState>();

            using (var cmd = _connection.CreateCommand())
            {
                cmd.CommandText = "SELECT X, Y, InitialHeightM, CurrentHeightM, TargetHeightM, CentroidLat, CentroidLon, HaulPath FROM FieldState";
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        States.Add(new BinState(
                            reader.GetInt32(0),
                            reader.GetInt32(1),
                            reader.GetDouble(2),
                            reader.GetDouble(3),
                            reader.GetDouble(4),
                            reader.GetDouble(5),
                            reader.GetDouble(6),
                            reader.GetInt32(7)
                        ));
                    }
                }
            }

            return States.ToArray();
        }

        /// <summary>
        /// Gets the benchmarks from the database
        /// </summary>
        /// <returns>Array of benchmarks</returns>
        public BenchMark[] GetBenchMarks()
        {
            if (_connection == null) throw new InvalidOperationException("Database is not open.");

            List<BenchMark> BenchMarks = new List<BenchMark>();

            using (var cmd = _connection.CreateCommand())
            {
                cmd.CommandText = "SELECT Latitude, Longitude, Name, ElevationM FROM BenchMarks";
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        BenchMarks.Add(new BenchMark(
                            reader.GetDouble(0),
                            reader.GetDouble(1),
                            reader.GetString(2),
                            reader.GetDouble(3)
                            ));
                    }
                }
            }

            return BenchMarks.ToArray();
        }

        /// <summary>
        /// Gets all of the haul arrows in the database
        /// </summary>
        /// <returns>List of haul arrows</returns>
        public HaulArrow[] GetHaulArrows()
        {
            if (_connection == null) throw new InvalidOperationException("Database is not open.");

            List<HaulArrow> Arrows = new List<HaulArrow>();

            using (var cmd = _connection.CreateCommand())
            {
                cmd.CommandText = "SELECT Latitude, Longitude, Heading FROM HaulArrows";
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        Arrows.Add(new HaulArrow(
                            reader.GetDouble(0),
                            reader.GetDouble(1),
                            reader.GetDouble(2)
                            ));
                    }
                }
            }

            return Arrows.ToArray();
        }

        /// <summary>
        /// Stores a set of bins into the database
        /// </summary>
        /// <param name="Bins">Bins to store</param>
        public void AddBinStates(List<Bin> Bins)
        {
            if (_connection == null) throw new InvalidOperationException("Database is not open.");
            using (var transaction = _connection.BeginTransaction())
            {
                using (var command = _connection.CreateCommand())
                {
                    // Create command and parameters
                    command.CommandText = "INSERT INTO FieldState (X, Y, InitialHeightM, CurrentHeightM, TargetHeightM, CentroidLat, CentroidLon, HaulPath) VALUES (@X, @Y, @InitialHeightM, @CurrentHeightM, @TargetHeightM, @CentroidLat, @CentroidLon, @HaulPath)";
                    var param1 = command.Parameters.Add("@X", SqliteType.Integer);
                    var param2 = command.Parameters.Add("@Y", SqliteType.Integer);
                    var param3 = command.Parameters.Add("@InitialHeightM", SqliteType.Real);
                    var param4 = command.Parameters.Add("@CurrentHeightM", SqliteType.Real);
                    var param5 = command.Parameters.Add("@TargetHeightM", SqliteType.Real);
                    var param6 = command.Parameters.AddWithValue("@CentroidLat", SqliteType.Real);
                    var param7 = command.Parameters.AddWithValue("@CentroidLon", SqliteType.Real);
                    var param8 = command.Parameters.Add("@HaulPath", SqliteType.Integer);

                    foreach (Bin b in Bins)
                    {
                        // For each row, only update parameter values
                        param1.Value = b.X;
                        param2.Value = b.Y;
                        param3.Value = b.InitialElevationM;
                        param4.Value = b.CurrentElevationM;
                        param5.Value = b.TargetElevationM;
                        param6.Value = b.Centroid.Latitude;
                        param7.Value = b.Centroid.Longitude;
                        param8.Value = b.HaulPath;
                        command.ExecuteNonQuery();
                    }
                }

                transaction.Commit();
            }
        }

        /// <summary>
        /// Starts a journaled leveling operation and reserves the next OperationId.
        /// </summary>
        public int BeginLevelingOperation
            (
            LevelingOperationType operationType
            )
        {
            lock (_operationSync)
            {
                if (_connection == null) throw new InvalidOperationException("Database is not open.");
                if (_activeJournalOperation != null) throw new InvalidOperationException("A leveling operation is already active.");
                if (_databaseFilePath == null) throw new InvalidOperationException("Database path is not available.");

                string dbFolder = Path.GetDirectoryName(_databaseFilePath) ?? ".";
                string[] existingActiveJournals = Directory.GetFiles(dbFolder, "op_*.journal.active");
                if (existingActiveJournals.Length > 0)
                {
                    RecoverFromJournalInternal();
                    existingActiveJournals = Directory.GetFiles(dbFolder, "op_*.journal.active");
                    if (existingActiveJournals.Length > 0)
                    {
                        SetPossibleDataLossTrue();
                        throw new InvalidOperationException("Cannot start a new operation while an active journal already exists.");
                    }
                }

                int nextOperationId = GetNextLevelingOperationId();
                string journalPath = Path.Combine(dbFolder, $"op_{nextOperationId}.journal.active");
                try
                {
                    FileStream stream = new FileStream
                        (
                        journalPath,
                        FileMode.CreateNew,
                        FileAccess.ReadWrite,
                        FileShare.None,
                        4096,
                        FileOptions.WriteThrough
                        );
                    WriteJournalHeader(stream, nextOperationId, operationType);
                    _activeJournalOperation = new JournalOperationState
                    {
                        OperationId = nextOperationId,
                        OperationType = operationType,
                        StartedAtMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                        JournalPath = journalPath,
                        Stream = stream,
                        Sequence = 0,
                        SinceFlushCount = 0
                    };
                    return nextOperationId;
                }
                catch
                {
                    RenameJournalToError(journalPath);
                    SetPossibleDataLossTrue();
                    throw;
                }
            }
        }

        /// <summary>
        /// Adds one bin delta to the active journaled leveling operation.
        /// </summary>
        public void AddLevelingOperationBinDelta
            (
            int x,
            int y,
            double deltaHeightM
            )
        {
            lock (_operationSync)
            {
                if (_activeJournalOperation == null) throw new InvalidOperationException("No active leveling operation.");

                try
                {
                    _activeJournalOperation.Deltas.Add
                        (
                        new LevelingOperationBinDelta
                        {
                            X = x,
                            Y = y,
                            DeltaHeightM = deltaHeightM
                        }
                        );

                    _activeJournalOperation.Sequence++;
                    WriteJournalRecord
                        (
                        _activeJournalOperation.Stream,
                        _activeJournalOperation.Sequence,
                        x,
                        y,
                        deltaHeightM
                        );

                    _activeJournalOperation.SinceFlushCount++;
                    int flushRecords = Math.Max(1, JournalFlushRecords);
                    if (_activeJournalOperation.SinceFlushCount >= flushRecords)
                    {
                        _activeJournalOperation.Stream.Flush(true);
                        _activeJournalOperation.SinceFlushCount = 0;
                    }
                }
                catch
                {
                    HandleJournalWriteFailure();
                    throw;
                }
            }
        }

        /// <summary>
        /// Commits the active leveling operation to SQLite in one transaction and removes its journal file.
        /// </summary>
        public void CommitLevelingOperation
            (
            double completedCutCY,
            double completedFillCY
            )
        {
            lock (_operationSync)
            {
                if (_connection == null) throw new InvalidOperationException("Database is not open.");
                if (_activeJournalOperation == null) return;

                JournalOperationState op = _activeJournalOperation;
                _activeJournalOperation = null;

                try
                {
                    op.Stream.Flush(true);
                    long completedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                    PersistLevelingOperationInternal(op, completedAt, completedCutCY, completedFillCY);
                    op.Stream.Dispose();
                    if (File.Exists(op.JournalPath))
                    {
                        File.Delete(op.JournalPath);
                    }
                }
                catch
                {
                    try
                    {
                        op.Stream.Dispose();
                    }
                    catch
                    {
                        // best-effort cleanup only
                    }
                    RenameJournalToError(op.JournalPath);
                    SetPossibleDataLossTrue();
                    throw;
                }
            }
        }

        /// <summary>
        /// Recovers unresolved journal files at startup according to operation-id replay rules.
        /// </summary>
        public void RecoverFromJournal
            (
            )
        {
            lock (_operationSync)
            {
                RecoverFromJournalInternal();
            }
        }

        /// <summary>
        /// Internal startup recovery routine; caller controls synchronization.
        /// </summary>
        private void RecoverFromJournalInternal
            (
            )
        {
            if (_connection == null || _databaseFilePath == null) return;

            string dbFolder = Path.GetDirectoryName(_databaseFilePath) ?? ".";
            string quarantineFolder = Path.Combine(dbFolder, "journal-quarantine");
            if (!Directory.Exists(quarantineFolder))
            {
                Directory.CreateDirectory(quarantineFolder);
            }

            string[] allJournalFiles = Directory.GetFiles(dbFolder, "op_*.journal.*");
            if (allJournalFiles.Length == 0) return;

            int highestId = GetCurrentHighestOperationId();
            int expectedId = highestId + 1;

            List<string> activeFiles = allJournalFiles
                .Where(f => string.Equals(Path.GetExtension(f), ".active", StringComparison.OrdinalIgnoreCase))
                .OrderBy(f => TryParseOperationIdFromPath(f))
                .ToList();
            List<string> errorFiles = allJournalFiles
                .Where(f => string.Equals(Path.GetExtension(f), ".error", StringComparison.OrdinalIgnoreCase))
                .ToList();

            foreach (string errorFile in errorFiles)
            {
                SetPossibleDataLossTrue();
                MoveToQuarantine(errorFile, quarantineFolder);
            }

            string? replayFile = activeFiles.FirstOrDefault(f => TryParseOperationIdFromPath(f) == expectedId);

            foreach (string activeFile in activeFiles)
            {
                if (activeFile == replayFile) continue;
                SetPossibleDataLossTrue();
                MoveToQuarantine(activeFile, quarantineFolder);
            }

            if (replayFile == null) return;

            try
            {
                bool replayed = ReplayJournalFile(replayFile);
                if (replayed && File.Exists(replayFile))
                {
                    File.Delete(replayFile);
                }
                else if (!replayed)
                {
                    SetPossibleDataLossTrue();
                    MoveToQuarantine(replayFile, quarantineFolder);
                }
            }
            catch
            {
                SetPossibleDataLossTrue();
                MoveToQuarantine(replayFile, quarantineFolder);
            }
        }

        /// <summary>
        /// Gets the next operation id to be used for journaled operations.
        /// </summary>
        private int GetNextLevelingOperationId
            (
            )
        {
            if (_connection == null) throw new InvalidOperationException("Database is not open.");
            using (var cmd = _connection.CreateCommand())
            {
                cmd.CommandText = "SELECT IFNULL(MAX(OperationId), 0) + 1 FROM LevelingOperation";
                object? value = cmd.ExecuteScalar();
                return Convert.ToInt32(value ?? 1, CultureInfo.InvariantCulture);
            }
        }

        /// <summary>
        /// Gets the current highest persisted operation id.
        /// </summary>
        private int GetCurrentHighestOperationId
            (
            )
        {
            if (_connection == null) throw new InvalidOperationException("Database is not open.");
            using (var cmd = _connection.CreateCommand())
            {
                cmd.CommandText = "SELECT IFNULL(MAX(OperationId), 0) FROM LevelingOperation";
                object? value = cmd.ExecuteScalar();
                return Convert.ToInt32(value ?? 0, CultureInfo.InvariantCulture);
            }
        }

        /// <summary>
        /// Writes the fixed-size journal header.
        /// </summary>
        private void WriteJournalHeader
            (
            FileStream stream,
            int operationId,
            LevelingOperationType operationType
            )
        {
            long startedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            Span<byte> header = stackalloc byte[JOURNAL_HEADER_SIZE];
            BinaryPrimitives.WriteInt32LittleEndian(header.Slice(0, 4), JOURNAL_MAGIC);
            BinaryPrimitives.WriteInt32LittleEndian(header.Slice(4, 4), JOURNAL_VERSION);
            BinaryPrimitives.WriteInt32LittleEndian(header.Slice(8, 4), operationId);
            BinaryPrimitives.WriteInt32LittleEndian(header.Slice(12, 4), (int)operationType);
            BinaryPrimitives.WriteInt64LittleEndian(header.Slice(16, 8), startedAt);
            uint crc = ComputeCrc32(header.Slice(0, 28));
            BinaryPrimitives.WriteUInt32LittleEndian(header.Slice(28, 4), crc);
            stream.Write(header);
            stream.Flush(true);
        }

        /// <summary>
        /// Writes one fixed-size journal record.
        /// </summary>
        private void WriteJournalRecord
            (
            FileStream stream,
            int seq,
            int x,
            int y,
            double deltaHeightM
            )
        {
            long timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            Span<byte> record = stackalloc byte[JOURNAL_RECORD_SIZE];
            BinaryPrimitives.WriteInt32LittleEndian(record.Slice(0, 4), seq);
            BinaryPrimitives.WriteInt32LittleEndian(record.Slice(4, 4), x);
            BinaryPrimitives.WriteInt32LittleEndian(record.Slice(8, 4), y);
            BinaryPrimitives.WriteDoubleLittleEndian(record.Slice(12, 8), deltaHeightM);
            BinaryPrimitives.WriteInt64LittleEndian(record.Slice(20, 8), timestamp);
            uint crc = ComputeCrc32(record.Slice(0, 28));
            BinaryPrimitives.WriteUInt32LittleEndian(record.Slice(28, 4), crc);
            stream.Write(record);
        }

        /// <summary>
        /// Handles journal-write failures by marking data-loss and renaming the active journal.
        /// </summary>
        private void HandleJournalWriteFailure
            (
            )
        {
            if (_activeJournalOperation == null) return;

            string journalPath = _activeJournalOperation.JournalPath;
            try
            {
                _activeJournalOperation.Stream.Dispose();
            }
            catch
            {
                // best-effort cleanup only
            }

            try
            {
                RenameJournalToError(journalPath);
            }
            catch
            {
                // best-effort rename only
            }
            finally
            {
                _activeJournalOperation = null;
                SetPossibleDataLossTrue();
            }
        }

        /// <summary>
        /// Renames an active journal file to .error.
        /// </summary>
        private static void RenameJournalToError
            (
            string journalPath
            )
        {
            string errorPath = Path.ChangeExtension(journalPath, ".error");
            if (!File.Exists(journalPath)) return;
            if (File.Exists(errorPath))
            {
                File.Delete(errorPath);
            }
            File.Move(journalPath, errorPath);
        }

        /// <summary>
        /// Writes one leveling operation and all deltas in a single SQLite transaction.
        /// </summary>
        private void PersistLevelingOperationInternal
            (
            JournalOperationState operation,
            long completedAtMs,
            double completedCutCY,
            double completedFillCY
            )
        {
            if (_connection == null) throw new InvalidOperationException("Database is not open.");

            using (SqliteTransaction transaction = _connection.BeginTransaction())
            {
                using (SqliteCommand opCmd = _connection.CreateCommand())
                {
                    opCmd.Transaction = transaction;
                    opCmd.CommandText = "INSERT INTO LevelingOperation (OperationId, OperationType, StartedAtMs, CompletedAtMs) VALUES (@OperationId, @OperationType, @StartedAtMs, @CompletedAtMs)";
                    opCmd.Parameters.AddWithValue("@OperationId", operation.OperationId);
                    opCmd.Parameters.AddWithValue("@OperationType", operation.OperationType.ToString());
                    opCmd.Parameters.AddWithValue("@StartedAtMs", operation.StartedAtMs);
                    opCmd.Parameters.AddWithValue("@CompletedAtMs", completedAtMs);
                    opCmd.ExecuteNonQuery();
                }

                using (SqliteCommand insertDeltaCmd = _connection.CreateCommand())
                {
                    insertDeltaCmd.Transaction = transaction;
                    insertDeltaCmd.CommandText = "INSERT INTO LevelingOperationBin (OperationId, X, Y, DeltaHeightM) VALUES (@OperationId, @X, @Y, @DeltaHeightM)";
                    SqliteParameter pOperationId = insertDeltaCmd.Parameters.Add("@OperationId", SqliteType.Integer);
                    SqliteParameter pX = insertDeltaCmd.Parameters.Add("@X", SqliteType.Integer);
                    SqliteParameter pY = insertDeltaCmd.Parameters.Add("@Y", SqliteType.Integer);
                    SqliteParameter pDelta = insertDeltaCmd.Parameters.Add("@DeltaHeightM", SqliteType.Real);
                    foreach (LevelingOperationBinDelta delta in operation.Deltas)
                    {
                        pOperationId.Value = operation.OperationId;
                        pX.Value = delta.X;
                        pY.Value = delta.Y;
                        pDelta.Value = delta.DeltaHeightM;
                        insertDeltaCmd.ExecuteNonQuery();
                    }
                }

                using (SqliteCommand updateStateCmd = _connection.CreateCommand())
                {
                    updateStateCmd.Transaction = transaction;
                    updateStateCmd.CommandText = "UPDATE FieldState SET CurrentHeightM = CurrentHeightM + @DeltaHeightM WHERE X = @X AND Y = @Y";
                    SqliteParameter pDelta = updateStateCmd.Parameters.Add("@DeltaHeightM", SqliteType.Real);
                    SqliteParameter pX = updateStateCmd.Parameters.Add("@X", SqliteType.Integer);
                    SqliteParameter pY = updateStateCmd.Parameters.Add("@Y", SqliteType.Integer);
                    foreach (LevelingOperationBinDelta delta in operation.Deltas)
                    {
                        pDelta.Value = delta.DeltaHeightM;
                        pX.Value = delta.X;
                        pY.Value = delta.Y;
                        updateStateCmd.ExecuteNonQuery();
                    }
                }

                UpsertDataInTransaction(transaction, DataNames.CompletedCutCY, completedCutCY);
                UpsertDataInTransaction(transaction, DataNames.CompletedFillCY, completedFillCY);
                transaction.Commit();
            }
        }

        /// <summary>
        /// Upserts one data key/value pair within an existing transaction.
        /// </summary>
        private void UpsertDataInTransaction
            (
            SqliteTransaction transaction,
            DataNames name,
            double value
            )
        {
            if (_connection == null) throw new InvalidOperationException("Database is not open.");
            using (SqliteCommand cmd = _connection.CreateCommand())
            {
                cmd.Transaction = transaction;
                cmd.CommandText = "UPDATE Data SET Value = @Value WHERE Name = @Name";
                cmd.Parameters.AddWithValue("@Name", name.ToString());
                cmd.Parameters.AddWithValue("@Value", value);
                int rows = cmd.ExecuteNonQuery();
                if (rows == 0)
                {
                    cmd.CommandText = "INSERT INTO Data (Name, Value) VALUES (@Name, @Value)";
                    cmd.ExecuteNonQuery();
                }
            }
        }

        /// <summary>
        /// Replays one active journal file when its operation id is exactly the next expected id.
        /// </summary>
        private bool ReplayJournalFile
            (
            string journalPath
            )
        {
            if (_connection == null) throw new InvalidOperationException("Database is not open.");

            ReadJournalPayload payload = ReadJournal(journalPath);
            int highest = GetCurrentHighestOperationId();
            if (payload.OperationId != highest + 1)
            {
                SetPossibleDataLossTrue();
                return false;
            }

            JournalOperationState op = new JournalOperationState
            {
                OperationId = payload.OperationId,
                OperationType = payload.OperationType,
                StartedAtMs = payload.StartedAtMs,
                JournalPath = journalPath,
                Stream = File.OpenRead(journalPath)
            };
            op.Deltas.AddRange(payload.Deltas);

            double cutDeltaM = payload.Deltas.Where(d => d.DeltaHeightM < 0).Sum(d => -d.DeltaHeightM);
            double fillDeltaM = payload.Deltas.Where(d => d.DeltaHeightM > 0).Sum(d => d.DeltaHeightM);
            const double BIN_SIZE_M = 0.6096;
            const double CUBIC_YARDS_PER_CUBIC_METER = 1.30795061931439;
            double opCutCY = BIN_SIZE_M * BIN_SIZE_M * cutDeltaM * CUBIC_YARDS_PER_CUBIC_METER;
            double opFillCY = (BIN_SIZE_M * BIN_SIZE_M * fillDeltaM * CUBIC_YARDS_PER_CUBIC_METER) / Field.CUT_FILL_RATIO;
            double completedCutCY = GetData(DataNames.CompletedCutCY) + opCutCY;
            double completedFillCY = GetData(DataNames.CompletedFillCY) + opFillCY;
            long completedAtMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            PersistLevelingOperationInternal(op, completedAtMs, completedCutCY, completedFillCY);
            op.Stream.Dispose();
            return true;
        }

        /// <summary>
        /// Moves one journal file to quarantine without overwriting an existing file.
        /// </summary>
        private static void MoveToQuarantine
            (
            string filePath,
            string quarantineFolder
            )
        {
            if (!File.Exists(filePath)) return;
            string fileName = Path.GetFileName(filePath);
            string destinationPath = Path.Combine(quarantineFolder, fileName);
            if (File.Exists(destinationPath))
            {
                string stamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString(CultureInfo.InvariantCulture);
                destinationPath = Path.Combine(quarantineFolder, $"{stamp}_{fileName}");
            }
            File.Move(filePath, destinationPath);
        }

        /// <summary>
        /// Extracts the operation id from a journal filename.
        /// </summary>
        private static int TryParseOperationIdFromPath
            (
            string filePath
            )
        {
            string fileName = Path.GetFileName(filePath);
            if (!fileName.StartsWith("op_", StringComparison.OrdinalIgnoreCase)) return int.MinValue;
            int underscore = fileName.IndexOf('_');
            int dot = fileName.IndexOf('.');
            if (underscore < 0 || dot < 0 || dot <= underscore + 1) return int.MinValue;
            string idPart = fileName.Substring(underscore + 1, dot - underscore - 1);
            return int.TryParse(idPart, NumberStyles.Integer, CultureInfo.InvariantCulture, out int id) ? id : int.MinValue;
        }

        /// <summary>
        /// Deserializes a journal file and validates fixed-size header/records with CRC.
        /// </summary>
        private ReadJournalPayload ReadJournal
            (
            string journalPath
            )
        {
            using (FileStream stream = File.OpenRead(journalPath))
            {
                byte[] header = new byte[JOURNAL_HEADER_SIZE];
                int readHeader = stream.Read(header, 0, JOURNAL_HEADER_SIZE);
                if (readHeader != JOURNAL_HEADER_SIZE) throw new InvalidDataException("Journal header is incomplete.");

                int magic = BinaryPrimitives.ReadInt32LittleEndian(header.AsSpan(0, 4));
                int version = BinaryPrimitives.ReadInt32LittleEndian(header.AsSpan(4, 4));
                int operationId = BinaryPrimitives.ReadInt32LittleEndian(header.AsSpan(8, 4));
                int operationTypeRaw = BinaryPrimitives.ReadInt32LittleEndian(header.AsSpan(12, 4));
                long startedAtMs = BinaryPrimitives.ReadInt64LittleEndian(header.AsSpan(16, 8));
                uint storedHeaderCrc = BinaryPrimitives.ReadUInt32LittleEndian(header.AsSpan(28, 4));
                uint computedHeaderCrc = ComputeCrc32(header.AsSpan(0, 28));
                if (magic != JOURNAL_MAGIC || version != JOURNAL_VERSION || storedHeaderCrc != computedHeaderCrc)
                {
                    throw new InvalidDataException("Journal header CRC or version is invalid.");
                }

                List<LevelingOperationBinDelta> deltas = new List<LevelingOperationBinDelta>();
                byte[] record = new byte[JOURNAL_RECORD_SIZE];
                while (true)
                {
                    int bytesRead = stream.Read(record, 0, JOURNAL_RECORD_SIZE);
                    if (bytesRead == 0) break;
                    if (bytesRead != JOURNAL_RECORD_SIZE) throw new InvalidDataException("Journal record is incomplete.");

                    uint storedCrc = BinaryPrimitives.ReadUInt32LittleEndian(record.AsSpan(28, 4));
                    uint computedCrc = ComputeCrc32(record.AsSpan(0, 28));
                    if (storedCrc != computedCrc) throw new InvalidDataException("Journal record CRC is invalid.");

                    int x = BinaryPrimitives.ReadInt32LittleEndian(record.AsSpan(4, 4));
                    int y = BinaryPrimitives.ReadInt32LittleEndian(record.AsSpan(8, 4));
                    double delta = BinaryPrimitives.ReadDoubleLittleEndian(record.AsSpan(12, 8));
                    deltas.Add(new LevelingOperationBinDelta { X = x, Y = y, DeltaHeightM = delta });
                }

                return new ReadJournalPayload
                {
                    OperationId = operationId,
                    OperationType = (LevelingOperationType)operationTypeRaw,
                    StartedAtMs = startedAtMs,
                    Deltas = deltas
                };
            }
        }

        /// <summary>
        /// CRC32 helper used by fixed-size journal header/records.
        /// </summary>
        private static uint ComputeCrc32
            (
            ReadOnlySpan<byte> bytes
            )
        {
            uint crc = 0xFFFFFFFFu;
            for (int i = 0; i < bytes.Length; i++)
            {
                crc ^= bytes[i];
                for (int bit = 0; bit < 8; bit++)
                {
                    bool lsb = (crc & 1u) != 0;
                    crc >>= 1;
                    if (lsb) crc ^= 0xEDB88320u;
                }
            }
            return ~crc;
        }

        /// <summary>
        /// Materialized payload from one parsed journal file.
        /// </summary>
        private sealed class ReadJournalPayload
        {
            public int OperationId { get; set; }
            public LevelingOperationType OperationType { get; set; }
            public long StartedAtMs { get; set; }
            public List<LevelingOperationBinDelta> Deltas { get; set; } = new List<LevelingOperationBinDelta>();
        }

        /// <summary>
        /// Upserts a data item
        /// </summary>
        /// <param name="Name">Data item to upsert</param>
        /// <param name="Value">New value</param>
        public void SetData
            (
            DataNames Name,
            double Value
            )
        {
            if (_connection == null) throw new InvalidOperationException("Database is not open.");
            using (var cmd = _connection.CreateCommand())
            {
                try
                {
                    // First try to update an existing setting
                    cmd.CommandText = "UPDATE Data SET Value = @Value WHERE Name = @Name";
                    cmd.Parameters.AddWithValue("@Name", Name.ToString());
                    cmd.Parameters.AddWithValue("@Value", Value);
                    var rows = cmd.ExecuteNonQuery();

                    // If no row was updated, insert a new one
                    if (rows == 0)
                    {
                        cmd.CommandText = "INSERT INTO Data (Name, Value) VALUES (@Name, @Value)";
                        cmd.ExecuteNonQuery();
                    }
                }
                catch (SqliteException)
                {
                    cmd.CommandText = "INSERT INTO Data (Name, Value) VALUES (@Name, @Value)";
                    cmd.Parameters.AddWithValue("@Name", Name.ToString());
                    cmd.Parameters.AddWithValue("@Value", Value);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        /// <summary>
        /// Upserts a boolean data item
        /// </summary>
        /// <param name="Name">Data item to upsert</param>
        /// <param name="Value">Boolean value</param>
        public void SetBoolData
            (
            DataNames Name,
            bool Value
            )
        {
            if (_connection == null) throw new InvalidOperationException("Database is not open.");
            using (var cmd = _connection.CreateCommand())
            {
                try
                {
                    // First try to update an existing setting
                    cmd.CommandText = "UPDATE Data SET Value = @Value WHERE Name = @Name";
                    cmd.Parameters.AddWithValue("@Name", Name.ToString());
                    cmd.Parameters.AddWithValue("@Value", Value);
                    var rows = cmd.ExecuteNonQuery();

                    // If no row was updated, insert a new one
                    if (rows == 0)
                    {
                        cmd.CommandText = "INSERT INTO Data (Name, Value) VALUES (@Name, @Value)";
                        cmd.ExecuteNonQuery();
                    }
                }
                catch (SqliteException)
                {
                    cmd.CommandText = "INSERT INTO Data (Name, Value) VALUES (@Name, @Value)";
                    cmd.Parameters.AddWithValue("@Name", Name.ToString());
                    cmd.Parameters.AddWithValue("@Value", Value);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        /// <summary>
        /// Gets the value of a data item
        /// </summary>
        /// <param name="name">Data to get</param>
        public double GetData(DataNames name)
        {
            if (_connection == null) throw new InvalidOperationException("Database is not open.");

            using (var cmd = _connection.CreateCommand())
            {
                try
                {
                    cmd.CommandText = "SELECT Value FROM Data WHERE Name = @Name LIMIT 1";
                    cmd.Parameters.AddWithValue("@Name", name.ToString());
                    var result = cmd.ExecuteScalar();

                    if (result == null || result is DBNull) return 0;

                    return Convert.ToDouble(result);
                }
                catch (SqliteException)
                {
                    return 0;
                }
            }
        }

        /// <summary>
        /// Gets the value of a boolean data item
        /// </summary>
        /// <param name="name">Data to get</param>
        /// <returns>boolean value</returns>
        public bool GetBoolData(DataNames name)
        {
            if (_connection == null) throw new InvalidOperationException("Database is not open.");

            using (var cmd = _connection.CreateCommand())
            {
                try
                {
                    cmd.CommandText = "SELECT Value FROM Data WHERE Name = @Name LIMIT 1";
                    cmd.Parameters.AddWithValue("@Name", name.ToString());
                    var result = cmd.ExecuteScalar();

                    if (result == null || result is DBNull) return false;

                    return Convert.ToBoolean(result);
                }
                catch (SqliteException)
                {
                    return false;
                }
            }
        }

        /// <summary>
        /// Gets the path to haul soil
        /// </summary>
        /// <param name="StartBin">The starting bin for the haul</param>
        /// <returns>The haul path or empty list for no path</returns>
        public List<Coordinate> GetHaulPath
            (
            Bin StartBin
            )
        {
            // if not cutting then there won't be a haul path
            if (StartBin.CurrentElevationM <= StartBin.TargetElevationM)
            {
                return new List<Coordinate>();
            }

            // if no haul path then nothing to get
            if (StartBin.HaulPath == 0)
            {
                return new List<Coordinate>();
            }

            if (_connection == null) throw new InvalidOperationException("Database is not open.");

            List<Coordinate> Path = new List<Coordinate>();

            using (var cmd = _connection.CreateCommand())
            {
                try
                {
                    cmd.CommandText = "SELECT Latitude, Longitude FROM HaulPaths WHERE HaulPath = @HaulPath ORDER BY PointNumber";
                    cmd.Parameters.AddWithValue("@HaulPath", StartBin.HaulPath.ToString());
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            Path.Add(new Coordinate(reader.GetDouble(0), reader.GetDouble(1)));
                        }
                    }

                    return Path;
                }
                catch (SqliteException)
                {
                    return new List<Coordinate>();
                }
            }
        }
    }
}
