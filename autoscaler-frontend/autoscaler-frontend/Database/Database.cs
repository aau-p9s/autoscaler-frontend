using System.Data;
using System.IO.Compression;
using System.Net.Http.Headers;
using System.Net.Mime;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Data.Sqlite;

class Database{
    readonly string Path;
    readonly SqliteConnection Connection;
    public static Database Singleton = new(ArgumentParser.Get("--database"));
    public Database(string path){
        Path = path;
        Connection = new SqliteConnection($"Data Source={Path}");
    }

    public void Init() {
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

    async void UpdateThread() {
        var generator = new PrometheusGenerator();
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
            try {
                HttpClientHandler handler = new()
                {
                    ClientCertificateOptions = ClientCertificateOption.Manual,
                    SslProtocols = SslProtocols.Tls12
                };
                handler.ClientCertificates.Add(new X509Certificate2(X509Certificate2.CreateFromCertFile("/var/run/secrets/kubernetes.io/serviceaccount/ca.crt")));
                Console.WriteLine("created handler");
                HttpClient client = new(handler);
                StreamReader stream = new("/var/run/secrets/kubernetes.io/serviceaccount/token");
                Console.WriteLine("Reading token");
                string token = stream.ReadToEnd();
                using(var request = new HttpRequestMessage()) {
                    request.Method = HttpMethod.Patch;
                    request.RequestUri = new Uri($"{ArgumentParser.Get("--kube-api")}/apis/apps/v1/namespaces/default/deployments/{ArgumentParser.Get("--deployment")}/scale");
                    request.Content = new StringContent(JsonSerializer.Serialize(patchData), new MediaTypeHeaderValue("application/merge-patch+json"));
                    request.Headers.Add("Authorization", $"Bearer {token}");
                    Console.WriteLine("Created request");
                    var response = await client.SendAsync(request);
                    Console.WriteLine("kube api response: " + await response.Content.ReadAsStringAsync());
                    if(response.StatusCode != System.Net.HttpStatusCode.OK) {
                        Console.WriteLine(await response.Content.ReadAsStringAsync());
                        Environment.Exit(1);
                    }
                }
            }
            catch (HttpRequestException e) {
                Console.WriteLine("no api seems to be available, running offline...");
                Console.WriteLine(e.Message);
                if(e.InnerException != null)
                    Console.WriteLine(e.InnerException.Message);
            }
            Thread.Sleep(int.Parse(ArgumentParser.Get("--period")));
        }
    }
}
