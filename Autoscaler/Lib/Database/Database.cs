using System.Data;
using System.IO.Compression;
using System.Net.Http.Headers;
using System.Net.Mime;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using System.Text.Json.Nodes;
using autoscaler_frontend;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Data.Sqlite;

namespace Autoscaler.Lib.Database;

public class Database {
    readonly string Path;
    readonly SqliteConnection Connection;
    private bool _isManualChange = false;
    public bool IsManualChange => _isManualChange;

    public Database(string path){
        Path = path;
        Connection = new SqliteConnection($"Data Source={Path}");
        Connection.Open();
        var command = Connection.CreateCommand();
        command.CommandText = @"
            CREATE TABLE IF NOT EXISTS historical (
                id INTEGER PRIMARY KEY,
                timestamp DATETIME NOT NULL,
                amount INTEGER NOT NULL
            );
            CREATE TABLE IF NOT EXISTS forecasts (
                id INTEGER PRIMARY KEY,
                timestamp DATETIME NOT NULL,
                amount INTEGER NOT NULL,
                fetch_time DATETIME NOT NULL
            );
            CREATE TABLE IF NOT EXISTS settings (
                id INTEGER PRIMARY KEY,
                scaleup INTEGER NOT NULL,
                scaledown INTEGER NOT NULL,
                scaleperiod INTEGER NOT NULL,
                UNIQUE(scaleup, scaledown, scaleperiod)
            );

            INSERT or IGNORE INTO settings (scaleup, scaledown, scaleperiod) VALUES (50, 20, 60000)

        ";
            //INSERT OR IGNORE INTO settings (scaleup, scaledown, scaleperiod) VALUES ('0', '0', '0')
        command.ExecuteNonQuery();
    }
    
    public void Add(DateTime time, int value) {
        var command = Connection.CreateCommand();
        command.CommandText = @"
            INSERT INTO historical (timestamp, amount) VALUES ($time, $amount)
        ";
        command.Parameters.AddWithValue("$time", time);
        command.Parameters.AddWithValue("$amount", value);
        command.ExecuteNonQuery();
    }
    
    public Dictionary<int, int> GetByTimestamp(DateTime time) {
        Dictionary<int, int> result = new();
        var command = Connection.CreateCommand();
        command.CommandText = @"
            SELECT id, amount FROM historical WHERE timestamp = $time
        ";
        command.Parameters.AddWithValue("$time", time);
        using(var reader = command.ExecuteReader()) {
            while(reader.Read()) {
                result.Add(reader.GetInt32(0), reader.GetInt32(1));
            }
        }
        return result;
    }

    public void SetSettings(Settings settings) {
        var command = Connection.CreateCommand();
        var oldSettings = GetSettings();

        if (settings.ScaleUp == null)
            settings.ScaleUp = oldSettings.ScaleUp;
        if (settings.ScaleDown == null)
            settings.ScaleDown = oldSettings.ScaleDown;
        if (settings.ScalePeriod == null)
            settings.ScalePeriod = oldSettings.ScalePeriod;
            
        command.CommandText = @"
            UPDATE settings SET scaleup = $scaleup, scaledown = $scaledown, scaleperiod = $scaleperiod WHERE id = $id
        ";
        command.Parameters.AddWithValue("$scaleup", settings.ScaleUp);
        command.Parameters.AddWithValue("$scaledown", settings.ScaleDown);
        command.Parameters.AddWithValue("$scaleperiod", settings.ScalePeriod);
        command.Parameters.AddWithValue("$id", settings.Id);

        command.ExecuteNonQuery();

    }

