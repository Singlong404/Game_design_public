using UnityEngine;
using UnityEngine.UI;
namespace MirrorTrip
{
    public sealed class SprintStatusUI : MonoBehaviour
    {
        public ProductionUI ui;
        public Text label;
        private void Update()
        {
            if(!ui||!label)return;
            var motor=ui.session.MirrorActive?ui.session.mirrorPlayer:ui.session.surfacePlayer;
            label.text=motor.SprintRecovery>0?$"Ctrl 冲刺 · 冷却 {motor.SprintRecovery:F1}s":$"Ctrl 冲刺 · {motor.SprintRemaining:F1}s";
        }
    }
}
