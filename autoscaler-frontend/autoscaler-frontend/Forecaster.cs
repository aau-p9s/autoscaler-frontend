using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace autoscaler_frontend;

public class Forecast {
    public readonly int Amount;
    public readonly DateTime Timestamp;
    public Forecast(int amount, DateTime timestamp) {
        Amount = amount;
        Timestamp = timestamp;
    }
}

public class Forecaster
{
    private Dictionary<DateTime, int> Predictions = new();

    public static Forecaster Singleton = new();
    public Forecaster() {
    }

    public Dictionary<DateTime, int> Prediction() {
        var now = DateTime.Now;
        Dictionary<DateTime, int> final = new();
        foreach(var (key, value) in Predictions) {
            if(key > now)
                final[key] = value;
        }
        return final;
    }

    public Dictionary<DateTime, int> Historic() {
        var now = DateTime.Now;
        Dictionary<DateTime, int> final = new();
        foreach(var (key, value) in Predictions) {
            if(key <= now)
                final[key] = value;
        }
        return final;
    }

    public Forecast NextForecast() {
        Console.WriteLine($"Count: {Predictions.Count}");
        if(Predictions.Count == 0) 
            Run();
        var next = Predictions.Min(date => date.Key);
        var forecast = new Forecast(Predictions[next], next);
        Predictions.Remove(next);
        return forecast;
    }

    private void Run() {
        // get predictions
        Process Predicter = new();
        Predicter.StartInfo.RedirectStandardOutput = true;
        Predicter.StartInfo.RedirectStandardInput = true;
        Predicter.StartInfo.FileName = ArgumentParser.Get("--scaler");
        Predicter.Start();
        var historical = Database.Singleton.AllHistorical();
        Predicter.StandardInput.WriteLine(JsonSerializer.Serialize(historical));
        var line = Predicter.StandardOutput.ReadLine();
        if (line == null) return;
        var data = JsonSerializer.Deserialize<JsonArray>(line);
        if (data == null) return;
        Dictionary<DateTime, int> newPredictions = new();
        foreach(var (time, value) in data.Select(item => new Tuple<DateTime, int>(DateTime.Parse((string)item["time"]), (int)item["value"]))) {
            newPredictions[time] = value;
        }
        Predictions = newPredictions;
    }
}
