using UnityEngine;
namespace MirrorTrip
{
    // Reusable art prefab. Bind to existing logic; never edits collision geometry or puzzle state.
    [DefaultExecutionOrder(500)]
    public sealed class InteractiveInkView : MonoBehaviour
    {
        public enum Kind { Notice, Chest, Mirror, Switch, Door, Goal, Card }
        public Kind kind;
        public WorldSession session;
        public TextInteractable entry;
        public SpriteRenderer ink, accent;
        public Sprite[] states;
        public Vector2 size=Vector2.one;
        public bool mirrorSide;
        public bool placementPreview;
        public bool preserveAspect;
        public bool sliced;
        private float fittedScale;
        public int State { get; private set; }
        private Vector3 origin;
        private float transition;
        private float motionClock;
        private bool wasTriggered;
        private void Awake(){origin=transform.localPosition;if(!session)session=FindAnyObjectByType<WorldSession>();if(kind==Kind.Chest||!entry)entry=GetComponentInParent<TextInteractable>();if(entry)mirrorSide=entry.mirrorSide;}
        private void LateUpdate()
        {
            if(!session||!ink||states==null||states.Length==0)return;
            bool active=session.MirrorActive==mirrorSide;
            if(active&&session.CanInteract)motionClock+=Time.deltaTime;
            bool triggered=kind==Kind.Chest?entry&&entry.Opened:session.puzzleSwitch.IsOn;
            if(triggered!=wasTriggered){transition=0;wasTriggered=triggered;}
            if(triggered&&!session.Paused&&!(session.presentation&&session.presentation.SettingsOpen))transition+=Time.unscaledDeltaTime;
            var actor=mirrorSide?session.mirrorPlayer:session.surfacePlayer;
            bool near=active&&Vector2.Distance(actor.transform.position,transform.position)<(entry?entry.interactionRadius:2.7f);
            int next=0;
            switch(kind)
            {
                case Kind.Notice:next=near?1:0;break;
                case Kind.Chest:next=triggered?1:0;break;
                case Kind.Mirror:next=triggered?2:near?1:0;break;
                case Kind.Switch:next=triggered?(transition<.16f?1:2):0;break;
                case Kind.Goal:next=session.Completed?2:triggered?1:0;break;
                case Kind.Door:next=triggered?(transition<.5f?1:2):0;break;
                case Kind.Card:next=(int)(motionClock*8)%states.Length;break;
            }
            State=Mathf.Min(next,states.Length-1);ink.sprite=states[State];ink.color=Color.white;
            ink.transform.localScale=new Vector3(size.x/ink.sprite.bounds.size.x,size.y/ink.sprite.bounds.size.y,1);
            if(preserveAspect)
            {
                if(fittedScale<=0){float w=0,h=0;foreach(var frame in states){w=Mathf.Max(w,frame.bounds.size.x);h=Mathf.Max(h,frame.bounds.size.y);}fittedScale=Mathf.Min(size.x/w,size.y/h);}
                ink.transform.localScale=new Vector3(fittedScale,fittedScale,1);
            }
            if(sliced){ink.drawMode=SpriteDrawMode.Sliced;ink.transform.localScale=Vector3.one;ink.size=size;}
            if(kind==Kind.Door)
            {
                ink.enabled=!triggered||transition<.55f;
                transform.localPosition=origin+Vector3.up*(triggered?Mathf.SmoothStep(0,size.y,transition/.55f):0);
            }
            if(kind==Kind.Chest&&triggered)transform.localPosition=origin+Vector3.up*(transition<.3f?.045f*Mathf.Sin(transition/.3f*Mathf.PI):0);
            if(kind==Kind.Card)
            {
                var projectile=GetComponentInParent<CardProjectile>();
                if(projectile)transform.localRotation=Quaternion.Euler(0,0,Mathf.Atan2(projectile.Direction.y,projectile.Direction.x)*Mathf.Rad2Deg-90);
            }
            if(accent)
            {
                accent.enabled=kind!=Kind.Notice&&kind!=Kind.Chest||near||triggered;
                var color=kind==Kind.Switch||kind==Kind.Card?GameplayAccent.Red:GameplayAccent.Blue;
                if(placementPreview)color=session.placement.DestinationValid?GameplayAccent.Blue:GameplayAccent.Red;
                if(!active)color=new Color(color.grayscale,color.grayscale,color.grayscale,1);
                accent.color=color;
                accent.transform.parent.localScale=new Vector3(1,triggered&&transition<.25f?1+Mathf.Sin(transition*12)*.25f:1,1);
            }
        }
    }
}
