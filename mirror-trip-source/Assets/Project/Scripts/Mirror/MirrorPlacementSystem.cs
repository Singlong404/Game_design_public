using UnityEngine;
namespace MirrorTrip
{
    public sealed class MirrorPlacementSystem : MonoBehaviour
    {
        public WorldSession session;
        public PlacementWallLink[] links;
        public SceneMirror[] sceneMirrors;
        public GameObject surfacePrefab, mirrorPrefab;
        public Transform surfaceContainer, mirrorContainer, surfacePreviewContainer, mirrorPreviewContainer;
        public float interactionRadius = 2.7f;
        public LayerMask surfaceTerrain, mirrorTerrain;
        public SceneMirror Carried { get; private set; }
        public bool IsCarrying => Carried;
        public bool DestinationValid { get; private set; }
        public string PreviewReason { get; private set; }
        private readonly Collider2D[] overlaps = new Collider2D[32];
        private readonly RaycastHit2D[] occlusions = new RaycastHit2D[32];
        private GameObject surfacePreview, mirrorPreview, carryVisual;
        public Vector3 CarryVisualPosition => carryVisual ? carryVisual.transform.position : Vector3.zero;
        public Vector3 CarryVisualRight => carryVisual ? carryVisual.transform.right : Vector3.right;
        private void Awake()
        {
            surfacePreview = MakePreview(surfacePrefab, surfacePreviewContainer);
            mirrorPreview = MakePreview(mirrorPrefab, mirrorPreviewContainer);
            carryVisual = MakePreview(surfacePrefab, surfacePreviewContainer); carryVisual.name = "CarriedMirrorVisual";
            carryVisual.SetActive(false); HidePreview();
        }
        private GameObject MakePreview(GameObject prefab, Transform container)
        {
            var preview = Instantiate(prefab, container); preview.name = "PlacementPreview";
            if(session.presentation)
            {
                var accent=preview.GetComponent<GameplayAccent>();if(!accent)accent=preview.AddComponent<GameplayAccent>();
                accent.role=GameplayAccent.Role.Preview;accent.session=session;
                var ink=preview.GetComponentInChildren<InteractiveInkView>();
                accent.visuals=ink && ink.accent ? new[]{ink.accent} : preview.GetComponentsInChildren<SpriteRenderer>();
                if(ink)ink.placementPreview=true;
            }
            foreach (var collider in preview.GetComponentsInChildren<Collider2D>()) collider.enabled = false;
            return preview;
        }
        public SceneMirror FindNearest()
        {
            SceneMirror nearest = null; float best = float.MaxValue;
            foreach (var pair in sceneMirrors)
            {
                if (!pair || !pair.Available || !InteractionReach.CanReach(session.surfacePlayer,pair.surface.transform,interactionRadius)) continue;
                float distance = Vector2.Distance(session.surfacePlayer.transform.position, pair.surface.transform.position);
                if (distance < best) { best = distance; nearest = pair; }
            }
            return nearest;
        }
        public bool BeginCarry(SceneMirror pair, out string reason)
        {
            reason = "靠近场景中的镜子后按 E。";
            if (!session.CanSurfaceInteract || IsCarrying || !pair || !pair.Available || System.Array.IndexOf(sceneMirrors, pair) < 0) return false;
            if (pair.IsLocked) { reason = "机关已完成，这面镜子已经固定。"; return false; }
            if (!InteractionReach.CanReach(session.surfacePlayer,pair.surface.transform,interactionRadius)) { reason = "距离太远，或中间有墙。"; return false; }
            Carried = pair;
            Destroy(surfacePreview);Destroy(mirrorPreview);Destroy(carryVisual);
            surfacePreview=MakeGhost(pair.surface,surfacePreviewContainer);
            mirrorPreview=MakeGhost(pair.response,mirrorPreviewContainer);
            carryVisual=MakeGhost(pair.surface,surfacePreviewContainer);carryVisual.name="CarriedMirrorVisual";
            pair.surface.SetActive(false); pair.response.SetActive(false);
            carryVisual.transform.localScale=pair.surface.transform.localScale;
            carryVisual.SetActive(true); Physics2D.SyncTransforms(); TickCarry();
            reason = "WASD 搬运，有效预览处按 E 放下；Esc 放回原处。"; return true;
        }
        private GameObject MakeGhost(GameObject source,Transform container)
        {
            var ghost=new GameObject("PlacementPreview");ghost.layer=source.layer;ghost.transform.SetParent(container,false);
            ghost.transform.localScale=source.transform.lossyScale;
            foreach(var original in source.GetComponentsInChildren<SpriteRenderer>(true))
            {
                var child=new GameObject(original.name);child.layer=original.gameObject.layer;child.transform.SetParent(ghost.transform,false);
                child.transform.localPosition=source.transform.InverseTransformPoint(original.transform.position);
                child.transform.localRotation=Quaternion.Inverse(source.transform.rotation)*original.transform.rotation;
                var scale=original.transform.lossyScale;var basis=source.transform.lossyScale;
                child.transform.localScale=new Vector3(scale.x/basis.x,scale.y/basis.y,scale.z/basis.z);
                var r=child.AddComponent<SpriteRenderer>();r.sprite=original.sprite;r.sharedMaterial=original.sharedMaterial;
                r.drawMode=original.drawMode;r.size=original.size;r.flipX=original.flipX;r.flipY=original.flipY;
                r.sortingLayerID=original.sortingLayerID;r.sortingOrder=original.sortingOrder+5;r.color=original.color;
            }
            var accent=ghost.AddComponent<GameplayAccent>();accent.session=session;accent.role=GameplayAccent.Role.Preview;accent.visuals=ghost.GetComponentsInChildren<SpriteRenderer>();
            ghost.SetActive(false);return ghost;
        }
        private PlacementWallLink Destination(out float t)
        {
            PlacementWallLink nearest = null; float best = interactionRadius; t = 0;
            var point = (Vector2)session.surfacePlayer.transform.position;
            foreach (var link in links)
            {
                if (!link || !Carried || !link.Fits(Carried.Length)) continue;
                float minimum=link.MinFor(Carried.Length);
                float candidate = Mathf.Clamp(link.Project(point),minimum,1-minimum);
                float distance = Vector2.Distance(point, link.SurfacePosition(candidate));
                if (distance < best) { best = distance; nearest = link; t = candidate; }
            }
            return nearest;
        }
        public void TickCarry()
        {
            if (!IsCarrying) { HidePreview(); return; }
            carryVisual.transform.SetPositionAndRotation(session.surfacePlayer.transform.position + (Vector3)session.surfacePlayer.Facing * .8f,
                Quaternion.Euler(0, 0, Mathf.Atan2(session.surfacePlayer.Facing.y,session.surfacePlayer.Facing.x)*Mathf.Rad2Deg+90));
            var link = Destination(out float t);
            DestinationValid = ValidateDestination(link, t, out var reason); PreviewReason = reason;
            if (link) Preview(link, t, DestinationValid); else HidePreview();
        }
        public bool TryDrop(out string reason)
        {
            var link = Destination(out float t);
            if (!ValidateDestination(link, t, out reason)) return false;
            var pair = Carried;
            pair.surface.transform.SetPositionAndRotation(link.SurfacePosition(t), Quaternion.Euler(0, 0, pair.Angle(link)));
            pair.response.transform.SetPositionAndRotation(link.MirrorPosition(t), Quaternion.Euler(0, 0, pair.Angle(link)));
            pair.link = link; pair.t = t; FinishCarry();
            reason = "镜子已放下，两侧位置同步。Shift 切换到里世界。"; return true;
        }
        public void CancelCarry() { if (IsCarrying) FinishCarry(); }
        private void FinishCarry()
        {
            Carried.surface.SetActive(true); Carried.response.SetActive(true); Carried = null;
            carryVisual.SetActive(false); DestinationValid = false; HidePreview(); Physics2D.SyncTransforms();
        }
        public void LockPuzzle(string puzzleId)
        {
            if (IsCarrying && Carried.puzzleId == puzzleId) CancelCarry();
            foreach (var pair in sceneMirrors) if (pair && pair.puzzleId == puzzleId) pair.Lock();
        }
        public bool ValidateDestination(PlacementWallLink link, float t, out string reason)
        {
            reason = "[E] 放下镜子";
            if (!IsCarrying || !session.CanSurfaceInteract || Carried.IsLocked) { reason = "当前无法搬运镜子。"; return false; }
            if (!link || !link.Fits(Carried.Length) || !float.IsFinite(t) || t < link.MinFor(Carried.Length)-.001f || t > 1-link.MinFor(Carried.Length)+.001f)
            { reason = "请走到引导条的有效范围内。"; return false; }
            var surface = link.SurfacePosition(t); var mirror = link.MirrorPosition(t);
            if (!CanReach(surface)) { reason = "放置位置太远，或中间有墙。"; return false; }
            var size = Carried.Footprint;float angle=Carried.Angle(link);
            if (Blocked(surface, size, angle, surfaceTerrain) || Blocked(mirror, size, angle, mirrorTerrain))
            { reason = "镜面与建筑重叠。"; return false; }
            int mask = LayerMask.GetMask("SurfacePlayer", "MirrorPlayer", "MirrorSurface");
            if (Blocked(surface, size + Vector2.one * .04f, angle, mask) || Blocked(mirror, size + Vector2.one * .04f, angle, mask))
            { reason = "镜面与角色或其他镜子重叠。"; return false; }
            if (session.cards.Overlaps(mirror, size, angle)) { reason = "冻结的卡牌占用了这里。"; return false; }
            return true;
        }
        private bool Blocked(Vector2 position, Vector2 size, float angle, LayerMask mask)
        {
            var filter = new ContactFilter2D { useLayerMask = true, layerMask = mask, useTriggers = true };
            return Physics2D.OverlapBox(position, size, angle, filter, overlaps) > 0;
        }
        public bool CanReach(Vector2 point)
        {
            return InteractionReach.Clear(session.surfacePlayer,point,interactionRadius);
        }
        private void Preview(PlacementWallLink link, float t, bool valid)
        {
            surfacePreview.SetActive(true); mirrorPreview.SetActive(true);
            if(Carried){surfacePreview.transform.localScale=Carried.surface.transform.localScale;mirrorPreview.transform.localScale=Carried.response.transform.localScale;}
            surfacePreview.transform.SetPositionAndRotation(link.SurfacePosition(t), Quaternion.Euler(0, 0, Carried.Angle(link)));
            mirrorPreview.transform.SetPositionAndRotation(link.MirrorPosition(t), Quaternion.Euler(0, 0, Carried.Angle(link)));
            var color = valid ? new Color(.45f, 1, .65f, .55f) : new Color(1, .3f, .25f, .6f);
            foreach (var renderer in surfacePreview.GetComponentsInChildren<SpriteRenderer>()) renderer.color = color;
            foreach (var renderer in mirrorPreview.GetComponentsInChildren<SpriteRenderer>()) renderer.color = color;
        }
        public void HidePreview() { if (surfacePreview) surfacePreview.SetActive(false); if (mirrorPreview) mirrorPreview.SetActive(false); }
    }
}
