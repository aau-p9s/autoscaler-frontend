using System.Data;
using System.IO.Compression;
using System.Net.Http.Headers;
using System.Net.Mime;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Data.Sqlite;

class Database{
    readonly string Source;
    readonly SqliteConnection Connection;
    public Database(string source){
        Source = source;
        Connection = new SqliteConnection($"Data Source={Source}");
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
            )
        ";
        command.ExecuteNonQuery();

        var thread = new Thread(UpdateThread);
        thread.Start();
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

    async void UpdateThread() {
        var generator = new PrometheusGenerator();
        HttpClient client = new();
        while(true) {
            var data = await generator.GetMetrics();
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
                command.Parameters.AddWithValue("amount", value);
                command.ExecuteNonQuery();
            }
            Console.WriteLine("Successfully fetched historical data");
            // TODO: get ML results here, instead of hardcoding it
            var replicas = 2;
            // scale cluster
            // get replicaset name
            Dictionary<string, Dictionary<string, int>> patchData = new() {{
                "spec", new() {{
                    "replicas",replicas
                }}
            }};
            using(var request = new HttpRequestMessage()) {
                request.Method = HttpMethod.Patch;
                request.RequestUri = new Uri("http://localhost:8001/apis/apps/v1/namespaces/default/deployments/stregsystemet-deployment/scale");
                request.Content = JsonContent.Create(patchData);
                request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/merge-patch+json");
                request.Content.Headers.Add("Content-Type", "application/merge-patch+json");
                var response = await client.SendAsync(request);
                if(response.StatusCode != System.Net.HttpStatusCode.OK) {
                    Console.WriteLine(await response.Content.ReadAsStringAsync());
                    Environment.Exit(1);
                }
            }
            Thread.Sleep(15000);
        }
    }
}
