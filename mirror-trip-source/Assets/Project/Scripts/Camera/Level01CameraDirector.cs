using System;
using System.Collections.Generic;
using UnityEngine;

namespace MirrorTrip
{
    [Serializable] public sealed class LevelCameraCue
    {
        public string id;
        public LevelProgressPoint point;
        public CardSwitch source;
        public bool fixedView,breathing,tight;
        public float size=8;
        public Vector3 position=new Vector3(0,0,-10);
        public Vector2 sway,playerDrift;
    }
    [Serializable] public sealed class LevelCameraSave
    {
        public string current;
        public string[] visited;
        public Vector2 playerAnchor;
    }
    public sealed class Level01CameraDirector : MonoBehaviour
    {
        public WorldSession session;
        public PlayerFollowCamera follow;
        public LevelCameraCue[] cues=Array.Empty<LevelCameraCue>();
        readonly HashSet<string> visited=new HashSet<string>();
        string current;
        Vector2 playerAnchor;
        public string Current=>current;
        public bool Ending=>visited.Contains("point_h");
        public void Trigger(string id)
        {
            foreach(var cue in cues)if(cue.id==id){visited.Add(id);current=id;playerAnchor=session.mirrorPlayer.transform.position;Apply(cue,false);return;}
        }
        public void Tick()
        {
            if(!session.MirrorActive||!session.CanInteract)return;
            foreach(var cue in cues)
            {
                if(visited.Contains(cue.id))continue;
                if(cue.point?cue.point.Contains(session.mirrorPlayer):cue.source&&cue.source.IsOn)
                {
                    visited.Add(cue.id);
                    // An early switch reached out of sequence must not pull the camera back after the snow/ending.
                    if(cue.source&&(visited.Contains("point_e")||Ending))continue;
                    current=cue.id;playerAnchor=session.mirrorPlayer.transform.position;Apply(cue,false);
                }
            }
        }
        void Apply(LevelCameraCue cue,bool instant)
        {
            if(cue.fixedView)follow.SetScenicView(cue.position,cue.size,playerAnchor,cue.playerDrift,instant);
            else follow.SetFollowView(cue.size,instant);
            follow.SetAtmosphere(cue.sway,9,cue.breathing?4.5f:0,cue.breathing?6:0,cue.tight);
        }
        public LevelCameraSave Capture()=>new LevelCameraSave{current=current,visited=new List<string>(visited).ToArray(),playerAnchor=playerAnchor};
        public void Restore(LevelCameraSave data)
        {
            visited.Clear();current=null;
            if(data==null)return;
            if(data.visited!=null)foreach(var id in data.visited)visited.Add(id);
            current=data.current;playerAnchor=data.playerAnchor;
            foreach(var cue in cues)if(cue.id==current){Apply(cue,true);break;}
        }
    }
}
