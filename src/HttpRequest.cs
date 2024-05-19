using System.Net.Sockets;
using System.Text;

enum RequestMethod { GET, POST, PUT, DELETE };

class HttpRequest
{

    public RequestMethod Method { get; set; }

    public string Path { get; set; }

    public string HttpVersion { get; set; }


    public Dictionary<string, string> Headers { get; set; }

    public string Body { get; set; }

    //constructor 

    public HttpRequest()
    {
        Method = RequestMethod.GET;
        Path = string.Empty;
        HttpVersion = string.Empty;
        Headers = new Dictionary<string, string>();
        Body = string.Empty;

    }

    public static async Task<HttpRequest> CreateAsync(NetworkStream stream)
    {
        HttpRequest request = new HttpRequest();
        await request.ParseRequest(stream);
        return request;
    }

    public async Task ParseRequest(NetworkStream stream)
    {
        StreamReader reader = new StreamReader(stream, Encoding.UTF8);
        string? requestLine = await reader.ReadLineAsync();
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
        HttpVersion = lineSplit[2];

        Headers = new Dictionary<string, string>();
        string? currHeader = reader.ReadLine();
        while (currHeader?.Length > 0)
        {
            string[] header = currHeader.Split(":", 2);
            Headers.Add(header[0], header[1]);
            currHeader = reader.ReadLine();
        }

        // Read body
        if (Headers.TryGetValue("Content-Length", out string? contentLengthValue) &&
            int.TryParse(contentLengthValue, out int contentLength))
        {
            char[] buffer = new char[contentLength];
            int totalRead = 0;
            while (totalRead < contentLength)
            {
                int bytesRead = await reader.ReadAsync(buffer, totalRead, contentLength - totalRead);
                if (bytesRead == 0)
                {
                    break;
                }
                totalRead += bytesRead;
            }
            Body = new string(buffer, 0, totalRead);
            Console.WriteLine("Body: " + Body);
        }


    }
}