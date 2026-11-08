using UnityEngine;
namespace MirrorTrip
{
    // A manipulable surface-world representation, not a second independent portal network.
    [DefaultExecutionOrder(550)]
    public sealed class SurfaceMirrorProxy : MonoBehaviour
    {
        public GameObject response;
        public SpriteRenderer[] visuals;
        public Collider2D contact;
        public bool movable;
        void LateUpdate()
        {
            if(!response)return;
            var mechanism=response.GetComponent<MechanismMirror>();
            Color color=mechanism?MuchaPalette.State(mechanism.kind,mechanism.Enabled,mechanism.Used,mechanism.Stored):MuchaPalette.Ordinary;
            bool visible=response.activeInHierarchy&&(!mechanism||!mechanism.hideWhenDisabled||mechanism.Enabled);
            foreach(var r in visuals)if(r){r.enabled=visible;r.color=r.name=="InkOutline"?Color.black:color;}
            if(contact)contact.enabled=visible;
        }
    }
}
