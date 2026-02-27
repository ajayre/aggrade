using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static AgGrade.Data.Database;

namespace AgGrade.Data
{
    public class Database
    {
        private SqliteConnection? _connection;

        /// <summary>Row from Events table.</summary>
        public class Event
        {
            public enum EventTypes
            {
                Speed,
                Heading,
                FrontBladeHeight,
                RearBladeHeight,
                TractorLocation,
                FrontScraperLocation,
                RearScraperLocation
            }

            public int EventID { get; set; }
            public EventTypes Type { get; set; }
            public string Details { get; set; }
            /// <summary>Tenths of seconds since 1970-01-01 00:00:00 UTC.</summary>
            public long Timestamp { get; set; }

            /// <summary>Creates an event with Timestamp set to current time in GMT.</summary>
            public Event
                (
                )
            {
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            }

            /// <summary>Creates an event with the given type and details; Timestamp is set to current time in GMT.</summary>
            public Event
                (
                EventTypes Type,
                string Details
                )
            {
                this.Type = Type;
                this.Details = Details ?? "";
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            }
        }

        /// <summary>Row from FieldState table (one per bin).</summary>
        public class BinState
        {
            public int BinID { get; set; }
            public int X { get; set; }
            public int Y { get; set; }
            public double HeightM { get; set; }

            /// <summary>Creates an empty bin state.</summary>
            public BinState() { }

            /// <summary>Creates a bin state with the given coordinates and height. BinID is assigned by the database on insert.</summary>
            public BinState
                (
                int x,
                int y,
                double heightM
                )
            {
                X = x;
                Y = y;
                HeightM = heightM;
            }
        }

        /// <summary>Row from BinHistory table.</summary>
        public class BinChange
        {
            public int BinHistoryID { get; set; }
            public int X { get; set; }
            public int Y { get; set; }
            public double HeightChangeM { get; set; }
            /// <summary>Milliseconds since 1970-01-01 00:00:00 UTC.</summary>
            public long Timestamp { get; set; }

            /// <summary>Creates a bin change with Timestamp set to current time in GMT.</summary>
            public BinChange
                (
                )
            {
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            }

            /// <summary>Creates a bin change with the given bin and height change; Timestamp is set to current time in GMT.</summary>
            public BinChange
                (
                int x,
                int y,
                double heightChangeM
                )
            {
                X = x;
                Y = y;
                HeightChangeM = heightChangeM;
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            }
        }

        /// <summary>Row from Settings table.</summary>
        public class Setting
        {
            public enum SettingNames
            {
                XOffsetM,
                YOffsetM,
                HeightOffsetM
            }

            public int SettingID { get; set; }
            public SettingNames Name { get; set; }
            public int Value { get; set; }

            /// <summary>Creates an empty setting.</summary>
            public Setting() { }

            /// <summary>Creates a setting with the given name and value. SettingID is assigned by the database on insert.</summary>
            public Setting
                (
                SettingNames name,
                int value
                )
            {
                Name = name;
                Value = value;
            }
        }

