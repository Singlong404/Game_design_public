using UnityEngine;
namespace MirrorTrip
{
    [RequireComponent(typeof(BoxCollider2D))]
    public sealed class LevelProgressPoint : MonoBehaviour
    {
        public enum Stage { UnlockSurface, ScenicView, FollowView }
        public Stage stage;
        public BoxCollider2D area;
        private void Awake(){if(!area)area=GetComponent<BoxCollider2D>();foreach(var r in GetComponentsInChildren<SpriteRenderer>())r.enabled=false;}
        public bool Contains(WorldPlayerMotor actor)=>area&&area.enabled&&area.bounds.Intersects(actor.Bounds);
        private void OnDrawGizmos(){var box=area?area:GetComponent<BoxCollider2D>();if(!box)return;var old=Gizmos.matrix;Gizmos.matrix=transform.localToWorldMatrix;Gizmos.color=stage==Stage.UnlockSurface?Color.cyan:Color.yellow;Gizmos.DrawWireCube(box.offset,box.size);Gizmos.matrix=old;}
    }
}
