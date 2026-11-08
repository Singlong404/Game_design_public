using UnityEngine;
namespace MirrorTrip
{
    // Scene-authored pair: carrying moves these objects, never an inventory copy.
    public sealed class SceneMirror : MonoBehaviour
    {
        public string mirrorId = "MIRROR_01", puzzleId = "PAVILION_01";
        public PlacementWallLink link;
        [Range(0, 1)] public float t;
        public GameObject surface, response;
        public float rotationOffset;
        public bool movable=true;
        public bool Available=>movable&&surface&&response&&response.activeInHierarchy&&(!response.TryGetComponent<MechanismMirror>(out var m)||!m.hideWhenDisabled||m.Enabled);
        public float Angle(PlacementWallLink wall)=>wall.angle+rotationOffset;
        public Vector2 Footprint
        {
            get
            {
                var box=response?response.GetComponentInChildren<BoxCollider2D>(true):null;
                if(!box)return new Vector2(1.1f,.14f);
                return new Vector2(Mathf.Abs(box.size.x*box.transform.lossyScale.x),Mathf.Abs(box.size.y*box.transform.lossyScale.y));
            }
        }
        public float Length=>Mathf.Max(Footprint.x,Footprint.y);
        public bool IsLocked { get; private set; }
        public void Restore(PlacementWallLink wall,float position,bool locked)
        {
            link=wall;t=position;IsLocked=false;
            surface.transform.SetPositionAndRotation(wall.SurfacePosition(t),Quaternion.Euler(0,0,Angle(wall)));
            response.transform.SetPositionAndRotation(wall.MirrorPosition(t),Quaternion.Euler(0,0,Angle(wall)));
            surface.SetActive(true);response.SetActive(true);if(locked)Lock();
        }
        public void Lock()
        {
            if (IsLocked) return;
            IsLocked = true;
            foreach (var part in new[] { surface, response })
                foreach (var renderer in part.GetComponentsInChildren<SpriteRenderer>(true))
                    renderer.color = Color.Lerp(MuchaPalette.Ordinary,MuchaPalette.Locked,.65f);
        }
    }
}
