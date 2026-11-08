using UnityEngine;
namespace MirrorTrip
{
    // Moving/scaling a placement guide in the level editor updates its paired track.
    [ExecuteAlways, RequireComponent(typeof(PlacementWallLink))]
    public sealed class MirrorPlacementArea : MonoBehaviour
    {
        public PlacementWallLink link;
        public SpriteRenderer guide;
        [Tooltip("Keep the mirror face slightly in front of the wall guide.")]
        public float normalOffset;
        private void Awake(){Sync();}
        private void LateUpdate(){if(!Application.isPlaying)Sync();}
        public void Sync()
        {
            if(!link||!guide||!guide.sprite||!link.mirrorWorld||!link.surfaceWorld)return;
            var bounds=guide.sprite.bounds;
            link.responseStart=(Vector2)guide.transform.TransformPoint(bounds.center+Vector3.down*bounds.extents.y)-(Vector2)link.mirrorWorld.position;
            link.responseEnd=(Vector2)guide.transform.TransformPoint(bounds.center+Vector3.up*bounds.extents.y)-(Vector2)link.mirrorWorld.position;
            var tangent=link.responseEnd-link.responseStart;
            Vector2 normal=Quaternion.Euler(0,0,Mathf.Atan2(tangent.y,tangent.x)*Mathf.Rad2Deg+link.rotationOffset)*Vector3.down;
            link.responseStart+=normal*normalOffset;link.responseEnd+=normal*normalOffset;
            link.start=link.responseStart;link.end=link.responseEnd;
        }
    }
}
