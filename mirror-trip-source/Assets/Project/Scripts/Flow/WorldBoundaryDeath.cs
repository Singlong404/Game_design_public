using UnityEngine;
namespace MirrorTrip
{
    // World-relative bounds are editable independently; only the active actor is tested.
    public sealed class WorldBoundaryDeath:MonoBehaviour
    {
        public WorldSession session;
        public Rect mirrorBounds=new Rect(100,-130,370,175),surfaceBounds=new Rect(100,-130,370,175);
        public void Check(bool settling=false)
        {
            if(!settling&&(!session.CanInteract||session.MovementLocked))return;
            var p=session.MirrorActive?session.mirrorPlayer:session.surfacePlayer;
            if(!(session.MirrorActive?mirrorBounds:surfaceBounds).Contains(p.LocalPosition))session.KillOutOfBounds();
        }
        void OnDrawGizmosSelected()
        {
            if(!session)return;
            Draw(mirrorBounds,session.mirrorPlayer.worldRoot,Color.magenta);Draw(surfaceBounds,session.surfacePlayer.worldRoot,Color.cyan);
        }
        void Draw(Rect r,Transform root,Color c){Gizmos.color=c;Gizmos.DrawWireCube(root.position+(Vector3)r.center,new Vector3(r.width,r.height,1));}
    }
}
