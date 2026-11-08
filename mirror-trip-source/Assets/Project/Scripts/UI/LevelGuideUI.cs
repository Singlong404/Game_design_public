using UnityEngine;
using UnityEngine.UI;
namespace MirrorTrip
{
 public sealed class LevelGuideUI:MonoBehaviour
 {
  public ProductionUI ui; public LevelGuideBook book; public Sprite paper;
  public GameObject panel; public bool IsOpen=>panel&&panel.activeSelf;
  public int Selected{get;private set;}
  public Text heading,steps,pageCount; public Image photograph; public Button entryButton;
  [System.NonSerialized] public Text contextText;
  Button previous,next; bool zoomed; Text zoomLabel; Sprite buttonSprite; Texture2D buttonTexture;
  Font Font=>ui.dialogue.font;
  public bool CanOpen=>ui&&!ui.TitleOpen&&!ui.StoryOpen&&!ui.SettingsOpen&&!ui.session.Paused&&!ui.session.Reading&&!ui.session.Fallen&&!ui.session.Exited&&!ui.session.Loading&&!(ui.journal&&ui.journal.IsOpen)&&!(ui.saveSlots&&ui.saveSlots.IsOpen)&&!(ui.session.finale&&(ui.session.finale.InputBlocked||ui.session.finale.Cinematic));
  void Awake(){if(!ui)ui=GetComponent<ProductionUI>();ui.guide=this;Build();}
  public static RectTransform Rect(string name,Transform parent,Vector2 pos,Vector2 size)
  {var g=new GameObject(name,typeof(RectTransform),typeof(CanvasRenderer));var r=g.GetComponent<RectTransform>();r.SetParent(parent,false);r.anchorMin=r.anchorMax=r.pivot=Vector2.zero;r.anchoredPosition=pos;r.sizeDelta=size;return r;}
  Text Label(string name,Transform parent,Vector2 pos,Vector2 size,string value,int fontSize=26)
  {var r=Rect(name,parent,pos,size);var t=r.gameObject.AddComponent<Text>();t.font=Font;t.fontSize=fontSize;t.color=new Color(.07f,.07f,.07f);t.text=value;t.supportRichText=false;t.raycastTarget=false;t.alignment=TextAnchor.MiddleLeft;return t;}
  Sprite WhiteButton()
  {
   if(buttonSprite)return buttonSprite;
   const int n=64;buttonTexture=new Texture2D(n,n,TextureFormat.RGBA32,false);buttonTexture.name="Guide_WhiteRoundedButton";
   var pixels=new Color[n*n];
   for(int y=0;y<n;y++)for(int x=0;x<n;x++)
   {float dx=Mathf.Max(Mathf.Abs(x-31.5f)-23,0),dy=Mathf.Max(Mathf.Abs(y-31.5f)-23,0);float d=Mathf.Sqrt(dx*dx+dy*dy)-7.5f;float a=Mathf.Clamp01(.5f-d);float edge=Mathf.Clamp01(-d-1.5f);pixels[y*n+x]=new Color(Mathf.Lerp(.08f,1,edge),Mathf.Lerp(.08f,1,edge),Mathf.Lerp(.08f,1,edge),a);}
   buttonTexture.SetPixels(pixels);buttonTexture.Apply();buttonSprite=Sprite.Create(buttonTexture,new Rect(0,0,n,n),new Vector2(.5f,.5f),100,0,SpriteMeshType.FullRect,new Vector4(12,12,12,12));return buttonSprite;
  }
  Button ButtonAt(string name,Transform parent,Vector2 pos,Vector2 size,string value,UnityEngine.Events.UnityAction action)
  {var r=Rect(name,parent,pos,size);var img=r.gameObject.AddComponent<Image>();img.sprite=WhiteButton();img.type=Image.Type.Sliced;var b=r.gameObject.AddComponent<Button>();b.targetGraphic=img;var c=b.colors;c.normalColor=Color.white;c.highlightedColor=new Color(.96f,.96f,.96f);c.pressedColor=new Color(.88f,.88f,.88f);c.disabledColor=new Color(1,1,1,.5f);b.colors=c;var t=Label("Label",r,new Vector2(8,2),size-new Vector2(16,4),value,25);t.alignment=TextAnchor.MiddleCenter;b.onClick.AddListener(action);return b;}
  void Build()
  {
   entryButton=ui.pause;
   var root=(RectTransform)ui.titleScreen.transform.parent;
   panel=Rect("LevelGuide_Panel",root,Vector2.zero,new Vector2(1920,1080)).gameObject;
   panel.AddComponent<Image>().color=new Color(0,0,0,.72f);
   var page=Rect("Handdrawn_Paper",panel.transform,new Vector2(90,70),new Vector2(1740,940));var bg=page.gameObject.AddComponent<Image>();bg.sprite=paper;bg.raycastTarget=false;
   heading=Label("GuideTitle",page,new Vector2(245,829),new Vector2(1100,60),"第一关",37);
   ButtonAt("Close",page,new Vector2(1475,841),new Vector2(165,49),"关闭  Esc",Close);
   var pic=Rect("AnnotatedScreenshot",page,new Vector2(100,230),new Vector2(1060,565));photograph=pic.gameObject.AddComponent<Image>();photograph.preserveAspect=true;
   var zoom=pic.gameObject.AddComponent<Button>();zoom.transition=Selectable.Transition.None;zoom.onClick.AddListener(()=>SetZoom(!zoomed));
   steps=Label("SolutionSteps",page,new Vector2(1190,215),new Vector2(445,585),"",26);steps.alignment=TextAnchor.UpperLeft;steps.lineSpacing=1.04f;
   previous=ButtonAt("PreviousSection",page,new Vector2(110,105),new Vector2(210,49),"上一关",()=>Select(Selected-1));
   pageCount=Label("PageCount",page,new Vector2(345,105),new Vector2(165,49),"",26);pageCount.alignment=TextAnchor.MiddleCenter;
   ButtonAt("CurrentSection",page,new Vector2(545,105),new Vector2(280,49),"定位当前关卡",()=>Select(CurrentPage()));
   zoomLabel=ButtonAt("ZoomDiagram",page,new Vector2(915,105),new Vector2(250,49),"放大图示",()=>SetZoom(!zoomed)).GetComponentInChildren<Text>();
   next=ButtonAt("NextSection",page,new Vector2(1260,105),new Vector2(230,49),"下一关",()=>Select(Selected+1));
   panel.SetActive(false);Select(0);
  }
  public void Open(){if(!CanOpen)return;Select(CurrentPage());panel.SetActive(true);panel.transform.SetAsLastSibling();ui.session.SetMovement(Vector2.zero);ui.session.SetSprint(false);ui.ConsumeModalInput();}
  public void Close(){if(panel)panel.SetActive(false);if(ui)ui.ConsumeModalInput();}
  public void Toggle(){if(IsOpen)Close();else Open();}
  public void Select(int index)
  {if(!book||book.pages.Length==0)return;SetZoom(false);Selected=Mathf.Clamp(index,0,book.pages.Length-1);var p=book.pages[Selected];heading.text=p.title;steps.text=p.steps;photograph.sprite=p.screenshot;photograph.enabled=p.screenshot;pageCount.text=$"{Selected+1:00} / {book.pages.Length:00}";previous.interactable=Selected>0;next.interactable=Selected<book.pages.Length-1;}
  public void SetZoom(bool value)
  {zoomed=value;if(!photograph)return;photograph.rectTransform.anchoredPosition=value?new Vector2(215,180):new Vector2(100,230);photograph.rectTransform.sizeDelta=value?new Vector2(1290,640):new Vector2(1060,565);if(steps)steps.gameObject.SetActive(!value);if(zoomLabel)zoomLabel.text=value?"恢复步骤":"放大图示";}
  public int CurrentPage()
  {
   if(!book||book.pages.Length==0)return 0;var s=ui.session;var actor=s.MirrorActive?s.mirrorPlayer:s.surfacePlayer;float best=float.MaxValue;int selected=0;
   for(int i=0;i<book.pages.Length;i++)
   {var p=book.pages[i];if(p.locations!=null&&p.locations.Length>0){foreach(var r in p.locations){if(r.mirrorSide!=s.MirrorActive)continue;float d=Vector2.SqrMagnitude((Vector2)actor.transform.position-r.center);if(d<best){best=d;selected=i;}}}
    else if(p.mirrorSide==s.MirrorActive){float d=Vector2.SqrMagnitude((Vector2)actor.transform.position-p.center);if(d<best){best=d;selected=i;}}}
   return selected;
  }
  void LateUpdate(){if(IsOpen&&(ui.TitleOpen||ui.SettingsOpen||ui.StoryOpen||ui.session.Reading||ui.session.Fallen||ui.session.Loading||ui.session.Exited||(ui.session.finale&&ui.session.finale.Cinematic)))Close();}
  void OnDestroy(){if(buttonSprite)Destroy(buttonSprite);if(buttonTexture)Destroy(buttonTexture);}
 }
}
