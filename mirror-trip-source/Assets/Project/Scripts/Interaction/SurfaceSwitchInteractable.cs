using UnityEngine;
namespace MirrorTrip
{
    public sealed class SurfaceSwitchInteractable : MonoBehaviour
    {
        public CardSwitch target;
        public WorldSession session;
        public float radius=1.8f,holdDuration=.45f;
        public float Progress {get;private set;}
        private readonly RaycastHit2D[] sightHits=new RaycastHit2D[8];
        public bool InReach()
        {
            if(!isActiveAndEnabled||!session.CanSurfaceInteract||session.placement.IsCarrying||target.IsOn)return false;
            var actor=session.surfacePlayer;
            var shape=target.GetComponent<Collider2D>();
            Vector2 point=shape?shape.ClosestPoint(actor.transform.position):(Vector2)transform.position;
            if(Vector2.Distance(actor.transform.position,point)>radius)return false;
            var filter=new ContactFilter2D{useLayerMask=true,layerMask=actor.terrainMask,useTriggers=false};
            return Physics2D.Linecast(actor.transform.position,point,filter,sightHits)==0;
        }
        public void Hold(bool held,float delta)
        {
            if(!held||!InReach()){Progress=0;return;}
            Progress+=delta/holdDuration;
            if(Progress>=1){target.Hit();Progress=0;session.ShowMessage("机关已开启。里世界发生了变化。",4);}
        }
    }
}