        /// <summary>
        /// Creates a new empty database file with the desired filename and opens it for subsequent access.
        /// </summary>
        /// <param name="FileName">Path and name of database file to create</param>
        public void Create(string FileName)
        {
            _connection?.Dispose();
            var connection = new SqliteConnection($"Data Source={FileName}");
            connection.Open();

            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = @"CREATE TABLE Events (
                        EventID INTEGER PRIMARY KEY AUTOINCREMENT,
                        Type TEXT,
                        Details TEXT,
                        Timestamp INTEGER
                    )";
                cmd.ExecuteNonQuery();
            }
            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = @"CREATE TABLE FieldState (
                        BinID INTEGER PRIMARY KEY AUTOINCREMENT,
                        X INTEGER,
                        Y INTEGER,
                        HeightM REAL
                    )";
                cmd.ExecuteNonQuery();
            }
            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = "CREATE UNIQUE INDEX idx_fieldstate_xy ON FieldState(X, Y)";
                cmd.ExecuteNonQuery();
            }
            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = @"CREATE TABLE BinHistory (
                        BinHistoryID INTEGER PRIMARY KEY AUTOINCREMENT,
                        X INTEGER,
                        Y INTEGER,
                        HeightChangeM REAL,
                        Timestamp INTEGER
                    )";
                cmd.ExecuteNonQuery();
            }
            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = @"CREATE TABLE Settings (
                        SettingID INTEGER PRIMARY KEY AUTOINCREMENT,
                        Name TEXT,
                        Value INTEGER
                    )";
                cmd.ExecuteNonQuery();
            }
            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = "PRAGMA user_version = 1";
                cmd.ExecuteNonQuery();
            }

            _connection = connection;
        }

        /// <summary>
        /// Opens an existing database file for use. The connection is kept open until Close or Dispose.
        /// </summary>
        /// <param name="FileName">Path and name of database file to open</param>
        public void Open(string FileName)
        {
            _connection?.Dispose();
            _connection = new SqliteConnection($"Data Source={FileName}");
            _connection.Open();
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
            _connection?.Dispose();
            _connection = null;
        }

        /// <summary>
        /// Inserts an event into the Events table. EventID is assigned by the database.
        /// </summary>
        public void AddEvent(Event evt)
        {
            if (_connection == null) throw new InvalidOperationException("Database is not open.");
            using (var cmd = _connection.CreateCommand())
            {
                cmd.CommandText = "INSERT INTO Events (Type, Details, Timestamp) VALUES (@Type, @Details, @Timestamp)";
                cmd.Parameters.AddWithValue("@Type", evt.Type.ToString());
                cmd.Parameters.AddWithValue("@Details", evt.Details ?? "");
                cmd.Parameters.AddWithValue("@Timestamp", evt.Timestamp);
                cmd.ExecuteNonQuery();
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
                cmd.CommandText = "SELECT BinID, X, Y, HeightM FROM FieldState WHERE X = @X AND Y = @Y";
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
                        HeightM = reader.GetDouble(3)
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
        public void UpdateBinState(int x, int y, double heightM)
        {
            if (_connection == null) throw new InvalidOperationException("Database is not open.");
            using (var cmd = _connection.CreateCommand())
            {
                cmd.CommandText = "UPDATE FieldState SET HeightM = @HeightM WHERE X = @X AND Y = @Y";
                cmd.Parameters.AddWithValue("@X", x);
                cmd.Parameters.AddWithValue("@Y", y);
                cmd.Parameters.AddWithValue("@HeightM", heightM);
                cmd.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// Inserts a bin row into the FieldState table with the given coordinates and height.
        /// </summary>
        /// <param name="x">Bin X coordinate</param>
        /// <param name="y">Bin Y coordinate</param>
        /// <param name="heightM">Height in meters</param>
        public void AddBinState(int x, int y, double heightM)
        {
            if (_connection == null) throw new InvalidOperationException("Database is not open.");
            using (var cmd = _connection.CreateCommand())
            {
                cmd.CommandText = "INSERT INTO FieldState (X, Y, HeightM) VALUES (@X, @Y, @HeightM)";
                cmd.Parameters.AddWithValue("@X", x);
                cmd.Parameters.AddWithValue("@Y", y);
                cmd.Parameters.AddWithValue("@HeightM", heightM);
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
                cmd.CommandText = "INSERT INTO FieldState (X, Y, HeightM) VALUES (@X, @Y, @HeightM)";
                cmd.Parameters.AddWithValue("@X", binState.X);
                cmd.Parameters.AddWithValue("@Y", binState.Y);
                cmd.Parameters.AddWithValue("@HeightM", binState.HeightM);
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
                cmd.CommandText = "SELECT X, Y, HeightM FROM FieldState";
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        States.Add(new BinState(
                            reader.GetInt32(0),
                            reader.GetInt32(1),
                            reader.GetDouble(2)
                        ));
                    }
                }
            }

            return States.ToArray();
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
                    command.CommandText = "INSERT INTO FieldState (X, Y, HeightM) VALUES (@X, @Y, @HeightM)";
                    var param1 = command.Parameters.Add("@X", SqliteType.Integer);
                    var param2 = command.Parameters.Add("@Y", SqliteType.Integer);
                    var param3 = command.Parameters.Add("@HeightM", SqliteType.Real);

                    foreach (Bin b in Bins)
                    {
                        // For each row, only update parameter values
                        param1.Value = b.X;
                        param2.Value = b.Y;
                        param3.Value = b.ExistingElevationM;
                        command.ExecuteNonQuery();
                    }
                }

                transaction.Commit();
            }
        }

        /// <summary>
        /// Inserts a row into the BinHistory table.
        /// </summary>
        /// <param name="X">Bin X coordinate</param>
        /// <param name="Y">Bin Y coordinate</param>
        /// <param name="HeightChangeM">Change in height in meters (negative = cut)</param>
        public void AddBinHistory
            (
            int x,
            int y,
            double HeightChangeM
            )
        {
            if (_connection == null) throw new InvalidOperationException("Database is not open.");

            long Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            using (var cmd = _connection.CreateCommand())
            {
                cmd.CommandText = "INSERT INTO BinHistory (X, Y, HeightChangeM, Timestamp) VALUES (@X, @Y, @HeightChangeM, @Timestamp)";
                cmd.Parameters.AddWithValue("@X", x);
                cmd.Parameters.AddWithValue("@Y", y);
                cmd.Parameters.AddWithValue("@HeightChangeM", HeightChangeM);
                cmd.Parameters.AddWithValue("@Timestamp", Timestamp);
                cmd.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// Inserts a row into the BinHistory table.
        /// </summary>
        public void AddBinHistory(BinChange binChange)
        {
            if (_connection == null) throw new InvalidOperationException("Database is not open.");
            using (var cmd = _connection.CreateCommand())
            {
                cmd.CommandText = "INSERT INTO BinHistory (X, Y, HeightChangeM, Timestamp) VALUES (@X, @Y, @HeightChangeM, @Timestamp)";
                cmd.Parameters.AddWithValue("@X", binChange.X);
                cmd.Parameters.AddWithValue("@Y", binChange.Y);
                cmd.Parameters.AddWithValue("@HeightChangeM", binChange.HeightChangeM);
                cmd.Parameters.AddWithValue("@Timestamp", binChange.Timestamp);
                cmd.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// Inserts a setting into the Settings table.
        /// </summary>
        public void AddSetting(Setting setting)
        {
            if (_connection == null) throw new InvalidOperationException("Database is not open.");
            using (var cmd = _connection.CreateCommand())
            {
                cmd.CommandText = "INSERT INTO Settings (Name, Value) VALUES (@Name, @Value)";
                cmd.Parameters.AddWithValue("@Name", setting.Name.ToString());
                cmd.Parameters.AddWithValue("@Value", setting.Value);
                cmd.ExecuteNonQuery();
            }
        }
    }
}
