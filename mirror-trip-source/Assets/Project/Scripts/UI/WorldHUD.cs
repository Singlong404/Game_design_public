using UnityEngine;
using UnityEngine.UI;
namespace MirrorTrip
{
    public sealed class WorldHUD : MonoBehaviour
    {
        public Text mirrorTitle, surfaceTitle, mirrorState, status, objective;
        public GameObject pausePanel, resultPanel, readingPanel;
        public Text resultText, interactionPrompt, readingTitle, readingBody, readingFooter;
        public RectTransform[] pointerBlockers;
        public Button pauseButton, retryButton, resumeButton, pauseRetryButton, resultRetryButton, readingNext, readingClose;
        public WorldSession session;
        public ProductionUI production;
        private Font textFont;
        private void Start()
        {
            if (production) return;
            textFont = Font.CreateDynamicFontFromOSFont(new[] { "Microsoft YaHei", "SimHei", "Arial Unicode MS" }, 28);
            foreach (var text in new[] { status, mirrorState, interactionPrompt, readingTitle, readingBody, readingFooter,
                readingNext.GetComponentInChildren<Text>(), readingClose.GetComponentInChildren<Text>() }) text.font = textFont;
            pauseButton.onClick.AddListener(session.TogglePause);
            retryButton.onClick.AddListener(session.Retry);
            resumeButton.onClick.AddListener(session.TogglePause);
            pauseRetryButton.onClick.AddListener(session.Retry);
            resultRetryButton.onClick.AddListener(session.Retry);
            readingNext.onClick.AddListener(session.interactions.Advance);
            readingClose.onClick.AddListener(session.interactions.Close);
        }
        private void OnDestroy() { if (textFont) Destroy(textFont); }
        public bool BlocksPointer(Vector2 screen)
        {
            if (production) return production.BlocksPointer(screen);
            foreach (var rect in pointerBlockers)
                if (rect && rect.gameObject.activeInHierarchy && RectTransformUtility.RectangleContainsScreenPoint(rect, screen)) return true;
            return false;
        }
        public void Refresh(WorldSession state)
        {
            if (production) { production.Refresh(); return; }
            bool running = !state.Paused && !state.Reading && !state.Exited && !state.Fallen;
            mirrorTitle.text = "MIRROR WORLD" + (state.MirrorActive && running ? "  / ACTIVE" : "  / FROZEN");
            surfaceTitle.text = "SURFACE WORLD" + (!state.MirrorActive && running ? "  / ACTIVE" : "  / FROZEN");
            int locked = 0; foreach (var mirror in state.placement.sceneMirrors) if (mirror.IsLocked) locked++;
            mirrorState.text = state.placement.IsCarrying ? "镜子：搬运中" : locked > 0 ? "镜子：机关完成，已固定" : "镜子：可搬运";
            status.text = state.Message;
            interactionPrompt.text = state.interactions.Prompt;
            objective.text = state.Completed ? "RESCUER -> GREEN EXIT" : state.puzzleSwitch.IsOn ? "FRIEND -> PURPLE GOAL" : "MOVE MIRROR -> REFLECT CARD -> SWITCH";
            pausePanel.SetActive(state.Paused); readingPanel.SetActive(state.Reading);
            resultPanel.SetActive(state.Exited || state.Fallen);
            resultText.text = state.Exited ? "FIRST CHAMBER COMPLETE" : "FALLEN - RETRY";
            if (state.Reading)
            {
                var dialog = state.interactions;
                readingTitle.text = dialog.CurrentEntry.title; readingBody.text = dialog.PageText;
                bool last = dialog.PageIndex + 1 >= dialog.PageCount;
                readingFooter.text = (dialog.PageIndex + 1) + " / " + dialog.PageCount + "    [E] " + (last ? "关闭" : "下一页") + "    [Esc] 关闭";
                readingNext.GetComponentInChildren<Text>().text = last ? "关闭 [E]" : "下一页 [E]";
                readingClose.GetComponentInChildren<Text>().text = "关闭 [Esc]";
            }
        }
    }
}
