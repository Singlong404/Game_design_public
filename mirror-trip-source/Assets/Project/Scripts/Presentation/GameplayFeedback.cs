using System;
using UnityEngine;
namespace MirrorTrip
{
    public enum FeedbackKind { Activate,Hit,Sprint,Land,Flame,Fragments,Teleport }
    [Serializable] public sealed class FeedbackClip {public FeedbackKind kind;public Sprite[] frames;public float fps=16,width=2;public AudioClip sound;}
    public sealed class GameplayFeedback:MonoBehaviour
    {
        public WorldSession session;
        public FeedbackClip[] clips=Array.Empty<FeedbackClip>();
        public Material spriteMaterial;
        public int capacity=24;
        public int TotalPlayed{get;private set;}
        public int ActiveCount{get{int n=0;foreach(var p in pool)if(p.active)n++;return n;}}
        sealed class Burst {public SpriteRenderer view;public FeedbackClip clip;public float clock;public bool active;}
        Burst[] pool=Array.Empty<Burst>();AudioSource audioSource;bool wasGrounded,wasSprinting,wasMirror;Vector2 lastMirrorPosition;float dashClock,soundClock;
        void Awake()
        {
            pool=new Burst[capacity];for(int i=0;i<pool.Length;i++){var g=new GameObject("FX_Burst_"+i);g.transform.SetParent(transform,false);var r=g.AddComponent<SpriteRenderer>();r.sharedMaterial=spriteMaterial;r.sortingOrder=45;r.enabled=false;pool[i]=new Burst{view=r};}
            audioSource=gameObject.AddComponent<AudioSource>();audioSource.playOnAwake=false;audioSource.spatialBlend=0;audioSource.volume=.25f;
            lastMirrorPosition=session.mirrorPlayer.transform.position;wasMirror=session.MirrorActive;
        }
        public void Play(FeedbackKind kind,Vector2 position,bool mirror=true,float scale=1,bool flip=false)
        {
            if(!session||session.Paused||session.Loading||session.presentation.TitleOpen)return;
            var clip=Array.Find(clips,c=>c.kind==kind);if(clip==null||clip.frames==null||clip.frames.Length==0)return;
            var b=Array.Find(pool,p=>!p.active)??pool[TotalPlayed%pool.Length];b.active=true;b.clock=0;b.clip=clip;b.view.gameObject.layer=LayerMask.NameToLayer(mirror?"MirrorWorld":"SurfaceWorld");
            b.view.transform.position=new Vector3(position.x,position.y,-.3f);b.view.flipX=flip;b.view.sprite=clip.frames[0];b.view.enabled=true;
            float width=clip.frames[0].bounds.size.x;b.view.transform.localScale=Vector3.one*(clip.width*scale/Mathf.Max(.01f,width));b.view.color=new Color(1,1,1,kind==FeedbackKind.Sprint?.3f:1);b.view.sortingOrder=kind==FeedbackKind.Sprint?15:45;TotalPlayed++;
            if(clip.sound&&Time.unscaledTime>soundClock){audioSource.PlayOneShot(clip.sound,.3f*session.presentation.effectsVolume.value);soundClock=Time.unscaledTime+.06f;}
            if(kind==FeedbackKind.Fragments)b.view.sprite=clip.frames[TotalPlayed%clip.frames.Length];
            if(kind==FeedbackKind.Activate)for(int i=0;i<3;i++)Play(FeedbackKind.Fragments,position+UnityEngine.Random.insideUnitCircle*.35f,mirror,.35f);
        }
        void Update()
        {
            if(session&&(session.Fallen||session.presentation.TitleOpen)){Clear();return;}
            if(!session||session.Paused||session.presentation.BlocksGameplay||session.Fallen)return;
            foreach(var b in pool)if(b.active){b.clock+=Time.deltaTime;if(b.clip.kind==FeedbackKind.Fragments){b.view.transform.position+=new Vector3(.12f,.35f,0)*Time.deltaTime;b.view.color=new Color(1,1,1,Mathf.Clamp01(1-b.clock/.65f));if(b.clock>.65f){b.active=false;b.view.enabled=false;}continue;}int frame=(int)(b.clock*b.clip.fps);if(frame>=b.clip.frames.Length){b.active=false;b.view.enabled=false;}else b.view.sprite=b.clip.frames[frame];}
            var p=session.mirrorPlayer;
            if(session.MirrorActive&&session.CanInteract)
            {
                if(!wasGrounded&&p.Grounded&&Vector2.Distance(lastMirrorPosition,p.transform.position)<3)Play(FeedbackKind.Land,new Vector2(p.Bounds.center.x,p.Bounds.min.y+.05f));
                dashClock-=Time.deltaTime;if(p.Sprinting&&(!wasSprinting||dashClock<=0)){Play(FeedbackKind.Sprint,p.Bounds.center,true,1,p.Facing.x>0);dashClock=.22f;}
            }
            if(wasMirror!=session.MirrorActive){var active=session.MirrorActive?p:session.surfacePlayer;Play(FeedbackKind.Teleport,active.Bounds.center,session.MirrorActive,.8f);}
            wasGrounded=p.Grounded;wasSprinting=p.Sprinting;wasMirror=session.MirrorActive;lastMirrorPosition=p.transform.position;
        }
        public void Clear(){foreach(var b in pool){b.active=false;b.view.enabled=false;}if(audioSource)audioSource.Stop();}
    }
}
