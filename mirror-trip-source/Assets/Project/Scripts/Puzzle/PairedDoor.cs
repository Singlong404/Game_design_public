using UnityEngine;

namespace MirrorTrip
{
    public sealed class PairedDoor : MonoBehaviour
    {
        public Collider2D[] collisions;
        public SpriteRenderer[] visuals;
        public bool IsOpen { get; private set; }
        public string persistentId;
        public bool liftToOpen;
        public float liftHeight=5, liftDuration=.8f;
        public WorldSession session;
        private Vector3 closedPosition;
        private float amount, targetAmount;
        private bool initialized;
        private void Awake(){Initialize();}
        private void Initialize(){if(initialized)return;closedPosition=transform.position;initialized=true;}
        public void Restore(bool open)
        {SetOpen(open,false);}
        public void SetOpen(bool open,bool animate)
        {
            Initialize();IsOpen=open;targetAmount=open?1:0;
            if(!liftToOpen){foreach(var c in collisions)if(c)c.enabled=!open;foreach(var v in visuals)if(v)v.enabled=!open;}
            else {if(!animate)amount=targetAmount;ApplyLift();}
            Physics2D.SyncTransforms();
        }
        private void Update()
        {
            if(!liftToOpen||Mathf.Approximately(amount,targetAmount))return;
            if(session&&(!session.MirrorActive||session.Paused||session.PresentationBlocked||session.Reading||session.Fallen))return;
            amount=Mathf.MoveTowards(amount,targetAmount,Time.deltaTime/Mathf.Max(.05f,liftDuration));ApplyLift();Physics2D.SyncTransforms();
        }
        private void ApplyLift(){transform.position=closedPosition+Vector3.up*(liftHeight*Mathf.SmoothStep(0,1,amount));foreach(var v in visuals)if(v)v.enabled=true;foreach(var c in collisions)if(c)c.enabled=amount<.999f;}
        public void Open()
        {
            if (IsOpen) return;
            SetOpen(true,true);
        }
    }
}
