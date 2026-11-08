using UnityEngine;
namespace MirrorTrip
{
    [System.Serializable]
    public sealed class CollectedNote
    {
        public string id, title;
        public string[] pages;
        public static CollectedNote From(TextInteractable entry) => new CollectedNote {id=entry.entryId,title=entry.title,pages=entry.pages!=null?(string[])entry.pages.Clone():new string[0]};
    }
    public sealed class TextInteractable : MonoBehaviour
    {
        public string entryId, title;
        [TextArea(3, 8)] public string[] pages;
        public bool mirrorSide, isChest;
        public float interactionRadius = 1.5f;
        public SpriteRenderer visual;
        public bool Opened { get; private set; }
        private void Awake()
        {
            if(!isChest||!string.IsNullOrWhiteSpace(entryId))return;
            string path="";for(var t=transform;t;t=t.parent)path="/"+t.name+"["+t.GetSiblingIndex()+"]"+path;
            entryId="NOTE_"+Hash128.Compute(gameObject.scene.path+path);
        }
        public string Prompt => isChest ? (Opened ? "[E / 点击] 重读纸条 · 已收入见闻" : "[E / 点击] 打开宝箱") : "[E] 阅读公告";
        public void RestoreOpened(bool opened){Opened=opened;}
        public void MarkOpened()
        {
            Opened = true;
            if (isChest && visual) visual.color = new Color32(164, 175, 157, 255);
        }
    }
}
