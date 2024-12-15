class ArgumentParser {
    private static List<string> Args = new();
    private static Dictionary<string, string> ArgMap = new(){
        {"--period", "604800000"}, // 1 week
        { "--scaler", "./Autoscaler/autoscaler.py" },
        {"--database", ":memory:"},
        {"--prometheus-addr", "http://localhost:30000"},
        {"--deployment", "stregsystemet-deployment"},
        {"--kube-api", "http://localhost:8001"}
    };
    public static void SetArgs(string[] args){
        for(int i = 0; i < args.Length; i++)
            Args.Add(args[i]);
    }
    public static string Get(string key) {
        return Args.Contains(key) ? Args[Args.IndexOf(key)+1] : ArgMap[key];
    }
}