using UnityEngine;
namespace MirrorTrip
{
    // Query from the body, not the foot pivot. Triggers and the target itself do not occlude interaction.
    public static class InteractionReach
    {
        public static Vector2 TargetPoint(Transform target,Vector2 origin)
        {
            var contact=target.GetComponentInChildren<Collider2D>();
            if(contact&&contact.enabled)return contact.ClosestPoint(origin);
            foreach(var r in target.GetComponentsInChildren<SpriteRenderer>())
                if(r.enabled&&r.sprite)return r.bounds.ClosestPoint(origin);
            return target.position;
        }
        public static bool CanReach(WorldPlayerMotor actor,Transform target,float radius)
        {
            Vector2 origin=actor.Bounds.center,point=TargetPoint(target,origin);
            return Clear(actor,point,radius,target);
        }
        public static bool Clear(WorldPlayerMotor actor,Vector2 point,float radius,Transform target=null)
        {
            Vector2 origin=actor.Bounds.center,delta=point-origin;
            if(delta.magnitude>radius)return false;
            var filter=new ContactFilter2D{useLayerMask=true,layerMask=actor.terrainMask,useTriggers=false};
            var hits=new RaycastHit2D[32];
            int count=Physics2D.Raycast(origin,delta.normalized,filter,hits,delta.magnitude);
            for(int i=0;i<count;i++)
            {
                var hit=hits[i];
                if(hit.collider==actor.body||target&&(hit.transform==target||hit.transform.IsChildOf(target)))continue;
                if(hit.distance<delta.magnitude-.04f)return false;
            }
            return true;
        }
    }
}
