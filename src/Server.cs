using System.Net;
using System.Net.Sockets;
using System.Reflection.Metadata;
using System.Text;
using System.Text.RegularExpressions;


class Program
{
    static async Task Main(string[] args)
    {
        // Uncomment this block to pass the first stage
        TcpListener server = new TcpListener(IPAddress.Any, 4221);
        Console.WriteLine("Starting server...");
        server.Start();
        Console.WriteLine("Server started on port 4421");

        while (true)
        {
            var client = server.AcceptSocketAsync();
            handleStuff(client);
        }


        async void handleStuff(Task<Socket> clientTask)
        {
            var client = await clientTask;
            Console.WriteLine("Client connected");
            NetworkStream stream = new NetworkStream(client);
            StreamReader reader = new StreamReader(stream, Encoding.UTF8);
            StreamWriter writer = new StreamWriter(stream, Encoding.UTF8);

            string? requestLine = reader.ReadLine();

            string[] lineSplit = requestLine.Split(" ");
            string method = lineSplit[0];
            string path = lineSplit[1];
            string httpVersion = lineSplit[2];

            Dictionary<string, string> headers = new Dictionary<string, string>();
            string? currHeader = reader.ReadLine();
            while (currHeader?.Length > 0)
            {
                string[] header = currHeader.Split(":", 2);
                string headerName = header[0];
                string headerValue = header[1];

                headers.Add(headerName.ToLower(), headerValue.Trim());
                currHeader = reader.ReadLine();
            }

            string? body = reader.ReadLine();

            Regex echoRegex = new Regex("^/echo/(.*)");
            Regex userAgentRegex = new Regex("^(/user-agent)$");

            string responseString = "HTTP/1.1 404 Not Found\r\n\r\n";
            var userAgentPath = userAgentRegex.Match(path);
            if (userAgentPath.Success)
            {
                Console.WriteLine("matches user agent");
                string? userAgentValue;
                headers.TryGetValue("user-agent", out userAgentValue);
                responseString = $"HTTP/1.1 200 OK\r\nContent-Type: text/plain\r\nContent-Length: {userAgentValue?.Length}\r\n\r\n{userAgentValue}";
            }
            var match = echoRegex.Match(path);
            if (match.Success && match.Groups.Count > 1)
            {
                Console.WriteLine("Matches echo");
                string toEcho = match.Groups[1].Value;
                responseString = $"HTTP/1.1 200 OK\r\nContent-Type: text/plain\r\nContent-Length: {toEcho.Length}\r\n\r\n{toEcho}";
            }
            if (path == "/")
            {
                responseString = "HTTP/1.1 200 OK\r\n\r\n";
            }
            if (args.Length >= 2)
            {
                Console.WriteLine("Directory2:");
                if (args[0].ToLower() == "--directory")
                {

                    string baseDir = args[1];
                    var filesRegex = new Regex("^/files/(.*)");
                    var filesMatch = filesRegex.Match(path);
                    if (filesMatch.Success && filesMatch.Groups.Count > 1)
                    {
                        string fileName = filesMatch.Groups[1].Value;
                        string filepath = baseDir + "/" + fileName;

                        if (method == "POST")
                        {
                            string fileContent = body ?? "";
                            FileStream fileStream = File.OpenWrite(filepath);
                            StreamWriter fwriter = new StreamWriter(fileStream);
                            fwriter.Write(fileContent);
                            responseString = $"HTTP/1.1 201 OK\r\n\r\n";

                        }
                        else if (method == "GET")
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
            }
            byte[] data = Encoding.ASCII.GetBytes(responseString);

            // Write the response data to the network stream.
            stream.Write(data, 0, data.Length);
            Console.WriteLine("Sent: {0}", responseString);

            // Close the connection to the client.
            client.Close();
        }
    }
}
