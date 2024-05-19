using System.Net;
using System.Net.Sockets;
using System.Reflection.Metadata;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;
using System.IO.Compression;


class Program
{
    static string? BaseDir;

    static string[] AcceptedEncodings = { "gzip" };
    static async Task Main(string[] args)
    {
        TcpListener server = new TcpListener(IPAddress.Any, 4221);
        Console.WriteLine("Starting server...");
        server.Start();
        Console.WriteLine("Server started on port 4421");
        Program pg = new Program();

        if (args.Length >= 2)
        {
            Console.WriteLine("Setting directory: {0}", args[1]);
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
                    httpResponse.AddHeader("Content-Encoding", trimmedEncoding);

                    if (trimmedEncoding == "gzip")
                    {
                        httpResponse.AddHeader("Content-Encoding", "gzip");
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
            Console.WriteLine("Client connected");
            NetworkStream stream = new NetworkStream(client);
            StreamReader reader = new StreamReader(stream, Encoding.UTF8);
            StreamWriter writer = new StreamWriter(stream, Encoding.UTF8);

            var request = await HttpRequest.CreateAsync(stream);

            Console.WriteLine("Received: {0} {1} {2}", request.Method, request.Path, request.HttpVersion);

            Regex echoRegex = new Regex("^/echo/(.*)");
            Regex userAgentRegex = new Regex("^(/user-agent)$");

            string responseString = "HTTP/1.1 404 Not Found\r\n\r\n";
            var userAgentPath = userAgentRegex.Match(request.Path);
            if (userAgentPath.Success)
            {
                Console.WriteLine("matches user agent");
                string? userAgentValue;
                request.Headers.TryGetValue("user-agent", out userAgentValue);
                Console.WriteLine("User agent: {0}", userAgentValue);
                responseString = $"HTTP/1.1 200 OK\r\nContent-Type: text/plain\r\nContent-Length: {userAgentValue?.Length}\r\n\r\n{userAgentValue}";
            }
            var match = echoRegex.Match(request.Path);
            if (match.Success && match.Groups.Count > 1)
            {
                Console.WriteLine("Matches echo");
                string toEcho = match.Groups[1].Value;

                var response = new HttpResponse(200, "OK", toEcho);
                processEncodings(request, response);
                response.AddHeader("content-type", "text/plain");

                var rs = response.ToString();
                responseString = response.ToString();

                if (response.Headers.ContainsKey("Content-Encoding") && response.Headers["Content-Encoding"] == "gzip")
                {
                    Console.WriteLine("Gzipping response");
                    responseString = response.ToGzippedString();
                }

                Console.WriteLine("Echo: {0}, {1}", toEcho, rs);
                // responseString = $"HTTP/1.1 200 OK\r\nContent-Type: text/plain\r\nContent-Length: {toEcho.Length}\r\n\r\n{toEcho}";
            }
            if (request.Path == "/")
            {
                responseString = "HTTP/1.1 200 OK\r\n\r\n";
            }
            Console.WriteLine("checking basedir");
            if (!string.IsNullOrEmpty(BaseDir))
            {
                Console.WriteLine("basedir not null");
                var filesRegex = new Regex("^/files/(.*)");
                var filesMatch = filesRegex.Match(request.Path);
                if (filesMatch.Success && filesMatch.Groups.Count > 1)
                {
                    string fileName = filesMatch.Groups[1].Value;
                    string filepath = BaseDir + "/" + fileName;
                    if (request.Method == RequestMethod.POST)
                    {

                        Console.WriteLine("here2");
                        Console.WriteLine("file Content: {0}", request.Body);

                        string fileContent = request.Body ?? "";

                        StreamWriter fwriter = new StreamWriter(filepath);
                        fwriter.Write(fileContent.Replace("\0", string.Empty));
                        responseString = $"HTTP/1.1 201 Created\r\n\r\n";
                        fwriter.Close();


                    }
                    else if (request.Method == RequestMethod.GET)
                    {

                        if (File.Exists(filepath))
                        {
                            // Open the file for reading
                            FileStream fileStream = File.OpenRead(filepath);
                            StreamReader reader2 = new StreamReader(fileStream);
                            string content = reader2.ReadToEnd();
                            responseString = $"HTTP/1.1 200 OK\r\nContent-Type: application/octet-stream\r\nContent-Length: {content.Length}\r\n\r\n{content}";
                        }
                    }
                }
            }
            byte[] data = Encoding.ASCII.GetBytes(responseString);

            // Write the response data to the network stream.
            stream.Write(data, 0, data.Length);
            Console.WriteLine("Sent: {0}", responseString);

            // Close the connection to the client.
            client.Close();
        });
    }
}
