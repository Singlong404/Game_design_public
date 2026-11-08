using UnityEngine;
namespace MirrorTrip
{
    [DefaultExecutionOrder(-90)]
    public sealed class WorldViewportLayout : MonoBehaviour
    {
        public Camera leftCamera, rightCamera;
        public RectTransform leftWindow, rightWindow;
        private readonly Vector3[] corners = new Vector3[4];
        private void LateUpdate() { Set(leftCamera,leftWindow);Set(rightCamera,rightWindow); }
        private void Set(Camera view,RectTransform window)
        {
            window.GetWorldCorners(corners);
            view.rect=new Rect(corners[0].x/Screen.width,corners[0].y/Screen.height,
                (corners[2].x-corners[0].x)/Screen.width,(corners[2].y-corners[0].y)/Screen.height);
        }
    }
}
