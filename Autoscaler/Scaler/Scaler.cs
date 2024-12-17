using Autoscaler.Lib.Database;
using Autoscaler.Lib.Forecasts;
using Autoscaler.Lib.Kubernetes;

namespace Autoscaler.Lib.Autoscaler;

class Scaler {
    

    private readonly Database.Database Database;
    private readonly string Deployment;
    private readonly int Period;
    readonly string KubeAddr;
    readonly string PrometheusAddr;
    readonly string Script;
    private readonly string Retrainer;
    readonly Thread thread;
    public Scaler(Database.Database database, string deployment, int period, string kubeAddr, string prometheusAddr, string script, string retrainer) {
        Database = database;
        Deployment = deployment;
        Period = period;
        KubeAddr = kubeAddr;
        PrometheusAddr = prometheusAddr;
        Script = script;
        Retrainer = retrainer;
        thread = new(Scale);
        thread.Start();
    }
    public async void Scale() {
        Prometheus prometheus = new(PrometheusAddr);
        Kubernetes.Kubernetes kubernetes = new(KubeAddr);
        Forecaster forecaster = new(Database, Script, Period, Retrainer);
        Forecast forecast = new();
        await forecaster.Run();
        while(true) {
            var data = await prometheus.QueryRange("sum(rate(container_cpu_usage_seconds_total{container=~\"stregsystemet\"}[5m]))/4*100", DateTime.Now.AddDays(-7), DateTime.Now);
            Database.InsertHistorical(data);
            var settings = Database.GetSettings();
            var replicas = await kubernetes.Replicas(Deployment);
            Console.WriteLine($"current replicas: {replicas}");
            try{
                forecast = forecaster.Next();
            } catch
            {
                Database.RemoveAllForecasts();
                await forecaster.Run();
            }

            Forecast newestHistorical = new();
            try
            {
                newestHistorical = Database.GetNewestHistorical();
            } catch
            {
                Console.WriteLine("Prometheus is either down or there is no data which should not happen");
            }

            //Check if the the forecasted value is within 20% of the newest historical value
            if (!(forecast.Value < newestHistorical.Value * 0.8) || !(forecast.Value > newestHistorical.Value * 1.2) || Database.IsManualChange)
            {
                await forecaster.RetrainModel();
                await forecaster.Run();
            }
            
            //var replicas = 1;
            if(forecast.Value > settings.ScaleUp)
                replicas++;
            if(forecast.Value <= settings.ScaleDown && replicas > 1)
                replicas--;
                
            Dictionary<string, Dictionary<string, int>> patchData = new() {{
                "spec", new() {{
                    "replicas",replicas
                }}
            }};
            Console.WriteLine($"Forecasted value: {forecast.Value}");
            Console.WriteLine($"replicas: {replicas}");

            await kubernetes.Patch($"/apis/apps/v1/namespaces/default/deployments/{Deployment}/scale", patchData);

            forecast = forecaster.Next();
            var delay = (forecast.Timestamp - DateTime.Now).TotalMilliseconds;
            if(forecast.Timestamp > DateTime.Now)
                Thread.Sleep((int)delay);
        }
    }
}