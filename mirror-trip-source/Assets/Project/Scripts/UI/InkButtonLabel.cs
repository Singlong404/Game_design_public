using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
namespace MirrorTrip
{
    public sealed class InkButtonLabel : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public Button button;
        public Text label;
        public bool selectedWhenDisabled;
        private bool hover;
        public void OnPointerEnter(PointerEventData e){hover=true;}
        public void OnPointerExit(PointerEventData e){hover=false;}
        private void OnDisable(){hover=false;}
        private void LateUpdate()
        {
            bool focus=button.interactable&&(hover||(EventSystem.current&&EventSystem.current.currentSelectedGameObject==gameObject));
            label.color=focus||(!button.interactable&&selectedWhenDisabled)?Color.white:button.interactable?new Color(.08f,.08f,.08f):new Color(.5f,.5f,.5f);
        }
    }
}
