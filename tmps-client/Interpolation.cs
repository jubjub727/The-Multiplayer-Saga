using MathNet.Numerics;
using Networking;
using Riptide.Utils;
using System.Diagnostics;

namespace tmpsclient;
public class Interpolation
{
    double[] pointsArrayX = new double[Utils.PacketDepth];
    double[] pointsArrayY = new double[Utils.PacketDepth];
    double[] pointsArrayZ = new double[Utils.PacketDepth];
    double[] pointsArrayRX = new double[Utils.PacketDepth];
    double[] pointsArrayRY = new double[Utils.PacketDepth];
    double[] pointsArrayRZ = new double[Utils.PacketDepth];
    double[] pointsArrayVX = new double[Utils.PacketDepth];
    double[] pointsArrayVY = new double[Utils.PacketDepth];
    double[] pointsArrayVZ = new double[Utils.PacketDepth];
    double[] valuesArrayX = new double[Utils.PacketDepth];
    double[] valuesArrayY = new double[Utils.PacketDepth];
    double[] valuesArrayZ = new double[Utils.PacketDepth];
    double[] valuesArrayRX = new double[Utils.PacketDepth];
    double[] valuesArrayRY = new double[Utils.PacketDepth];
    double[] valuesArrayRZ = new double[Utils.PacketDepth];
    double[] valuesArrayVX = new double[Utils.PacketDepth];
    double[] valuesArrayVY = new double[Utils.PacketDepth];
    double[] valuesArrayVZ = new double[Utils.PacketDepth];

    private Stopwatch TimeSinceLastTick;

    public Interpolation(Stopwatch timeSinceLastTick)
    {
        TimeSinceLastTick = timeSinceLastTick;
    }

    public void Interpolate(List<NetworkedPlayer> PlayerPool)
    {
        foreach (NetworkedPlayer player in PlayerPool)
        {
            if (player.PreviousTransforms.Count < Utils.PacketDepth) {
                //Console.WriteLine("Only Have ({0}/{1}) Transforms", player.PreviousTransforms.Count, PacketDepth);
                continue;
            }
            if (player.Entity == nint.Zero) {
                //Console.WriteLine("No Entity for {0}({1})", player.Name, player.PlayerId);
                continue;
            }

            long globalElapsedTime = 0;
            long elapsedTime = 0;

            int count = 0;

            foreach (PreviousTransform previousTransform in player.PreviousTransforms)
            {
                if (count < Utils.PacketDepth - Utils.PacketOffset)
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
                pointsArrayVX[count] = globalElapsedTime;
                pointsArrayVY[count] = globalElapsedTime;
                pointsArrayVZ[count] = globalElapsedTime;

                valuesArrayX[count] = (double)previousTransform.Transform.X;
                valuesArrayY[count] = (double)previousTransform.Transform.Y;
                valuesArrayZ[count] = (double)previousTransform.Transform.Z;
                valuesArrayRX[count] = (double)previousTransform.Transform.RX;
                valuesArrayRY[count] = (double)previousTransform.Transform.RY;
                valuesArrayRZ[count] = (double)previousTransform.Transform.RZ;
                valuesArrayVX[count] = (double)previousTransform.Transform.VX;
                valuesArrayVY[count] = (double)previousTransform.Transform.VY;
                valuesArrayVZ[count] = (double)previousTransform.Transform.VZ;

                count++;
            }

            elapsedTime += TimeSinceLastTick.ElapsedTicks;

            var methodX = MathNet.Numerics.Interpolate.Linear(pointsArrayX, valuesArrayX);
            var methodY = MathNet.Numerics.Interpolate.Linear(pointsArrayY, valuesArrayY);
            var methodZ = MathNet.Numerics.Interpolate.Linear(pointsArrayZ, valuesArrayZ);
            var methodRX = MathNet.Numerics.Interpolate.Linear(pointsArrayRX, valuesArrayRX);
            var methodRY = MathNet.Numerics.Interpolate.Linear(pointsArrayRY, valuesArrayRY);
            var methodRZ = MathNet.Numerics.Interpolate.Linear(pointsArrayRZ, valuesArrayRZ);
            var methodVX = MathNet.Numerics.Interpolate.Linear(pointsArrayVX, valuesArrayVX);
            var methodVY = MathNet.Numerics.Interpolate.Linear(pointsArrayVY, valuesArrayVY);
            var methodVZ = MathNet.Numerics.Interpolate.Linear(pointsArrayVZ, valuesArrayVZ);

            Transform transform = player.Transform;

            transform.X = (float)methodX.Interpolate(elapsedTime);
            transform.Y = (float)methodY.Interpolate(elapsedTime);
            transform.Z = (float)methodZ.Interpolate(elapsedTime);
            transform.RX = (float)methodRX.Interpolate(elapsedTime);
            transform.RY = (float)methodRY.Interpolate(elapsedTime);
            transform.RZ = (float)methodRZ.Interpolate(elapsedTime);
            transform.VX = (float)methodVX.Interpolate(elapsedTime);
            transform.VY = (float)methodVY.Interpolate(elapsedTime);
            transform.VZ = (float)methodVZ.Interpolate(elapsedTime);

            if (transform.IsBadTransform())
            {
                RiptideLogger.Log(LogType.Error, String.Format("Calculated bad Transform for {0}({1})", player.Name, player.PlayerId));
                continue;
            }

            player.ApplyTransform(transform);
        }
        TimeSinceLastTick.Restart();
    }
}
