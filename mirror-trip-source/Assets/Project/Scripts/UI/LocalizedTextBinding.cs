using UnityEngine;
using UnityEngine.UI;
namespace MirrorTrip
{
    // Public hidden fields intentionally survive Instantiate(template), including original Chinese.
    [DisallowMultipleComponent] public sealed class LocalizedTextBinding:MonoBehaviour
    {
        [HideInInspector] public string source,lastRendered;
        [HideInInspector] public Font originalFont;
        [HideInInspector] public bool initialized,originalBestFit;
        [HideInInspector] public int originalMin,originalMax;
        public void Render(Text t,GameLocalization language)
        {
            if(!initialized){source=t.text;originalFont=t.font==language.englishFont&&language.chineseFont?language.chineseFont:t.font;originalBestFit=t.resizeTextForBestFit;originalMin=t.resizeTextMinSize;originalMax=t.resizeTextMaxSize;initialized=true;}
            else if(t.text!=lastRendered)source=t.text;
            string target=language.English?language.Translate(source):source;
            if(t.text!=target)t.text=target;
            lastRendered=target;
            var font=language.English?language.englishFont:originalFont;
            if(t.font!=font)t.font=font;
            if(language.English && !t.GetComponent<ContentSizeFitter>())
            {
                // Keep the authored upper bound and readability, without cumulative font shrinking.
                t.resizeTextForBestFit=true;
                t.resizeTextMinSize=Mathf.Max(16,Mathf.RoundToInt(t.fontSize*.65f));
                t.resizeTextMaxSize=t.fontSize;
            }
            else {t.resizeTextForBestFit=originalBestFit;t.resizeTextMinSize=originalMin;t.resizeTextMaxSize=originalMax;}
        }
    }
}
