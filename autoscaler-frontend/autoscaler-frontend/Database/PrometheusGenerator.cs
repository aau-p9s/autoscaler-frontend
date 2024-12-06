using System.Net.Sockets;
using System.Text.Json.Nodes;
using System.Web;

class PrometheusGenerator {
    private readonly HttpClient client;
    public PrometheusGenerator() {
        client = new HttpClient();
    }

    public async Task<IEnumerable<Tuple<int, double>>> GetMetrics() {
        var query = BuildQuery("sum(rate(container_cpu_usage_seconds_total{container=~\"stregsystemet\"}[1m]))/4*100");
        List<Tuple<int, double>> result_list = new();
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
                    result_list.Add(new Tuple<int, double>((int)(double)value[0], double.Parse((string)value[1])));
                }
                catch(NullReferenceException e) {
                    Console.WriteLine(e);
                }
            }
        }
        // todo: maybe let prometheus descide what max is if possible?
        end:
        return result_list;
    }
    public string BuildQuery(string target) {
        var addr = ArgumentParser.Get("--prometheus-addr");
        var urlEncodedTarget = HttpUtility.UrlEncode(target);
        var baseQuery = $"{addr}/api/v1/query_range?query={urlEncodedTarget}";
        var timeNow = DateTime.Now;
        var time7DaysAgo = timeNow.AddDays(-7);
        return baseQuery + "&start=" + ToRFC3339(time7DaysAgo) + "&end=" + ToRFC3339(timeNow) + "&step=60s";
    }

    private string ToRFC3339(DateTime date) {
        return date.ToUniversalTime().ToString("yyyy-MM-dd'T'HH:mm:ss.fffK");
    }
}
