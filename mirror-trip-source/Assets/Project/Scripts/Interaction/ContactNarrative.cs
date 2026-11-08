using UnityEngine;
namespace MirrorTrip
{
    [RequireComponent(typeof(BoxCollider2D))]
    public sealed class ContactNarrative : MonoBehaviour
    {
        public string persistentId, speaker="旅人";
        [Header("对话正文（单独一行 --- 表示分页）")]
        [TextArea(5,16)]public string text;
        [HideInInspector]public string[] pages; // Compatibility with existing scene data.
        public bool mirrorSide=true, oncePerJourney=true, female=true;
        public int expression;
        public bool Visited { get; private set; }
        public WorldSession session;
        public CardSwitch requiredSwitch;
        public bool triggerOnSwitch;
        public StorySequence sequence;
        [Tooltip("Small guidance that does not stop play or open a portrait panel.")]
        public bool guidanceOnly,waitForGround=true;
        public float guidanceSeconds=6;
        private bool pendingSwitch,pendingContact;
        private BoxCollider2D area;
        private bool inside;
        public string[] DialoguePages
        {
            get
            {
                if(string.IsNullOrWhiteSpace(text))return pages;
                return System.Text.RegularExpressions.Regex.Split(text.Trim(),@"(?m)^\s*---\s*$");
            }
        }
        private void OnDestroy(){if(requiredSwitch)requiredSwitch.StateApplied-=SwitchApplied;}
        private void SwitchApplied(bool on,bool notify){if(triggerOnSwitch&&on&&notify&&!Visited)pendingSwitch=true;if(!on)pendingSwitch=false;}
        private void Awake(){if(requiredSwitch)requiredSwitch.StateApplied+=SwitchApplied;area=GetComponent<BoxCollider2D>();if(!session)session=FindAnyObjectByType<WorldSession>();}
        private void Update()
        {
            if(!session||!session.CanInteract||session.MirrorActive!=mirrorSide)return;
            if(session.finale&&session.finale.AtFinalPoint)return;
            if(requiredSwitch&&!requiredSwitch.IsOn)return;
            bool touch=area.bounds.Intersects((mirrorSide?session.mirrorPlayer:session.surfacePlayer).Bounds);
            if(!triggerOnSwitch&&touch&&!inside)pendingContact=true;
            if((pendingSwitch||pendingContact)&&(!Visited||!oncePerJourney))
            {
                if(waitForGround&&mirrorSide&&!session.mirrorPlayer.Grounded)return;
                var content=DialoguePages;
                if(sequence&&sequence.lines.Length>0){Visited=true;pendingSwitch=pendingContact=false;session.presentation.ShowSequence(sequence);}
                else if(content!=null&&content.Length>0){Visited=true;pendingSwitch=pendingContact=false;if(guidanceOnly)session.ShowMessage(content[0],guidanceSeconds);else session.presentation.ShowStory(speaker,content,female,expression);}
            }
            inside=touch;
        }
        public void Restore(bool visited){Visited=visited;inside=false;pendingSwitch=pendingContact=false;}
        private void OnDrawGizmos()
        {
            if(Application.isPlaying)return;
            var b=GetComponent<BoxCollider2D>();if(!b)return;
            var previous=Gizmos.matrix;Gizmos.matrix=transform.localToWorldMatrix;
            Gizmos.color=MuchaPalette.Portal;Gizmos.DrawCube(b.offset,new Vector3(b.size.x,b.size.y,.02f));
            Gizmos.matrix=previous;
        }
    }
}
