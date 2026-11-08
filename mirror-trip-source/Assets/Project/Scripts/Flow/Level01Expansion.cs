using UnityEngine;
namespace MirrorTrip
{
    [System.Serializable] public sealed class Level01ExpansionSave {public bool d,e,f,g;public float normalCameraSize=8;}
    public sealed class Level01Expansion : MonoBehaviour
    {
        public WorldSession session;
        public LevelProgressPoint pointD,pointE,pointF,pointG;
        public float firstFallLimit=5.3f,secondFallLimit=7.93f;
        public CardSwitch switch22,switch23;
        public MechanismMirror portalM1,portalM2,portalM3;
        public ParticleSystem snow;
        public bool ReachedD{get;private set;}
        public bool ReachedE{get;private set;}
        public bool ReachedF{get;private set;}
        public bool ReachedG{get;private set;}
        public bool SnowActive=>ReachedF&&!ReachedG;
        public float FallLimit=>ReachedE?secondFallLimit:firstFallLimit;
        public float FallDistance=>airborne?Mathf.Max(0,peak-session.mirrorPlayer.transform.position.y):0;
        float peak,normalSpeed,normalSize;bool airborne,normalSprint;
        PlayerFollowCamera Follow=>session.levelSequence.mirrorFollow;
        void Awake(){normalSpeed=session.mirrorPlayer.moveSpeed;normalSprint=session.mirrorPlayer.sprintAllowed;normalSize=8;switch22.StateChanged+=SyncRoute;switch23.StateChanged+=SyncRoute;}
        void Start(){SyncRoute(false);ResetFall();}
        void OnDestroy(){if(switch22)switch22.StateChanged-=SyncRoute;if(switch23)switch23.StateChanged-=SyncRoute;}
        public void SyncRoute(bool unused=false)
        {
            bool reroute=switch23.IsOn,open=switch22.IsOn||reroute;
            portalM1.SetEnabled(open);portalM2.SetEnabled(open&&!reroute);portalM3.SetEnabled(reroute);
            portalM1.partner=reroute?portalM3:portalM2;portalM2.partner=reroute?null:portalM1;portalM3.partner=null;
            portalM1.exitOnly=portalM2.exitOnly=false;portalM3.exitOnly=true;
        }
        public void Tick()
        {
            if(!session.MirrorActive||!session.CanInteract)return;
            var actor=session.mirrorPlayer;
            if(!ReachedD&&pointD.Contains(actor)){ReachedD=true;ResetFall();}
            if(!ReachedE&&pointE.Contains(actor)){ReachedD=ReachedE=true;}
            if(!ReachedF&&pointF.Contains(actor)){ReachedF=true;normalSize=Follow.halfHeight;ApplySnow(false);}
            if(!ReachedG&&pointG.Contains(actor)){ReachedG=true;ApplySnow(false);}
            SampleFall(actor.transform.position.y,actor.Grounded);
        }
        // Only a completed landing can kill. Each teleport begins a fresh descent.
        public void SampleFall(float y,bool grounded)
        {
            if(!ReachedD){peak=y;airborne=!grounded;return;}
            if(!grounded){peak=Mathf.Max(peak,y);airborne=true;}
            else{if(airborne&&peak-y>FallLimit+.001f)session.KillByFall();peak=y;airborne=false;}
        }
        public void ResetFall(){peak=session.mirrorPlayer.transform.position.y;airborne=!session.mirrorPlayer.Grounded;}
        void ApplySnow(bool instant)
        {
            session.mirrorPlayer.moveSpeed=SnowActive?normalSpeed*.5f:normalSpeed;
            session.mirrorPlayer.sprintAllowed=SnowActive?false:normalSprint;
            session.mirrorPlayer.ResetTransient();
            if(ReachedF&&!session.cameraDirector)Follow.SetFollowView(SnowActive?3:normalSize,instant);
            if(!snow)return;
            if(SnowActive)snow.Play();else snow.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
        }
        void LateUpdate()
        {
            if(!snow||!SnowActive)return;
            var cam=session.mirrorCamera;var p=cam.transform.position;
            // Keep the storm around the entire viewport during fast downward travel, not only above it.
            snow.transform.position=new Vector3(p.x,p.y,-2);
            var shape=snow.shape;shape.scale=new Vector3(Mathf.Max(24,cam.orthographicSize*cam.aspect*2+14),Mathf.Max(20,cam.orthographicSize*2+14),1);
            bool running=session.MirrorActive&&session.CanInteract;
            if(running&&snow.isPaused)snow.Play();else if(!running&&snow.isPlaying)snow.Pause();
        }
        public Level01ExpansionSave Capture()=>new Level01ExpansionSave{d=ReachedD,e=ReachedE,f=ReachedF,g=ReachedG,normalCameraSize=normalSize};
        public void Restore(Level01ExpansionSave data)
        {
            ReachedD=data!=null&&data.d;ReachedE=data!=null&&data.e;ReachedF=data!=null&&data.f;ReachedG=data!=null&&data.g;
            normalSize=data!=null&&data.normalCameraSize>0?data.normalCameraSize:8;SyncRoute();ApplySnow(true);ResetFall();
        }
    }
}
