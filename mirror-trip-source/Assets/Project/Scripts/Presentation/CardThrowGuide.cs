using UnityEngine;
namespace MirrorTrip
{
    // Straight throwing preview only. Stops at the same first blocking surface as the card.
    public sealed class CardThrowGuide : MonoBehaviour
    {
        public WorldSession session;
        public Material material;
        public float length=3.2f, width=.022f;
        private readonly LineRenderer[] dashes=new LineRenderer[16];
        private readonly RaycastHit2D[] hits=new RaycastHit2D[32];
        public float VisibleLength { get; private set; }
        private void Awake()
        {
            for(int i=0;i<dashes.Length;i++)
            {
                var go=new GameObject("Dash_"+i);go.layer=gameObject.layer;go.transform.SetParent(transform,false);
                var line=go.AddComponent<LineRenderer>();line.sharedMaterial=material;line.useWorldSpace=true;line.positionCount=2;
                line.startWidth=line.endWidth=width;line.startColor=line.endColor=new Color(.38f,.38f,.38f,.65f);
                line.sortingLayerName="Effects";line.sortingOrder=20;line.numCapVertices=2;line.enabled=false;dashes[i]=line;
            }
        }
        private void LateUpdate()
        {
            bool show=session&&session.CanShoot;
            foreach(var d in dashes)d.enabled=false;
            if(!show){VisibleLength=0;return;}
            Vector2 origin=session.mirrorPlayer.AimOrigin,direction=session.AimDirection.normalized;
            float distance=length;var filter=new ContactFilter2D{useLayerMask=true,layerMask=session.cards.collisionMask,useTriggers=true};
            int count=Physics2D.CircleCast(origin,session.cards.radius,direction,filter,hits,length);
            for(int i=0;i<count;i++)
            {
                var c=hits[i].collider;
                if(c.isTrigger&&!c.GetComponentInParent<CardReflector>()&&!c.GetComponentInParent<CardSwitch>())continue;
                distance=Mathf.Min(distance,hits[i].distance);
            }
            VisibleLength=distance;
            for(int i=0;i<dashes.Length;i++)
            {
                float a=.28f+i*.2f;if(a>=distance)break;float b=Mathf.Min(a+.11f,distance);
                dashes[i].enabled=true;dashes[i].SetPosition(0,origin+direction*a);dashes[i].SetPosition(1,origin+direction*b);
            }
        }
    }
}
