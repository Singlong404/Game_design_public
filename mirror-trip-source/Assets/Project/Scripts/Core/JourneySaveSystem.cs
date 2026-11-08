using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Security.Cryptography;
using System.Collections.Generic;
using UnityEngine;
namespace MirrorTrip
{
    [Serializable]public sealed class MirrorSave { public string id,link;public float t;public bool locked,hasPose;public Vector2 surfacePosition,responsePosition;public float surfaceAngle,responseAngle; }
    [Serializable]public sealed class FlagSave { public string id;public bool value; }
    [Serializable]public sealed class MechanismSave { public string id;public bool enabled,stored,used,child;public int reflections; }
    [Serializable]public sealed class FlyingCardSave { public Vector2 position,direction;public float life;public int reflections;public bool child; }
    [Serializable]public sealed class JourneySnapshot
    {
        public int version=1,sequence;
        public string runId,scene,checkpointId,checkpointName,savedUtc;
        public Vector2 surfacePosition,mirrorPosition;
        public bool mirrorActive,completed,exited;
        public MirrorSave[] mirrors;
        public FlagSave[] switches,doors,texts;
        public MechanismSave[] mechanisms;
        public FlyingCardSave[] cards;
        public string[] discovered,lit;
        public CollectedNote[] collectedNotes;
        public Level01ProgressSave level01Progress;
        public Level01ExpansionSave level01Expansion;
        public LevelCameraSave levelCamera;
        public LevelFinaleSave levelFinale;
        public StoryProgressSave narrative;
    }
    [Serializable]public sealed class RunCheckpointHistory {public string runId;public List<string> ids=new List<string>();}
    [Serializable]public sealed class JourneyArchive
    {
        public int version=1,sequence;
        public JourneySnapshot[] slots=new JourneySnapshot[3];
        public List<RunCheckpointHistory> runs=new List<RunCheckpointHistory>();
    }
    [Serializable]internal sealed class SaveEnvelope {public string payload,sha256;}

