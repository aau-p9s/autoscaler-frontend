using System.Collections;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Nodes;
using Autoscaler.Lib.Database;

namespace Autoscaler.Lib.Forecasts;

public class Forecaster
{
    PredictionResult Predictions = new();
    readonly string Script;
    readonly int Period;
    readonly string Retrainer;
    readonly Database.Database Database;
    

    public Forecaster(Database.Database database, string script, int period, string retrainer) {
        Script = script;
        Period = period;
        Database = database;
        Retrainer = retrainer;
    }
    public Forecast Next() {
        if (Predictions.Time.Count == 0) return null;
        var time = Predictions.Time[0];
        var amount = Predictions.Amount[0];
        Predictions.Time.RemoveAt(0);
        Predictions.Amount.RemoveAt(0);
        return new Forecast(time, amount);
    }

    public async Task Run() {
        Database.RemoveAllForecasts();
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
        Process process = new();
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.RedirectStandardInput = true;
        process.StartInfo.FileName = Script;
        process.StartInfo.Arguments = "10";
        process.Start();
        var line = await process.StandardOutput.ReadLineAsync();
        Console.WriteLine("got data");
        if (line == null) return;
        try
        {
            var data = JsonSerializer.Deserialize<PredictionResult>(line,options);
            if (data == null)
            {
                Console.WriteLine("Deserialization returned null.");
                return;
            }
    
            Console.WriteLine(data.Time.Count);
            foreach (var item in data.Time)
                Console.WriteLine(item);
            foreach (var item in data.Amount)
            {
                Console.WriteLine(item);
            }
            Predictions = data;
            List<Forecast> forecast = new();
            for (int i = 0; i < data.Time.Count; i++)
            {
                forecast.Add(new Forecast(data.Time[i], data.Amount[i]));
            }
            Database.InsertForecast(forecast);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Deserialization failed: {ex.Message}");
        }
    }

    public async Task RetrainModel()
    {
        Process process = new();
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.RedirectStandardInput = true;
        process.StartInfo.FileName = Retrainer;
        process.StartInfo.Arguments = "10";

        process.Start();

        var historical = Database.AllHistorical();
        await process.StandardInput.WriteLineAsync(JsonSerializer.Serialize(historical));
        process.StandardInput.Close();

        await process.WaitForExitAsync();

        Database.RemoveAllHistorical();
    }
}