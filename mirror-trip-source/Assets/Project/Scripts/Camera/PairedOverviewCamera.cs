using UnityEngine;

namespace MirrorTrip
{
    [RequireComponent(typeof(Camera))]
    public sealed class PairedOverviewCamera : MonoBehaviour
    {
        public Transform worldRoot;
        public Vector2 localCenter = new Vector2(13.75f, 5.8f);
        public Vector2 frameSize = new Vector2(30, 15.5f);
        private Camera view;
        private void Awake() { view = GetComponent<Camera>(); }
        private void LateUpdate()
        {
            // Identical world-relative framing: both copies of the pavilion stay comparable.
            float aspect = Mathf.Max(.1f, Screen.width * view.rect.width / Mathf.Max(1, Screen.height * view.rect.height));
            view.orthographicSize = Mathf.Max(frameSize.y * .5f, frameSize.x * .5f / aspect);
            transform.position = worldRoot.position + new Vector3(localCenter.x, localCenter.y, -10);
        }
    }
}
