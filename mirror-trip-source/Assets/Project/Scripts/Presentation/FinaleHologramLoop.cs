using UnityEngine;
namespace MirrorTrip
{
    public sealed class FinaleHologramLoop:MonoBehaviour
    {
        public WorldSession session;
        public SpriteRenderer view;
        public Texture2D sheet;
        public Material inkMaterial;
        public float cycleSeconds=9,localWidth=15.366f;
        public bool oneShotReach=true;
        public float reachSeconds=16;
        public bool Playing;
        Sprite original;Material originalMaterial;Sprite[] frames;Material material;float clock;MaterialPropertyBlock props;
        void Awake()
        {
            original=view.sprite;originalMaterial=view.sharedMaterial;frames=new Sprite[8];
            for(int i=0;i<8;i++)frames[i]=Sprite.Create(sheet,new Rect(i%4*sheet.width/4,(1-i/4)*sheet.height/2,sheet.width/4,sheet.height/2),new Vector2(.5f,.5f),(sheet.width/4f)/localWidth,0,SpriteMeshType.FullRect);
            material=new Material(inkMaterial);props=new MaterialPropertyBlock();
            view.enabled=Playing;
        }
        public void SetPlaying(bool value){Playing=value;view.enabled=value;if(!value){view.sprite=original;view.sharedMaterial=originalMaterial;view.SetPropertyBlock(null);clock=0;}}
        static Vector4 UV(int i)=>new Vector4(i%4*.25f,(1-i/4)*.5f,.25f,.5f);
        void LateUpdate()
        {
            view.enabled=Playing;if(!Playing)return;
            if(!session.Paused&&!session.presentation.BlocksGameplay)clock+=Time.unscaledDeltaTime;
            float t=(oneShotReach?Mathf.Clamp01(clock/Mathf.Max(.1f,reachSeconds)):Mathf.PingPong(clock/cycleSeconds*2,1))*7;int a=Mathf.FloorToInt(t),b=Mathf.Min(a+1,7);
            view.sprite=frames[a];view.sharedMaterial=material;
            props.SetVector("_RectA",UV(a));props.SetVector("_RectB",UV(b));props.SetFloat("_Blend",Mathf.SmoothStep(0,1,t-a));props.SetFloat("_Clock",clock);
            props.SetFloat("_Strength",session.presentation.MotionReduced?.22f:.85f);view.SetPropertyBlock(props);
        }
        void OnDestroy(){if(frames!=null)foreach(var f in frames)if(f)Destroy(f);if(material)Destroy(material);}
    }
}
