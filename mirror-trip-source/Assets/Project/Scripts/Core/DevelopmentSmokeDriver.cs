#if DEBUG && !UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;

namespace MirrorTrip
{
    // Opt-in standalone build verification. Nothing is instantiated during ordinary play.
    public sealed class DevelopmentSmokeDriver : MonoBehaviour
    {
        private string output;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (!Debug.isDebugBuild || !Environment.GetCommandLineArgs().Contains("--mirror-trip-smoke")) return;
            var host = new GameObject("DevelopmentSmokeDriver").AddComponent<DevelopmentSmokeDriver>();
            DontDestroyOnLoad(host.gameObject);
        }
        private void Start()
        {
            Application.runInBackground = true;
            output = Path.GetFullPath(Path.Combine(Application.dataPath, "../Smoke")); Directory.CreateDirectory(output);
            StartCoroutine(Guard(Exercise()));
        }
        private IEnumerator Guard(IEnumerator scenario)
        {
            while (true)
            {
                bool next; object current = null;
                try { next = scenario.MoveNext(); if (next) current = scenario.Current; }
                catch (Exception ex) { File.WriteAllText(Path.Combine(output, "result.txt"), "FAIL\n" + ex); Application.Quit(1); yield break; }
                if (!next) break;
                yield return current;
            }
            File.WriteAllText(Path.Combine(output, "result.txt"), "PASS\nWindows standalone full solution, paired gate, completion and surface exit.\n" + DateTime.UtcNow.ToString("O"));
            Application.Quit(0);
        }
        private IEnumerator Exercise()
        {
            for (int i = 0; i < 12; i++) yield return null;
            var session = FindAnyObjectByType<WorldSession>(); Require(session, "No session in standalone entry scene");
            session.ExternalControl = true;
            session.surfacePlayer.SetLocalPosition(new Vector2(13.3f, 1));
            Require(session.interactions.Press() && session.Reading, "Read notice");
            for (int i = 0; i < 4; i++) yield return null;
            ScreenCapture.CaptureScreenshot(Path.Combine(output, "notice.png"));
            for (int i = 0; i < 4; i++) yield return null;
            session.interactions.Close(); session.surfacePlayer.SetLocalPosition(new Vector2(16, 1));
            for (int i = 0; i < 4; i++) yield return null;
            ScreenCapture.CaptureScreenshot(Path.Combine(output, "initial.png"));
            for (int i = 0; i < 3; i++) yield return null;
            var frame = new WaitForFixedUpdate();
            var pair = session.placement.sceneMirrors[0];
            Require(session.interactions.Press() && session.placement.IsCarrying, "Pick existing mirror");
            session.SetMovement(Vector2.right);
            for (int i = 0; i < 7; i++) yield return frame;
            session.SetMovement(Vector2.zero);
            Require(session.interactions.Press() && !session.placement.IsCarrying, "Walk and drop same mirror");
            session.SwitchWorld(); Require(session.Shoot(new Vector2(.6f, 1.88f)), "Shoot");
            for (int i = 0; i < 180 && !session.puzzleSwitch.IsOn; i++) yield return frame;
            Require(session.puzzleSwitch.IsOn && pair.IsLocked && session.cards.TotalReflections > 0 && session.door.IsOpen, "Reflected switch / door");
            session.SetMovement(Vector2.right);
            for (int i = 0; i < 350 && !session.Completed; i++) yield return frame;
            Require(session.Completed && !session.MirrorActive, "Goal / return to surface");
            session.SetMovement(Vector2.right);
            for (int i = 0; i < 350 && !session.Exited; i++) yield return frame;
            Require(session.Exited && session.CompletionCount == 1, "Surface exit");
            ScreenCapture.CaptureScreenshot(Path.Combine(output, "completed.png"));
            for (int i = 0; i < 20; i++) yield return null;
            Require(File.Exists(Path.Combine(output, "initial.png")) && File.Exists(Path.Combine(output, "completed.png")), "Standalone screenshots");
        }
        private static void Require(bool condition, string reason) { if (!condition) throw new InvalidOperationException(reason); }
    }
}
#endif
