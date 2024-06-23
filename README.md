# Baksteen.Net.LIRC
This library enables your .NET application to send infrared remote control commands to other devices _or_ receive infrared remote control commands

It works by connecting to a local (via unix domain socket) or remote (via tcp-ip) LIRC daemon, implementing the protocol described at [https://lirc.org/html/lircd.html](https://lirc.org/html/lircd.html) .

## Usage

```csharp

    using System.Net;
    using Baksteen.Net.LIRC;

    ...

    // create client
    await using var client = new LIRCClient();

    // hook up event to receive remote control commands
    client.OnEvent = async ev =>
    {
        Console.WriteLine($"event: {ev}");
        await Task.CompletedTask;
    };
    
    // connect to local LIRC daemon 
    await client.Connect(new UnixDomainSocketEndPoint("/var/run/lirc/lircd"));

    // alternatively: connect to remote LIRC daemon over TCP
    // await client.Connect(new IPEndPoint(IPAddress.Parse("192.168.1.220"), 8765));

    // list available remote controls
    var remoteControls = await client.ListRemoteControls();

    foreach(var remoteControl in remoteControls)
    {
        Console.WriteLine(remoteControl);
    }

    // list available remote control keys for a specific remote
    var remoteControlKeys = await client.ListRemoteControlKeys("samsung");

    foreach(var remoteControlKey in remoteControlKeys)
    {
        Console.WriteLine(remoteControlKey);
    }

    // send IR command, no repeats
    await client.SendOnce("samsung","POWER_ON",0);

```