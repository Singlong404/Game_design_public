using UnityEngine;
namespace MirrorTrip
{
    // Distance-based alternating stamps; teleporting never draws a trail across the level.
    public sealed class SurfaceFootstepFX:MonoBehaviour
    {
        public WorldSession session;public Sprite[] frames;public Material material;
        public float spacing=.65f,lifetime=1.45f,width=.55f,opacity=.42f;
        public int Emitted{get;private set;}
        public int ActiveCount{get{int n=0;foreach(var p in pool)if(p.age<lifetime)n++;return n;}}
        sealed class Stamp{public SpriteRenderer view;public float age=100;}
        Stamp[] pool;Vector2 last;float distance;int cursor;bool left;
        void Awake(){pool=new Stamp[24];for(int i=0;i<pool.Length;i++){var g=new GameObject("Footprint_"+i);g.transform.SetParent(transform,false);g.layer=LayerMask.NameToLayer("SurfaceWorld");var r=g.AddComponent<SpriteRenderer>();r.sharedMaterial=material;r.sortingOrder=20;r.enabled=false;pool[i]=new Stamp{view=r};}last=session.surfacePlayer.transform.position;}
        void LateUpdate()
        {
            Vector2 current=session.surfacePlayer.transform.position,delta=current-last;last=current;
            if(session.Fallen||session.Loading||session.presentation.TitleOpen){Clear();return;}
            if(session.Paused||session.presentation.BlocksGameplay){distance=0;return;}
            foreach(var p in pool)if(p.age<lifetime){p.age+=Time.deltaTime;float t=p.age/lifetime;p.view.enabled=t<1;p.view.color=new Color(.7f,.7f,.7f,opacity*(1-t));p.view.sprite=frames[Mathf.Min(frames.Length-1,(int)(t*frames.Length))];}
            if(session.MirrorActive||!session.CanInteract||delta.magnitude>2){distance=0;return;}
            if(delta.sqrMagnitude<.000001f)return;
            distance+=delta.magnitude;if(distance<spacing)return;distance%=spacing;
            var direction=delta.normalized;var side=new Vector2(-direction.y,direction.x)*(left?.14f:-.14f);left=!left;
            var stamp=pool[cursor++%pool.Length];stamp.age=0;stamp.view.enabled=true;stamp.view.sprite=frames[0];
            stamp.view.transform.position=new Vector3(current.x+side.x-direction.x*.25f,current.y+side.y-direction.y*.25f,-.1f);
            stamp.view.transform.rotation=Quaternion.Euler(0,0,Mathf.Atan2(direction.y,direction.x)*Mathf.Rad2Deg-90);
            stamp.view.transform.localScale=Vector3.one*(width/frames[0].bounds.size.x);stamp.view.color=new Color(.7f,.7f,.7f,opacity);Emitted++;
        }
        public void Clear(){if(pool!=null)foreach(var p in pool){p.age=100;p.view.enabled=false;}distance=0;last=session.surfacePlayer.transform.position;}
    }
}
