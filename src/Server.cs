using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;

// You can use print statements as follows for debugging, they'll be visible when running tests.
Console.WriteLine("Logs from your program will appear here!");

// Uncomment this block to pass the first stage
TcpListener server = new TcpListener(IPAddress.Any, 4221);
server.Start();
Socket client = server.AcceptSocket(); // wait for client

NetworkStream stream = new NetworkStream(client);
StreamReader reader = new StreamReader(stream, Encoding.UTF8);
StreamWriter writer = new StreamWriter(stream, Encoding.UTF8);


string? requestLine = reader.ReadLine();
Console.WriteLine("request recieved2 {0}", requestLine);

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

    headers.Add(headerName.ToLower(), headerValue);
    currHeader = reader.ReadLine();
}

Regex echoRegex = new Regex("^(/echo/(.*))$");
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





byte[] data = Encoding.ASCII.GetBytes(responseString);

// Write the response data to the network stream.
stream.Write(data, 0, data.Length);
Console.WriteLine("Sent: {0}", responseString);

// Close the connection to the client.
client.Close();