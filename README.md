# Baksteen.Net.LIRC
This library enables your .NET application to send and receive infrared remote control commands. This means it can control other devices via IR commands, or have your application be controlled by an IR remote.

It works by connecting to a local (via unix domain socket) or remote (via tcp-ip) LIRC daemon, implementing the protocol described at https://lirc.org/html/lircd.html . The LIRC daemon itself typically runs on a cheap single board computer (e.g. a raspberry pi) that can send/receive IR signals with a little bit of hardware.

Some usecases could be:
- create IR controlled robot using csharp/dotnet.
- create a multiroom IR repeater
- create an universal remote control application
- hook up 'dumb' climate control into a custom home automation system


## Library usage

```csharp

    using System.Net;
    using Baksteen.Net.LIRC;

    // create and connect client
    await using var client = await LIRCClient.Connect(
        new UnixDomainSocketEndPoint("/var/run/lirc/lircd"),
        new LIRCClientSettings
        {
            // hook up event to receive remote control commands
            OnLIRCEventAsync = async ev =>
            {
                Console.WriteLine($"event: {ev}");
                await Task.CompletedTask;
            }       
        }
    );

    // alternatively: to connect to remote LIRC daemon over TCP
    // ... new IPEndPoint(IPAddress.Parse("192.168.1.220"), 8765))

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
    await client.SendOnce("samsung", "POWER_ON" ,0);

```

## How to set up LIRC hardware and software on linux

To set up LIRC on linux on raspberry pi, I used the following guides and sites:

- https://www.instructables.com/Setup-IR-Remote-Control-Using-LIRC-for-the-Raspber/
- https://www.instructables.com/Raspberry-Pi-Zero-Universal-Remote/
- https://www.digikey.com/en/maker/tutorials/2021/how-to-send-and-receive-ir-signals-with-a-raspberry-pi
- https://www.lirc.org/

Supported IR remote controls configuration files (.lircd.conf) can be found here: https://lirc-remotes.sourceforge.net/remotes-table.html
