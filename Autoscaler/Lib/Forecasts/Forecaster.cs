using System.Diagnostics;
using System.Text.Json.Nodes;

namespace Autoscaler;

public class Forecaster
{
    private Thread thread;
    private Dictionary<DateTime, int> Predictions = new();
    public Forecaster() {
        thread = new Thread(Run);
    }
    public void Start() {
        thread.Start();
    }

    private void Run() {
        while(true) {
            // get predictions
            Process Predicter = new();
            Predicter.StartInfo.RedirectStandardOutput = true;
            Predicter.StartInfo.FileName = Autoscaler.Args.Get("--scaler");
            Predicter.Start();
            var line = Predicter.StandardOutput.ReadLine();
            if (line == null) continue;

            var data = System.Text.Json.JsonSerializer.Deserialize<JsonArray>(line);
            if (data == null) continue;

            foreach(var (time, value) in data.Select(item => new Tuple<DateTime, int>(DateTime.Parse((string)item["time"]), (int)item["value"]))) {
                Predictions[time] = value;
            }
            Thread.Sleep(int.Parse(Autoscaler.Args.Get("--period")));
        }
    }
}
