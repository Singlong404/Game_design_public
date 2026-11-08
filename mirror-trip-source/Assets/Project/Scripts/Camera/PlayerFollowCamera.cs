using UnityEngine;
namespace MirrorTrip
{
    [RequireComponent(typeof(Camera),typeof(CameraEdgeBuffer))]
    [DefaultExecutionOrder(50)]
    public sealed class PlayerFollowCamera : MonoBehaviour
    {
        public WorldPlayerMotor target;
        public WorldSession session;
        public float halfHeight=3.6f,followTime=.16f;
        public Vector2 deadZone=new Vector2(.40f,.28f);
        public Vector2 framingOffset=new Vector2(0,.65f);
        private Camera view;
        private CameraEdgeBuffer edges;
        private Vector2 center,velocity;
        private bool initialized;
        private bool scenic, smoothSize;
        private Vector3 scenicAnchor;
        private Vector2 scenicPlayerAnchor, maximumDrift;
        private float sizeVelocity;
        private float motionClock;
        private bool tightTracking, breathing;
        private Vector2 sway;
        private float swayPeriod=10,minimumSize,maximumSize;
        public bool Scenic=>scenic;
        public void SetAtmosphere(Vector2 amplitude,float period=10,float minSize=0,float maxSize=0,bool tight=false)
        {sway=amplitude;swayPeriod=Mathf.Max(4,period);minimumSize=minSize;maximumSize=maxSize;breathing=minSize>0&&maxSize>minSize;tightTracking=tight;motionClock=0;}
        public void SetScenicView(Vector3 anchor,float size,Vector2 playerAnchor,Vector2 drift,bool instant=false)
        {
            SetAtmosphere(Vector2.zero);scenic=true;scenicAnchor=anchor;scenicPlayerAnchor=playerAnchor;maximumDrift=drift;halfHeight=size;smoothSize=!instant;velocity=Vector2.zero;
            if(instant){transform.position=anchor;if(view)view.orthographicSize=size;}initialized=true;
        }
        public void SetFollowView(float size,bool instant=false){SetAtmosphere(Vector2.zero);scenic=false;halfHeight=size;smoothSize=!instant;center=(Vector2)(transform.position-target.worldRoot.position);velocity=Vector2.zero;initialized=!instant;}
        public Vector2 LocalCenter=>center;
        private void Awake(){view=GetComponent<Camera>();edges=GetComponent<CameraEdgeBuffer>();}
        public void SnapToTarget(){initialized=false;}
        private void LateUpdate()
        {
            if(!target)return;
            float aspect=Mathf.Max(.3f,view.pixelWidth/(float)Mathf.Max(1,view.pixelHeight));
            bool frozen=session&&(session.Paused||session.Reading||session.Fallen||session.Exited||session.PresentationBlocked);
            if(!frozen)motionClock+=Time.unscaledDeltaTime;
            float requestedSize=breathing?Mathf.Lerp(minimumSize,maximumSize,.5f+.5f*Mathf.Sin(motionClock*Mathf.PI*2/18f)):halfHeight;
            if(!frozen)view.orthographicSize=smoothSize||breathing?Mathf.SmoothDamp(view.orthographicSize,requestedSize,ref sizeVelocity,breathing?1.8f:.45f,Mathf.Infinity,Time.unscaledDeltaTime):requestedSize;
            Vector2 ambient=new Vector2(Mathf.Sin(motionClock*Mathf.PI*2/swayPeriod)*sway.x,Mathf.Sin(motionClock*Mathf.PI*2/(swayPeriod*1.31f))*sway.y);
            if(scenic)
            {
                if(!frozen)
                {
                    var delta=((Vector2)target.transform.position-scenicPlayerAnchor)*.08f;
                    var desiredWorld=(Vector2)scenicAnchor+new Vector2(Mathf.Clamp(delta.x,-maximumDrift.x,maximumDrift.x),Mathf.Clamp(delta.y,-maximumDrift.y,maximumDrift.y))+ambient;
                    var p=Vector2.SmoothDamp(transform.position,desiredWorld,ref velocity,.8f,Mathf.Infinity,Time.unscaledDeltaTime);
                    transform.position=new Vector3(p.x,p.y,scenicAnchor.z);
                }
                return;
            }
            var desired=target.LocalPosition+(tightTracking?new Vector2(0,Mathf.Clamp(target.Velocity.y*.055f,-.65f,.12f)):framingOffset)+ambient;
            var half=new Vector2(view.orthographicSize*aspect,view.orthographicSize);
            if(!initialized){center=edges.Constrain(desired,half);initialized=true;velocity=Vector2.zero;}
            if(!frozen)
            {
                var delta=desired-center;
                var zone=tightTracking?Vector2.zero:deadZone;
                var goal=center+new Vector2(Mathf.Sign(delta.x)*Mathf.Max(0,Mathf.Abs(delta.x)-zone.x),Mathf.Sign(delta.y)*Mathf.Max(0,Mathf.Abs(delta.y)-zone.y));
                goal=edges.Constrain(goal,half);
                center=Vector2.SmoothDamp(center,goal,ref velocity,tightTracking?.035f:followTime,Mathf.Infinity,Time.unscaledDeltaTime);
                if(tightTracking)center=goal+Vector2.ClampMagnitude(center-goal,.25f);
                center=new Vector2(Mathf.Clamp(center.x,edges.worldMin.x+half.x,edges.worldMax.x-half.x),Mathf.Clamp(center.y,edges.worldMin.y+half.y,edges.worldMax.y-half.y));
            }
            transform.position=target.worldRoot.position+new Vector3(center.x,center.y,-10);
        }
    }
}
