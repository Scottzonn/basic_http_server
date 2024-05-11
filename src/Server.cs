using System.Net;
using System.Net.Sockets;

// You can use print statements as follows for debugging, they'll be visible when running tests.
Console.WriteLine("Logs from your program will appear here!");

// Uncomment this block to pass the first stage
TcpListener server = new TcpListener(IPAddress.Any, 4221);
server.Start();
Socket client = server.AcceptSocket(); // wait for clien  // Send a response to the client.

string responseString = "HTTP/1.1 200 OK\r\n\r\n";
byte[] data = System.Text.Encoding.ASCII.GetBytes(responseString);

// Get a network stream from the accepted client.
using (NetworkStream stream = new NetworkStream(client))
{
    // Write the response data to the network stream.
    stream.Write(data, 0, data.Length);
    Console.WriteLine("Sent: {0}", responseString);
}

// Close the connection to the client.
client.Close();