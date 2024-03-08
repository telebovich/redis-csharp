using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Runtime.Caching;
using System.Text;

Dictionary<string, string> db = new Dictionary<string, string>();

var ParseLength = (byte[] bytes, int start) =>
{
    int len = 0;
    int i = start + 1;
    int consumed = 1;

    // 0x0D is '\r' hex value
    while (bytes[i] != 0x0D)
    {
        len = (len * 10) + (bytes[i] - '0');
        i++;
        consumed++;
    }

    // + 2 to account for ending '\r\n'
    return (len, consumed + 2);
};

var ParseBulkStr = (Byte[] bytes, int start) =>
{
    var (len, consumed) = ParseLength(bytes, start);
    Byte[] payload = new Byte[len];
    Array.Copy(bytes, start + consumed, payload, 0, len);
    var parsed = Encoding.ASCII.GetString(payload);
    // Console.WriteLine("ParsedBulkStr: {0}", parsed);

    // + 2 to account for ending '\r\n'
    return (parsed, consumed + len + 2);
};

var ParseClientMsg = (Byte[] msg) =>
{
    // Console.WriteLine("Client msg: {0}", Encoding.ASCII.GetString(msg));

    var (bulkStrsToParse, cursor) = ParseLength(msg, 0);
    String bulkStr = "";

    for (int i = 0; i < bulkStrsToParse; i++)
    {
        // Console.WriteLine("Cursor: {0}", cursor);
        var (str, consumed) = ParseBulkStr(msg, cursor);
        bulkStr += (str + " ");
        cursor += consumed;
    }
    bulkStr = bulkStr.TrimEnd();

    // Console.WriteLine("BulkStr: {0}", bulkStr);
    return bulkStr;
};

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

var ProcessSet = (string s) =>
{
    var parts = s.Split(" ");

    if (parts.Length != 3 || parts[0] != "set")
    {
        throw new ArgumentException("Expected: set <k> <v>, but got {0}", s);
    }

    db.Add(parts[1], parts[2]);

    return "+OK\r\n";
};

var ProcessGet = (string s) =>
{
    var parts = s.Split(" ");
    
    if (parts.Length != 2 || parts[0] != "get")
    {
        throw new ArgumentException("Expected: get <k>, but got {0}", s);
    }
    
    if (!db.ContainsKey(parts[1]))
    {
        return "$-1\r\n";
    }

    var val = db[parts[1]];

    return String.Format("${0}\r\n{1}\r\n", val.Length, val);
};

var HandleClient = (TcpClient client) =>
{
    NetworkStream stream = client.GetStream();
    
    byte[] buffer = new byte[1024];

    while (true)
    {
        int bytesRead = stream.Read(buffer);

        if (bytesRead == 0) return;

        if (bytesRead < buffer.Length)
        {
            Array.Resize(ref buffer, bytesRead);
        }

        String command = ParseClientMsg(buffer);

        // wtf
        var response = command.ToLower() switch
        {
            var s when s.StartsWith("ping") => ProcessPing(s),
            var s when s.StartsWith("echo") => ProcessEcho(s),
            var s when s.StartsWith("set") => ProcessSet(s),
            var s when s.StartsWith("get") => ProcessGet(s),
            _ => throw new ArgumentException(string.Format("Received unknown redis command: {0}", command)),
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
