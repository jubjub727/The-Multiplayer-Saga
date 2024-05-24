using Networking;
using System.Diagnostics;

namespace tmpsclient;
public class Interpolation
{
    private const int PacketDepth = 32;
    private const int PacketOffset = 5;

    double[] pointsArrayX = new double[PacketDepth];
    double[] pointsArrayY = new double[PacketDepth];
    double[] pointsArrayZ = new double[PacketDepth];
    double[] pointsArrayRX = new double[PacketDepth];
    double[] pointsArrayRY = new double[PacketDepth];
    double[] pointsArrayRZ = new double[PacketDepth];
    double[] valuesArrayX = new double[PacketDepth];
    double[] valuesArrayY = new double[PacketDepth];
    double[] valuesArrayZ = new double[PacketDepth];
    double[] valuesArrayRX = new double[PacketDepth];
    double[] valuesArrayRY = new double[PacketDepth];
    double[] valuesArrayRZ = new double[PacketDepth];

    private Stopwatch TimeSinceLastTick;

    public Interpolation(Stopwatch timeSinceLastTick)
    {
        TimeSinceLastTick = timeSinceLastTick;
    }

    public void Interpolate(List<NetworkedPlayer> PlayerPool)
    {
        foreach (NetworkedPlayer player in PlayerPool)
        {
            if (player.PreviousTransforms.Count < PacketDepth) {
                continue;
            }

            if (player.Entity == nint.Zero) {
                continue;
            }

            long globalElapsedTime = 0;
            long elapsedTime = 0;

            int count = 0;

            foreach (PreviousTransform previousTransform in player.PreviousTransforms)
            {
                if (count < PacketDepth - PacketOffset)
                {
                    elapsedTime += previousTransform.ElapsedTime;
                }

                globalElapsedTime += previousTransform.ElapsedTime;

                pointsArrayX[count] = globalElapsedTime;
                pointsArrayY[count] = globalElapsedTime;
                pointsArrayZ[count] = globalElapsedTime;
                pointsArrayRX[count] = globalElapsedTime;
                pointsArrayRY[count] = globalElapsedTime;
                pointsArrayRZ[count] = globalElapsedTime;

                valuesArrayX[count] = (double)previousTransform.Transform.X;
                valuesArrayY[count] = (double)previousTransform.Transform.Y;
                valuesArrayZ[count] = (double)previousTransform.Transform.Z;
                valuesArrayRX[count] = (double)previousTransform.Transform.RX;
                valuesArrayRY[count] = (double)previousTransform.Transform.RY;
                valuesArrayRZ[count] = (double)previousTransform.Transform.RZ;

                count++;
            }

            elapsedTime += TimeSinceLastTick.ElapsedTicks;

            var methodX = MathNet.Numerics.Interpolate.Linear(pointsArrayX, valuesArrayX);
            var methodY = MathNet.Numerics.Interpolate.Linear(pointsArrayY, valuesArrayY);
            var methodZ = MathNet.Numerics.Interpolate.Linear(pointsArrayZ, valuesArrayZ);
            var methodRX = MathNet.Numerics.Interpolate.Linear(pointsArrayRX, valuesArrayRX);
            var methodRY = MathNet.Numerics.Interpolate.Linear(pointsArrayRY, valuesArrayRY);
            var methodRZ = MathNet.Numerics.Interpolate.Linear(pointsArrayRZ, valuesArrayRZ);

            Transform transform = player.Transform;

            transform.X = (float)methodX.Interpolate(elapsedTime);
            transform.Y = (float)methodY.Interpolate(elapsedTime);
            transform.Z = (float)methodZ.Interpolate(elapsedTime);
            transform.RX = (float)methodRX.Interpolate(elapsedTime);
            transform.RY = (float)methodRY.Interpolate(elapsedTime);
            transform.RZ = (float)methodRZ.Interpolate(elapsedTime);

            if (transform.IsBadTransform())
            {
                continue;
            }

            player.ApplyTransform(transform);
        }
    }
}
