using System.Net;
using System.Net.Sockets;
using System.Threading;
using Baksteen.Net.LIRC;

namespace lircclient;

internal class Program
{
    static async Task Main(string[] args)
    {
        await using var client = new LIRCClient();

        client.OnLIRCEventAsync = async ev =>
        {
            Console.WriteLine($"event: {ev}");
            await Task.CompletedTask;
        };

        await client.Connect(new IPEndPoint(IPAddress.Parse("192.168.1.220"), 8765));
        await client.Connect(new UnixDomainSocketEndPoint("/var/run/lirc/lircd"));

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

        await client.SendOnce("amino-aria7", "KEY_PLAY", 0);

        Console.WriteLine("press enter to stop listening for IR events..");
        Console.ReadLine();
    }
}
