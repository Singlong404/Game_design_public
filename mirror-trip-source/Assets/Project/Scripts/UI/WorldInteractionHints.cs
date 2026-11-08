using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
namespace MirrorTrip
{
    public sealed class WorldInteractionHints:MonoBehaviour
    {
        public WorldSession session;
        public Font handFont;
        sealed class Hint {public RectTransform rect;public Text text;public SceneMirror pair;}
        readonly List<Hint> markers=new List<Hint>();Hint key;Canvas canvas;RectTransform canvasRect;
        public bool KeyVisible=>key!=null&&key.rect.gameObject.activeSelf;
        public int VisibleMovableCount {get;private set;}
        void Start()
        {
            var g=new GameObject("InteractionHints_手绘提示",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler));g.transform.SetParent(transform,false);
            canvas=g.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=110;canvasRect=g.GetComponent<RectTransform>();
            var scaler=g.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1920,1080);scaler.matchWidthOrHeight=.5f;
            key=Create("E",new Vector2(46,48));
            foreach(var p in session.placement.sceneMirrors)if(p&&p.movable){var h=Create("可搬动",new Vector2(98,32));h.pair=p;markers.Add(h);}
        }
        Hint Create(string label,Vector2 size)
        {
            var g=new GameObject(label,typeof(RectTransform),typeof(InkHintFrame));g.transform.SetParent(canvas.transform,false);var r=g.GetComponent<RectTransform>();r.sizeDelta=size;
            var ink=g.GetComponent<InkHintFrame>();ink.raycastTarget=false;ink.color=new Color(.12f,.12f,.12f,.95f);
            var child=new GameObject("Label",typeof(RectTransform),typeof(Text));child.transform.SetParent(g.transform,false);var cr=child.GetComponent<RectTransform>();cr.anchorMin=Vector2.zero;cr.anchorMax=Vector2.one;cr.offsetMin=cr.offsetMax=Vector2.zero;
            var t=child.GetComponent<Text>();t.text=label;t.font=handFont;t.alignment=TextAnchor.MiddleCenter;t.fontSize=label=="E"?32:21;t.color=new Color(.08f,.08f,.08f);t.raycastTarget=false;
            return new Hint{rect=r,text=t};
        }
        void LateUpdate()
        {
            if(!canvas)return;
            bool show=session.CanInteract&&!session.MovementLocked&&!(session.finale&&session.finale.Cinematic);
            canvas.enabled=show;VisibleMovableCount=0;if(!show){key.rect.gameObject.SetActive(false);return;}
            var target=session.interactions.HotkeyTarget;Position(key,target,16);
            foreach(var h in markers)
            {
                var p=h.pair;bool valid=p&&p.Available&&!p.IsLocked&&session.placement.Carried!=p;
                Transform t=valid?(session.MirrorActive?p.response:p.surface).transform:null;
                if(Position(h,t,target==t?75:20))VisibleMovableCount++;
            }
        }
        bool Position(Hint h,Transform target,float pixels)
        {
            h.rect.gameObject.SetActive(false);if(!target||!target.gameObject.activeInHierarchy)return false;
            var camera=session.MirrorActive?session.mirrorCamera:session.surfaceCamera;
            Vector3 anchor=target.position;var rs=target.GetComponentsInChildren<SpriteRenderer>();
            bool found=false;Bounds b=default;foreach(var r in rs)if(r.enabled){if(!found){b=r.bounds;found=true;}else b.Encapsulate(r.bounds);}
            if(found)anchor=new Vector3(b.center.x,b.max.y,0);
            var uv=camera.WorldToViewportPoint(anchor);if(uv.z<0||uv.x<.05f||uv.x>.95f||uv.y<.04f||uv.y>.92f)return false;
            var screen=(Vector2)camera.WorldToScreenPoint(anchor)+Vector2.up*pixels*canvas.scaleFactor;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect,screen,null,out var local);h.rect.anchoredPosition=local;h.rect.gameObject.SetActive(true);return true;
        }
    }
    // A slightly irregular paper keycap, drawn as UI geometry with the project's hand font.
    public sealed class InkHintFrame:MaskableGraphic
    {
        protected override void OnPopulateMesh(VertexHelper v)
        {
            v.Clear();var r=rectTransform.rect;Vector2[] p={new Vector2(r.xMin+3,r.yMin+2),new Vector2(r.xMax-2,r.yMin),new Vector2(r.xMax,r.yMax-3),new Vector2(r.xMin,r.yMax)};
            int start=v.currentVertCount;foreach(var a in p)v.AddVert(a,new Color(.97f,.96f,.93f,.96f),Vector2.zero);v.AddTriangle(start,start+1,start+2);v.AddTriangle(start,start+2,start+3);
            for(int i=0;i<4;i++)Line(v,p[i],p[(i+1)%4],1.4f);
            Line(v,p[0]+new Vector2(4,3),p[1]+new Vector2(-5,3),.6f);
        }
        void Line(VertexHelper v,Vector2 a,Vector2 b,float w){var d=(b-a).normalized;var n=new Vector2(-d.y,d.x)*w*.5f;int i=v.currentVertCount;v.AddVert(a-n,color,Vector2.zero);v.AddVert(a+n,color,Vector2.zero);v.AddVert(b+n,color,Vector2.zero);v.AddVert(b-n,color,Vector2.zero);v.AddTriangle(i,i+1,i+2);v.AddTriangle(i,i+2,i+3);}
    }
}
