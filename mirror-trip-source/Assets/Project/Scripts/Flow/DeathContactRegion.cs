using System;
using UnityEngine;
namespace MirrorTrip
{
    // Put on a "death" parent. Its child shapes are lethal; empty space between them is safe.
    public sealed class DeathContactRegion:MonoBehaviour
    {
        public WorldSession session;
        public bool surface=true,mirror=true;
        public float contactTolerance=.045f;
        Collider2D[] shapes=Array.Empty<Collider2D>();
        void Start(){Rebuild();}
        void OnTransformChildrenChanged(){Rebuild();}
        public void Rebuild(){shapes=GetComponentsInChildren<Collider2D>(true);if(!session)session=FindAnyObjectByType<WorldSession>();}
        void FixedUpdate(){Check();}
        public bool Check()
        {
            if(!session||!session.CanInteract||session.MirrorActive&&!mirror||!session.MirrorActive&&!surface)return false;
            var actor=session.MirrorActive?session.mirrorPlayer:session.surfacePlayer;
            var bounds=actor.Bounds;bounds.Expand(contactTolerance*2);
            foreach(var shape in shapes)
            {
                if(!shape||!shape.enabled||!shape.gameObject.activeInHierarchy||shape==actor.body||!shape.bounds.Intersects(bounds))continue;
                var d=actor.body.Distance(shape);
                if(d.isValid&&(d.isOverlapped||d.distance<=contactTolerance))
                {session.KillByFall();session.ShowMessage("碰到了危险区域。读取存档或重新开始。",30);return true;}
            }
            return false;
        }
    }
}
