class ArgumentParser {
    private static List<string> Args = new();
    private static Dictionary<string, string> ArgMap = new(){
        {"--period", "604800000"}, // 1 week
        { "--scaler", "../../autoscaler/autoscaler.py" }
    };
    public static void SetArgs(string[] args){
        for(int i = 0; i < args.Length; i++)
            Args.Add(args[i]);
    }
    public static string Get(string key) {
        return Args.Contains(key) ? Args[Args.IndexOf(key)+1] : ArgMap[key];
    }
}