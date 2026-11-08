using UnityEngine;
using System.Collections.Generic;
namespace MirrorTrip
{
    [DefaultExecutionOrder(1000)]
    public sealed class MonochromePresentation : MonoBehaviour
    {
        // Also catches state colours on existing mirrors, switches and runtime previews.
        private readonly List<SpriteRenderer> renderers = new List<SpriteRenderer>();
        private float nextScan;
        private WorldSession session;
        private void LateUpdate()
        {
            if (Time.unscaledTime >= nextScan) { GetComponentsInChildren(true, renderers); nextScan=Time.unscaledTime+.2f; }
            foreach (var r in renderers)
            {
                if (!r) continue;
                // Mirror type colours remain readable on both sides of the split screen.
                if(r.GetComponentInParent<SurfaceMirrorProxy>()||r.GetComponentInParent<MechanismMirror>())continue;
                var accent=r.GetComponentInParent<GameplayAccent>();
                if(accent&&(accent.role==GameplayAccent.Role.Mirror||accent.role==GameplayAccent.Role.PlacementRail||accent.role==GameplayAccent.Role.Preview))continue;
                if(!session) session=GetComponentInChildren<WorldSession>();
                bool mirror = session && (r.transform.IsChildOf(session.mirrorPlayer.worldRoot) || r.gameObject.layer == LayerMask.NameToLayer("Card") || r.gameObject.layer == LayerMask.NameToLayer("MirrorSurface"));
                if ((r.GetComponentInParent<GameplayAccent>() || r.GetComponentInParent<MechanismMirror>()) && (!session || mirror==session.MirrorActive)) continue;
                var c = r.color;
                if (Mathf.Abs(c.r-c.g) < .001f && Mathf.Abs(c.g-c.b) < .001f) continue;
                float g = c.grayscale; r.color = new Color(g,g,g,c.a);
            }
        }
    }
}
