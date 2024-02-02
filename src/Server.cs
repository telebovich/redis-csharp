using System.Net;
using System.Net.Sockets;
using System.Text;

// You can use print statements as follows for debugging, they'll be visible when running tests.
Console.WriteLine("Logs from your program will appear here!");

// Uncomment this block to pass the first stage
TcpListener server = new TcpListener(IPAddress.Any, 6379);

server.Start();

string final = "";

string response = "+PONG\r\n";

while (true) {
    Socket socket = server.AcceptSocket(); // wait for client

    byte[] buffer = new byte[socket.SendBufferSize];

    int bytesRead = socket.Receive(buffer);

    if (bytesRead < buffer.Length)
    {
        Array.Resize(ref buffer, bytesRead);
    }

    string data = Encoding.ASCII.GetString(buffer, 0, bytesRead);

    Console.WriteLine("Received message: {0}", data);
    
    string[] dataMessages = data.Split('\n');

    foreach (string message in dataMessages) {
        final += response;
    }

    byte[] bytes = Encoding.ASCII.GetBytes(response);

    int i = socket.Send(bytes);
}
