using System;
using System.Collections.Generic;
using UnityEngine;
namespace MirrorTrip
{
    public sealed class LevelAudioDirector:MonoBehaviour
    {
        public WorldSession session;
        [Header("Only verified CC0 clips; see CC0_20261107/音频许可清单.json")]
        public AudioClip exploration,uncertainty,wind,stepSurface,stepMirror,throwCard,mechanism,door,glitch,teleport,death,checkpoint,page,chest;
        [Range(0,1)]public float musicGain=.28f,windGain=.18f,effectGain=.55f;
        public float crossfadeSeconds=2.4f;
        public int EffectsPlayed{get;private set;}
        AudioSource a,b,w,fx;float blend,steps;Vector2 last;bool lastMirror,fallen,clip;string screen;
        readonly Dictionary<CardSwitch,Action<bool,bool>> switches=new Dictionary<CardSwitch,Action<bool,bool>>();
        readonly Dictionary<TorchCheckpoint,bool> torches=new Dictionary<TorchCheckpoint,bool>();
        void Start()
        {
            a=Source("Music_MirrorCity",true);b=Source("Music_AnswerLost",true);w=Source("Ambience_BlackSnow",true);fx=Source("SFX_CC0",false);
            a.clip=exploration;b.clip=uncertainty;w.clip=wind;
            foreach(var src in new[]{a,b,w})if(src.clip)src.Play();
            foreach(var sw in session.transform.root.GetComponentsInChildren<CardSwitch>(true)){Action<bool,bool> h=(on,notify)=>{if(on&&notify){Play(mechanism,.6f);Play(door,.25f);}};switches[sw]=h;sw.StateApplied+=h;}
            foreach(var t in session.transform.root.GetComponentsInChildren<TorchCheckpoint>(true))torches[t]=t.Lit;
            session.interactions.EntryDiscovered+=OnDiscovered;
            session.presentation.ScreenChanged+=ScreenChanged;
            last=(session.MirrorActive?session.mirrorPlayer:session.surfacePlayer).transform.position;lastMirror=session.MirrorActive;
        }
        AudioSource Source(string name,bool loop){var g=new GameObject(name);g.transform.SetParent(transform,false);var s=g.AddComponent<AudioSource>();s.playOnAwake=false;s.loop=loop;s.spatialBlend=0;s.volume=0;return s;}
        void OnDestroy(){foreach(var p in switches)if(p.Key)p.Key.StateApplied-=p.Value;if(session){session.interactions.EntryDiscovered-=OnDiscovered;session.presentation.ScreenChanged-=ScreenChanged;}}
        void OnDiscovered(string id){Play(chest,.65f);}
        void ScreenChanged(string value){if(screen!=value&&value!="Death"&&value!="Title")Play(page,.2f);screen=value;}
        public void PlayThrow(){Play(throwCard,.4f);if(session.storyDirector)session.storyDirector.Shot();}
        public void Play(AudioClip c,float gain=1){if(!fx||!c||session.presentation.TitleOpen)return;fx.volume=effectGain*session.presentation.effectsVolume.value;fx.PlayOneShot(c,gain);EffectsPlayed++;}
        void Update()
        {
            if(!a||!session)return;var ui=session.presentation;var f=session.finale;
            bool silent=ui.TitleOpen||session.Loading||session.Exited||(f&&f.EndingCard);
            bool paused=session.Paused||ui.SettingsOpen;
            bool tense=f&&(f.LostContact||f.Recovered&&!f.Cinematic);
            blend=Mathf.MoveTowards(blend,tense?1:0,Time.unscaledDeltaTime/Mathf.Max(.1f,crossfadeSeconds));
            float duck=ui.StoryOpen?.46f:session.storyDirector&&!string.IsNullOrEmpty(session.storyDirector.BottomText)?.7f:1;
            float volume=silent?0:session.Fallen?.08f:paused?.15f:duck;
            float target=musicGain*ui.musicVolume.value*volume;
            a.volume=Mathf.MoveTowards(a.volume,target*(1-blend)*(session.MirrorActive?1:.65f),Time.unscaledDeltaTime*.3f);
            b.volume=Mathf.MoveTowards(b.volume,target*blend,Time.unscaledDeltaTime*.3f);
            bool snow=session.expansion&&(session.expansion.ReachedF&&!session.expansion.ReachedG)||f&&f.AtFinalPoint;
            w.volume=Mathf.MoveTowards(w.volume,!silent&&!session.Fallen&&snow?windGain*ui.effectsVolume.value*(paused?.15f:1):0,Time.unscaledDeltaTime*.35f);
            if(session.Fallen&&!fallen){fx.Stop();Play(death,.65f);}fallen=session.Fallen;
            if(f&&f.ClipPlaying&&!clip)Play(glitch,.7f);clip=f&&f.ClipPlaying;
            var actor=session.MirrorActive?session.mirrorPlayer:session.surfacePlayer;Vector2 pos=actor.transform.position;float distance=Vector2.Distance(pos,last);
            if(lastMirror!=session.MirrorActive){steps=0;Play(teleport,.4f);}
            else if(session.CanInteract&&!session.MovementLocked&&distance<2)
            {if(!session.MirrorActive||actor.Grounded){steps+=distance;if(steps>.85f){steps=0;Play(session.MirrorActive?stepMirror:stepSurface,session.MirrorActive?.3f:.25f);}}}
            else steps=0;
            last=pos;lastMirror=session.MirrorActive;
            foreach(var t in new List<TorchCheckpoint>(torches.Keys)){if(!t)continue;if(t.Lit&&!torches[t])Play(checkpoint,.4f);torches[t]=t.Lit;}
        }
    }
}
