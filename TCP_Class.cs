using System;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

public class TcpClientService
{
    private TcpClient _client;
    private NetworkStream _stream;
    public bool Client_Connected = false;

    public int RCV_LEN = 0;
    public byte[] RCV_Bytes = null;






    //public event EventHandler<string> Message_Connected;


    /*
    public async Task<bool> ConnectAsync(string host, int port)
    {
        try
        {
            _client = new TcpClient();
            await _client.ConnectAsync(host, port);
            _stream = _client.GetStream();
            return true;
        }
        catch (Exception ex)
        {
            //Console.WriteLine($"Connection failed: {ex.Message}");
            return false;
        }
    }
    */

    public async Task ConnectAsync(string host, int port)
    {
        if (Client_Connected == true)
        {
            return;
        }

        try
        {
            _client = new TcpClient();
            await _client.ConnectAsync(host, port);
            _stream = _client.GetStream();
            Client_Connected = true;

            //Message_Connected?.Invoke(this, "Client_Connected = true;");
        }
        catch (Exception ex)
        {
            //if (_stream == null) throw new InvalidOperationException(ex.Message);
            //Console.WriteLine($"Connection failed: {ex.Message}");
            Client_Connected = false;

            //Message_Connected?.Invoke(this, "Client_Connected = false;");



        }
    }



    //public async Task SendAsync(byte[] data)
    //{
    //    if (_stream == null)
    //    {
    //        return;
    //    }


    //    //if (_stream == null) throw new InvalidOperationException("Not connected");
    //    //byte[] data = Encoding.UTF8.GetBytes(message);
    //    await _stream.WriteAsync(data, 0, data.Length);
    //}

    ////public async Task<string> ReceiveAsync()
    ////{
    ////    if (_stream == null) throw new InvalidOperationException("Not connected");
    ////    byte[] buffer = new byte[1024];
    ////    int bytes = await _stream.ReadAsync(buffer, 0, buffer.Length);
    ////    return Encoding.UTF8.GetString(buffer, 0, bytes);
    ////}
    /// <summary>
    /// 
    public async Task SendAsync(byte[] data)
    {
        if (_stream == null)
            throw new InvalidOperationException("TCP stream is not connected.");

        await _stream.WriteAsync(data, 0, data.Length);
        await _stream.FlushAsync();
    }

    /// </summary>
    /// <returns></returns>

    public async Task ReceiveAsync()
    {
        RCV_LEN = -2;

        if (_stream == null)
        {
            RCV_Bytes = null;
            RCV_LEN = -1;
            return;
        }

        //if (_stream == null) throw new InvalidOperationException("Not connected");
        byte[] buffer = new byte[10000];

        try
        {
            int bytes = await _stream.ReadAsync(buffer, 0, buffer.Length);
            RCV_LEN = bytes;
            RCV_Bytes = new byte[RCV_LEN];
            for (int i = 0; i < RCV_LEN; i++)
            {
                RCV_Bytes[i] = buffer[i];
            }
        }
        catch (Exception ex)
        {
            RCV_Bytes = null;
            RCV_LEN = -3;
        }

        //return Encoding.UTF8.GetString(buffer, 0, bytes);
    }



    public void Disconnect()
    {
        Client_Connected = false;
        _stream?.Close();
        _client?.Close();
    }
}
