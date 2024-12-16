using Autoscaler.Lib.Database;
using Autoscaler.Lib.Kubernetes;

namespace Autoscaler.Lib.Autoscaler;

class Scaler {
    

    private readonly Database.Database Database;
    private readonly string Deployment;
    private readonly int Period;
    readonly string KubeAddr;
    readonly string PrometheusAddr;
    readonly Thread thread;
    public Scaler(Database.Database database, string deployment, int period, string kubeAddr, string prometheusAddr) {
        Database = database;
        Deployment = deployment;
        Period = period;
        KubeAddr = kubeAddr;
        PrometheusAddr = prometheusAddr;
        thread = new(Scale);
        thread.Start();
    }
    public async void Scale() {
        Prometheus prometheus = new(PrometheusAddr);
        Kubernetes.Kubernetes kubernetes = new(KubeAddr);
        while(true) {
            var data = await prometheus.QueryRange("sum(rate(container_cpu_usage_seconds_total{container=~\"stregsystemet\"}[5m]))/4*100", DateTime.Now.AddDays(-7), DateTime.Now);
            Database.InsertHistorical(data);
            Database.Clean();
            // TODO: get ML results here, instead of hardcoding it
            var replicas = 2;
            // scale cluster
            // get replicaset name
            Dictionary<string, Dictionary<string, int>> patchData = new() {{
                "spec", new() {{
                    "replicas",replicas
                }}
            }};
            kubernetes.Patch($"/apis/apps/v1/namespaces/default/deployments/{Deployment}/scale", patchData);
            
            Thread.Sleep(Period);
        }
    }
}