using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
namespace MirrorTrip
{
    [Serializable] public sealed class LevelFinaleSave { public bool lost,recovered,finalPoint,hint,iconsRestored,ending; }
    [DefaultExecutionOrder(250)]
    public sealed class Level01FinaleFlow:MonoBehaviour
    {
        public WorldSession session;
        public LevelProgressPoint point05,pointFin,pointD,pointG;
        [Tooltip("Surface-world safe arrival after point_G restores contact. World position is authored on this marker.")]
        public Transform surfaceRecoveryPoint;
        public CardSwitch lossSwitch,finalSwitch;
        public MechanismMirror portalD1;
        public GameObject[] hiddenIcons;
        public Image decorativeFrame;
        public Material badgeCutoutMaterial;
        public Camera endingCamera;
        public FinaleHologramLoop hologram;
        public ParticleSystem finalSnow;
        public Texture2D arrivalSheet,lossSheet;
        public Sprite cinematicFrame,subtitlePlate;
        public StorySequence endingSequence;
        [TextArea] public string arrivalCaption="血魔：听得到吗？\n男人：听到了。你在哪里？",lossCaption="男人：等一下，这边的灯——\n〔连接中断〕";
        public float closingFadeSeconds=.6f,endingHoldSeconds=1;
        [Header("Finale camera motion")]
        [Tooltip("Slow push-in over the existing ending duration. Fraction of the authored camera size.")]
        [Range(0,.2f)] public float endingZoomFraction=.10f;
        [Tooltip("Maximum handheld displacement in world units; does not move the UI.")]
        [Range(0,.2f)] public float endingShakeDistance=.045f;
        [Range(0,1)] public float endingShakeDegrees=.18f;
        [Range(.1f,5)] public float endingShakeFrequency=1.7f;
        [TextArea] public string[] closingClickPages={"又是…………一个人了吗？","AA_建筑设计沈工"};
        public int ClosingPage {get;private set;}
        public string ClosingText=>closingText?closingText.text:"";
        public bool WaitingForGround{get;private set;}
        public bool EndingCard{get;private set;}
        public float clipDuration=1.6f,subtitleSeconds=4.8f;
        [TextArea] public string[] endingLines={
            "血魔：你回来……了？",
            "男人：我……一直……在。",
            "血魔：那就碰一下。一下就好。",
            "男人：我……想……和你……",
            "血魔：这句话。你已经说过了。",
            "血魔：同一个停顿。同一口气。",
            "男人：明天……还要……上班……",
            "血魔：明天是什么？我给你编过吗？",
            "血魔：没有公司。没有下班。没有镜子那边的人。",
            "血魔：是我。太久了……我一个人。",
            "血魔：连一个会离开我的人……也得自己想。",
            "男人：要……永远……在一起……",
            "血魔：嗯。这次……我知道是谁在说话了。",
            "〔镜子没有回答。雪还在落。〕"};
        public bool AtFinalPoint{get;private set;}
        public bool Cinematic{get;private set;}
        public bool LostContact{get;private set;}
        public bool Recovered{get;private set;}
        public bool MovementLocked=>AtFinalPoint||InputBlocked;
        public bool InputBlocked=>clipRemaining>0||Cinematic||endingPending;
        public bool SwitchLocked=>LostContact||AtFinalPoint||InputBlocked;
        public bool ClipPlaying=>clipRemaining>0;
        bool hintShown,iconsRestored,endingPending;float clipRemaining,endingClock,hintRemaining;
        int clipKind,originalSurfaceMask;CameraClearFlags originalClear;Color originalBackground;
        CanvasGroup gameplay;GameObject overlay,clipPanel,cinemaPanel,blackout,hintPanel;RawImage clipImage;Text clipCaption,endingText,hintText;
        WorldViewportLayout layout;
        Material originalFrameMaterial,badgeMaterial;
        GameObject closingPanel;Image closingBlack;Text closingText,closingHint;float cardClock;int cardFrame;
        Vector3 endingCameraOrigin;Quaternion endingCameraRotation;float endingCameraSize;bool endingCameraCaptured;
        public bool BlackoutVisible=>blackout&&blackout.activeInHierarchy;
        void Awake()
        {
            originalSurfaceMask=session.surfaceCamera.cullingMask;originalClear=session.surfaceCamera.clearFlags;originalBackground=session.surfaceCamera.backgroundColor;
            gameplay=session.presentation.gameplayScreen.GetComponent<CanvasGroup>();if(!gameplay)gameplay=session.presentation.gameplayScreen.AddComponent<CanvasGroup>();
            layout=session.transform.root.GetComponentInChildren<WorldViewportLayout>(true);
            BuildOverlay();endingCamera.enabled=false;
            if(decorativeFrame&&badgeCutoutMaterial){originalFrameMaterial=decorativeFrame.material;badgeMaterial=new Material(badgeCutoutMaterial);badgeMaterial.SetVector("_SpriteUV",UnityEngine.Sprites.DataUtility.GetOuterUV(decorativeFrame.sprite));}
            lossSwitch.StateApplied+=LossChanged;finalSwitch.StateApplied+=FinalChanged;
        }
        void OnDestroy(){RestoreEndingCamera();if(lossSwitch)lossSwitch.StateApplied-=LossChanged;if(finalSwitch)finalSwitch.StateApplied-=FinalChanged;if(overlay)Destroy(overlay);if(badgeMaterial)Destroy(badgeMaterial);}
        public void PlayArrival(){clipKind=0;clipRemaining=clipDuration;session.SetMovement(Vector2.zero);}
        void LossChanged(bool on,bool animate){if(on&&animate&&!LostContact&&!Recovered)BeginLoss();}
        // Defer until the projectile iteration has finished before clearing its pool.
        void FinalChanged(bool on,bool animate){if(on&&animate&&!Cinematic)endingPending=true;}
        public void BeginLoss()
        {
            LostContact=true;session.ForceMirrorWorld();session.mirrorPlayer.SetLocalPosition(session.mirrorPlayer.LocalPosition);clipKind=1;clipRemaining=clipDuration;
            ApplyBlackout();if(!session.storyDirector)session.ShowMessage("血魔：喂？……又是一个人了吗……",8);
        }
        void ApplyBlackout()
        {
            session.surfaceCamera.cullingMask=LostContact?0:originalSurfaceMask;
            session.surfaceCamera.clearFlags=LostContact?CameraClearFlags.SolidColor:originalClear;
            session.surfaceCamera.backgroundColor=LostContact?Color.black:originalBackground;
        }
        public void Tick()
        {
            if(!session.MirrorActive||!session.CanInteract)return;
            var actor=session.mirrorPlayer;
            if(!iconsRestored&&pointD.Contains(actor))iconsRestored=true;
            if(!hintShown&&point05.Contains(actor)){hintShown=true;if(!session.storyDirector){hintRemaining=8;session.ShowMessage("血魔：下面……有路。跳下去。别碰到那些碎石。",8);}}
            if(!Recovered&&pointG.Contains(actor))RecoverContact();
            if(!AtFinalPoint&&pointFin.Contains(actor))EnterFinalPoint();
        }
        public void RecoverContact()
        {
            if(Recovered)return;
            Recovered=true;
            if(!LostContact)return;
            // Relocate the inactive actor before switching is unlocked. Do this once, not on every Shift or load.
            if(surfaceRecoveryPoint)
            {
                session.placement.CancelCarry();
                session.surfacePlayer.SetLocalPosition((Vector2)surfaceRecoveryPoint.position-(Vector2)session.surfacePlayer.worldRoot.position);
                session.surfacePlayer.ResetTransient();
                session.surfaceCamera.GetComponent<PlayerFollowCamera>()?.SnapToTarget();
            }
            LostContact=false;ApplyBlackout();
            if(!session.storyDirector)session.ShowMessage("微弱的声音：我……在……听得……到。",7);
        }
        public void EnterFinalPoint()
        {
            AtFinalPoint=true;session.ForceMirrorWorld();WaitingForGround=!session.mirrorPlayer.Grounded;session.SetMovement(Vector2.zero);session.SetSprint(false);
            if(!WaitingForGround)session.mirrorPlayer.SetLocalPosition(session.mirrorPlayer.LocalPosition);
            FrameFinalPoint();
            if(finalSnow)finalSnow.Play();if(!session.storyDirector)session.ShowMessage("前面的开关。再试一次。",30);
        }
        void FrameFinalPoint()
        {
            Vector3 center=(session.mirrorPlayer.transform.position+finalSwitch.transform.position)*.5f;center.z=-10;
            float size=Mathf.Max(4,(Mathf.Abs(session.mirrorPlayer.transform.position.x-finalSwitch.transform.position.x)*.5f+1.4f)/Mathf.Max(.4f,session.mirrorCamera.aspect));
            var view=session.mirrorCamera;Vector3 playerUV=view.WorldToViewportPoint(session.mirrorPlayer.transform.position),switchUV=view.WorldToViewportPoint(finalSwitch.transform.position);
            bool offscreen=playerUV.x<.12f||playerUV.x>.88f||playerUV.y<.15f||playerUV.y>.85f||switchUV.x<.12f||switchUV.x>.88f||switchUV.y<.15f||switchUV.y>.85f;
            var follow=view.GetComponent<PlayerFollowCamera>();follow.SetScenicView(center,size,session.mirrorPlayer.transform.position,Vector2.zero,offscreen);follow.SetAtmosphere(Vector2.zero,9,0,0,false);
        }
        public void CheckGrounded(){if(!WaitingForGround||!session.mirrorPlayer.Grounded)return;WaitingForGround=false;session.mirrorPlayer.SetLocalPosition(session.mirrorPlayer.LocalPosition);FrameFinalPoint();}
        public void BeginEnding()
        {
            if(Cinematic)return;if(!AtFinalPoint)EnterFinalPoint();
            if(WaitingForGround){endingPending=true;return;}
            Cinematic=true;endingPending=false;clipRemaining=0;endingClock=0;session.cards.Clear();session.interactions.Close();session.presentation.CloseStory();
            session.surfaceCamera.enabled=session.mirrorCamera.enabled=false;if(layout)layout.enabled=false;
            CaptureEndingCamera();endingCamera.enabled=true;hologram.SetPlaying(true);session.SetMovement(Vector2.zero);
        }
        void CaptureEndingCamera()
        {
            if(endingCameraCaptured||!endingCamera)return;
            endingCameraOrigin=endingCamera.transform.localPosition;endingCameraRotation=endingCamera.transform.localRotation;
            endingCameraSize=endingCamera.orthographicSize;endingCameraCaptured=true;
        }
        void RestoreEndingCamera()
        {
            if(!endingCameraCaptured)return;
            if(endingCamera){endingCamera.transform.localPosition=endingCameraOrigin;endingCamera.transform.localRotation=endingCameraRotation;endingCamera.orthographicSize=endingCameraSize;}
            endingCameraCaptured=false;
        }
        void ApplyEndingCameraMotion()
        {
            if(!endingCameraCaptured||!endingCamera)return;
            // Evaluate from the original pose and the paused cinematic clock, never accumulate offsets.
            bool reduced=session.presentation.MotionReduced;
            float duration=Mathf.Max(.1f,(endingSequence?endingSequence.Duration:endingLines.Length*subtitleSeconds)+endingHoldSeconds);
            float progress=Mathf.Clamp01(endingClock/duration);
            float zoom=reduced?0:Mathf.Clamp(endingZoomFraction,0,.2f)*Mathf.SmoothStep(0,1,progress);
            endingCamera.orthographicSize=Mathf.Max(.01f,endingCameraSize*(1-zoom));
            float envelope=Mathf.SmoothStep(0,1,Mathf.Clamp01(endingClock/.8f))*Mathf.SmoothStep(0,1,Mathf.Clamp01(duration-endingClock));
            float strength=reduced?0:envelope*Mathf.Lerp(.45f,1,Mathf.SmoothStep(0,1,progress));
            float phase=endingClock*Mathf.Clamp(endingShakeFrequency,.1f,5);
            float x=(Mathf.PerlinNoise(13.7f,phase)-.5f)*2,y=(Mathf.PerlinNoise(phase,41.3f)-.5f)*2;
            float roll=(Mathf.PerlinNoise(73.1f,phase)-.5f)*2;
            endingCamera.transform.localPosition=endingCameraOrigin+new Vector3(x,y,0)*Mathf.Clamp(endingShakeDistance,0,.2f)*strength;
            endingCamera.transform.localRotation=endingCameraRotation*Quaternion.Euler(0,0,roll*Mathf.Clamp(endingShakeDegrees,0,1)*strength);
        }
        bool MenuOpen=>session.Paused||session.presentation.BlocksGameplay||session.Fallen;
        void Update()
        {
            if(MenuOpen)return;
            if(endingPending&&!WaitingForGround)BeginEnding();
            if(clipRemaining>0)clipRemaining=Mathf.Max(0,clipRemaining-Time.unscaledDeltaTime);
            if(Cinematic&&!EndingCard){endingClock+=Time.unscaledDeltaTime;float duration=endingSequence?endingSequence.Duration:endingLines.Length*subtitleSeconds;if(endingClock>=duration+endingHoldSeconds)ShowEndingCard();}
            if(EndingCard){cardClock+=Time.unscaledDeltaTime;if(!session.ExternalControl&&cardClock>closingFadeSeconds+.35f&&Time.frameCount>cardFrame&&Mouse.current!=null&&Mouse.current.leftButton.wasPressedThisFrame)ReturnFromEnding();}
            if(hintRemaining>0)hintRemaining-=Time.unscaledDeltaTime;
        }
        void LateUpdate()
        {
            bool menu=MenuOpen;
            if(Cinematic&&!EndingCard&&!menu)ApplyEndingCameraMotion();
            foreach(var icon in hiddenIcons)if(icon)icon.SetActive(!portalD1.Enabled||iconsRestored);
            if(decorativeFrame&&badgeMaterial)decorativeFrame.material=portalD1.Enabled&&!iconsRestored?badgeMaterial:originalFrameMaterial;
            gameplay.alpha=Cinematic&&!menu?0:1;gameplay.blocksRaycasts=!Cinematic||menu;gameplay.interactable=!Cinematic||menu;
            blackout.SetActive(LostContact&&!Cinematic&&!menu&&!session.Reading&&!session.Exited&&!session.Loading);
            // This panel is inside gameplay, underneath the original illustrated frame and flowers.
            RectTransform window=layout.rightWindow;var r=(RectTransform)blackout.transform;
            r.anchorMin=window.anchorMin;r.anchorMax=window.anchorMax;r.pivot=window.pivot;r.sizeDelta=window.sizeDelta;r.anchoredPosition=window.anchoredPosition;
            if(LostContact)session.presentation.rightState.text="失联 · 无信号";
            if(AtFinalPoint&&!Cinematic)session.presentation.leftState.text="最后一张牌 · 鼠标瞄准 / 左键发射";
            clipPanel.SetActive(ClipPlaying&&!menu);
            if(ClipPlaying)
            {
                var sheet=clipKind==0?arrivalSheet:lossSheet;clipImage.texture=sheet;
                int frame=session.presentation.MotionReduced?(clipKind==0?0:6):Mathf.Clamp((int)((1-clipRemaining/clipDuration)*8),0,7);
                clipImage.uvRect=new Rect(frame%4*.25f,(1-frame/4)*.5f,.25f,.5f);
                clipImage.GetComponent<AspectRatioFitter>().aspectRatio=(sheet.width/4f)/(sheet.height/2f);
                clipCaption.text=clipKind==0?arrivalCaption:lossCaption;
            }
            cinemaPanel.SetActive(Cinematic&&!menu);
            if(Cinematic)
            {
                float h=Mathf.Min(1,Screen.width/(float)Screen.height/2.39f);endingCamera.rect=new Rect(0,(1-h)*.5f,1,h);
                SetAnchors((RectTransform)cinemaPanel.transform.Find("TopInkBar"),new Vector2(0,(1+h)*.5f),Vector2.one);
                SetAnchors((RectTransform)cinemaPanel.transform.Find("BottomInkBar"),Vector2.zero,new Vector2(1,(1-h)*.5f));
                var beat=endingSequence?endingSequence.At(endingClock):null;
                endingText.text=beat!=null?beat.speaker+"："+beat.text:endingLines.Length>0?endingLines[Mathf.Min(endingLines.Length-1,(int)(endingClock/subtitleSeconds))]:"";
                var art=cinemaPanel.transform.Find("Handdrawn_CinematicFrame");if(art)SetAnchors((RectTransform)art,new Vector2(0,(1-h)*.5f),new Vector2(1,(1+h)*.5f));
            }
            closingPanel.SetActive(EndingCard&&!menu);
            if(EndingCard){float alpha=Mathf.Clamp01(cardClock/Mathf.Max(.05f,closingFadeSeconds));closingBlack.color=new Color(0,0,0,alpha);closingText.color=new Color(1,1,1,Mathf.Clamp01((alpha-.5f)*2));closingHint.gameObject.SetActive(cardClock>closingFadeSeconds+.35f);}
            hintPanel.SetActive(hintRemaining>0&&!menu&&!Cinematic&&!ClipPlaying);hintText.text="往下跳。避开碎石。";
            if(finalSnow&&AtFinalPoint&&!EndingCard)
            {
                var cam=Cinematic?endingCamera:session.mirrorCamera;finalSnow.transform.position=new Vector3(cam.transform.position.x,cam.transform.position.y,-3);
                var shape=finalSnow.shape;shape.scale=new Vector3(Mathf.Max(30,cam.orthographicSize*cam.aspect*2+12),Mathf.Max(24,cam.orthographicSize*2+12),1);
                if(menu&&finalSnow.isPlaying)finalSnow.Pause();else if(!menu&&!finalSnow.isPlaying)finalSnow.Play();
            }
        }
        public void ShowEndingCard(){if(EndingCard)return;EndingCard=true;ClosingPage=0;cardClock=0;cardFrame=Time.frameCount;closingHint.text="点击继续";closingText.text=endingSequence?endingSequence.closingText:"今天先到这里。\n明天，我再往前走一点。";if(finalSnow)finalSnow.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);}
        public bool ReturnFromEnding()
        {
            if(!EndingCard||cardClock<closingFadeSeconds+.35f||Time.frameCount<=cardFrame)return false;
            cardFrame=Time.frameCount;
            if(ClosingPage<closingClickPages.Length)
            {
                closingText.text=closingClickPages[ClosingPage++];
                closingHint.text=ClosingPage<closingClickPages.Length?"点击继续":"点击返回主菜单";
            }
            else{TitleCompletionView.MarkCompleted();session.presentation.ReturnToTitle();}
            return true;
        }
        public void EndPresentationForMenu()
        {
            RestoreEndingCamera();
            EndingCard=Cinematic=AtFinalPoint=WaitingForGround=endingPending=false;clipRemaining=0;endingCamera.enabled=false;hologram.SetPlaying(false);
            session.surfaceCamera.enabled=session.mirrorCamera.enabled=true;if(layout)layout.enabled=true;
            if(finalSnow)finalSnow.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);gameplay.alpha=1;gameplay.blocksRaycasts=gameplay.interactable=true;
            if(blackout)blackout.SetActive(false);if(closingPanel)closingPanel.SetActive(false);if(cinemaPanel)cinemaPanel.SetActive(false);if(clipPanel)clipPanel.SetActive(false);
        }
        public LevelFinaleSave Capture()=>new LevelFinaleSave{lost=LostContact,recovered=Recovered,finalPoint=AtFinalPoint,hint=hintShown,iconsRestored=iconsRestored,ending=Cinematic};
        public void Restore(LevelFinaleSave data)
        {
            RestoreEndingCamera();
            // Older checkpoint files predate this flow; infer new story gates from saved switches.
            if(data==null)data=new LevelFinaleSave{lost=lossSwitch.IsOn&&!session.expansion.ReachedG,recovered=session.expansion.ReachedG,iconsRestored=session.expansion.ReachedD,finalPoint=finalSwitch.IsOn,ending=finalSwitch.IsOn};
            LostContact=data!=null&&data.lost;Recovered=data!=null&&data.recovered;AtFinalPoint=data!=null&&data.finalPoint;hintShown=data!=null&&data.hint;iconsRestored=data!=null&&data.iconsRestored;
            clipRemaining=hintRemaining=endingClock=0;endingPending=false;Cinematic=false;hologram.SetPlaying(false);endingCamera.enabled=false;
            EndingCard=false;WaitingForGround=AtFinalPoint&&!session.mirrorPlayer.Grounded;
            session.surfaceCamera.enabled=session.mirrorCamera.enabled=true;if(layout)layout.enabled=true;ApplyBlackout();
            if(finalSnow){if(AtFinalPoint)finalSnow.Play();else finalSnow.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);}
            if(LostContact||AtFinalPoint)session.ForceMirrorWorld();if(data!=null&&data.ending)BeginEnding();
        }
        static void SetAnchors(RectTransform r,Vector2 a,Vector2 b){r.anchorMin=a;r.anchorMax=b;r.offsetMin=r.offsetMax=Vector2.zero;}
        GameObject Node(string name,Transform parent,Vector2 a,Vector2 b){var g=new GameObject(name,typeof(RectTransform));g.transform.SetParent(parent,false);SetAnchors((RectTransform)g.transform,a,b);return g;}
        Image Panel(string name,Transform parent,Vector2 a,Vector2 b,Color color){var g=Node(name,parent,a,b);var img=g.AddComponent<Image>();img.color=color;img.raycastTarget=false;return img;}
        Text Label(string name,Transform parent,Vector2 a,Vector2 b,int size){var t=Node(name,parent,a,b).AddComponent<Text>();t.font=session.presentation.dialogue.font;t.fontSize=size;t.alignment=TextAnchor.MiddleCenter;t.color=new Color(.92f,.9f,.87f);t.raycastTarget=false;t.supportRichText=false;return t;}
        void BuildOverlay()
        {
            overlay=new GameObject("NarrativeFinale_Overlay",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler));overlay.transform.SetParent(session.transform.root,false);
            var canvas=overlay.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=250;
            var scaler=overlay.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1920,1080);scaler.matchWidthOrHeight=.5f;
            blackout=Panel("Surface_NoSignal",session.presentation.gameplayScreen.transform,Vector2.zero,Vector2.one,Color.black).gameObject;
            blackout.transform.SetSiblingIndex(decorativeFrame.transform.GetSiblingIndex());
            clipPanel=Panel("Glitch_Cutscene",overlay.transform,Vector2.zero,Vector2.one,Color.black).gameObject;
            var frame=Node("FrameAspect",clipPanel.transform,new Vector2(.22f,.13f),new Vector2(.78f,.94f));
            clipImage=Node("GeneratedFrames",frame.transform,Vector2.zero,Vector2.one).AddComponent<RawImage>();clipImage.raycastTarget=false;
            clipImage.gameObject.AddComponent<AspectRatioFitter>().aspectMode=AspectRatioFitter.AspectMode.FitInParent;
            clipCaption=Label("Whisper",clipPanel.transform,new Vector2(.12f,.02f),new Vector2(.88f,.16f),29);
            cinemaPanel=Node("Ending_Cinema",overlay.transform,Vector2.zero,Vector2.one);
            // Bars are opaque, the center remains transparent for the dedicated world camera.
            Panel("TopInkBar",cinemaPanel.transform,new Vector2(0,.88f),Vector2.one,Color.black);
            Panel("BottomInkBar",cinemaPanel.transform,Vector2.zero,new Vector2(1,.12f),Color.black);
            if(cinematicFrame){var frameArt=Panel("Handdrawn_CinematicFrame",cinemaPanel.transform,new Vector2(0,.128f),new Vector2(1,.872f),Color.white);frameArt.sprite=cinematicFrame;}
            if(subtitlePlate){var plate=Panel("Handdrawn_SubtitlePlate",cinemaPanel.transform,new Vector2(.05f,-.055f),new Vector2(.95f,.18f),Color.white);plate.sprite=subtitlePlate;plate.preserveAspect=false;}
            endingText=Label("EndingDialogue",cinemaPanel.transform,new Vector2(.10f,.01f),new Vector2(.90f,.115f),29);
            if(subtitlePlate)endingText.color=new Color(.1f,.1f,.1f);
            closingPanel=Panel("Final_BlackCard",overlay.transform,Vector2.zero,Vector2.one,Color.black).gameObject;closingBlack=closingPanel.GetComponent<Image>();
            closingText=Label("Editable_LastWords",closingPanel.transform,new Vector2(.15f,.36f),new Vector2(.85f,.65f),36);
            closingHint=Label("ReturnHint",closingPanel.transform,new Vector2(.3f,.14f),new Vector2(.7f,.20f),20);closingHint.text="点击返回主菜单";closingPanel.SetActive(false);
            hintPanel=Panel("DescentHintPaper",overlay.transform,new Vector2(.24f,.12f),new Vector2(.76f,.18f),new Color(.025f,.025f,.025f,.92f)).gameObject;
            hintText=Label("DescentHint",hintPanel.transform,Vector2.zero,Vector2.one,29);
            clipPanel.SetActive(false);cinemaPanel.SetActive(false);blackout.SetActive(false);hintPanel.SetActive(false);
        }
    }
}
