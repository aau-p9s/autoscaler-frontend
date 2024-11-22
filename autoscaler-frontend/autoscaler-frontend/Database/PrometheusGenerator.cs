using System.Net.Sockets;
using System.Text.Json.Nodes;

class PrometheusGenerator {
    private readonly HttpClient client;
    private readonly string query;
    public PrometheusGenerator() {
        client = new HttpClient();
        var addr = ArgumentParser.Get("--prometheus-addr");
        query = $"{addr}/api/v1/query_range?query=container_network_receive_packets_total%7Bpod%3D~%22stregsystemet.%2A%22%7D&start=2024-11-22T08:00:00.000Z&end=2024-11-22T12:00:00.000Z&step=15s";
    }

    public async Task<IEnumerable<Tuple<int, int>>> GetMetrics() {
        var response = await client.GetAsync(query);
        var json = await response.Content.ReadFromJsonAsync<JsonObject>();
        IEnumerable<Tuple<int, int>> result_list = new List<Tuple<int, int>>();
        foreach(var result in json["data"]["result"].AsArray()) {
            foreach(var (x, y) in result["values"].AsArray().Select(value => ((int)value[0].AsValue(), (int)value[1].AsValue()))) {
                result_list.Append(new Tuple<int, int>(x, y));
            }
        }
        Console.WriteLine(result_list.Count());
        return result_list;
    }
}