using UnityEngine;
namespace MirrorTrip
{
    // The object remains editable in the scene; runtime visibility never disables its controller.
    public sealed class LevelObjectActuator : MonoBehaviour
    {
        public enum Effect { Move, Open, Appear, Disappear }
        public CardSwitch source;
        public WorldSession session;
        public Effect effect;
        public Vector3 restingPosition, activatedPosition;
        public float restingAngle, activatedAngle, duration=.8f;
        public Renderer[] visuals;
        public Collider2D[] collisions;
        public bool Active {get;private set;}
        public bool hideWhenOpen;
        Vector3 from;float fromAngle,elapsed;bool moving;
        void Awake()
        {
            // Moving the authored object in Edit Mode should remain the new baseline.
            Vector3 authored=transform.localPosition;
            if(effect==Effect.Appear){Vector3 offset=restingPosition-activatedPosition;activatedPosition=authored;restingPosition=authored+offset;}
            else if(effect==Effect.Open){Vector3 travel=activatedPosition-restingPosition;restingPosition=authored;activatedPosition=authored+travel;}
            else if(effect==Effect.Disappear)restingPosition=activatedPosition=authored;
            else restingPosition=authored;
            restingAngle=transform.localEulerAngles.z;
            if(effect!=Effect.Move)activatedAngle=restingAngle;
            source.StateApplied+=Apply;
        }
        void Start(){Apply(source.IsOn,false);}
        void OnDestroy(){if(source)source.StateApplied-=Apply;}
        public void Apply(bool on,bool animate)
        {
            Active=on;from=transform.localPosition;fromAngle=transform.localEulerAngles.z;elapsed=0;
            moving=animate&&duration>0;
            if(!moving)Pose(1);
            Visibility();
        }
        void Update()
        {
            if(!moving||session&&(session.Paused||session.Reading||session.Fallen||session.PresentationBlocked))return;
            elapsed+=Time.deltaTime;float t=Mathf.Clamp01(elapsed/duration);Pose(t*t*(3-2*t));
            if(t>=1){moving=false;Visibility();}
        }
        void Pose(float t)
        {
            transform.localPosition=Vector3.Lerp(from,Active?activatedPosition:restingPosition,t);
            transform.localRotation=Quaternion.Euler(0,0,Mathf.LerpAngle(fromAngle,Active?activatedAngle:restingAngle,t));
            Physics2D.SyncTransforms();
        }
        void Visibility()
        {
            bool visible=effect==Effect.Appear?Active:effect!=Effect.Disappear||!Active;
            if(effect==Effect.Open&&hideWhenOpen&&Active&&!moving)visible=false;
            foreach(var r in visuals)if(r)r.enabled=visible;
            foreach(var c in collisions)if(c)c.enabled=visible&&!(effect==Effect.Open&&Active&&!moving);
        }
    }
}
