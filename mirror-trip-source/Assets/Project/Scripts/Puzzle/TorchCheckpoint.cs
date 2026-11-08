using UnityEngine;
namespace MirrorTrip
{
    [RequireComponent(typeof(BoxCollider2D))]
    public sealed class TorchCheckpoint : MonoBehaviour
    {
        public string persistentId, displayName="存档点";
        public WorldSession session;
        public Transform respawnPoint, flameRoot;
        public LineRenderer[] flames;
        public SpriteRenderer flameInk;
        public Sprite[] flameFrames;
        public float framesPerSecond=9;
        public int CurrentFlameFrame { get; private set; }
        public bool Lit { get; private set; }
        private BoxCollider2D area;
        private float clock,retryAfter;
        private void Awake(){area=GetComponent<BoxCollider2D>();if(!session)session=FindAnyObjectByType<WorldSession>();ApplyRenderOrder();Restore(false);}
        // Sorting layers outrank sortingOrder: keep both torch and flame behind actors.
        public void ApplyRenderOrder(){foreach(var r in GetComponentsInChildren<Renderer>(true)){r.sortingLayerName="Default";r.sortingOrder=r==flameInk?181:180;}}
        private void Update()
        {
            if(!session)return;
            if(!Lit&&Time.unscaledTime>retryAfter&&session.MirrorActive&&session.CanInteract&&area.bounds.Intersects(session.mirrorPlayer.Bounds))
            {if(!Activate())retryAfter=Time.unscaledTime+3;}
            if(!Lit||!session.MirrorActive||!session.CanInteract)return;
            if(session.presentation&&session.presentation.MotionReduced)return;
            clock+=Time.deltaTime;
            if(flameInk&&flameFrames!=null&&flameFrames.Length>0)
            {
                CurrentFlameFrame=(int)(clock*framesPerSecond)%flameFrames.Length;
                flameInk.sprite=flameFrames[CurrentFlameFrame];
                flameInk.transform.localRotation=Quaternion.Euler(0,0,Mathf.Sin(clock*2.8f)*1.8f);
                return;
            }
            for(int j=0;j<flames.Length;j++)
            {
                var line=flames[j];if(!line)continue;int n=15;line.positionCount=n;
                for(int i=0;i<n;i++)
                {
                    float t=i/(float)(n-1),a=t*Mathf.PI*2;
                    float y=(1-Mathf.Cos(a))*.29f;
                    float x=Mathf.Sin(a)*(.13f+.035f*j)*(1-y*.9f)+Mathf.Sin(clock*3+j+y*4)*y*.13f;
                    line.SetPosition(i,new Vector3(x,y*(1+j*.12f),0));
                }
            }
        }
        public bool Activate()
        {
            if(Lit||!session||!session.saves||!session.MirrorActive||!session.CanInteract)return false;
            return session.saves.Activate(this);
        }
        public void Restore(bool lit)
        {
            Lit=lit;clock=0;CurrentFlameFrame=0;
            if(flameRoot)flameRoot.gameObject.SetActive(lit);
            if(flameInk&&flameFrames!=null&&flameFrames.Length>0){flameInk.sprite=flameFrames[0];flameInk.transform.localRotation=Quaternion.identity;}
        }
    }
}
