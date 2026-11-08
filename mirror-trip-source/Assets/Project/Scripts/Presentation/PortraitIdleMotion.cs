using System;
using UnityEngine;
namespace MirrorTrip
{
    [Serializable] public sealed class PortraitBlinkClip { public Sprite open,half,closed; public Vector2 openOffset,halfOffset,closedOffset; }
    public sealed class PortraitIdleMotion:MonoBehaviour
    {
        public PortraitBlinkClip[] female=Array.Empty<PortraitBlinkClip>(),male=Array.Empty<PortraitBlinkClip>();
        public Vector2 interval=new Vector2(1.2f,2.6f);
        public Vector2 CurrentOffset {get;private set;}
        public float halfSeconds=.07f,closedSeconds=.10f;
        public int BlinkCount{get;private set;}
        public int Phase{get;private set;}
        float untilBlink,clock;bool blinking,initialized,lastFemale;int lastExpression=-1,lastFrame=-1;
        public Sprite Resolve(bool woman,int expression,bool reduced,bool visible)
        {
            var clips=woman?female:male;if(clips.Length==0)return null;
            expression=Mathf.Clamp(expression,0,clips.Length-1);var clip=clips[expression];
            if(!initialized||lastFemale!=woman||lastExpression!=expression){initialized=true;lastFemale=woman;lastExpression=expression;ResetTimer();}
            if(reduced||!visible){blinking=false;Phase=0;CurrentOffset=clip.openOffset;return clip.open;}
            if(lastFrame!=Time.frameCount)
            {
                lastFrame=Time.frameCount;
                if(blinking){clock+=Time.unscaledDeltaTime;if(clock>=halfSeconds*2+closedSeconds)ResetTimer();}
                else{untilBlink-=Time.unscaledDeltaTime;if(untilBlink<=0){blinking=true;clock=0;BlinkCount++;}}
            }
            Phase=!blinking?0:clock<halfSeconds?1:clock<halfSeconds+closedSeconds?2:1;
            CurrentOffset=Phase==2?clip.closedOffset:Phase==1?clip.halfOffset:clip.openOffset;
            return Phase==2?clip.closed:Phase==1?clip.half:clip.open;
        }
        void ResetTimer(){blinking=false;clock=0;Phase=0;untilBlink=UnityEngine.Random.Range(interval.x,interval.y);}
    }
}
