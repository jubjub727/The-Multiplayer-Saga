using Networking;

Serialization.LoadAvailableTypes();
Console.WriteLine("Loaded Types");

NetworkedPlayer player = new NetworkedPlayer(1337);
player.Name = "poopypoop";
player.Transform.X = 4.5f;

DataSegment[] dataSegments = new DataSegment[1];
dataSegments[0] = new DataSegment(player);

Tick tick = new Tick(dataSegments);
Byte[] buffer = tick.Serialize();

Packet packet = new Packet(buffer);
Tick newTick = packet.Deserialize();

Console.WriteLine(newTick.DataSegments[0].Data.Name);
Console.WriteLine(player.Transform.X);