    // One atomic transaction contains the three slots AND the per-run activation ledger.
    // Loading an older slot cannot re-arm a torch already used later in the same run.
    [DefaultExecutionOrder(200)]
    public sealed class JourneySaveSystem : MonoBehaviour
    {
        public WorldSession session;
        [Tooltip("Separate saves for authored levels; blank preserves legacy test saves.")]
        public string archiveNamespace = "";
        public static string TestDirectory;
        public static JourneySnapshot PendingLoad;
        public JourneyArchive Archive { get; private set; }
        public string RunId { get; private set; }
        public string LastError { get; private set; }
        public bool HasSaves => Archive!=null&&Archive.slots.Any(s=>s!=null);
        public string DirectoryPath => string.IsNullOrEmpty(TestDirectory)?Path.Combine(Application.persistentDataPath,"JourneySaves",archiveNamespace):TestDirectory;
        public string FilePath => Path.Combine(DirectoryPath,"journeys-v1.json");
        private void Awake(){if(!session)session=GetComponent<WorldSession>();session.saves=this;ReadArchive();RunId=Guid.NewGuid().ToString("N");}
        private void Start()
        {
            var pending=PendingLoad;PendingLoad=null;
            if(pending!=null)
            {
                if(ValidateSnapshot(pending,out var reason))Restore(pending);
                else{LastError=reason;session.presentation.ReturnToTitle();session.ShowMessage(reason,12);}
            }
        }
        private static string Hash(string s){using(var sha=SHA256.Create())return Convert.ToBase64String(sha.ComputeHash(Encoding.UTF8.GetBytes(s)));}
        private JourneyArchive ReadFile(string path)
        {
            if(!File.Exists(path))return null;
            var envelope=JsonUtility.FromJson<SaveEnvelope>(File.ReadAllText(path,Encoding.UTF8));
            if(envelope==null||string.IsNullOrEmpty(envelope.payload)||Hash(envelope.payload)!=envelope.sha256)throw new InvalidDataException("存档校验失败");
            var data=JsonUtility.FromJson<JourneyArchive>(envelope.payload);
            if(data==null||data.version!=1||data.slots==null||data.slots.Length!=3||data.runs==null||data.sequence<0)throw new InvalidDataException("存档格式不兼容");
            return data;
        }
        public void ReadArchive()
        {
            LastError="";
            try{Archive=ReadFile(FilePath)??ReadFile(FilePath+".bak")??new JourneyArchive();}
            catch(Exception e)
            {
                try{Archive=ReadFile(FilePath+".bak");}catch{Archive=null;}
                LastError=Archive!=null?"主存档损坏，已恢复上一份备份。":"无法读取存档："+e.Message;
                Archive??=new JourneyArchive();
            }
        }
        private void WriteArchive(JourneyArchive value)
        {
            Directory.CreateDirectory(DirectoryPath);
            string payload=JsonUtility.ToJson(value),temp=FilePath+".tmp";
            byte[] bytes=Encoding.UTF8.GetBytes(JsonUtility.ToJson(new SaveEnvelope{payload=payload,sha256=Hash(payload)},true));
            using(var file=new FileStream(temp,FileMode.Create,FileAccess.Write,FileShare.None)){file.Write(bytes,0,bytes.Length);file.Flush(true);}
            if(File.Exists(FilePath))File.Replace(temp,FilePath,FilePath+".bak");else File.Move(temp,FilePath);
        }
        private IEnumerable<T> All<T>() where T:Component => session.transform.root.GetComponentsInChildren<T>(true);
        public JourneySnapshot Capture(TorchCheckpoint torch)
        {
            return new JourneySnapshot
            {
                levelCamera=session.cameraDirector?session.cameraDirector.Capture():null,
                levelFinale=session.finale?session.finale.Capture():null,
                runId=RunId,scene=session.gameObject.scene.path,checkpointId=torch.persistentId,checkpointName=torch.displayName,savedUtc=DateTime.UtcNow.ToString("o"),
                surfacePosition=session.surfacePlayer.LocalPosition,
                mirrorPosition=(Vector2)(torch.respawnPoint?torch.respawnPoint.position:session.mirrorPlayer.transform.position)-(Vector2)session.mirrorPlayer.worldRoot.position,
                mirrorActive=session.MirrorActive,completed=session.Completed,exited=session.Exited,
                mirrors=session.placement.sceneMirrors.Select(m=>new MirrorSave{id=m.mirrorId,link=m.link.pairId,t=m.t,locked=m.IsLocked,hasPose=true,surfacePosition=m.surface.transform.position,responsePosition=m.response.transform.position,surfaceAngle=m.surface.transform.eulerAngles.z,responseAngle=m.response.transform.eulerAngles.z}).ToArray(),
                narrative=session.storyDirector?session.storyDirector.Capture():null,
                switches=All<CardSwitch>().Select(s=>new FlagSave{id=s.persistentId,value=s.IsOn}).ToArray(),
                doors=All<PairedDoor>().Select(d=>new FlagSave{id=d.persistentId,value=d.IsOpen}).ToArray(),
                texts=All<ContactNarrative>().Select(t=>new FlagSave{id=t.persistentId,value=t.Visited}).ToArray(),
                mechanisms=All<MechanismMirror>().Select(m=>new MechanismSave{id=m.persistentId,enabled=m.Enabled,stored=m.Stored,used=m.Used,reflections=m.StoredReflections,child=m.StoredChild}).ToArray(),
                cards=session.cards.ActiveCards.Select(c=>new FlyingCardSave{position=c.Position,direction=c.Direction,life=c.RemainingLife,reflections=c.Reflections,child=c.IsCopy}).ToArray(),
                discovered=session.interactions.DiscoveredIds.ToArray(),collectedNotes=session.interactions.CollectedNotes.ToArray(),level01Progress=session.levelSequence?session.levelSequence.Capture():null,level01Expansion=session.expansion?session.expansion.Capture():null,lit=All<TorchCheckpoint>().Where(t=>t.Lit||t==torch).Select(t=>t.persistentId).ToArray()
            };
        }
        public bool Activate(TorchCheckpoint torch)
        {
            var history=Archive.runs.Find(r=>r.runId==RunId);
            if(torch.Lit||history!=null&&history.ids.Contains(torch.persistentId))return false;
            var next=JsonUtility.FromJson<JourneyArchive>(JsonUtility.ToJson(Archive));
            var snapshot=Capture(torch);
            if(!ValidateSnapshot(snapshot,out var reason)){LastError=reason;session.ShowMessage(reason,8);return false;}
            snapshot.sequence=++next.sequence;int slot=(snapshot.sequence-1)%3;
            next.slots[slot]=snapshot;
            var run=next.runs.Find(r=>r.runId==RunId);
            if(run==null){run=new RunCheckpointHistory{runId=RunId};next.runs.Add(run);}
            run.ids.Add(torch.persistentId);
            next.runs.RemoveAll(r=>r.runId!=RunId&&!next.slots.Any(s=>s!=null&&s.runId==r.runId));
            try{WriteArchive(next);}catch(Exception e){LastError="存档未写入："+e.Message;session.ShowMessage(LastError,8);return false;}
            Archive=next;LastError="";torch.Restore(true);
            session.ShowMessage($"已保存至存档 {slot+1}：{torch.displayName}",5);return true;
        }
        private static bool Finite(Vector2 p)=>float.IsFinite(p.x)&&float.IsFinite(p.y);
        public bool ValidateSnapshot(JourneySnapshot s,out string error)
        {
            error="存档与当前关卡不匹配，未读取。";
            if(s==null||s.version!=1||s.scene!=session.gameObject.scene.path||string.IsNullOrEmpty(s.runId)||!Finite(s.surfacePosition)||!Finite(s.mirrorPosition)
                ||s.mirrors==null||s.switches==null||s.doors==null||s.texts==null||s.mechanisms==null||s.discovered==null||s.lit==null||s.cards==null)return false;
            var torchIds=All<TorchCheckpoint>().Select(t=>t.persistentId).ToArray();
            if(!IdsMatch(torchIds,torchIds))return false;
            if(!IdsMatch(s.mirrors.Select(m=>m.id),session.placement.sceneMirrors.Select(m=>m.mirrorId))
                ||!IdsMatch(s.switches.Select(v=>v.id),All<CardSwitch>().Select(v=>v.persistentId))
                ||!IdsMatch(s.doors.Select(v=>v.id),All<PairedDoor>().Select(v=>v.persistentId))
                ||!NarrativeIdsCompatible(s.texts.Select(v=>v.id),All<ContactNarrative>().Select(v=>v.persistentId))
                ||!IdsMatch(s.mechanisms.Select(v=>v.id),All<MechanismMirror>().Select(v=>v.persistentId)))return false;
            if(s.mirrors.Any(m=>!float.IsFinite(m.t)||m.t<0||m.t>1||!session.placement.links.Any(l=>l.pairId==m.link)||(m.hasPose&&(!Finite(m.surfacePosition)||!Finite(m.responsePosition)||!float.IsFinite(m.surfaceAngle)||!float.IsFinite(m.responseAngle))))
                ||s.cards.Length>session.cards.maximumCards||s.cards.Any(c=>!Finite(c.position)||!Finite(c.direction)||!float.IsFinite(c.life)||c.life<=0))return false;
            error="";return true;
        }
        private static bool IdsMatch(IEnumerable<string>a,IEnumerable<string>b)
        {var x=a.ToArray();var y=b.ToArray();return x.All(v=>!string.IsNullOrWhiteSpace(v))&&x.Distinct().Count()==x.Length&&x.OrderBy(v=>v).SequenceEqual(y.OrderBy(v=>v));}
        private static bool NarrativeIdsCompatible(IEnumerable<string> saved,IEnumerable<string> current)
        {
            var a=saved.ToArray();var b=current.ToArray();
            return a.All(v=>!string.IsNullOrWhiteSpace(v))&&a.Distinct().Count()==a.Length&&b.Distinct().Count()==b.Length&&a.All(b.Contains)
                &&b.Except(a).All(id=>id.StartsWith("L01_STORY_")||id.StartsWith("L01_HINT_"));
        }
        public string CheckpointLabel(JourneySnapshot snapshot)
        {
            var checkpoint=All<TorchCheckpoint>().FirstOrDefault(t=>t.persistentId==snapshot.checkpointId);
            return checkpoint?checkpoint.displayName:"存档点";
        }
        public int LatestSlot()
        {
            int index=-1,sequence=-1;for(int i=0;i<3;i++)if(Archive.slots[i]!=null&&Archive.slots[i].sequence>sequence){index=i;sequence=Archive.slots[i].sequence;}return index;
        }
        public bool LoadSlot(int slot)
        {
            if(session.Loading||slot<0||slot>2)return false;
            var snapshot=Archive.slots[slot];
            if(!ValidateSnapshot(snapshot,out var reason)){LastError=reason;return false;}
            PendingLoad=snapshot;session.Retry();return true;
        }
        public void NewJourney(){PendingLoad=null;session.Retry();}
        public void Recover(){int latest=LatestSlot();if(latest>=0)LoadSlot(latest);else NewJourney();}
        public void Restore(JourneySnapshot s)
        {
            RunId=s.runId;session.RestoreProgress(s.mirrorActive,s.completed,s.exited);
            session.surfacePlayer.SetLocalPosition(s.surfacePosition);session.mirrorPlayer.SetLocalPosition(s.mirrorPosition);
            foreach(var state in s.mirrors){var pair=session.placement.sceneMirrors.First(m=>m.mirrorId==state.id);pair.Restore(session.placement.links.First(l=>l.pairId==state.link),state.t,state.locked);if(state.hasPose){pair.surface.transform.SetPositionAndRotation(state.surfacePosition,Quaternion.Euler(0,0,state.surfaceAngle));pair.response.transform.SetPositionAndRotation(state.responsePosition,Quaternion.Euler(0,0,state.responseAngle));}}
            foreach(var state in s.switches)All<CardSwitch>().First(v=>v.persistentId==state.id).SetState(state.value);
            foreach(var state in s.doors)All<PairedDoor>().First(v=>v.persistentId==state.id).Restore(state.value);
            foreach(var state in s.mechanisms)All<MechanismMirror>().First(v=>v.persistentId==state.id).Restore(state.enabled,state.stored,state.used,state.reflections,state.child);
            foreach(var state in s.texts)All<ContactNarrative>().First(v=>v.persistentId==state.id).Restore(state.value);
            var history=Archive.runs.Find(r=>r.runId==RunId);
            foreach(var torch in All<TorchCheckpoint>())torch.Restore(s.lit.Contains(torch.persistentId)||(history!=null&&history.ids.Contains(torch.persistentId)));
            session.interactions.RestoreDiscoveries(s.discovered);
            session.interactions.RestoreNotes(s.collectedNotes);
            if(session.levelSequence)session.levelSequence.Restore(s.level01Progress);
            if(session.expansion)session.expansion.Restore(s.level01Expansion);
            if(session.cameraDirector)session.cameraDirector.Restore(s.levelCamera);
            if(session.finale)session.finale.Restore(s.levelFinale);
            if(session.storyDirector)session.storyDirector.Restore(s.narrative);
            foreach(var card in s.cards)session.cards.SpawnDerived(card.position,card.direction,card.life,card.reflections,card.child,null);
            session.presentation.AfterLoad();Physics2D.SyncTransforms();session.ShowMessage("已读取存档："+CheckpointLabel(s),5);
        }
    }
}
