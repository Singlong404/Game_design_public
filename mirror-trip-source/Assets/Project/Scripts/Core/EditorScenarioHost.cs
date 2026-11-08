#if UNITY_EDITOR
using System.Collections;
using UnityEngine;
namespace MirrorTrip
{
    // Transient Play-test coroutine host; excluded from player builds and never saved in scenes.
    public sealed class EditorScenarioHost : MonoBehaviour
    {
        public void Run(IEnumerator scenario) { StartCoroutine(scenario); }
    }
}
#endif
