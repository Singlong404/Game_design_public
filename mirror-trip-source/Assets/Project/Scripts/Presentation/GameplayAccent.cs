using UnityEngine;
namespace MirrorTrip
{
        // Muted accents reserved for gameplay-critical surfaces.
    public sealed class GameplayAccent : MonoBehaviour
    {
        public enum Role{Mirror,PlacementRail,Card,Switch,Preview}
        public Role role;
        public WorldSession session;
        public SpriteRenderer[] visuals;
        public CardSwitch switchTarget;
        public SceneMirror mirrorTarget;
        public static readonly Color Blue=MuchaPalette.Ordinary,Yellow=MuchaPalette.Placement,Red=MuchaPalette.Switch;
        private void LateUpdate()
        {
            bool locked=switchTarget?switchTarget.IsOn:mirrorTarget?mirrorTarget.IsLocked:session&&session.puzzleSwitch.IsOn;
            Color c=role==Role.PlacementRail?Yellow:role==Role.Card?Red:role==Role.Switch?Red:Blue;
            if(locked&&role==Role.Mirror)c=Color.Lerp(Blue,MuchaPalette.Locked,.65f);
            if(locked&&role==Role.Switch)c=new Color32(128,128,128,255);
            if(role==Role.Preview){c=session&&session.placement.DestinationValid?Blue:Red;c.a=.8f;}
            foreach(var r in visuals)if(r)
            {
                r.color=c;
                var outline=r.transform.Find("InkOutline");if(outline)outline.GetComponent<SpriteRenderer>().color=Color.black;
            }
        }
    }
}
