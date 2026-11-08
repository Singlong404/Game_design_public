using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace MirrorTrip
{
    public sealed class WorldSession : MonoBehaviour
    {
        public InputActionAsset inputTemplate;
        public WorldPlayerMotor surfacePlayer, mirrorPlayer;
        public Camera surfaceCamera, mirrorCamera;
        public MirrorPlacementSystem placement;
        public InteractionSystem interactions;
        public CardSystem cards;
        public CardSwitch puzzleSwitch;
        public PairedDoor door;
        public BoxCollider2D goal, exit;
        public SpriteRenderer[] goalVisuals;
        public WorldHUD hud;
        public ProductionUI presentation;
        public JourneySaveSystem saves;
        public Level01Sequence levelSequence;
        public Level01Expansion expansion;
        public Level01CameraDirector cameraDirector;
        public Level01FinaleFlow finale;
        public WorldBoundaryDeath boundaryDeath;
        public SwitchFocusDirector switchFocus;
        public SurfaceFootstepFX surfaceFootsteps;
        public GameplayFeedback feedback;
        public LevelStoryDirector storyDirector;
        public LevelAudioDirector audioDirector;
        public bool MovementLocked=>(finale&&finale.MovementLocked)||(switchFocus&&switchFocus.Busy);
        public bool startInMirror;
        public MechanismMirror[] mechanismMirrors = new MechanismMirror[0];
        public CharacterFrameAnimator surfaceAnimation, mirrorAnimation;
        public Transform aimIndicator;
        [Header("World-relative playable bounds (Level authoring)")]
        public Vector2 surfaceMinimum = new Vector2(-1.5f, -2), surfaceMaximum = new Vector2(29, 14);
        public float mirrorFallHeight = -2;
        public bool MirrorActive { get; private set; }
        public bool Paused { get; private set; }
        public bool Completed { get; private set; }
        public bool Exited { get; private set; }
        public bool Fallen { get; private set; }
        public bool Loading { get; private set; }
        public int CompletionCount { get; private set; }
        public bool Reading => interactions && interactions.IsReading;
        public bool PresentationBlocked => (presentation && presentation.BlocksGameplay) || (levelSequence && levelSequence.Revealing) || (finale && finale.InputBlocked);
        public bool CanInteract => !Paused && !Reading && !Fallen && !Loading && !Exited && !PresentationBlocked && !(switchFocus&&switchFocus.Busy);
        public bool CanSurfaceInteract => !MirrorActive && !Completed && CanInteract;
        public bool CanShoot => MirrorActive && !Completed && CanInteract;
        public bool ExternalControl { get; set; }
        public string Message { get; private set; }
        private float messageUntil;
        private Vector2 movement, aim = Vector2.up;
        public Vector2 AimDirection => aim;
        private bool jumpHeld;
        private InputActionAsset controls;
        private InputActionMap surfaceMap, mirrorMap, globalMap;

        private void Awake()
        {
            controls = Instantiate(inputTemplate);
            surfaceMap = controls.FindActionMap("Surface", true);
            mirrorMap = controls.FindActionMap("Mirror", true);
            globalMap = controls.FindActionMap("Global", true);
            MirrorActive=startInMirror;globalMap.Enable(); ApplyInputContext();
            if(!storyDirector)ShowMessage("靠近现有镜子按 E 拿起，WASD 搬运，再按 E 放下。", 9);
        }

        private void OnDestroy()
        {
            if (controls != null) { controls.Disable(); Destroy(controls); }
        }

        private void Update()
        {
            if (!ExternalControl && !Loading && !(presentation && presentation.ConsumedInput))
            {
                if(!MovementLocked&&Mouse.current!=null&&Mouse.current.leftButton.wasPressedThisFrame&&interactions.TryClickChest(Mouse.current.position.ReadValue()))return;
                if (globalMap.FindAction("Retry").WasPressedThisFrame() && !PresentationBlocked) { Retry(); return; }
                if (globalMap.FindAction("Pause").WasPressedThisFrame())
                {
                    if (presentation && presentation.HandleEscape()) { }
                    else if (Reading) interactions.Close();
                    else if (placement.IsCarrying) { placement.CancelCarry(); ShowMessage("镜子已放回原处。"); }
                    else TogglePause();
                }
                if (!PresentationBlocked && !MovementLocked && globalMap.FindAction("Interact").WasPressedThisFrame())
                {
                    if (interactions.Press()) (MirrorActive ? mirrorAnimation : surfaceAnimation)?.PlayAction("Interact");
                }
                var switchAction=globalMap.FindAction("SwitchWorld");
                if(levelSequence&&levelSequence.holdShift)
                {
                    bool wantMirror=!switchAction.IsPressed()||!levelSequence.SurfaceUnlocked;
                    if(wantMirror!=MirrorActive&&!PresentationBlocked&&!Paused&&!Reading)
                    {if(wantMirror&&placement.IsCarrying)placement.CancelCarry();SwitchWorld();}
                }
                else if (switchAction.WasPressedThisFrame()) SwitchWorld();
                if (!Paused && !Reading && !Fallen && !Exited && !PresentationBlocked)
                {
                    SetSprint(Keyboard.current != null && (Keyboard.current.leftCtrlKey.isPressed || Keyboard.current.rightCtrlKey.isPressed));
                    if (MirrorActive)
                    {
                        movement = new Vector2(mirrorMap.FindAction("Move").ReadValue<float>(), 0);
                        jumpHeld = mirrorMap.FindAction("Jump").IsPressed();
                        if (!MovementLocked && mirrorMap.FindAction("Jump").WasPressedThisFrame()) mirrorPlayer.QueueJump();
                        var pointer = mirrorMap.FindAction("Aim").ReadValue<Vector2>();
                        if (PointerAllowed(mirrorCamera, pointer))
                        {
                            aim = ((Vector2)mirrorCamera.ScreenToWorldPoint(pointer) - mirrorPlayer.AimOrigin).normalized;
                            if (mirrorMap.FindAction("Throw").WasPressedThisFrame()) Shoot(aim);
                        }
                    }
                    else
                    {
                        movement = surfaceMap.FindAction("Move").ReadValue<Vector2>(); jumpHeld = false;

                    }
                }
            }
            if (aimIndicator)
            {
                aimIndicator.gameObject.SetActive(CanShoot);
                aimIndicator.position = mirrorPlayer.AimOrigin + aim * .65f;
                aimIndicator.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(aim.y, aim.x) * Mathf.Rad2Deg);
            }
            if (Time.unscaledTime > messageUntil) Message = "";
            if (interactions) { interactions.RefreshTarget(); interactions.TickSurfaceSwitch(!ExternalControl && Keyboard.current!=null && Keyboard.current.eKey.isPressed,Time.deltaTime); }
            if (hud) hud.Refresh(this);
        }

        private void FixedUpdate()
        {
            if(finale&&finale.WaitingForGround&&!Paused&&!Reading&&!Fallen&&!Loading&&!presentation.BlocksGameplay)
            {
                mirrorPlayer.Tick(Vector2.zero,false,Time.fixedDeltaTime);finale.CheckGrounded();
                // Final-point movement is locked, but aiming and projectile simulation remain live.
                cards.Tick(Time.fixedDeltaTime);
                if(boundaryDeath)boundaryDeath.Check(true);return;
            }
            if(switchFocus&&switchFocus.Busy)return;
            if (Paused || Reading || Fallen || Exited || Loading || PresentationBlocked) return;
            if (MirrorActive)
            {
                if(!MovementLocked)mirrorPlayer.Tick(movement, jumpHeld, Time.fixedDeltaTime);
                cards.Tick(Time.fixedDeltaTime);
            }
            else { surfacePlayer.Tick(movement, false, Time.fixedDeltaTime); placement.TickCarry(); }
            if(!MovementLocked)foreach(var mirror in mechanismMirrors)if(mirror&&mirror.isActiveAndEnabled&&mirror.mirrorSide==MirrorActive)mirror.Tick(Time.fixedDeltaTime);
            if(levelSequence)levelSequence.Tick();
            if(expansion)expansion.Tick();
            if(cameraDirector)cameraDirector.Tick();
            if(finale)finale.Tick();
            EvaluateProgress();
            if(boundaryDeath){boundaryDeath.Check();return;}
            var surfacePosition = surfacePlayer.LocalPosition;
            if (mirrorPlayer.LocalPosition.y < mirrorFallHeight || surfacePosition.y < surfaceMinimum.y || surfacePosition.y > surfaceMaximum.y || surfacePosition.x < surfaceMinimum.x || surfacePosition.x > surfaceMaximum.x)
            {
                Fallen = true; placement.CancelCarry(); ApplyInputContext(); ShowMessage("镜像破碎。按 R 重新开始。", 30);
            }
        }

        public void KillByFall() { if(Fallen)return;Fallen=true;movement=Vector2.zero;placement.CancelCarry();ApplyInputContext();ShowMessage("坠落过高，镜像破碎。按 R 重试或读取存档。",30); }

        public void SetMovement(Vector2 value, bool held = false) { movement = MovementLocked?Vector2.zero:value; jumpHeld = !MovementLocked&&held; }
        public void SetSprint(bool held) { (MirrorActive ? mirrorPlayer : surfacePlayer).SprintHeld = !MovementLocked&&held; }
        public void Jump() { if (MirrorActive && CanInteract && !MovementLocked) mirrorPlayer.QueueJump(); }
        public void ForceMirrorWorld() { placement.CancelCarry();MirrorActive=true;movement=Vector2.zero;jumpHeld=false;mirrorPlayer.ResetTransient();surfacePlayer.ResetTransient();ApplyInputContext(); }
        public void KillOutOfBounds() { KillByFall();ShowMessage("走出了镜面的边界。按 R 重试或读取存档。",30); }

        public bool SwitchWorld()
        {
            if(storyDirector&&storyDirector.ArrivalPending)return false;
            if(switchFocus&&switchFocus.Busy)return false;
            if (Paused || Reading || Completed || Fallen || Exited || Loading || PresentationBlocked) return false;
            if(finale&&finale.SwitchLocked){ShowMessage(finale.AtFinalPoint?"最后一张牌。瞄准前方的开关。":"没有回应……镜子另一边，只剩黑暗。",4);return false;}
            if(MirrorActive&&levelSequence&&!levelSequence.SurfaceUnlocked){ShowMessage("到达 point_A 后才能唤醒表世界。");return false;}
            if (placement.IsCarrying) { ShowMessage("请先按 E 放下镜子，或按 Esc 放回原处。"); return false; }
            placement.CancelCarry(); movement = Vector2.zero; jumpHeld = false;
            mirrorPlayer.SprintHeld = surfacePlayer.SprintHeld = false;
            MirrorActive = !MirrorActive; ApplyInputContext(); Physics2D.SyncTransforms();
            ShowMessage(MirrorActive ? "里世界：A/D 移动，空格跳跃，鼠标左键发射卡牌。" : "表世界：WASD 移动，E 阅读／拿起／放下。");
            return true;
        }

        public void TogglePause()
        {
            if (Loading || Reading || Exited || Fallen) return;
            Paused = !Paused; movement = Vector2.zero;
            ApplyInputContext();
        }

        private void ApplyInputContext()
        {
            surfaceMap.Disable(); mirrorMap.Disable();
            if (Paused || Reading || Loading || Fallen || Exited) return;
            if (MirrorActive) mirrorMap.Enable(); else surfaceMap.Enable();
        }

        public bool Shoot(Vector2 direction)
        {
            if (direction.sqrMagnitude > .001f) aim = direction.normalized;
            bool fired = CanShoot && cards.Fire(mirrorPlayer.AimOrigin, direction);
            if (fired && mirrorAnimation) mirrorAnimation.PlayAction("Throw");
            if(fired&&audioDirector)audioDirector.PlayThrow();
            return fired;
        }

        public bool PointerAllowed(Camera camera, Vector2 screen)
        {
            if (screen.x < 0 || screen.y < 0 || screen.x >= Screen.width || screen.y >= Screen.height || !camera.pixelRect.Contains(screen)) return false;
            if (EventSystem.current && EventSystem.current.IsPointerOverGameObject()) return false;
            return !hud || !hud.BlocksPointer(screen);
        }

        public void OpenPuzzleDoor()
        {
            door.Open(); placement.LockPuzzle(puzzleSwitch.puzzleId);
            ShowMessage("机关已开启，本段镜子已固定。向右走进目标镜。", 8);
        }

        public void EvaluateProgress()
        {
            if(levelSequence)return; // This authored level uses its own points, not the old test-scene finish trigger.
            if (Paused || Reading || Loading || Fallen || Exited || PresentationBlocked) return;
            if (!Completed && MirrorActive && puzzleSwitch.IsOn && goal.bounds.Intersects(mirrorPlayer.Bounds))
            {
                Completed = true; CompletionCount++; MirrorActive = false; movement = Vector2.zero;
                placement.CancelCarry(); cards.Clear();
                foreach (var visual in goalVisuals) visual.color = Color.gray;
                ApplyInputContext(); ShowMessage("镜中旅人已抵达。让表世界的旅人继续向右前往出口。", 10);
            }
            if (Completed && !MirrorActive && exit.bounds.Intersects(surfacePlayer.Bounds))
            {
                Exited = true; movement = Vector2.zero; ApplyInputContext();
                ShowMessage("第一章完成。按 R 再次游玩。", 60);
            }
        }

        public void ShowMessage(string value, float duration = 3)
        { Message = value; messageUntil = Time.unscaledTime + duration; }

        public void RestoreProgress(bool mirrorActive,bool completed,bool exited)
        {
            if(switchFocus)switchFocus.Cancel();
            if(feedback)feedback.Clear();
            if(surfaceFootsteps)surfaceFootsteps.Clear();
            MirrorActive=mirrorActive;Completed=completed;Exited=exited;Fallen=false;Paused=false;Loading=false;
            CompletionCount=completed?1:0;movement=Vector2.zero;jumpHeld=false;
            surfacePlayer.ResetTransient();mirrorPlayer.ResetTransient();interactions.Close();cards.Clear();placement.CancelCarry();
            ApplyInputContext();
            surfaceCamera.GetComponent<PlayerFollowCamera>()?.SnapToTarget();mirrorCamera.GetComponent<PlayerFollowCamera>()?.SnapToTarget();
        }
        public void Retry()
        {
            if (Loading) return;
            if (presentation) ProductionUI.SkipTitleOnce = true;
            interactions.Close(); Loading = true; Paused = false; placement.CancelCarry(); cards.Clear(); movement = Vector2.zero;
            ApplyInputContext(); StartCoroutine(Reload());
        }

        private IEnumerator Reload()
        {
            string path = gameObject.scene.path;
            var operation = SceneManager.LoadSceneAsync(path, LoadSceneMode.Single);
            if (operation == null)
            {
                Loading = false; ApplyInputContext(); ShowMessage("Reload failed. Retry is still available.", 8); yield break;
            }
            yield return operation;
        }
    }
}
