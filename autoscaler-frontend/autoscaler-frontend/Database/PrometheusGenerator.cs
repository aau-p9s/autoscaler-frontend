using System.Net.Sockets;
using System.Text.Json.Nodes;

class PrometheusGenerator {
    private readonly HttpClient client;
    public PrometheusGenerator() {
        client = new HttpClient();
        var addr = ArgumentParser.Get("--prometheus-addr");
    }

    public async Task<IEnumerable<Tuple<double, string>>> GetMetrics() {
        var query = build_query();
        var response = await client.GetAsync(query);
        var json = await response.Content.ReadFromJsonAsync<JsonObject>();
        List<Tuple<double, string>> result_list = new List<Tuple<double, string>>();
        foreach(var result in json["data"]["result"].AsArray()) {
            foreach(var (x, y) in result["values"].AsArray().Select(value => ((double)value[0].AsValue(), (string)value[1].AsValue()))) {
                result_list.Add(new Tuple<double, string>(x, y));
            }
        }
        return result_list;
    }
    public string build_query() {
        var addr = ArgumentParser.Get("--prometheus-addr");
        var baseQuery = $"{addr}/api/v1/query_range?query=container_network_receive_packets_total%7Bpod%3D~%22stregsystemet.%2A%22%7D";
        var timeNow = DateTime.Now;
        var time7DaysAgo = timeNow.AddDays(-7);
        return baseQuery + "&start=" + ToRFC3339(time7DaysAgo) + "&end=" + ToRFC3339(timeNow) + "&step=60s";
    }

     private string ToRFC3339(DateTime date)
     {
         return date.ToUniversalTime().ToString("yyyy-MM-dd'T'HH:mm:ss.fffK");
     }
}
