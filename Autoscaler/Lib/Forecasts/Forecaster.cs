using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Autoscaler.Lib.Forecasts;

public class Forecaster
{
    private Thread thread;
    private Dictionary<DateTime, int> Predictions = new();
    readonly string Script;
    readonly int Period;
    readonly Database.Database Database;

    public Forecaster(Database.Database database, string script, int period)
    {
        Script = script;
        Period = period;
        Database = database;
        thread = new Thread(Run);
    }

    public void Start()
    {
        thread.Start();
    }

    public Forecast NextForecast()
    {
        if (Predictions.Count == 0)
            Run();
        var next = Predictions.Min(date => date.Key);
        var forecast = new Forecast(next, Predictions[next]);
        Predictions.Remove(next);
        return forecast;
    }

    private void Run()
    {
        // get predictions
        Process Predicter = new();
        Predicter.StartInfo.RedirectStandardOutput = true;
        Predicter.StartInfo.RedirectStandardInput = true;
        Predicter.StartInfo.FileName = Script;
        Predicter.Start();
        var historical = Database.AllHistorical();
        //Predicter.StandardInput.WriteLine(JsonSerializer.Serialize(historical));
        var line = Predicter.StandardOutput.ReadLine();
        if (line == null) return;
        var data = JsonSerializer.Deserialize<JsonArray>(line);
        if (data == null) return;
        Dictionary<DateTime, int> newPredictions = new();
        foreach (var (time, value) in data.Select(item =>
                     new Tuple<DateTime, int>(DateTime.Parse((string)item["time"]), (int)item["value"])))
        {
            newPredictions[time] = value;
        }

        Predictions = newPredictions;
    }
}