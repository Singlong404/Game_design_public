using UnityEngine;
namespace MirrorTrip
{
    // Soft approach to authored world limits. The final clamp guarantees no view beyond limits.
    public sealed class CameraEdgeBuffer : MonoBehaviour
    {
        public Vector2 worldMin=new Vector2(-1,-1),worldMax=new Vector2(29,14);
        public float softZone=1.25f;
        public Vector2 Constrain(Vector2 center,Vector2 halfView)
        {
            return new Vector2(Edge(center.x,worldMin.x+halfView.x,worldMax.x-halfView.x),Edge(center.y,worldMin.y+halfView.y,worldMax.y-halfView.y));
        }
        private float Edge(float v,float min,float max)
        {
            if(min>=max)return (min+max)*.5f;
            float z=Mathf.Min(softZone,(max-min)*.25f);
            if(z<=0)return Mathf.Clamp(v,min,max);
            if(v<min+z){float t=Mathf.Clamp01((v-min)/z);return min+z*t*t*(2-t);}
            if(v>max-z){float t=Mathf.Clamp01((max-v)/z);return max-z*t*t*(2-t);}
            return v;
        }
    }
}
