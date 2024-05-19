using System.Net.Sockets;
using System.Text;

enum RequestMethod { GET, POST, PUT, DELETE };

class HttpRequest
{

    public RequestMethod Method { get; set; }

    public string Path { get; set; }

    public string httpVersion { get; set; }


    public Dictionary<string, string> Headers { get; set; }

    public string Body { get; set; }

    //constructor 

    public HttpRequest()
    {
    }

    public async Task ParseRequest(NetworkStream stream)
    {
        StreamReader reader = new StreamReader(stream, Encoding.UTF8);
        string? requestLine = await reader.ReadToEndAsync();
        string[] lineSplit = requestLine.Split(" ");

        Method = lineSplit[0].ToLower() switch
        {
            "get" => RequestMethod.GET,
            "post" => RequestMethod.POST,
            "put" => RequestMethod.PUT,
            "delete" => RequestMethod.DELETE,
            _ => throw new Exception("Invalid request method")
        };

        Path = lineSplit[1];
        httpVersion = lineSplit[2];

        Headers = new Dictionary<string, string>();
        string? currHeader = reader.ReadLine();
        while (currHeader?.Length > 0)
        {
            string[] header = currHeader.Split(":", 2);
            Headers.Add(header[0], header[1]);
            currHeader = reader.ReadLine();
        }
        Body = await reader.ReadToEndAsync();


    }
}