using System.Net;
using System.Net.Sockets;
using System.Threading;
using Baksteen.Net.LIRC;

namespace lirctcpclient;


internal class Program
{
    static async Task Main(string[] args)
    {
        await using var client = new LIRCClient();

        client.OnEvent = async ev =>
        {
            Console.WriteLine($"event: {ev}");
            await Task.CompletedTask;
        };

        //new UnixDomainSocketEndPoint("/usr/var/run/lirc/lircd");
        await client.Connect(new IPEndPoint(IPAddress.Parse("192.168.1.220"), 8765));

        var version = await client.GetVersion();
        Console.WriteLine($"lircd version {version}");

        var remoteControls = await client.ListRemoteControls();

        foreach(var remoteControl in remoteControls)
        {
            Console.WriteLine(remoteControl);
        }

        var remoteControlKeys = await client.ListRemoteControlKeys(remoteControls[1]);
        //var remoteControlKeys = await client.ListRemoteControlKeys("bestaatniet");

        foreach(var remoteControlKey in remoteControlKeys)
        {
            Console.WriteLine(remoteControlKey);
        }

        //await client.Test();
        Console.ReadLine();
    }
}
