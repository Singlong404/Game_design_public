using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace MirrorTrip
{
    public enum StoryTrigger { Start,Area,Switch,Surface,Arrival,TeleportAfterC,FinalPoint }
    [Serializable] public sealed class StoryCue
    {
        public string id,requires,afterText;
        public int node,priority=10,expiresAt;
        public StorySequence sequence;
        public bool portrait,mirror=true,ground;
        public StoryTrigger trigger;
        public Collider2D area;
        public CardSwitch gate;
        public bool beforeSwitch;
        [NonSerialized]public bool pending;
    }
    [Serializable] public sealed class IdleLine {public int id;public string text;}
    [Serializable] public sealed class StoryProgressSave {public string[] seen;public int[] idleUsed;public int stage,idleCount;}
    [Serializable] public sealed class StoryImport {public StoryImportNode[] nodes;public IdleLine[] idle;}
    [Serializable] public sealed class StoryImportNode {public int node;public StoryBeat[] lines;}
    // Authored references, separate bottom/modal channels, no timers tied to global timeScale.
    [DefaultExecutionOrder(-80)]
    public sealed class LevelStoryDirector:MonoBehaviour
    {
        public WorldSession session;
        public StoryCue[] cues=Array.Empty<StoryCue>();
        public IdleLine[] idleLines=Array.Empty<IdleLine>();
        public float stalledSeconds=90,idleMinimum=65,idleMaximum=95;
        public int Stage {get;private set;}
        public string BottomText {get;private set;}="";
        public bool ArrivalPending=>session.levelSequence&&session.levelSequence.SurfaceUnlocked&&!seen.Contains("MAIN_04");
        public string CurrentId=>active!=null?active.id:"";
        public event Action<int> NodeStarted;
        readonly HashSet<string> seen=new HashSet<string>();readonly HashSet<int> usedIdle=new HashSet<int>();
        readonly Dictionary<CardSwitch,Action<bool,bool>> handlers=new Dictionary<CardSwitch,Action<bool,bool>>();
        StoryCue active;int page,idleCount,idleThisPuzzle;float clock,quiet,stalled;bool modalStarted,lastTease,wasDead;Vector2 previousPosition;float lastShot=-100;
        public bool HasSeen(string id)=>seen.Contains(id);
        void Start()
        {
            session.storyDirector=this;previousPosition=session.mirrorPlayer.transform.position;
            foreach(var sw in session.transform.root.GetComponentsInChildren<CardSwitch>(true))
            {
                var captured=sw;Action<bool,bool> h=(on,notify)=>{if(!on||!notify)return;Progress();foreach(var c in cues)if(c.gate==captured&&c.trigger==StoryTrigger.Switch)c.pending=true;};
                handlers[sw]=h;sw.StateApplied+=h;
            }
        }
        void OnDestroy(){foreach(var pair in handlers)if(pair.Key)pair.Key.StateApplied-=pair.Value;}
        public void Progress(){stalled=0;idleThisPuzzle=0;quiet=Mathf.Max(quiet,20);}
        public void Shot(){lastShot=Time.unscaledTime;}
        bool Suspended=>session.Paused||session.Fallen||session.Loading||session.Exited||session.presentation.TitleOpen||session.presentation.SettingsOpen||session.Reading||(session.presentation.journal&&session.presentation.journal.IsOpen)||(session.presentation.saveSlots&&session.presentation.saveSlots.IsOpen);
        void Update()
        {
            if(!session)return;
            if(session.Fallen){wasDead=true;BottomText="";return;}
            if(wasDead){quiet=20;wasDead=false;}
            if(Suspended){BottomText="";return;}
            bool preview=session.switchFocus&&session.switchFocus.Busy;
            bool clip=session.finale&&session.finale.ClipPlaying;
            bool cinema=session.finale&&session.finale.Cinematic;
            if(cinema){active=null;BottomText="";return;}
            Vector2 now=session.mirrorPlayer.transform.position;bool teleported=Vector2.Distance(now,previousPosition)>3;previousPosition=now;
            foreach(var c in cues)
            {
                if(seen.Contains(c.id)||c.pending||c.expiresAt>0&&Stage>=c.expiresAt)continue;
                bool touch=c.area&&c.area.bounds.Intersects((c.mirror?session.mirrorPlayer:session.surfacePlayer).Bounds);
                if(c.trigger==StoryTrigger.Start)c.pending=true;
                else if(c.trigger==StoryTrigger.Arrival)c.pending=session.levelSequence.SurfaceUnlocked;
                else if(c.trigger==StoryTrigger.Surface)c.pending=!session.MirrorActive&&seen.Contains("MAIN_04");
                else if(c.trigger==StoryTrigger.FinalPoint)c.pending=session.finale.AtFinalPoint;
                else if(c.trigger==StoryTrigger.TeleportAfterC)c.pending=session.levelSequence.ReachedC&&teleported;
                else if(c.trigger==StoryTrigger.Area&&session.MirrorActive==c.mirror&&touch&&(!c.beforeSwitch||!c.gate.IsOn))c.pending=true;
            }
            // First contact/loss/safety beats interrupt low-priority walking subtitles.
            var urgent=cues.Where(c=>Eligible(c)&&(c.priority>=50||active!=null&&active.id=="HINT")).OrderByDescending(c=>c.priority).FirstOrDefault();
            if(active!=null&&!active.portrait&&urgent!=null&&urgent.priority>active.priority){active=null;BottomText="";}
            if(active!=null)
            {
                if(active.expiresAt>0&&Stage>=active.expiresAt||active.beforeSwitch&&active.gate.IsOn){active=null;BottomText="";}
                else if(active.portrait)
                {
                    if(modalStarted&&!session.presentation.StoryOpen){string hint=active.afterText;bool opening=active.id=="MAIN_01";active=null;modalStarted=false;Progress();if(!string.IsNullOrEmpty(hint))BeginTransient(hint,opening?10:5);}
                    return;
                }
                else if(!preview&&!clip&&!session.presentation.StoryOpen)
                {
                    BottomText=Format(active.sequence.lines[page]);clock-=Time.unscaledDeltaTime;
                    if(clock<=0){page++;if(page>=active.sequence.lines.Length){active=null;BottomText="";}else clock=active.sequence.lines[page].seconds;}
                    return;
                }
            }
            if(preview||clip||session.presentation.StoryOpen){BottomText="";return;}
            var next=cues.Where(Eligible).OrderByDescending(c=>c.priority).ThenBy(c=>c.node).FirstOrDefault();
            if(next!=null){Begin(next);return;}
            quiet-=Time.unscaledDeltaTime;stalled+=Time.unscaledDeltaTime;
            if(!session.MovementLocked&&quiet<=0&&stalled>=stalledSeconds)TryIdle();
        }
        bool Eligible(StoryCue c)
        {
            if(!c.pending||seen.Contains(c.id)||c.expiresAt>0&&Stage>=c.expiresAt||c.beforeSwitch&&c.gate.IsOn)return false;
            if(!string.IsNullOrEmpty(c.requires)&&!seen.Contains(c.requires))return false;
            // A switch-triggered cooperation page may wait until its own world becomes active.
            if(c.mirror!=session.MirrorActive)return false;
            if(c.ground&&c.trigger!=StoryTrigger.Start&&c.mirror&&!session.mirrorPlayer.Grounded)return false;
            return true;
        }
        void Begin(StoryCue c)
        {
            if(c.sequence==null||c.sequence.lines.Length==0){seen.Add(c.id);return;}
            active=c;page=0;clock=c.sequence.lines[0].seconds;seen.Add(c.id);Stage=Mathf.Max(Stage,c.node);Progress();NodeStarted?.Invoke(c.node);
            if(c.id.StartsWith("MAIN_08_P03_"))foreach(var other in cues)if(other.id.StartsWith("MAIN_08_P03_"))seen.Add(other.id);
            if(c.portrait){BottomText="";session.presentation.ShowSequence(c.sequence);modalStarted=true;}
            else BottomText=Format(c.sequence.lines[0]);
        }
        static string Format(StoryBeat b)=>string.IsNullOrEmpty(b.speaker)?b.text:b.speaker+"："+b.text;
        void BeginTransient(string text,float duration)
        {
            var seq=ScriptableObject.CreateInstance<StorySequence>();seq.hideFlags=HideFlags.DontSave;seq.lines=new[]{new StoryBeat{speaker="",text=text,seconds=duration}};
            transientAssets.Add(seq);active=new StoryCue{id="HINT",sequence=seq,priority=30};page=0;clock=duration;BottomText=text;
        }
        readonly List<StorySequence> transientAssets=new List<StorySequence>();
        void TryIdle()
        {
            if(!seen.Contains("MAIN_04")||Stage>=18||idleCount>=8||idleThisPuzzle>=2)return;
            bool lost=session.finale.LostContact,returned=seen.Contains("MAIN_15");
            int lo=lost?106:returned?121:1,hi=lost?120:returned?130:90;
            // Context-dependent lines are retained in data but excluded unless positively verified.
            int[] contextual={3,5,6,7,8,11,12,14,16,17,19,20,21,23,24,29,30,32,33,37,47,54,111,114,121,122,123,124,125,126,127,128};
            var options=idleLines.Where(l=>l.id>=lo&&l.id<=hi&&!usedIdle.Contains(l.id)&&(!lastTease||l.id>20)&&!contextual.Contains(l.id)).ToList();
            if(!lost&&!returned&&stalled>150&&session.CanSurfaceInteract&&session.levelSequence.SurfaceUnlocked&&!usedIdle.Contains(97))options.Add(idleLines.First(l=>l.id==97));
            if(seen.Contains("MAIN_09")&&lost&&!usedIdle.Contains(111))options.Add(idleLines.First(l=>l.id==111));
            if(Time.unscaledTime-lastShot<8&&!lost&&!returned&&!usedIdle.Contains(16))options.Add(idleLines.First(l=>l.id==16));
            if(options.Count==0)return;
            var pick=options[UnityEngine.Random.Range(0,options.Count)];usedIdle.Add(pick.id);lastTease=pick.id<=20;idleCount++;idleThisPuzzle++;quiet=UnityEngine.Random.Range(idleMinimum,idleMaximum);
            BeginTransient((pick.id>=76&&pick.id<=90?"男性：":"血魔：")+pick.text,Mathf.Clamp(pick.text.Length*.15f,4,7));
        }
        public StoryProgressSave Capture()=>new StoryProgressSave{seen=seen.ToArray(),idleUsed=usedIdle.ToArray(),stage=Stage,idleCount=idleCount};
        public void Restore(StoryProgressSave data)
        {
            active=null;modalStarted=false;BottomText="";seen.Clear();usedIdle.Clear();
            if(data!=null){foreach(var id in data.seen??Array.Empty<string>())seen.Add(id);foreach(var id in data.idleUsed??Array.Empty<int>())usedIdle.Add(id);Stage=data.stage;idleCount=data.idleCount;}
            else{Stage=session.finale.AtFinalPoint?19:session.finale.Recovered?15:session.finale.LostContact?12:session.expansion.ReachedE?11:session.expansion.ReachedD?10:session.levelSequence.ReachedC?7:session.levelSequence.SurfaceUnlocked?4:0;foreach(var c in cues)if(c.node<=Stage)seen.Add(c.id);}
            foreach(var c in cues)c.pending=false;Progress();previousPosition=session.mirrorPlayer.transform.position;
        }
        void OnDisable(){BottomText="";foreach(var seq in transientAssets)if(seq)Destroy(seq);transientAssets.Clear();}
    }
}