    public Settings GetSettings() {
        var command = Connection.CreateCommand();
        Settings Settings = new();
        command.CommandText = @"
            SELECT id, scaleup, scaledown, scaleperiod FROM settings
        ";
        using(var reader = command.ExecuteReader()) {
            reader.Read();
            Settings.Id = reader.GetInt32(0);
            Settings.ScaleUp = reader.GetInt32(1);
            Settings.ScaleDown = reader.GetInt32(2);
            Settings.ScalePeriod = reader.GetInt32(3);
            return Settings;
        }
    }
    public Dictionary<DateTime, int> AllHistorical() {
        var command = Connection.CreateCommand();
        command.CommandText = @"
            SELECT timestamp, amount FROM historical
        ";
        Dictionary<DateTime, int> result = new();
        using(var reader = command.ExecuteReader()) {
            while(reader.Read()) {
                result[reader.GetDateTime(0)] = reader.GetInt32(1);
            }
        }
        return result;
    }

    public void InsertHistorical(IEnumerable<Tuple<int, double>> data) {
        foreach(var (timestamp, value) in data) {
            var command = Connection.CreateCommand();
            command.CommandText = @"
                INSERT OR IGNORE INTO historical (id, timestamp, amount) VALUES (
                    (SELECT id FROM historical WHERE strftime('%Y-%m-%d-%H:%M', timestamp) = strftime('%Y-%m-%d-%H:%M', $time)),
                    $time,
                    $amount
                )
            ";
            command.Parameters.AddWithValue("$time",new DateTime(1970, 1, 1, 0, 0, 0).AddSeconds(timestamp));
            command.Parameters.AddWithValue("$amount", value);
            command.ExecuteNonQueryAsync();
        }
    }

    public Dictionary<DateTime, int> Historic(DateTime from) {
        var command = Connection.CreateCommand();
        command.CommandText = @"
            SELECT timestamp, amount FROM historical WHERE
                strftime('%Y-%m-%d-%H:%M', timestamp) >= strftime('%Y-%m-%d-%H:%M', $time)
        ";
        command.Parameters.AddWithValue("$time", from);
        Dictionary<DateTime, int> result = new();
        using(var reader = command.ExecuteReader()) {
            while(reader.Read()) {
                result[reader.GetDateTime(0)] = reader.GetInt32(1);
            }
        }
        return result;
    }

    public Dictionary<DateTime, int> Prediction(DateTime to) {
        var command = Connection.CreateCommand();
        command.CommandText = @"
            SELECT timestamp, amount FROM forecasts WHERE
                strftime('%Y-%m-%d-%H:%M', timestamp) <= strftime('%Y-%m-%d-%H:%M', $time)
        ";
        command.Parameters.AddWithValue("$time", to);
        Dictionary<DateTime, int> result = new();
        using(var reader = command.ExecuteReader()) {
            while(reader.Read()) {
                result[reader.GetDateTime(0)] = reader.GetInt32(1);
            }
        }
        return result;
    }
    
    public void ManualChange(Dictionary<DateTime,int> data) {
        foreach (var p in data)
        {
            // Delete existing rows with the same timestamp
            using (var deleteCommand = Connection.CreateCommand())
            {
                deleteCommand.CommandText = @"
                DELETE FROM forecasts WHERE timestamp = $time
            ";
                deleteCommand.Parameters.AddWithValue("$time", p.Key);
                deleteCommand.ExecuteNonQuery();
            }
        }
        foreach (var p in data)
        {
            // Insert new rows
            using (var command = Connection.CreateCommand())
            {
                command.CommandText = @"
                INSERT INTO forecasts (timestamp, amount, fetch_time) VALUES ($time, $amount, date('now'))
            ";
                command.Parameters.AddWithValue("$time", p.Key);
                command.Parameters.AddWithValue("$amount", p.Value);
                command.ExecuteNonQuery();
            }
        }
        _isManualChange = true;
    }


    public void Clean() {
        var command = Connection.CreateCommand();
        command.CommandText = @"
            DELETE FROM historical where
                timestamp <= date('now','-7 day');

            DELETE FROM forecasts where
                timestamp <= date('now');
        ";
        command.ExecuteNonQuery();
        _isManualChange = false;
    }
}
