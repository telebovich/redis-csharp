using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;

var ProcessPing = (string s) =>
{
    return s.ToLower() switch
    {
        "ping" => "+PONG\r\n",
        _ => string.Format("${0}\r\n{1}\r\n", s.Length - 5, s.Substring(5)),
    };
};

var ProcessEcho = (string s) =>
{
    return string.Format("${0}\r\n{1}\r\n", s.Length - 5, s.Substring(5));
};

var HandleClient = (TcpClient client) =>
{
    NetworkStream stream = client.GetStream();
    
    byte[] buffer = new byte[1024];

    while (true)
    {
        StringBuilder messageBuilder = new();

        do
        {
            int received = stream.Read(buffer);

            if (received > 0)
            {
                string receivedData = Encoding.ASCII.GetString(buffer, 0, received);

                messageBuilder.Append(receivedData);
            }
        }
        while (stream.DataAvailable);

        var command = messageBuilder.ToString();

        // wtf
        var response = command.ToLower() switch
        {
            var s when s.StartsWith("ping") => ProcessPing(s),
            var s when s.StartsWith("echo") => ProcessEcho(s)
        };

        byte[] bytes = Encoding.ASCII.GetBytes(response);

        stream.Write(bytes);
    }
};

// You can use print statements as follows for debugging, they'll be visible when running tests.
Console.WriteLine("Logs from your program will appear here!");

// Uncomment this block to pass the first stage
TcpListener server = new TcpListener(IPAddress.Any, 6379);

try 
{
    server.Start();

    while (true)
    {
        TcpClient tcpClient = await server.AcceptTcpClientAsync();
        
        new Thread(() => HandleClient(tcpClient)).Start();
    }
}
catch (Exception ex)
{
    Console.WriteLine(ex.Message);
}
