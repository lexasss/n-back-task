using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace NBackTask;

public class TcpServer : IDisposable
{
    public static int Port => 8963;

    public event EventHandler Started = delegate { };
    public event EventHandler ClientConnected = delegate { };
    public event EventHandler ClientDisconnected = delegate { };

    public bool IsListening { get; private set; } = false;
    public bool IsClientConnected { get; private set; } = false;
    public bool IsDisposed { get; private set; } = false;


    public void Dispose()
    {
        IsDisposed = true;

        GC.SuppressFinalize(this);
    }

    public void Restart(int delay)
    {
        var timer = new System.Timers.Timer(delay * 1000);
        timer.Elapsed += (s, a) => {
            timer.Stop();
            Start();
        };
        timer.Start();
    }

    public async void Send(string data)
    {
        if (_connection != null)
        {
            var bytes = Encoding.ASCII.GetBytes(data + "\n");
            await _connection.SendAsync(bytes);
        }
    }

    public async void Start()
    {
        var ipAddress = IPAddress.Any;
        var localEndPoint = new IPEndPoint(ipAddress, Port);

        var listener = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

        try
        {
            listener.Bind(localEndPoint);
            listener.Listen(100);

            IsListening = true;
            Started(this, new EventArgs());
            Debug.WriteLine($"listening on port {Port}...");

            while (!IsDisposed)
            {
                _connection = await listener.AcceptAsync();
                if (_connection == null)
                {
                    break;
                }

                IsClientConnected = true;
                ClientConnected(this, new EventArgs());
                Debug.WriteLine($"Connected");

                var data = new byte[1024];
                while (!IsDisposed)
                {
                    var buffer = new ArraySegment<byte>(data);
                    var byteCount = await _connection.ReceiveAsync(buffer, SocketFlags.None);

                    if (byteCount == 0 || buffer.Array == null)
                    {
                        break;
                    }
                    else
                    {
                        string msg = Encoding.ASCII.GetString(buffer.Array, 0, byteCount).Trim();
                        if (_digitSign != '.')
                        {
                            msg = msg.Replace('.', _digitSign);
                        }

                        Debug.WriteLine($"Data received: {msg}");
                    }
                }

                IsClientConnected = false;

                Debug.WriteLine($"Disconnected");
                ClientDisconnected(this, new EventArgs());

                _connection.Close();
                _connection = null;
            }

        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Connection error: {ex.Message}");
        }
        finally
        {
            listener.Close();
            listener.Dispose();
            IsListening = false;
        }

        if (IsClientConnected)
        {
            IsClientConnected = false;
            ClientDisconnected(this, new EventArgs());
        }

        Debug.WriteLine($"Exit");
    }

    // Internal

    private readonly char _digitSign = System.Globalization.NumberFormatInfo.CurrentInfo.NumberDecimalSeparator[0];

    private Socket? _connection;
}
