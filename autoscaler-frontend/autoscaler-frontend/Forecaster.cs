using System.Diagnostics;
using System.Text.Json.Nodes;

namespace autoscaler_frontend;



public class Forecaster
{
    private Thread Scaler;
    private Dictionary<DateTime, int> Predictions = new();

    public static Forecaster Singleton = new();
    public Forecaster() {
        Scaler = new Thread(Run);
    }
    public void Start() {
        Scaler.Start();
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

    private void Run() {
        while(true) {
            // get predictions
            Process Predicter = new();
            Predicter.StartInfo.RedirectStandardOutput = true;
            Predicter.StartInfo.FileName = "../../autoscaler/autoscaler.py";
            Predicter.Start();
            var line = Predicter.StandardOutput.ReadLine();
            if (line == null) continue;

            var data = System.Text.Json.JsonSerializer.Deserialize<JsonArray>(line);
            if (data == null) continue;

            foreach(var (time, value) in data.Select(item => new Tuple<DateTime, int>(DateTime.Parse((string)item["time"]), (int)item["value"]))) {
                Predictions[time] = value;
            }
            Thread.Sleep(int.Parse(ArgumentParser.get("--period")));
        }
    }
}
