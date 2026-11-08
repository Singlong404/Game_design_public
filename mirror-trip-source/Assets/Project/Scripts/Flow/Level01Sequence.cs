using UnityEngine;
namespace MirrorTrip
{
    [System.Serializable] public sealed class Level01ProgressSave {public bool pointA,pointB,pointC;public Vector2 scenicPlayerAnchor;}
    public sealed class Level01Sequence : MonoBehaviour
    {
        public WorldSession session;
        public LevelProgressPoint pointA,pointB,pointC;
        public PlayerFollowCamera mirrorFollow;
        public CardSwitch routeSwitch;
        public MechanismMirror portalA1,portalA2,portalA3;
        public bool holdShift;
        public Vector3 scenicPosition=new Vector3(201.97f,6.32f,-10);
        public float scenicSize=11,followSize=8;
        public Vector2 scenicDrift=new Vector2(.6f,.35f);
        public bool SurfaceUnlocked {get;private set;}
        public bool ReachedB {get;private set;}
        public bool ReachedC {get;private set;}
        public bool Revealing=>revealRemaining>0;
        private float revealRemaining;
        private Vector2 scenicPlayerAnchor;
        private float initialSize;
        private void Awake(){initialSize=mirrorFollow.halfHeight;routeSwitch.StateChanged+=SyncRoute;}
        private void Start(){SyncRoute(routeSwitch.IsOn);foreach(var s in session.transform.root.GetComponentsInChildren<CardSwitch>(true))s.SetState(s.IsOn);}
        private void OnDestroy(){if(routeSwitch)routeSwitch.StateChanged-=SyncRoute;}
        private void Update(){if(revealRemaining>0&&!session.Paused&&!session.presentation.SettingsOpen)revealRemaining=Mathf.Max(0,revealRemaining-Time.unscaledDeltaTime);}
        public void Tick()
        {
            if(!session.MirrorActive||!session.CanInteract)return;
            if(!SurfaceUnlocked&&pointA.Contains(session.mirrorPlayer))Reach(LevelProgressPoint.Stage.UnlockSurface);
            if(!ReachedB&&!ReachedC&&pointB.Contains(session.mirrorPlayer))Reach(LevelProgressPoint.Stage.ScenicView);
            if(!ReachedC&&pointC.Contains(session.mirrorPlayer))Reach(LevelProgressPoint.Stage.FollowView);
        }
        public void Reach(LevelProgressPoint.Stage stage)
        {
            if(stage==LevelProgressPoint.Stage.UnlockSurface&&!SurfaceUnlocked)
            {SurfaceUnlocked=true;revealRemaining=1.6f;session.SetMovement(Vector2.zero);if(session.finale)session.finale.PlayArrival();else session.presentation.PlayMaleReveal();if(session.cameraDirector)session.cameraDirector.Trigger("point_A");else mirrorFollow.SetFollowView(5);if(!session.storyDirector)session.ShowMessage(holdShift?"按住 Shift 进入表世界，松开返回里世界。":"按 Shift 切换表里世界。",8);}
            else if(stage==LevelProgressPoint.Stage.ScenicView&&!ReachedB&&!ReachedC)
            {ReachedB=true;scenicPlayerAnchor=session.mirrorPlayer.transform.position;if(!session.cameraDirector)mirrorFollow.SetScenicView(scenicPosition,scenicSize,scenicPlayerAnchor,scenicDrift);}
            else if(stage==LevelProgressPoint.Stage.FollowView&&!ReachedC)
            {ReachedC=true;if(!session.cameraDirector)mirrorFollow.SetFollowView(followSize);}
        }
        private void SyncRoute(bool active){portalA3.SetEnabled(active);portalA1.partner=active?portalA3:portalA2;}
        public Level01ProgressSave Capture()=>new Level01ProgressSave{pointA=SurfaceUnlocked,pointB=ReachedB,pointC=ReachedC,scenicPlayerAnchor=scenicPlayerAnchor};
        public void Restore(Level01ProgressSave saved)
        {
            SurfaceUnlocked=saved!=null&&saved.pointA;ReachedB=saved!=null&&saved.pointB;ReachedC=saved!=null&&saved.pointC;revealRemaining=0;
            if(ReachedC)mirrorFollow.SetFollowView(followSize,true);
            else if(ReachedB){scenicPlayerAnchor=saved.scenicPlayerAnchor;mirrorFollow.SetScenicView(scenicPosition,scenicSize,scenicPlayerAnchor,scenicDrift,true);}
            else mirrorFollow.SetFollowView(initialSize,true);
            SyncRoute(routeSwitch.IsOn);
        }
    }
}
