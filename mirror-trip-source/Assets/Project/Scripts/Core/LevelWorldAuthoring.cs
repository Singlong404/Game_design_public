using System.Linq;
using System.Collections.Generic;
using UnityEngine;
namespace MirrorTrip {
// Registers newly placed gameplay prefabs without hand-maintaining session arrays.
[DefaultExecutionOrder(-200)]
public sealed class LevelWorldAuthoring : MonoBehaviour {
 public WorldSession session;
 public Transform surfaceWorld, mirrorWorld;
 [Tooltip("Expand the camera/death bounds when terrain is extended.")]
 public bool automaticBounds=true;
 public float editingMargin=30;
 void Awake(){RefreshBindings();}
 [ContextMenu("Refresh gameplay bindings and bounds")]
 public void RefreshBindings(){
  if(!session)return;
  var root=transform;
  Unique(root.GetComponentsInChildren<CardSwitch>(true),x=>x.persistentId,(x,id)=>x.persistentId=id,"L01_SWITCH");
  Unique(root.GetComponentsInChildren<PairedDoor>(true),x=>x.persistentId,(x,id)=>x.persistentId=id,"L01_DOOR");
  Unique(root.GetComponentsInChildren<TorchCheckpoint>(true),x=>x.persistentId,(x,id)=>x.persistentId=id,"L01_TORCH");
  Unique(root.GetComponentsInChildren<ContactNarrative>(true),x=>x.persistentId,(x,id)=>x.persistentId=id,"L01_TEXT");
  Unique(root.GetComponentsInChildren<MechanismMirror>(true),x=>x.persistentId,(x,id)=>x.persistentId=id,"L01_MECHANISM");
  Unique(root.GetComponentsInChildren<SceneMirror>(true),x=>x.mirrorId,(x,id)=>x.mirrorId=id,"L01_PAIR");
  Unique(root.GetComponentsInChildren<PlacementWallLink>(true),x=>x.pairId,(x,id)=>x.pairId=id,"L01_TRACK");
  Unique(root.GetComponentsInChildren<TextInteractable>(true),x=>x.entryId,(x,id)=>x.entryId=id,"L01_NOTE");
  foreach(var entry in root.GetComponentsInChildren<TextInteractable>(true))
  {
   entry.mirrorSide=entry.transform.IsChildOf(mirrorWorld);
   foreach(var view in entry.GetComponentsInChildren<InteractiveInkView>(true)){view.entry=entry;view.mirrorSide=entry.mirrorSide;view.session=session;}
   if(entry.isChest)foreach(var renderer in entry.GetComponentsInChildren<Renderer>(true)){renderer.sortingLayerName="Default";renderer.sortingOrder=Mathf.Min(renderer.sortingOrder,190);}
  }
  session.placement.links=root.GetComponentsInChildren<PlacementWallLink>(true).Where(x=>x.enabled && x.gameObject.activeInHierarchy).ToArray();
  session.placement.sceneMirrors=root.GetComponentsInChildren<SceneMirror>(true).Where(x=>x.enabled && x.gameObject.activeInHierarchy).ToArray();
  session.mechanismMirrors=root.GetComponentsInChildren<MechanismMirror>(true).Where(x=>x.enabled && x.gameObject.activeInHierarchy).ToArray();
  session.interactions.surfaceSwitches=root.GetComponentsInChildren<SurfaceSwitchInteractable>(true);
  session.interactions.entries=root.GetComponentsInChildren<TextInteractable>(true).Where(x=>x.enabled && x.gameObject.activeInHierarchy).ToArray();
  foreach(var x in root.GetComponentsInChildren<CardSwitch>(true))x.session=session;
  foreach(var x in root.GetComponentsInChildren<TorchCheckpoint>(true))x.session=session;
  foreach(var x in root.GetComponentsInChildren<ContactNarrative>(true))x.session=session;
  foreach(var x in session.mechanismMirrors){x.session=session;x.mirrorSide=x.transform.IsChildOf(mirrorWorld);foreach(var t in x.GetComponentsInChildren<Transform>(true))t.gameObject.layer=x.mirrorSide?LayerMask.NameToLayer("MirrorSurface"):LayerMask.NameToLayer("SurfaceWorld");}
  if(!automaticBounds)return;
  Bounds b=new Bounds(session.mirrorPlayer.LocalPosition,Vector3.one);
  foreach(var c in mirrorWorld.GetComponentsInChildren<Collider2D>())if(!c.isTrigger && c.gameObject.layer==LayerMask.NameToLayer("MirrorWorld") && c.bounds.size.sqrMagnitude>0){var v=c.bounds;v.center-=mirrorWorld.position;b.Encapsulate(v);}
  session.surfaceMinimum=(Vector2)b.min-Vector2.one*editingMargin;session.surfaceMaximum=(Vector2)b.max+Vector2.one*editingMargin;session.mirrorFallHeight=b.min.y-editingMargin;
  foreach(var cam in new[]{session.surfaceCamera,session.mirrorCamera}){var edge=cam.GetComponent<CameraEdgeBuffer>();if(edge){edge.worldMin=session.surfaceMinimum;edge.worldMax=session.surfaceMaximum;}}
 }
 static void Unique<T>(T[] items,System.Func<T,string> read,System.Action<T,string> write,string prefix){var used=new HashSet<string>();for(int i=0;i<items.Length;i++){string id=read(items[i]);if(string.IsNullOrWhiteSpace(id)||!used.Add(id)){id=prefix+"_"+i;while(!used.Add(id))id+="_new";write(items[i],id);}}}
}
}
