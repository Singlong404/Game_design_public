using System;
using UnityEngine;
namespace MirrorTrip
{
    [Serializable] public sealed class BackgroundRegion
    {
        public string title;
        public float startX;
        public Vector2 origin;
        public Sprite far,middle,near;
        [Range(0,1)]public float farOpacity=.85f,middleOpacity=.3f,nearOpacity=.2f;
    }
    [DefaultExecutionOrder(300)]
    public sealed class LevelBackgroundRig:MonoBehaviour
    {
        public WorldSession session;
        public BackgroundRegion[] regions=Array.Empty<BackgroundRegion>();
        public Material spriteMaterial;
        public float blendSeconds=1.4f;
        [Header("Only the opening corridor; later regions retain their existing framing")]
        public bool alignOpeningGround;
        public float openingEndX=175,openingFloorY=1,openingGroundUV=.1f,openingBlendDistance=4;
        public string CurrentRegion=>current>=0?regions[current].title:"";
        public int RendererCount=>views!=null?views.Length:0;
        SpriteRenderer[,,] views;int current=-1,previous=-1;float blend=1;
        void Awake()
        {
            views=new SpriteRenderer[2,3,9];for(int bank=0;bank<2;bank++)
            {
                var parent=new GameObject(bank==0?"Current_Backdrop":"Fading_Backdrop").transform;parent.SetParent(transform,false);
                for(int layer=0;layer<3;layer++)for(int cell=0;cell<9;cell++)
                {
                    var g=new GameObject("Layer"+layer+"_Tile"+cell);g.transform.SetParent(parent,false);g.layer=LayerMask.NameToLayer("MirrorWorld");
                    var r=g.AddComponent<SpriteRenderer>();r.sharedMaterial=spriteMaterial;r.sortingOrder=-600+bank*40+layer*10;r.enabled=false;views[bank,layer,cell]=r;
                }
            }
        }
        void LateUpdate()
        {
            if(regions.Length==0)return;
            float x=session.mirrorPlayer.LocalPosition.x;int selected=0;for(int i=0;i<regions.Length;i++)if(x>=regions[i].startX)selected=i;
            if(selected!=current){previous=current;current=selected;blend=previous<0?1:0;}
            if(!session.Paused)blend=Mathf.MoveTowards(blend,1,Time.unscaledDeltaTime/Mathf.Max(.1f,blendSeconds));
            var camera=session.finale&&session.finale.Cinematic?session.finale.endingCamera:session.mirrorCamera;
            Render(0,current,blend,camera);Render(1,previous,1-blend,camera);
        }
        void Render(int bank,int index,float opacity,Camera camera)
        {
            for(int layer=0;layer<3;layer++)
            {
                var region=index>=0?regions[index]:null;Sprite sprite=region==null?null:layer==0?region.far:layer==1?region.middle:region.near;
                float alpha=region==null?0:opacity*(layer==0?region.farOpacity:layer==1?region.middleOpacity:region.nearOpacity);
                for(int cell=0;cell<9;cell++)views[bank,layer,cell].enabled=sprite&&alpha>.001f;
                if(!sprite||alpha<=.001f)continue;
                float height=Mathf.Max(camera.orthographicSize*2.35f,18),scale=height/sprite.bounds.size.y,width=sprite.bounds.size.x*scale;
                if(width<camera.orthographicSize*camera.aspect*2.3f){scale=camera.orthographicSize*camera.aspect*2.3f/sprite.bounds.size.x;width=sprite.bounds.size.x*scale;height=sprite.bounds.size.y*scale;}
                Vector2 movement=(Vector2)camera.transform.position-region.origin;float ratio=layer==0?.055f:layer==1?.12f:.22f;
                if(session.presentation.MotionReduced)ratio=0;
                Vector2 offset=new Vector2(Mathf.Repeat(-movement.x*ratio+width*.5f,width)-width*.5f,Mathf.Repeat(-movement.y*ratio+height*.5f,height)-height*.5f);
                float centerY=camera.transform.position.y+offset.y;
                if(alignOpeningGround&&index==0){float weight=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(openingEndX-openingBlendDistance,openingEndX,session.mirrorPlayer.LocalPosition.x));centerY=Mathf.Lerp(centerY,openingFloorY+(.5f-openingGroundUV)*height,weight);}
                for(int cell=0;cell<9;cell++)
                {
                    var r=views[bank,layer,cell];r.sprite=sprite;r.color=new Color(1,1,1,alpha);r.transform.localScale=Vector3.one*scale;
                    r.transform.position=new Vector3(camera.transform.position.x+offset.x+(cell%3-1)*width,centerY+(cell/3-1)*height,5+layer);
                }
            }
        }
    }
}
