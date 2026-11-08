using System;
using System.Collections.Generic;
using UnityEngine;
namespace MirrorTrip
{
    public sealed class InteractionSystem : MonoBehaviour
    {
        public WorldSession session;
        public TextInteractable[] entries;
        public bool IsReading => reading != null;
        public TextInteractable CurrentEntry => reading;
        public int PageIndex { get; private set; }
        public int PageCount => reading && reading.pages != null ? Mathf.Max(1, reading.pages.Length) : 0;
        public string PageText => reading && reading.pages != null && reading.pages.Length > 0 ? reading.pages[PageIndex] : "暂无内容。";
        public string Prompt { get; private set; }
        public IReadOnlyCollection<string> DiscoveredIds => discovered;
        public event Action<string> EntryDiscovered;
        private readonly HashSet<string> discovered = new HashSet<string>();
        private readonly Dictionary<string,CollectedNote> notes=new Dictionary<string,CollectedNote>();
        public IReadOnlyCollection<CollectedNote> CollectedNotes => notes.Values;
        private TextInteractable reading, nearbyText;
        private SceneMirror nearbyMirror;
        private MechanismMirror storage;
        public SurfaceSwitchInteractable[] surfaceSwitches=new SurfaceSwitchInteractable[0];
        private SurfaceSwitchInteractable nearbySwitch;
        public Transform HotkeyTarget => nearbySwitch?nearbySwitch.transform:nearbyText&&nearbyText.isChest?nearbyText.transform:nearbyMirror&&!nearbyMirror.IsLocked?nearbyMirror.surface.transform:null;
        public void TickSurfaceSwitch(bool held,float delta) { foreach(var item in surfaceSwitches)if(item)item.Hold(held&&item==nearbySwitch,delta); }
        public void RefreshTarget()
        {
            nearbyText = null; nearbyMirror = null; storage=null; nearbySwitch=null; Prompt = "";
            if (IsReading || !session.CanInteract) return;
            if (session.placement.IsCarrying)
            {
                session.placement.TickCarry();
                Prompt = session.placement.DestinationValid ? "[E] 放下镜子    [Esc] 放回原处" : "无法放下：" + session.placement.PreviewReason + "    [Esc] 放回原处";
                return;
            }
            foreach(var item in surfaceSwitches)if(item&&item.InReach()&&(!nearbySwitch||Vector2.Distance(item.transform.position,session.surfacePlayer.transform.position)<Vector2.Distance(nearbySwitch.transform.position,session.surfacePlayer.transform.position)))nearbySwitch=item;
            if(nearbySwitch){Prompt="按住 [E] 开启机关 · "+Mathf.RoundToInt(nearbySwitch.Progress*100)+"%";return;}
            var actor = session.MirrorActive ? session.mirrorPlayer : session.surfacePlayer;
            float nearest = float.MaxValue;
            foreach (var entry in entries)
            {
                if (!entry || !entry.isActiveAndEnabled || entry.mirrorSide != session.MirrorActive) continue;
                float distance = Vector2.Distance(actor.Bounds.center, InteractionReach.TargetPoint(entry.transform,actor.Bounds.center));
                if (distance >= nearest || !InteractionReach.CanReach(actor,entry.transform,entry.interactionRadius)) continue;
                nearest = distance; nearbyText = entry;
            }
            if (session.CanSurfaceInteract)
            {
                var candidate = session.placement.FindNearest();
                if (candidate && Vector2.Distance(actor.transform.position, candidate.surface.transform.position) < nearest)
                { nearbyMirror = candidate; nearbyText = null; }
            }
            if(session.MirrorActive)
            {
                float best=float.MaxValue;
                foreach(var m in session.mechanismMirrors)
                    if(m&&m.mirrorSide&&m.Stored&&m.Enabled&&Vector2.Distance(actor.transform.position,m.transform.position)<best)
                    {best=Vector2.Distance(actor.transform.position,m.transform.position);storage=m;}
            }
            if (nearbyText) Prompt = nearbyText.Prompt;
            else if(storage)Prompt="[E] 释放存储卡牌 · 朝旅人所在方向";
            else if (nearbyMirror) Prompt = nearbyMirror.IsLocked ? "镜子已固定：本段机关已完成" : "[E] 拿起镜子    WASD 搬运";
        }
        public bool Press()
        {
            if (IsReading) { Advance(); return true; }
            if (!session.CanInteract) return false;
            RefreshTarget();
            if(nearbySwitch)return false;
            if (session.placement.IsCarrying)
            {
                bool placed = session.placement.TryDrop(out var message); session.ShowMessage(message); return placed;
            }
            if (nearbyText)
            {
                return OpenEntry(nearbyText);
            }
            if(storage)return storage.Release();
            if (nearbyMirror)
            {
                bool picked = session.placement.BeginCarry(nearbyMirror, out var message); session.ShowMessage(message); return picked;
            }
            session.ShowMessage("靠近公告、宝箱或镜子后按 E。"); return false;
        }
        public bool OpenEntry(TextInteractable entry)
        {
            if(!entry||!session.CanInteract)return false;
            reading=null;PageIndex=0;entry.MarkOpened();session.SetMovement(Vector2.zero);
            if(!string.IsNullOrWhiteSpace(entry.entryId)&&discovered.Add(entry.entryId))EntryDiscovered?.Invoke(entry.entryId);
            if(entry.isChest&&session.presentation&&session.presentation.journal)
            {
                string id=entry.entryId??"";
                if(!notes.TryGetValue(id,out var note)){note=CollectedNote.From(entry);notes.Add(id,note);}
                session.presentation.journal.OpenNote(note);
            }
            else reading=entry;
            return true;
        }
        public bool TryClickChest(Vector2 screen)
        {
            if(!session.CanInteract)return false;
            var camera=session.MirrorActive?session.mirrorCamera:session.surfaceCamera;
            if(!session.PointerAllowed(camera,screen))return false;
            var actor=session.MirrorActive?session.mirrorPlayer:session.surfacePlayer;
            var point=(Vector2)camera.ScreenToWorldPoint(screen);
            foreach(var entry in entries)
            {
                if(!entry||!entry.isChest||!entry.isActiveAndEnabled||entry.mirrorSide!=session.MirrorActive)continue;
                if(!InteractionReach.CanReach(actor,entry.transform,entry.interactionRadius))continue;
                bool hit=false;foreach(var r in entry.GetComponentsInChildren<SpriteRenderer>())if(r.enabled&&r.bounds.Contains(new Vector3(point.x,point.y,r.bounds.center.z))){hit=true;break;}
                if(hit)return OpenEntry(entry);
            }
            return false;
        }
        public void RestoreDiscoveries(string[] ids)
        {
            discovered.Clear();foreach(var id in ids)discovered.Add(id);
            notes.Clear();
            foreach(var entry in entries)if(entry)
            {
                bool opened=discovered.Contains(entry.entryId);entry.RestoreOpened(opened);
                if(opened&&entry.isChest&&!string.IsNullOrWhiteSpace(entry.entryId)&&!notes.ContainsKey(entry.entryId))notes.Add(entry.entryId,CollectedNote.From(entry));
            }
        }
        public void RestoreNotes(CollectedNote[] saved)
        {
            if(saved==null)return; // Older saves resolve their note text from the authored entries above.
            foreach(var note in saved)if(note!=null&&!string.IsNullOrWhiteSpace(note.id)&&discovered.Contains(note.id))notes[note.id]=note;
        }
        public void Advance()
        {
            if (!IsReading) return;
            if (PageIndex + 1 < PageCount) PageIndex++;
            else Close();
        }
        public void Close() { reading = null; PageIndex = 0; session.SetMovement(Vector2.zero); }
    }
}
