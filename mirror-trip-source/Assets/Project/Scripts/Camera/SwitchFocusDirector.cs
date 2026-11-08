using System;
using UnityEngine;
namespace MirrorTrip
{
    [Serializable] public sealed class SwitchFocusBinding
    {
        public CardSwitch source;
        public Transform target;
        public bool mirrorSide=true;
        public Vector3 targetOffset;
        public float size=6;
        [NonSerialized] public Action<bool,bool> handler;
    }
    // Temporary camera presentation never changes the level director's cue or follow settings.
    [DefaultExecutionOrder(140)]
    public sealed class SwitchFocusDirector:MonoBehaviour
    {
        public WorldSession session;
        public SwitchFocusBinding[] bindings=Array.Empty<SwitchFocusBinding>();
        public float travelSeconds=.45f,holdSeconds=.75f,returnSeconds=.45f;
        public bool Blocking{get;private set;}
        public bool Busy=>Blocking||pending!=null;
        public string ActiveSwitch=>active!=null&&active.source?active.source.name:"";
        SwitchFocusBinding pending,active;Camera view;PlayerFollowCamera follow;
        bool followWasEnabled;Vector3 returnPosition,focusPosition;float returnSize,clock;
        string cueAtStart;Vector3 actorAtStart;
        void Awake(){foreach(var b in bindings){var bound=b;b.handler=(on,animate)=>{if(on&&animate)Request(bound);};if(b.source)b.source.StateApplied+=b.handler;}}
        void OnDestroy(){Cancel();foreach(var b in bindings)if(b.source)b.source.StateApplied-=b.handler;}
        public void Request(SwitchFocusBinding b){if(b!=null&&b.target)pending=b;}
        bool Interrupt=>session.Paused||session.Fallen||session.Loading||session.Reading||session.presentation.BlocksGameplay||(session.finale&&(session.finale.InputBlocked||session.finale.AtFinalPoint));
        void Update()
        {
            if(Interrupt){Cancel();return;}
            if(!Blocking&&pending!=null)Begin();
            if(Blocking&&session.cameraDirector&&session.cameraDirector.Current!=cueAtStart){Cancel();return;}
        }
        void Begin()
        {
            active=pending;pending=null;view=active.mirrorSide?session.mirrorCamera:session.surfaceCamera;follow=view.GetComponent<PlayerFollowCamera>();
            returnPosition=view.transform.position;returnSize=view.orthographicSize;followWasEnabled=follow&&follow.enabled;if(follow)follow.enabled=false;
            var actuator=active.target.GetComponent<LevelObjectActuator>();
            focusPosition=actuator&&(actuator.effect==LevelObjectActuator.Effect.Move||actuator.effect==LevelObjectActuator.Effect.Appear)?actuator.transform.parent.TransformPoint(actuator.activatedPosition):active.target.position+active.targetOffset;focusPosition.z=returnPosition.z;
            actorAtStart=(active.mirrorSide?session.mirrorPlayer:session.surfacePlayer).transform.position;
            cueAtStart=session.cameraDirector?session.cameraDirector.Current:null;clock=0;Blocking=true;session.SetMovement(Vector2.zero);session.SetSprint(false);
            session.ShowMessage(active.mirrorSide==session.MirrorActive?"机关有变化。":"另一边的机关有变化。",2);
        }
        void LateUpdate()
        {
            if(!Blocking||!view)return;
            if(Interrupt){Cancel();return;}
            clock+=Time.unscaledDeltaTime;
            float a=travelSeconds,b=a+holdSeconds,c=b+returnSeconds;
            float blend=clock<a?Mathf.SmoothStep(0,1,clock/Mathf.Max(.01f,a)):clock<b?1:1-Mathf.SmoothStep(0,1,(clock-b)/Mathf.Max(.01f,returnSeconds));
            if(session.presentation.MotionReduced)blend=clock<b?1:0;
            view.transform.position=Vector3.Lerp(returnPosition,focusPosition,blend);view.orthographicSize=Mathf.Lerp(returnSize,active.size,blend);
            if(clock>=c)Finish();
        }
        void Finish()
        {
            if(view){view.transform.position=returnPosition;view.orthographicSize=returnSize;}
            if(follow){follow.enabled=followWasEnabled;var actor=active.mirrorSide?session.mirrorPlayer:session.surfacePlayer;if(Vector3.Distance(actor.transform.position,actorAtStart)>.1f)follow.SnapToTarget();}
            Blocking=false;active=null;view=null;follow=null;
        }
        public void Cancel(){pending=null;if(Blocking)Finish();}
    }
}
