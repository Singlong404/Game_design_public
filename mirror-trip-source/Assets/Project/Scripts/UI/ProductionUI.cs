using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.UI;
namespace MirrorTrip
{
    [DefaultExecutionOrder(-100)]
    public sealed class ProductionUI : MonoBehaviour
    {
        public WorldSession session;
        public JourneySlotsUI saveSlots;
        public NotesJournalUI journal;
        public LevelGuideUI guide;
        public GameObject titleScreen, gameplayScreen, storyScreen, settingsScreen, pauseScreen, deathScreen;
        public GameObject failureDecoration, successDecoration;
        public GameObject[] settingsPages;
        public Image portrait, revealImage, leftInactive, rightInactive;
        public GameObject leftActiveFrame,rightActiveFrame;
        public Text switchNotice;
        public Sprite[] maleExpressions, femaleExpressions, revealFrames;
        public Sprite[] maleRevealFrames;
        private Sprite[] playingRevealFrames;
        public Text subtitle, interaction, objective, leftState, rightState, speaker, dialogue, pageHint, resultTitle, resultSubtitle;
        public bool showObjective = true;
        public bool animatePortraitBlink=true;
        public PortraitIdleMotion portraitMotion;
        public Vector2 portraitDisplaySize=new Vector2(480,600),portraitLeft=new Vector2(265,230),portraitRight=new Vector2(1130,230);
        public Button start, continueJourney, titleSettings, quit, pause, settings, resume, retry, pauseSettings, backTitle, closeSettings, storyNext, storyClose, deathRetry, deathTitle;
        public Button[] settingsTabs;
        public Slider masterVolume, musicVolume, effectsVolume, textSize;
        public Text masterValue, musicValue, effectsValue, textSizeValue;
        public Toggle fullscreen, reduceMotion;
        public UnityEvent onJourneyStarted, onReturnedToTitle, onChapterCompleted, onQuitRequested;
        public UnityEvent<float> onMusicVolumeChanged, onEffectsVolumeChanged;
        public event Action<string> ScreenChanged;
        public event Action<string> DiscoveryRecorded;
        public bool TitleOpen { get; private set; } = true;
        public bool SettingsOpen { get; private set; }
        public bool StoryOpen => storyLines != null && storyLines.Length > 0;
        public bool BlocksGameplay => TitleOpen || SettingsOpen || StoryOpen || (saveSlots&&saveSlots.IsOpen) || (journal&&journal.IsOpen) || (guide&&guide.IsOpen);
        public bool ConsumedInput => consumedFrame == Time.frameCount;
        public bool MotionReduced => reduceMotion && reduceMotion.isOn;
        public string CurrentScreen { get; private set; }
        public static bool SkipTitleOnce;
        private int consumedFrame = -1, storyIndex, expression;
        private string[] storyLines;
        private StorySequence storySequence;
        private string storySpeaker;
        private bool storyFemale, hadSwitch, hadExit, hasStarted;
        private float revealClock = -1, uiClock;
        private float tabStarted;
        private bool tabLong;
        private InputAction settingsKey, backKey, continueKey;
        private bool lastMirrorActive;
        private float switchNoticeUntil;
        private void Awake()
        {
            session.presentation = this;
            settingsKey=new InputAction("Settings",InputActionType.Button,"<Keyboard>/tab");settingsKey.Enable();
            backKey=new InputAction("Back",InputActionType.Button,"<Keyboard>/escape");backKey.Enable();
            continueKey=new InputAction("Continue",InputActionType.Button,"<Keyboard>/e");continueKey.Enable();
            TitleOpen = !SkipTitleOnce; hasStarted=SkipTitleOnce; SkipTitleOnce = false;
            start.onClick.AddListener(()=>{if(hasStarted)Restart();else StartJourney();}); continueJourney.onClick.AddListener(()=>{if(saveSlots)saveSlots.Open();else StartJourney();});
            titleSettings.onClick.AddListener(OpenSettings); quit.onClick.AddListener(Quit);
            pause.onClick.AddListener(()=>{if(guide)guide.Open();else session.TogglePause();}); settings.onClick.AddListener(OpenSettings);
            if(guide)pause.GetComponentInChildren<Text>().text="关卡提示";
            resume.onClick.AddListener(session.TogglePause); retry.onClick.AddListener(Restart);
            pauseSettings.onClick.AddListener(OpenSettings); backTitle.onClick.AddListener(ReturnToTitle);
            closeSettings.onClick.AddListener(CloseSettings); storyNext.onClick.AddListener(AdvanceStory); storyClose.onClick.AddListener(CloseStory);
            deathRetry.onClick.AddListener(()=>{if(session.saves)session.saves.Recover();else Restart();}); deathTitle.onClick.AddListener(ReturnToTitle);
            for (int i=0;i<settingsTabs.Length;i++) { int index=i; settingsTabs[i].onClick.AddListener(()=>SelectSettingsTab(index)); }
            masterVolume.SetValueWithoutNotify(PlayerPrefs.GetFloat("MirrorTrip.Master", .8f));
            musicVolume.SetValueWithoutNotify(PlayerPrefs.GetFloat("MirrorTrip.Music", .6f));
            effectsVolume.SetValueWithoutNotify(PlayerPrefs.GetFloat("MirrorTrip.Effects", .75f));
            textSize.SetValueWithoutNotify(PlayerPrefs.GetFloat("MirrorTrip.TextSize", 1));
            fullscreen.SetIsOnWithoutNotify(Screen.fullScreen);
            reduceMotion.SetIsOnWithoutNotify(PlayerPrefs.GetInt("MirrorTrip.ReduceMotion",0)==1);
            masterVolume.onValueChanged.AddListener(v=>{AudioListener.volume=v; SaveFloat("Master",v);});
            musicVolume.onValueChanged.AddListener(v=>{SaveFloat("Music",v); onMusicVolumeChanged.Invoke(v);});
            effectsVolume.onValueChanged.AddListener(v=>{SaveFloat("Effects",v); onEffectsVolumeChanged.Invoke(v);});
            textSize.onValueChanged.AddListener(v=>SaveFloat("TextSize",v));
            fullscreen.onValueChanged.AddListener(v=>Screen.fullScreen=v);
            reduceMotion.onValueChanged.AddListener(v=>{PlayerPrefs.SetInt("MirrorTrip.ReduceMotion",v?1:0);PlayerPrefs.Save();});
            AudioListener.volume=masterVolume.value; SelectSettingsTab(0);
            session.interactions.EntryDiscovered += OnDiscovery;
            Refresh();
        }
        private void OnDestroy()
        {
            if(session && session.interactions) session.interactions.EntryDiscovered -= OnDiscovery;
            settingsKey?.Dispose();backKey?.Dispose();continueKey?.Dispose();
        }
        private void OnDiscovery(string id) => DiscoveryRecorded?.Invoke(id);
        private static void SaveFloat(string key,float v) { PlayerPrefs.SetFloat("MirrorTrip."+key,v);PlayerPrefs.Save(); }
        private void Consume() { consumedFrame=Time.frameCount;session.SetMovement(Vector2.zero); }
        public void ConsumeModalInput() { Consume(); }
        public void StartJourney()
        {
            if(session.Fallen || session.Exited) { Restart(); return; }
            hasStarted=true;TitleOpen=false; SettingsOpen=false; if(session.Paused)session.TogglePause();
            Consume();onJourneyStarted.Invoke(); Refresh();
        }
        public void OpenSettings() { if(journal)journal.Close();SettingsOpen=true;Consume();Refresh(); }
        public void CloseSettings() { SettingsOpen=false;Consume();Refresh(); }
        public void ReturnToTitle()
        {
            if(session.switchFocus)session.switchFocus.Cancel();
            if(session.finale)session.finale.EndPresentationForMenu();
            SettingsOpen=false;if(journal)journal.Close();if(saveSlots)saveSlots.Close(); CloseStory();session.placement.CancelCarry();
            if(session.Paused)session.TogglePause();TitleOpen=true;Consume();onReturnedToTitle.Invoke();Refresh();
        }
        public void Restart() { SkipTitleOnce=true;if(session.saves)session.saves.NewJourney();else session.Retry(); }
        public void AfterLoad() { if(journal)journal.Close();hadSwitch=session.puzzleSwitch.IsOn;hadExit=session.Exited;revealClock=-1;revealImage.gameObject.SetActive(false);storyLines=null;hasStarted=true;TitleOpen=SettingsOpen=false;if(saveSlots)saveSlots.Close();Consume();Refresh(); }
        public void Quit()
        {
            onQuitRequested.Invoke();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying=false;
#else
            Application.Quit();
#endif
        }
        public void SelectSettingsTab(int index)
        {
            for(int i=0;i<settingsPages.Length;i++)
            { settingsPages[i].SetActive(i==index); settingsTabs[i].interactable=i!=index; }
        }
        // Future narrative/achievement hooks: callers provide text and choose an expression, no inventory required.
        public void ShowStory(string name, string[] pages, bool female=true, int expressionIndex=0)
        {
            storySequence=null;
            if(pages==null || pages.Length==0 || TitleOpen || session.Fallen || session.Exited)return;
            if(journal)journal.Close();session.interactions.Close();storySpeaker=name;storyLines=pages;storyIndex=0;storyFemale=female;
            expression=Mathf.Clamp(expressionIndex,0,7);Consume();Refresh();
        }
        public void ShowSequence(StorySequence sequence)
        {
            if(!sequence||sequence.lines.Length==0)return;
            var content=new string[sequence.lines.Length];for(int i=0;i<content.Length;i++)content[i]=sequence.lines[i].text;
            var first=sequence.lines[0];ShowStory(first.speaker,content,first.female,first.expression);storySequence=sequence;Refresh();
        }
        public void SetExpression(bool female,int index) { storyFemale=female;expression=Mathf.Clamp(index,0,7); }
        public void AdvanceStory()
        {
            Consume();
            if(session.Reading)session.interactions.Advance();
            else if(StoryOpen && ++storyIndex>=storyLines.Length)storyLines=null;
            Refresh();
        }
        public void CloseStory() { storyLines=null;session.interactions.Close();Consume();Refresh(); }
        public void PlayReveal() { playingRevealFrames=revealFrames;revealClock=0; }
        public void PlayMaleReveal() { playingRevealFrames=maleRevealFrames;revealClock=0; }
        public int RevealFrame { get; private set; }
        public bool HandleEscape()
        {
            if(guide&&guide.IsOpen){guide.Close();return true;}
            if(journal&&journal.IsOpen){journal.Close();return true;}
            if(saveSlots&&saveSlots.IsOpen){saveSlots.Close();return true;}
            if(SettingsOpen){CloseSettings();return true;}
            if(StoryOpen){CloseStory();return true;}
            return TitleOpen;
        }
        public bool BlocksPointer(Vector2 position)
        {
            if(BlocksGameplay || session.Paused || session.Reading || session.Fallen || session.Exited)return true;
            return !session.mirrorCamera.pixelRect.Contains(position) && !session.surfaceCamera.pixelRect.Contains(position);
        }
        private void Update()
        {
            uiClock+=Time.unscaledDeltaTime;
            if(lastMirrorActive!=session.MirrorActive)
            {
                lastMirrorActive=session.MirrorActive;switchNoticeUntil=Time.unscaledTime+1.0f;
            }
            if(!session.ExternalControl && !session.Loading)
            {
                if(settingsKey.WasPressedThisFrame()){tabStarted=Time.unscaledTime;tabLong=false;}
                if(settingsKey.IsPressed()&&!tabLong&&Time.unscaledTime-tabStarted>=.4f){tabLong=true;if(guide&&(guide.IsOpen||guide.CanOpen))guide.Toggle();}
                if(settingsKey.WasReleasedThisFrame()&&!tabLong){if(guide&&guide.IsOpen)guide.Close();else if(saveSlots&&saveSlots.IsOpen)saveSlots.Close();else if(SettingsOpen)CloseSettings();else OpenSettings();}
                if(backKey.WasPressedThisFrame() && HandleEscape())Consume();
                else if(continueKey.WasPressedThisFrame() && journal && journal.IsOpen)journal.AdvanceOrClose();
                else if(continueKey.WasPressedThisFrame() && StoryOpen && !SettingsOpen && !TitleOpen && !(saveSlots&&saveSlots.IsOpen))AdvanceStory();
            }
            if(!session.levelSequence && !hadSwitch && session.puzzleSwitch.IsOn)
            {
                hadSwitch=true;PlayReveal();
                ShowStory("镜中旅人",new[]{"门开了，镜子就留在这里。", "去吧。等你走到镜子的另一端，我们就能再次相遇。"},true,3);
            }
            if(!hadExit && session.Exited){hadExit=true;onChapterCompleted.Invoke();}
            if(revealClock>=0)
            {
                var frames=playingRevealFrames??revealFrames;
                revealClock+=Time.unscaledDeltaTime;
                float[] ends={.06f,.13f,.21f,.62f,.76f,.88f};int index=0;
                while(index<ends.Length&&revealClock>=ends[index])index++;
                RevealFrame=index;
                revealImage.gameObject.SetActive(!MotionReduced && index<frames.Length && !TitleOpen && !SettingsOpen && !session.Fallen && !session.Exited && !session.Paused);
                if(index<frames.Length)revealImage.sprite=frames[index];else revealClock=-1;
            }
            Refresh();
        }
        public void Refresh()
        {
            if(!session)return;
            bool reading=session.Reading || StoryOpen;
            subtitle.gameObject.SetActive(!reading);objective.gameObject.SetActive(!reading && showObjective);interaction.gameObject.SetActive(!reading);
            if(leftActiveFrame)interaction.gameObject.SetActive(false);
            var narrator=gameplayScreen.transform.Find("Narrator");if(narrator)narrator.gameObject.SetActive(!reading);
            titleScreen.SetActive(TitleOpen); gameplayScreen.SetActive(!TitleOpen);
            storyScreen.SetActive(!TitleOpen && reading && !SettingsOpen);
            settingsScreen.SetActive(SettingsOpen);
            pauseScreen.SetActive(!TitleOpen && session.Paused && !SettingsOpen && !reading);
            deathScreen.SetActive(!TitleOpen && (session.Fallen || session.Exited) && !SettingsOpen);
            string next=SettingsOpen?"Settings":TitleOpen?"Title":session.Fallen?"Death":session.Exited?"Complete":journal&&journal.IsOpen?"Notes":reading?"Story":session.Paused?"Pause":"Gameplay";
            if(next!=CurrentScreen){CurrentScreen=next;ScreenChanged?.Invoke(next);}
            continueJourney.interactable=session.saves?session.saves.HasSaves:hasStarted && !session.Fallen && !session.Exited;
            if(session.saves)deathRetry.GetComponentInChildren<Text>().text=session.saves.HasSaves?"回到最近存档":"重新开始";
            if(failureDecoration)failureDecoration.SetActive(!session.Exited);
            if(successDecoration)successDecoration.SetActive(session.Exited);
            leftInactive.gameObject.SetActive(!session.MirrorActive); rightInactive.gameObject.SetActive(session.MirrorActive);
            if(leftActiveFrame)leftActiveFrame.SetActive(session.MirrorActive);
            if(rightActiveFrame)rightActiveFrame.SetActive(!session.MirrorActive);
            leftState.text=session.MirrorActive?"▼ 当前操作  ·  A/D 移动 / 空格跳跃":"Ⅱ 时间静止";rightState.text=session.MirrorActive?"Ⅱ 时间静止":"▼ 当前操作  ·  WASD 移动";
            if(session.levelSequence&&!session.levelSequence.SurfaceUnlocked)rightState.text="尚未唤醒 · 到达 point_A 后解锁";
            leftState.color=session.MirrorActive?Color.white:new Color(.35f,.35f,.35f);rightState.color=session.MirrorActive?new Color(.35f,.35f,.35f):Color.white;
            if(switchNotice)
            {
                switchNotice.gameObject.SetActive(false);
                switchNotice.text="";
            }
            objective.text=session.Completed?"前往表世界出口":session.puzzleSwitch.IsOn?"镜子已固定 · 前往目标镜":"搬运镜子  →  反射卡牌  →  开启机关";
            interaction.text=session.interactions.Prompt;
            subtitle.text=string.IsNullOrEmpty(session.Message)?"":session.Message;
            if(leftActiveFrame&&!string.IsNullOrEmpty(session.interactions.Prompt))subtitle.text=session.interactions.Prompt;
            if(session.storyDirector&&!string.IsNullOrEmpty(session.storyDirector.BottomText))subtitle.text=session.storyDirector.BottomText;
            resultTitle.text=session.Exited?"镜途未尽":"镜像破碎";
            resultSubtitle.text=session.Exited?"第一章已完成。":"读取存档继续游戏，或重新开始。";
            if(reading)
            {
                if(StoryOpen&&storySequence&&storyIndex<storySequence.lines.Length){var beat=storySequence.lines[storyIndex];storyFemale=beat.female;storySpeaker=beat.speaker;expression=beat.expression;}
                bool female=StoryOpen?storyFemale:session.MirrorActive;
                var sprites=female?femaleExpressions:maleExpressions;
                int e=StoryOpen?expression:0;
                float blink=uiClock%4.3f;if(animatePortraitBlink && !MotionReduced && e==0 && blink>4)e=blink<4.08f?4:blink<4.2f?5:4;
                portrait.sprite=sprites[Mathf.Clamp(e,0,sprites.Length-1)];
                if(portraitMotion){var frame=portraitMotion.Resolve(female,e,MotionReduced,!SettingsOpen&&!TitleOpen&&!session.Paused&&!session.Fallen);if(frame)portrait.sprite=frame;}
                // Keep the active play window clear, regardless of which character is speaking.
                portrait.rectTransform.anchoredPosition=session.MirrorActive?portraitRight:portraitLeft;
                if(portraitMotion)portrait.rectTransform.anchoredPosition+=portraitMotion.CurrentOffset;
                portrait.rectTransform.sizeDelta=portraitDisplaySize;
                speaker.text=StoryOpen?storySpeaker:session.interactions.CurrentEntry.title;
                dialogue.text=StoryOpen?storyLines[storyIndex]:session.interactions.PageText;
                int page=StoryOpen?storyIndex:session.interactions.PageIndex, count=StoryOpen?storyLines.Length:session.interactions.PageCount;
                pageHint.text=$"{page+1} / {count}     E {(page+1<count?"继续":"关闭")}    Esc 返回";
                dialogue.fontSize=Mathf.RoundToInt(30*textSize.value);
            }
            masterValue.text=Mathf.RoundToInt(masterVolume.value*100).ToString();
            musicValue.text=Mathf.RoundToInt(musicVolume.value*100).ToString();
            effectsValue.text=Mathf.RoundToInt(effectsVolume.value*100).ToString();
            textSizeValue.text=Mathf.RoundToInt(textSize.value*100)+"%";
        }
    }
}
