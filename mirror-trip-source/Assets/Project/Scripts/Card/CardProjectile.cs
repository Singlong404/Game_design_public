using UnityEngine;

namespace MirrorTrip
{
    public sealed class CardProjectile : MonoBehaviour
    {
        public bool IsCopy { get; internal set; }
        private Vector2 direction;
        public bool alignToFlight;
        public Vector2 Direction { get=>direction; internal set {direction=value;if(alignToFlight&&value.sqrMagnitude>.001f)transform.rotation=Quaternion.Euler(0,0,Mathf.Atan2(value.y,value.x)*Mathf.Rad2Deg);} }
        public float RemainingLife { get; internal set; }
        public int Reflections { get; internal set; }
        public Collider2D LastMirror { get; internal set; }
        public Vector2 Position { get => transform.position; internal set => transform.position = new Vector3(value.x, value.y, 0); }
    }
}
