# Baksteen.Net.LIRC
This library enables your .NET application to send infrared remote control commands to other devices _or_ receive infrared remote control commands

It works by connecting to a local (via unix domain socket) or remote (via tcp-ip) LIRC daemon, implementing the protocol described at [https://lirc.org/html/lircd.html](https://lirc.org/html/lircd.html) .

## Library usage

```csharp

    using System.Net;
    using Baksteen.Net.LIRC;

    ...

    // create client
    await using var client = new LIRCClient();

    // hook up event to receive remote control commands
    client.OnLIRCEventAsync = async ev =>
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

## How to set up LIRC hardware and software on linux

To set up LIRC on linux on raspberry pi, I used the following guides and sites:

- https://www.instructables.com/Setup-IR-Remote-Control-Using-LIRC-for-the-Raspber/
- https://www.instructables.com/Raspberry-Pi-Zero-Universal-Remote/
- https://www.digikey.com/en/maker/tutorials/2021/how-to-send-and-receive-ir-signals-with-a-raspberry-pi
- https://www.lirc.org/

Supported IR remote controls configuration files (.lircd.conf) can be found here: https://lirc-remotes.sourceforge.net/remotes-table.html
