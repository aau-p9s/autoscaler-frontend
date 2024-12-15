using System.Text.Json;
using System.Text.Json.Nodes;

class Kubernetes {
    readonly HttpClientHandler handler;
    readonly HttpClient client;
    readonly Tuple<string, string>? authHeader;
    public Kubernetes() {
        handler = new() {
            ClientCertificateOptions = ClientCertificateOption.Manual,
            // TODO: Handle actual certificate
            ServerCertificateCustomValidationCallback = (_, _, _, _) => {
                return true;
            }
        };
        client = new(handler);
        if(File.Exists("/var/run/secrets/kubernetes.io/serviceaccount/token")) {
            StreamReader stream = new("/var/run/secrets/kubernetes.io/serviceaccount/token");
            authHeader = new("Authorization", $"Bearer {stream.ReadToEnd()}");
        }
        else 
            authHeader = null;
        
    }
    //public JsonObject Recv(Uri uri) {
    //}

    public void Patch(string endpoint, object body) {
        try{
            var request = new HttpRequestMessage {
                Method = HttpMethod.Patch,
                RequestUri = new Uri($"{ArgumentParser.Get("--kube-api")}"),
                Content = new StringContent(JsonSerializer.Serialize(body))
            };
            if (authHeader != null)
                request.Headers.Add(authHeader.Item1, authHeader.Item2);
            client.SendAsync(request);
        }
        catch(HttpRequestException e) {
            Console.WriteLine("no api seems to be available, running offline...");
            Console.WriteLine(e.Message);
            if(e.InnerException != null)
                Console.WriteLine(e.InnerException.Message);
        }
    }
}