using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MirrorTrip
{
    // Translate at the presentation boundary. Narrative assets and save identities stay Chinese.
    [DefaultExecutionOrder(-200)]
    public sealed class GameLocalization : MonoBehaviour
    {
        [Serializable] public class Entry { public string zh, en; }
        [Serializable] public class Catalog { public Entry[] entries; }
        public TextAsset catalog;
        public Font englishFont;
        public Font chineseFont;
        public Button languageButton;
        public Text languageLabel;
        public const string PreferenceKey = "MirrorTrip.Language";
        public static int? TestLanguage;
        public bool English { get; private set; }
        public static GameLocalization Instance { get; private set; }
        readonly Dictionary<string,string> exact = new Dictionary<string,string>();
        readonly Dictionary<string,string> cache = new Dictionary<string,string>();
        readonly HashSet<string> missing = new HashSet<string>();
        readonly List<Text> texts = new List<Text>();
        readonly List<GameObject> roots = new List<GameObject>();
        Entry[] fragments;
        bool refreshing;
        public IEnumerable<string> Missing => missing;
        public int EntryCount => exact.Count;

        void Awake()
        {
            Instance=this; LoadCatalog();
            English=(TestLanguage ?? PlayerPrefs.GetInt(PreferenceKey,0))==1;
            if(languageButton)languageButton.onClick.AddListener(ToggleLanguage);
        }
        void OnEnable(){Canvas.preWillRenderCanvases+=RefreshAll;}
        void OnDisable(){Canvas.preWillRenderCanvases-=RefreshAll;}
        void OnDestroy(){if(Instance==this)Instance=null;}
        public void LoadCatalog()
        {
            exact.Clear();cache.Clear();missing.Clear();
            if(catalog)foreach(var e in JsonUtility.FromJson<Catalog>(catalog.text).entries)
                if(!string.IsNullOrEmpty(e.zh)&&e.en!=null)exact[Normalize(e.zh)]=e.en;
            fragments=exact.Select(p=>new Entry{zh=p.Key,en=p.Value}).OrderByDescending(e=>e.zh.Length).ToArray();
        }
        public void ToggleLanguage()
        {
            var ui=GetComponent<WorldSession>()?.presentation;
            if(ui)ui.ConsumeModalInput();
            SetEnglish(!English,true);
        }
        public void SetEnglish(bool value,bool persist=false)
        {
            English=value;
            if(persist&&!TestLanguage.HasValue){PlayerPrefs.SetInt(PreferenceKey,value?1:0);PlayerPrefs.Save();}
            RefreshAll();
        }
        static string Normalize(string s)=>s.Replace("\r\n","\n").Replace('\r','\n');
        public static bool HasChinese(string s){foreach(var c in s)if(c>='\u4e00'&&c<='\u9fff')return true;return false;}
        public string Translate(string source)
        {
            if(string.IsNullOrEmpty(source))return source;
            source=Normalize(source);
            if(exact.TryGetValue(source,out var translated))return translated;
            if(cache.TryGetValue(source,out translated))return translated;
            if(fragments==null)LoadCatalog();
            translated=source;
            // Numbers and timestamps are runtime data; keep them outside authored translation keys.
            translated=Regex.Replace(translated,@"已收集 (\d+) 张纸条",m=>m.Groups[1].Value+" notes collected");
            translated=Regex.Replace(translated,@"第 (\d+) 次存档",m=>"Save #"+m.Groups[1].Value);
            translated=Regex.Replace(translated,@"已保存至存档 (\d+)：",m=>"Saved to slot "+m.Groups[1].Value+": ");
            foreach(var e in fragments)if(HasChinese(e.zh)&&translated.Contains(e.zh))translated=translated.Replace(e.zh,e.en);
            translated=translated.Replace('：',':').Replace('　',' ').Replace('｜','|').Replace('／','/');
            if(HasChinese(translated)&&missing.Add(source))Debug.LogWarning("[Localization] Missing English: "+source);
            cache[source]=translated;return translated;
        }
        public void RefreshAll()
        {
            if(refreshing||!catalog||!englishFont)return;
            refreshing=true;
            try
            {
                if(fragments==null)LoadCatalog();
                // Include inactive templates so their serialized binding is copied into cloned rows.
                roots.Clear();gameObject.scene.GetRootGameObjects(roots);
                foreach(var root in roots)
                {
                    texts.Clear();root.GetComponentsInChildren<Text>(true,texts);
                    foreach(var t in texts)
                    {
                        var binding=t.GetComponent<LocalizedTextBinding>();
                        if(!binding)binding=t.gameObject.AddComponent<LocalizedTextBinding>();
                        binding.Render(t,this);
                    }
                }
                if(languageLabel)languageLabel.text=English?"Language: English":"语言：中文 / English";
            }
            finally{refreshing=false;}
        }
    }
}
