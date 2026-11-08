using UnityEngine;

namespace MirrorTrip
{
    public sealed class CardSwitch : MonoBehaviour
    {
        public WorldSession session;
        public string puzzleId = "PAVILION_01";
        public SpriteRenderer[] visuals;
        public int minimumReflections = 1;
        public string persistentId;
        public bool toggleOnEveryHit;
        public bool opensPrimaryPuzzle = true;
        public PairedDoor[] linkedDoors = new PairedDoor[0];
        public MechanismMirror[] linkedMirrors = new MechanismMirror[0];
        public GameObject[] linkedPlatforms = new GameObject[0];
        public event System.Action<bool> StateChanged;
        public event System.Action<bool,bool> StateApplied;
        public UnityEngine.Events.UnityEvent turnedOn = new UnityEngine.Events.UnityEvent();
        public UnityEngine.Events.UnityEvent turnedOff = new UnityEngine.Events.UnityEvent();
        public bool IsOn { get; private set; }
        public int ActivationCount { get; private set; }
        public void Hit()
        {
            if(IsOn&&!toggleOnEveryHit)return;
            SetState(!IsOn,true);
            if(session&&session.feedback)session.feedback.Play(FeedbackKind.Activate,transform.position,gameObject.layer!=LayerMask.NameToLayer("SurfaceWorld"),.8f);
        }
        public void SetState(bool on,bool notify=false)
        {
            bool changed=IsOn!=on;IsOn=on;if(changed&&on&&notify)ActivationCount++;
            foreach(var v in visuals)if(v)v.color=on?new Color32(128,128,128,255):GameplayAccent.Red;
            foreach(var d in linkedDoors)if(d)d.SetOpen(on,notify);
            foreach(var m in linkedMirrors)if(m)m.SetEnabled(on);
            foreach(var platform in linkedPlatforms)if(platform)platform.SetActive(on);
            StateChanged?.Invoke(on);
            StateApplied?.Invoke(on,notify&&changed);
            if(notify&&changed)
            {
                if(on&&opensPrimaryPuzzle&&session)session.OpenPuzzleDoor();
                (on?turnedOn:turnedOff)?.Invoke();
            }
        }
    }
}
