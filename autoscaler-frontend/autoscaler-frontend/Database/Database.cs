using System.Data;
using Microsoft.Data.Sqlite;

class Database{
    readonly string Source;
    SqliteConnection connection;
    public Database(string source){
        Source = source;
        connection = new SqliteConnection($"Data Source={Source}");
        connection.Open();
        var command = connection.CreateCommand();
        command.CommandText = @"
            CREATE TABLE IF NOT EXISTS historical (
                id INTEGER PRIMARY KEY,
                timestamp DATETIME NOT NULL,
                amount INTEGER NOT NULL
            );
        ";
        command.ExecuteNonQuery();
    }
    public void Add(DateTime time, int value) {
        var command = connection.CreateCommand();
        command.CommandText = @"
            INSERT INTO historical(timestamp, amount) VALUES ($time, $amount)
        ";
        command.Parameters.AddWithValue("$time", time);
        command.Parameters.AddWithValue("$amount", value);
        command.ExecuteNonQuery();
    }
    public Dictionary<int, int> getByTimestamp(DateTime time) {
        Dictionary<int, int> result = new();
        var command = connection.CreateCommand();
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
}