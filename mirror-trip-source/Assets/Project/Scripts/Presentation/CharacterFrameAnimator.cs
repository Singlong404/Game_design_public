using UnityEngine;
namespace MirrorTrip
{
    // Presentation only: never moves the motor, collider or projectile origin.
    public sealed class CharacterFrameAnimator : MonoBehaviour
    {
        public WorldSession session;
        [Tooltip("Enable only for a standalone level test without WorldSession.")]
        public bool allowWithoutSession;
        public WorldPlayerMotor motor;
        public SpriteRenderer visual;
        public CharacterAnimationSet animations;
        public string CurrentAnimation { get; private set; }
        public int CurrentFrame { get; private set; }
        private CharacterAnimationSet.Sequence sequence;
        private float clock, actionRemaining;
        private string action, direction = "Down";
        private bool dead;
        private bool wasAirborne;
        private float landingRemaining;
        private Vector3 authoredVisualScale;
        private bool capturedVisualScale;
        private void Awake() { CaptureVisualScale(); }
        private void CaptureVisualScale()
        { if (!capturedVisualScale && visual) { authoredVisualScale=visual.transform.localScale;capturedVisualScale=true; } }
        private void ApplyFrame(int index)
        {
            CaptureVisualScale();
            CurrentFrame=index;visual.sprite=sequence.frames[index];
            float scale=sequence.visualScales!=null && index<sequence.visualScales.Length && sequence.visualScales[index]>0 ? sequence.visualScales[index] : 1;
            visual.transform.localScale=authoredVisualScale*scale;
        }
        private void OnDisable() { if(capturedVisualScale&&visual)visual.transform.localScale=authoredVisualScale; }
        public void PlayAction(string key)
        {
            var clip = animations.Find(key);
            if (clip == null) return;
            action = key; actionRemaining = clip.frames.Length / clip.fps;
            Select(key, true);
        }
        public void PlayHurt() => PlayAction("Hurt");
        private void Select(string key, bool restart = false)
        {
            if (!restart && CurrentAnimation == key) return;
            var next = animations.Find(key); if (next == null || next.frames.Length == 0) return;
            sequence = next; CurrentAnimation = key; clock = 0; ApplyFrame(0);
        }
        private void LateUpdate()
        {
            if ((!session && !allowWithoutSession) || !motor || !animations || !visual) return;
            if (session && session.Fallen)
            {
                if (!dead) { dead = true; actionRemaining = 0; Select("Death", true); }
                Advance(Time.unscaledDeltaTime); return;
            }
            if (session && (session.Paused || session.Reading || session.Exited || session.Loading || (session.presentation && session.presentation.BlocksGameplay))) return;
            if (session && session.MirrorActive != motor.sideView) return;
            if (action == "Interact" && motor.Velocity.sqrMagnitude > .01f) actionRemaining = 0;
            if (actionRemaining > 0)
            {
                actionRemaining -= Time.deltaTime; Advance(Time.deltaTime); return;
            }
            var v = motor.Velocity;
            bool moving = motor.sideView ? Mathf.Abs(v.x) > .02f : v.sqrMagnitude > .001f;
            if (motor.sideView)
            {
                if (Mathf.Abs(v.x) > .02f) visual.flipX = v.x < 0;
                if(!motor.Grounded)
                {
                    wasAirborne=true;Select("Jump");
                    int phase=v.y>5?0:v.y>2?1:v.y>-.8f?2:3;
                    if(sequence!=null)ApplyFrame(Mathf.Min(phase,sequence.frames.Length-1));
                    return;
                }
                if(wasAirborne){wasAirborne=false;landingRemaining=.14f;}
                if(landingRemaining>0)
                {
                    landingRemaining-=Time.deltaTime;Select("Jump");
                    if(sequence!=null)ApplyFrame(Mathf.Min(landingRemaining>.06f?4:5,sequence.frames.Length-1));return;
                }
                Select(moving ? (motor.Sprinting && animations.Find("Sprint")!=null ? "Sprint" : "Walk") : "Idle");
            }
            else
            {
                if (moving) direction = Mathf.Abs(v.x) > Mathf.Abs(v.y) ? (v.x < 0 ? "Left" : "Right") : (v.y < 0 ? "Down" : "Up");
                Select((moving ? "Walk" : "Idle") + direction);
            }
            Advance(Time.deltaTime * (!motor.sideView && motor.Sprinting ? 1.55f : 1));
        }
        private void Advance(float delta)
        {
            if (sequence == null) return;
            clock += delta;
            int index = Mathf.FloorToInt(clock * sequence.fps);
            CurrentFrame = sequence.loop ? index % sequence.frames.Length : Mathf.Min(index, sequence.frames.Length - 1);
            ApplyFrame(CurrentFrame);
        }
    }
}
