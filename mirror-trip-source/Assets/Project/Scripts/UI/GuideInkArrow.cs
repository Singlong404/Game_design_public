using UnityEngine;
using UnityEngine.UI;
namespace MirrorTrip
{
 // Crisp scalable ink arrow: white under-stroke keeps it readable on both worlds.
 [RequireComponent(typeof(CanvasRenderer))] public sealed class GuideInkArrow:MaskableGraphic
 {
  public Vector2 from,to;public float width=4;
  public void Set(Vector2 a,Vector2 b){from=a;to=b;SetVerticesDirty();}
  protected override void OnPopulateMesh(VertexHelper v){v.Clear();if((to-from).sqrMagnitude<1)return;var d=(to-from).normalized;var n=new Vector2(-d.y,d.x);var end=to-d*17;
   foreach(var w in new[]{width+5,width}){var c=w>width?Color.white:color;Line(v,from,to,w,c);Line(v,to,end+n*11,w,c);Line(v,to,end-n*11,w,c);}
  }
  static void Line(VertexHelper v,Vector2 a,Vector2 b,float w,Color c){var d=(b-a).normalized;var n=new Vector2(-d.y,d.x)*w*.5f;int i=v.currentVertCount;v.AddVert(a-n,c,Vector2.zero);v.AddVert(a+n,c,Vector2.zero);v.AddVert(b+n,c,Vector2.zero);v.AddVert(b-n,c,Vector2.zero);v.AddTriangle(i,i+1,i+2);v.AddTriangle(i,i+2,i+3);}
 }
}
