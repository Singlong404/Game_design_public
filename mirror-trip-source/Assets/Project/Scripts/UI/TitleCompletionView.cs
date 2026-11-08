using UnityEngine;
using UnityEngine.UI;
namespace MirrorTrip
{
    public sealed class TitleCompletionView:MonoBehaviour
    {
        public const string CompletionKey="MirrorTrip.Level01.Completed";
        public static bool? TestCompletion;
        public Image garden;
        public Sprite originalGarden,completedGarden;
        public static bool Completed=>TestCompletion??(PlayerPrefs.GetInt(CompletionKey,0)==1);
        public static void MarkCompleted(){if(TestCompletion.HasValue)TestCompletion=true;else{PlayerPrefs.SetInt(CompletionKey,1);PlayerPrefs.Save();}}
        void OnEnable(){Refresh();}
        public void Refresh(){if(garden)garden.sprite=Completed&&completedGarden?completedGarden:originalGarden;}
    }
}
