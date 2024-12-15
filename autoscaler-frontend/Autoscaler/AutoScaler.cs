class AutoScaler {
    public async void Run() {

        Prometheus prometheus = new();
        Kubernetes kubernetes = new();
        while(true) {
            var data = await prometheus.QueryRange("sum(rate(container_cpu_usage_seconds_total{container=~\"stregsystemet\"}[5m]))/4*100", DateTime.Now.AddDays(-7), DateTime.Now);
            Database.Singleton.InsertHistorical(data);
            Database.Singleton.Clean();
            // TODO: get ML results here, instead of hardcoding it
            var replicas = 2;
            // scale cluster
            // get replicaset name
            Dictionary<string, Dictionary<string, int>> patchData = new() {{
                "spec", new() {{
                    "replicas",replicas
                }}
            }};
            kubernetes.Patch($"/apis/apps/v1/namespaces/default/deployments/{ArgumentParser.Get("--deployment")}/scale", patchData);
            
            Thread.Sleep(int.Parse(ArgumentParser.Get("--period")));
        }
    }
}