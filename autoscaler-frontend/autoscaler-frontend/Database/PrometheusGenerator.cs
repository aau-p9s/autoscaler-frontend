using System.Net.Sockets;
using System.Text.Json.Nodes;

class PrometheusGenerator {
    private readonly HttpClient client;
    public PrometheusGenerator() {
        client = new HttpClient();
    }

    public async Task<IEnumerable<Tuple<double, double>>> GetMetrics() {
        var query = BuildQuery();
        List<Tuple<double, double>> result_list = new();
        HttpResponseMessage response;
        try {
            response = await client.GetAsync(query);
        }
        catch {
            Console.WriteLine("prometheus seems to be down...");
            goto end;
        }
        var json = await response.Content.ReadFromJsonAsync<JsonObject>();
        if(json == null)
            goto end;
        var data = json["data"];
        if(data == null)
            goto end;
        var result = data["result"];
        if(result == null)
            goto end;
        foreach(var item in result.AsArray()) {
            if(item == null)
                continue;
            var valuesObj = item["values"];
            if(valuesObj == null)
                continue;
            var values = valuesObj.AsArray();
            foreach(var value in values) {
                if(value == null)
                    continue;

                try{
                    result_list.Add(new Tuple<double, double>((double)value[0], double.Parse((string)value[1])));
                }
                catch(NullReferenceException e) {
                    Console.WriteLine(e);
                }
            }
        }
        // todo: maybe let prometheus descide what max is if possible?
        end:
        var max = result_list.Count > 0 ? result_list.Select((_, e) => e).Max() : 1.0;
        return result_list.Select((t, value) => new Tuple<double, double>(t.Item1, value/max * 100.0));
    }
    public string BuildQuery() {
        var addr = ArgumentParser.Get("--prometheus-addr");
        var baseQuery = $"{addr}/api/v1/query_range?query=container_network_receive_packets_total%7Bpod%3D~%22stregsystemet.%2A%22%7D";
        var timeNow = DateTime.Now;
        var time7DaysAgo = timeNow.AddDays(-7);
        return baseQuery + "&start=" + ToRFC3339(time7DaysAgo) + "&end=" + ToRFC3339(timeNow) + "&step=60s";
    }

    private string ToRFC3339(DateTime date) {
        return date.ToUniversalTime().ToString("yyyy-MM-dd'T'HH:mm:ss.fffK");
    }
}
