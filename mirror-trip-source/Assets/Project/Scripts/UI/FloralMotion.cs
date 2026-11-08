using UnityEngine;
namespace MirrorTrip
{
    public sealed class FloralMotion : MonoBehaviour
    {
        public ProductionUI ui;
        public float amplitude=1.3f, phase;
        private Quaternion rest;
        private void Awake(){rest=transform.localRotation;}
        private void Update()
        {
            transform.localRotation=rest*Quaternion.Euler(0,0,ui.MotionReduced?0:Mathf.Sin(Time.unscaledTime*.6f+phase)*amplitude);
        }
    }
}
