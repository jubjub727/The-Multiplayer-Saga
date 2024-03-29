using Networking;

Serialization.LoadAvailableTypes();

Console.WriteLine("Loaded Types");

while (true)
{
    TransformData transformData = new TransformData(1337);
    transformData.X = 1.45f;

    AnimationData animationData = new AnimationData("testAnimation");

    DataSegment[] dataSegments = new DataSegment[2];
    dataSegments[0] = new DataSegment(transformData);
    dataSegments[1] = new DataSegment(animationData);


    Tick tick = new Tick(dataSegments);

    Byte[] buffer = tick.Serialize();

    Packet packet = new Packet(buffer);

    Tick newTick = packet.Deserialize();

    Console.WriteLine(newTick.DataSegments[1].Data.Animation);
}