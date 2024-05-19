using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;
using System.Threading.Tasks;

class Program
{
    static string? BaseDir;
    static string[] AcceptedEncodings = { "gzip" };

    static async Task Main(string[] args)
    {
        TcpListener server = new TcpListener(IPAddress.Any, 4221);
        Console.WriteLine("Starting server...");
        server.Start();
        Console.WriteLine("Server started on port 4221");
        Program pg = new Program();

        if (args.Length >= 2)
        {
            if (args[0].ToLower() == "--directory")
            {
                BaseDir = args[1];
            }
        }

        while (true)
        {
            var client = await server.AcceptSocketAsync();
            pg.handleStuff(client);
        }
    }

    public void processEncodings(HttpRequest request, HttpResponse httpResponse)
    {
        string? acceptEncoding;
        request.Headers.TryGetValue("accept-encoding", out acceptEncoding);
        if (acceptEncoding != null)
        {
            string[] encodings = acceptEncoding.Split(",");
            foreach (string encoding in encodings)
            {
                string trimmedEncoding = encoding.Trim().ToLower();
                if (Array.Exists(AcceptedEncodings, e => e.Equals(trimmedEncoding, StringComparison.OrdinalIgnoreCase)))
                {
                    if (!httpResponse.Headers.ContainsKey("Content-Encoding"))
                    {
                        httpResponse.AddHeader("Content-Encoding", trimmedEncoding);
                    }
                    break;
                }
            }
        }
    }

    void handleStuff(Socket clientTask)
    {
        Task.Run(async () =>
        {
            var client = clientTask;
            NetworkStream stream = new NetworkStream(client);
            StreamReader reader = new StreamReader(stream, Encoding.UTF8);
            StreamWriter writer = new StreamWriter(stream, Encoding.UTF8);

            var request = await HttpRequest.CreateAsync(stream);
            bool routeMatched = false;


            Regex echoRegex = new Regex("^/echo/(.*)");
            Regex userAgentRegex = new Regex("^(/user-agent)$");

            var userAgentPath = userAgentRegex.Match(request.Path);
            if (userAgentPath.Success)
            {
                routeMatched = true;
                string? userAgentValue;
                request.Headers.TryGetValue("user-agent", out userAgentValue);
                string responseString = $"HTTP/1.1 200 OK\r\nContent-Type: text/plain\r\nContent-Length: {userAgentValue?.Length}\r\n\r\n{userAgentValue}";
                byte[] data = Encoding.ASCII.GetBytes(responseString);
                stream.Write(data, 0, data.Length);
            }

            var match = echoRegex.Match(request.Path);
            if (match.Success && match.Groups.Count > 1)
            {
                routeMatched = true;
                string toEcho = match.Groups[1].Value;

                var response = new HttpResponse(200, "OK", toEcho);
                processEncodings(request, response);
                response.AddHeader("Content-Type", "text/plain");

                byte[] responseBytes = response.ToByteArray(response.Headers.ContainsKey("Content-Encoding"));

                stream.Write(responseBytes, 0, responseBytes.Length);
            }

            if (request.Path == "/")
            {
                routeMatched = true;
                string responseString = "HTTP/1.1 200 OK\r\n\r\n";
                byte[] data = Encoding.ASCII.GetBytes(responseString);
                stream.Write(data, 0, data.Length);
            }

            if (!routeMatched && !string.IsNullOrEmpty(BaseDir))
            {
                var filesRegex = new Regex("^/files/(.*)");
                var filesMatch = filesRegex.Match(request.Path);
                if (filesMatch.Success && filesMatch.Groups.Count > 1)
                {
                    routeMatched = true;
                    string fileName = filesMatch.Groups[1].Value;
                    string filepath = Path.Combine(BaseDir, fileName);
                    if (request.Method == RequestMethod.POST)
                    {

                        string fileContent = request.Body ?? "";

                        using (StreamWriter fwriter = new StreamWriter(filepath))
                        {
                            fwriter.Write(fileContent.Replace("\0", string.Empty));
                        }
                        string responseString = $"HTTP/1.1 201 Created\r\n\r\n";
                        byte[] data = Encoding.ASCII.GetBytes(responseString);
                        stream.Write(data, 0, data.Length);
                    }
                    else if (request.Method == RequestMethod.GET && File.Exists(filepath))
                    {
                        using (FileStream fileStream = File.OpenRead(filepath))
                        using (StreamReader reader2 = new StreamReader(fileStream))
                        {
                            string content = reader2.ReadToEnd();
                            string responseString = $"HTTP/1.1 200 OK\r\nContent-Type: application/octet-stream\r\nContent-Length: {content.Length}\r\n\r\n{content}";
                            byte[] data = Encoding.ASCII.GetBytes(responseString);
                            stream.Write(data, 0, data.Length);
                        }
                    }
                    else
                    {
                        string responseString = "HTTP/1.1 404 Not Found\r\n\r\n";
                        byte[] data = Encoding.ASCII.GetBytes(responseString);
                        stream.Write(data, 0, data.Length);
                    }

                }
            }

            if (!routeMatched)
            {
                string responseString = "HTTP/1.1 404 Not Found\r\n\r\n";
                byte[] data = Encoding.ASCII.GetBytes(responseString);
                stream.Write(data, 0, data.Length);
            }

            client.Close();
        });
    }
}
