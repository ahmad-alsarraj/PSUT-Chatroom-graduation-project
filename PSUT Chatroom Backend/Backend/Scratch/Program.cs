using System.Net.WebSockets;
using System.Text;

namespace Scratch;
public class Program
{
    public static async Task Main()
    {
        const string auth = "6:637769186010591497", convId = "-1";
        ClientWebSocket ws = new();
        ws.Options.SetRequestHeader("Authorization", auth);
        await ws.ConnectAsync(new Uri($"ws://localhost:1234/Conversation/Subscribe"), default).ConfigureAwait(false);
        byte[] buff = new byte[5000];
        var res = await ws.ReceiveAsync(buff, default).ConfigureAwait(false);
        while (res.Count != 0)
        {
            Console.WriteLine(Encoding.Default.GetString(buff.AsSpan(0, res.Count)));
            if (res.CloseStatus != null) { break; }
            res = await ws.ReceiveAsync(buff, default).ConfigureAwait(false);
        }
    }
}