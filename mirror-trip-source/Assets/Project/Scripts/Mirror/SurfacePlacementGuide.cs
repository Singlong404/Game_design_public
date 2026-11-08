using UnityEngine;
namespace MirrorTrip
{
    [ExecuteAlways]
    public sealed class SurfacePlacementGuide : MonoBehaviour
    {
        public MirrorPlacementArea source;
        public SpriteRenderer visual;
        void LateUpdate()
        {
            if(!source||!source.link||!source.link.surfaceWorld||!source.link.mirrorWorld||!source.guide||!visual)return;
            transform.SetPositionAndRotation(source.guide.transform.position+source.link.surfaceWorld.position-source.link.mirrorWorld.position,source.guide.transform.rotation);
            var scale=source.guide.transform.lossyScale;var p=transform.parent?transform.parent.lossyScale:Vector3.one;
            transform.localScale=new Vector3(scale.x/p.x,scale.y/p.y,scale.z/p.z);
            visual.sprite=source.guide.sprite;visual.color=MuchaPalette.Placement;
        }
    }
}
