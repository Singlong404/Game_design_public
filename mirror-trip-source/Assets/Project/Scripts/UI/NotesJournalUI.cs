using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace MirrorTrip
{
    // Collected note IDs live in InteractionSystem/JourneySnapshot, not a second save file.
    public sealed class NotesJournalUI : MonoBehaviour
    {
        public ProductionUI ui;
        public GameObject panel, collectionRoot, readerRoot;
        public RectTransform paper, rowsRoot;
        public Button entryButton, closeButton, previousButton, nextButton, rowTemplate;
        public Text entryLabel, title, body, pageLabel, collectionCount, emptyLabel, status;
        public ScrollRect bodyScroll, collectionScroll;
        public bool IsOpen => panel && panel.activeSelf;
        public bool IsCollection { get; private set; }
        public CollectedNote Selected { get; private set; }
        public int PageIndex { get; private set; }
        public int PageCount => Selected!=null && Selected.pages!=null ? Mathf.Max(1,Selected.pages.Length) : 0;
        private readonly List<Button> rows=new List<Button>();
        private string collectionSignature;

        private void Awake()
        {
            if(!ui)ui=GetComponentInParent<ProductionUI>();ui.journal=this;
            entryButton.onClick.AddListener(OpenCollection);closeButton.onClick.AddListener(Close);
            previousButton.onClick.AddListener(Previous);nextButton.onClick.AddListener(Next);
            rowTemplate.gameObject.SetActive(false);panel.SetActive(false);
        }
        public CollectedNote[] CollectedEntries()
        {
            return ui.session.interactions.CollectedNotes.ToArray();
        }
        public void OpenNote(CollectedNote note)
        {
            if(note==null)return;
            IsCollection=false;collectionRoot.SetActive(false);paper.anchoredPosition=Vector2.zero;
            panel.SetActive(true);panel.transform.SetAsLastSibling();SelectNote(note);ui.ConsumeModalInput();ui.Refresh();
        }
        public void OpenCollection()
        {
            if(ui.TitleOpen||ui.SettingsOpen||ui.StoryOpen||ui.session.Reading||ui.session.Fallen||ui.session.Exited||ui.session.Loading||ui.session.Paused)return;
            IsCollection=true;collectionRoot.SetActive(true);paper.anchoredPosition=new Vector2(220,0);
            panel.SetActive(true);panel.transform.SetAsLastSibling();RebuildRows();
            var notes=CollectedEntries();SelectNote(notes.FirstOrDefault());ui.ConsumeModalInput();ui.Refresh();
        }
        public void SelectNote(CollectedNote note)
        {
            Selected=note;PageIndex=0;readerRoot.SetActive(note!=null);RefreshPage();
            if(bodyScroll)bodyScroll.verticalNormalizedPosition=1;
            foreach(var row in rows)row.interactable=note==null||row.name!="Note_"+note.id;
            ui.ConsumeModalInput();
        }
        public void Close()
        {
            if(panel)panel.SetActive(false);Selected=null;PageIndex=0;
            if(ui){ui.ConsumeModalInput();ui.Refresh();}
        }
        public void Previous(){if(PageIndex<=0)return;PageIndex--;RefreshPage();ui.ConsumeModalInput();}
        public void Next(){if(PageIndex+1>=PageCount)return;PageIndex++;RefreshPage();ui.ConsumeModalInput();}
        public void AdvanceOrClose(){if(PageIndex+1<PageCount)Next();else Close();}
        private void RebuildRows()
        {
            foreach(var row in rows)Destroy(row.gameObject);rows.Clear();var notes=CollectedEntries();
            collectionSignature=string.Join("|",notes.Select(e=>e.id));
            for(int i=0;i<notes.Length;i++)
            {
                var note=notes[i];var row=Instantiate(rowTemplate,rowsRoot);row.gameObject.SetActive(true);row.name="Note_"+note.id;
                var rect=(RectTransform)row.transform;rect.anchorMin=rect.anchorMax=new Vector2(.5f,1);rect.pivot=new Vector2(.5f,1);rect.anchoredPosition=new Vector2(0,-i*84);rect.sizeDelta=new Vector2(330,70);
                row.GetComponentInChildren<Text>().text=$"{i+1:00}  {NoteTitle(note)}";
                row.onClick.AddListener(()=>SelectNote(note));rows.Add(row);
            }
            rowsRoot.sizeDelta=new Vector2(rowsRoot.sizeDelta.x,Mathf.Max(650,notes.Length*84));
            collectionCount.text=$"已收集 {notes.Length} 张纸条";emptyLabel.gameObject.SetActive(notes.Length==0);
            if(collectionScroll)collectionScroll.verticalNormalizedPosition=1;
        }
        private static string NoteTitle(CollectedNote note)=>string.IsNullOrWhiteSpace(note.title)?"无题纸条":note.title;
        private void RefreshPage()
        {
            if(Selected==null)return;
            title.text=NoteTitle(Selected);body.text=Selected.pages!=null&&Selected.pages.Length>0?Selected.pages[PageIndex]:"纸上没有留下文字。";
            body.fontSize=Mathf.RoundToInt(27*(ui.textSize?ui.textSize.value:1));
            pageLabel.text=$"{PageIndex+1} / {PageCount}";status.text="已收入见闻 · Esc 收起";
            previousButton.interactable=PageIndex>0;nextButton.interactable=PageIndex+1<PageCount;
            Canvas.ForceUpdateCanvases();if(bodyScroll)bodyScroll.verticalNormalizedPosition=1;
        }
        private void LateUpdate()
        {
            if(!ui)return;
            int count=ui.session.interactions.CollectedNotes.Count;entryLabel.text=count>0?$"见闻  {count:00}":"见闻";
            entryButton.interactable=!ui.TitleOpen&&!ui.SettingsOpen&&!ui.StoryOpen&&!ui.session.Reading&&!ui.session.Fallen&&!ui.session.Exited&&!ui.session.Loading&&!ui.session.Paused;
            if(IsOpen&&IsCollection)
            {
                var notes=CollectedEntries();string signature=string.Join("|",notes.Select(e=>e.id));
                if(signature!=collectionSignature){string selected=Selected!=null?Selected.id:null;RebuildRows();SelectNote(notes.FirstOrDefault(e=>e.id==selected)??notes.FirstOrDefault());}
            }
        }
    }
}
