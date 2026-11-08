using System;
using UnityEngine;
using UnityEngine.UI;
namespace MirrorTrip
{
    public sealed class JourneySlotsUI : MonoBehaviour
    {
        public ProductionUI ui;
        public GameObject panel;
        public Button[] slots;
        public Text[] labels;
        public Text footer;
        public Button close,settingsEntry,deathEntry;
        public bool IsOpen => panel&&panel.activeSelf;
        private void Awake()
        {
            close.onClick.AddListener(Close);settingsEntry.onClick.AddListener(Open);deathEntry.onClick.AddListener(Open);
            for(int i=0;i<slots.Length;i++){int index=i;slots[i].onClick.AddListener(()=>Load(index));}
            panel.SetActive(false);
        }
        public void Open(){panel.SetActive(true);panel.transform.SetAsLastSibling();ui.session.SetMovement(Vector2.zero);Refresh();}
        public void Close(){panel.SetActive(false);ui.session.SetMovement(Vector2.zero);}
        public void Load(int index){if(ui.session.saves.LoadSlot(index))Close();else Refresh();}
        private void Update(){if(IsOpen)Refresh();}
        public void Refresh()
        {
            var save=ui.session.saves;if(!save)return;int latest=save.LatestSlot();
            for(int i=0;i<3;i++)
            {
                var data=save.Archive.slots[i];bool valid=save.ValidateSnapshot(data,out _);slots[i].interactable=valid;
                string time=data!=null&&DateTime.TryParse(data.savedUtc,out var date)?date.ToLocalTime().ToString("MM/dd HH:mm"):"";
                labels[i].text=data==null?$"0{i+1}    空存档\n          尚无保存记录":$"0{i+1}    {save.CheckpointLabel(data)}  {(latest==i?"· 最近":"")}\n          {time}    第 {data.sequence} 次存档"+(valid?"":"  · 版本不兼容");
            }
            footer.text=string.IsNullOrEmpty(save.LastError)?"接触里世界存档点自动保存 · 存档 1 → 2 → 3 循环覆盖\n每次游戏每个存档点仅保存一次；点击存档条读取存档。":save.LastError;
        }
    }
}
