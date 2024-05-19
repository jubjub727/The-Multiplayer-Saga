namespace Networking
{
    public class PreviousTransform
    {
        public Transform Transform;
        public long ElapsedTime;

        public PreviousTransform(Transform transform, long elapsedTime)
        {
            Transform = transform;
            ElapsedTime = elapsedTime;
        }
    }
}
