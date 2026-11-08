using UnityEngine;

namespace MirrorTrip
{
    public sealed class CardReflector : MonoBehaviour
    {
        public Vector2 localFront = Vector2.down;
        public int instanceId;
        public Vector2 Normal => ((Vector2)transform.TransformDirection(localFront)).normalized;
        public bool Reflect(Vector2 incoming, out Vector2 outgoing)
        {
            var normal = Normal;
            outgoing = Vector2.Reflect(incoming, normal).normalized;
            return Vector2.Dot(incoming, normal) < -.001f;
        }
    }
}
