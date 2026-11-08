using UnityEngine;

namespace MirrorTrip
{
    public sealed class PlacementWallLink : MonoBehaviour
    {
        public string pairId;
        public Transform surfaceWorld, mirrorWorld;
        public Vector2 start, end;
        public Vector2 responseStart, responseEnd;
        public bool reverse;
        public float mirrorLength = 1.1f;
        public float mirrorThickness = .14f;
        public float rotationOffset;
        // Mirror faces follow the placement guide instead of a fixed 45-degree tilt.
        public float angle => Mathf.Atan2(end.y - start.y, end.x - start.x) * Mathf.Rad2Deg+rotationOffset;
        public float endpointMargin = .08f;
        [Tooltip("Small authored overhang permitted at the rail ends; does not bypass collision checks.")]
        public float allowedOverhang=.2f;
        public float MinFor(float length)=>Mathf.Clamp01((length*.5f+endpointMargin-allowedOverhang)/Mathf.Min(Vector2.Distance(start,end),Vector2.Distance(responseStart,responseEnd)));
        public bool Fits(float length)=>surfaceWorld&&mirrorWorld&&!string.IsNullOrEmpty(pairId)&&Mathf.Min(Vector2.Distance(start,end),Vector2.Distance(responseStart,responseEnd))>.01f&&MinFor(length)<=.5f;
        public float MinimumT => Mathf.Max((mirrorLength * .5f + endpointMargin) / Vector2.Distance(start, end),
            (mirrorLength * .5f + endpointMargin) / Vector2.Distance(responseStart, responseEnd));
        public float MaximumT => 1 - MinimumT;
        public bool IsValid => !string.IsNullOrEmpty(pairId) && surfaceWorld && mirrorWorld
            && Vector2.Distance(start, end) > mirrorLength + endpointMargin * 2
            && Vector2.Distance(responseStart, responseEnd) > mirrorLength + endpointMargin * 2;
        public Vector2 SurfacePosition(float t) => (Vector2)surfaceWorld.position + Vector2.LerpUnclamped(start, end, t);
        public Vector2 MirrorPosition(float t) => (Vector2)mirrorWorld.position + Vector2.LerpUnclamped(responseStart, responseEnd, reverse ? 1 - t : t);
        public float Project(Vector2 worldPoint)
        {
            var a = SurfacePosition(0); var tangent = end - start;
            return Vector2.Dot(worldPoint - a, tangent) / tangent.sqrMagnitude;
        }
    }
}
