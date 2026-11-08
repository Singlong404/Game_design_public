using System;
using UnityEngine;
namespace MirrorTrip
{
    [Serializable] public sealed class StoryBeat
    {
        public string speaker="血魔";
        [TextArea(2,6)] public string text;
        public bool female=true;
        [Range(0,5)] public int expression;
        [Min(.5f)] public float seconds=3.5f;
    }
    [CreateAssetMenu(menuName="Mirror Trip/剧情片段",fileName="Story_Sequence")]
    public sealed class StorySequence:ScriptableObject
    {
        public string chapter;
        public StoryBeat[] lines=Array.Empty<StoryBeat>();
        [TextArea(2,6)] public string closingText="今天先到这里。\n明天，我再往前走一点。";
        public float Duration {get{float time=0;foreach(var line in lines)time+=Mathf.Max(.5f,line.seconds);return time;}}
        public StoryBeat At(float time){foreach(var line in lines){time-=Mathf.Max(.5f,line.seconds);if(time<0)return line;}return lines.Length>0?lines[lines.Length-1]:null;}
    }
}
