using System;
using UnityEngine;
namespace MirrorTrip
{
 [Serializable] public struct GuideLocation { public bool mirrorSide; public Vector2 center; }
 [Serializable] public sealed class GuidePage
 {
  public string id,title,steps;
  public bool mirrorSide=true;
  public Vector2 center;
  public float size=12;
  public string[] targets=Array.Empty<string>();
  public Vector2[] badges=Array.Empty<Vector2>();
  public string[] activate=Array.Empty<string>();
  public Sprite screenshot;
  public GuideLocation[] locations=Array.Empty<GuideLocation>();
 }
 [CreateAssetMenu(menuName="Mirror Trip/Guide Book")]
 public sealed class LevelGuideBook:ScriptableObject { public GuidePage[] pages=Array.Empty<GuidePage>(); }
}
