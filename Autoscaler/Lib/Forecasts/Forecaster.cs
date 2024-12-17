using System.Collections.Immutable;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Nodes;
using Autoscaler.Lib.Autoscaler;
using Autoscaler.Lib.Database;

namespace Autoscaler.Lib.Forecasts;

class PredictionResult {
    public List<DateTime> Time {get; set;}
    public List<int> Amount {get; set;}
}

public class Forecaster
{
    Dictionary<DateTime, int> Predictions = new();
    readonly string Script;
    readonly int Period;
    readonly Database.Database Database;

    public Forecaster(Database.Database database, string script, int period) {
        Script = script;
        Period = period;
        Database = database;
    }
    public async Task<Forecast> NextForecast() {
        Console.WriteLine($"Count: {Predictions.Count}");
        if(Predictions.Count == 0)
            await Run();
        var next = Predictions.Min(date => date.Key);
        var forecast = new Forecast(next, Predictions[next]);
        Predictions.Remove(next);
        return forecast;
    }

    private async Task<int> Run() {
        // get predictions
        Process process = new();
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.RedirectStandardInput = true;
        process.StartInfo.FileName = Script;
        process.StartInfo.Arguments = "10";
        process.Start();
        var historical = Database.AllHistorical();
        process.StandardInput.WriteLine(JsonSerializer.Serialize(historical));
        var line = await process.StandardOutput.ReadLineAsync();
        Console.WriteLine("got data");
        if (line == null) return 1;
        var data = JsonSerializer.Deserialize<PredictionResult>(line);
        Console.WriteLine(data.Time.Count);
        foreach(var item in data.Time)
            Console.WriteLine(item);
        if (data == null) return 1;
        //Dictionary<DateTime, int> newPredictions = new();
        //for(int i = 0; i < data["time"].AsArray().Count; i++) {
        //    Console.WriteLine(data["time"]);
        //    var time = data["time"].AsArray().ToList();
        //    var amount = data["time"].AsArray().ToList();
        //    newPredictions[(DateTime)data["time"].AsArray()[i]] = (int)data["amount"].AsArray()[i];
        //}
        //Predictions = newPredictions;
        return 0;
    }
}
