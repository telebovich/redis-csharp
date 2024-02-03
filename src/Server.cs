using System.Net;
using System.Net.Sockets;
using System.Text;

// You can use print statements as follows for debugging, they'll be visible when running tests.
Console.WriteLine("Logs from your program will appear here!");

// Uncomment this block to pass the first stage
TcpListener server = new TcpListener(IPAddress.Any, 6379);

try 
{
    server.Start();

    using TcpClient handler = await server.AcceptTcpClientAsync();

    await using NetworkStream stream = handler.GetStream();

    while (true)
    {

        byte[] buffer = new byte[1024];

        StringBuilder messageBuilder = new();

        do
        {
            int received = await stream.ReadAsync(buffer);

            if (received > 0)
            {
                string receivedData = Encoding.ASCII.GetString(buffer, 0, received);
                
                messageBuilder.Append(receivedData);
            }
        }
        while (stream.DataAvailable);
        
        Console.WriteLine(messageBuilder.ToString());

        string response = "+PONG\r\n";

        byte[] bytes = Encoding.ASCII.GetBytes(response);

        await stream.WriteAsync(bytes);
    }
}
catch (Exception ex)
{
    Console.WriteLine(ex.Message);
}
finally
{
    server.Stop();
}
