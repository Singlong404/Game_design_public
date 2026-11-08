using UnityEngine;

namespace MirrorTrip
{
    // Kinematic swept movement keeps frozen bodies queryable. No global time scale changes.
    public sealed class WorldPlayerMotor : MonoBehaviour
    {
        public bool sideView;
        public Transform worldRoot;
        public BoxCollider2D body;
        public Rigidbody2D rigidBody;
        public Transform aimOrigin;
        public LayerMask terrainMask;
        public float moveSpeed = 4.5f;
        public float jumpSpeed = 8.5f;
        public float gravity = 24f;
        [Header("Hold Ctrl sprint")]
        public float sprintMultiplier = 1.7f, sprintDuration = 1.2f, sprintCooldown = 2f;
        public bool sprintAllowed=true;
        public bool SprintHeld { get; set; }
        public bool Sprinting { get; private set; }
        public float SprintRemaining { get; private set; } = 1.2f;
        public float SprintRecovery { get; private set; }
        private bool sprintNeedsRelease;
        private void Awake() { SprintRemaining = sprintDuration; }
        public Vector2 Velocity { get; private set; }
        public Vector2 Facing { get; private set; } = Vector2.down;
        public bool Grounded { get; private set; }
        public Vector2 LocalPosition => (Vector2)transform.position - (Vector2)worldRoot.position;
        public Vector2 AimOrigin => aimOrigin != null ? (Vector2)aimOrigin.position : (Vector2)transform.position;
        public Bounds Bounds => body.bounds;
        private float jumpBuffer, coyote;
        private readonly RaycastHit2D[] hits = new RaycastHit2D[24];
        private const float Skin = .015f;

        public void QueueJump() { if (sideView) jumpBuffer = .12f; }

        public void Tick(Vector2 movement, bool jumpHeld, float delta)
        {
            bool moving = sideView ? Mathf.Abs(movement.x) > .01f : movement.sqrMagnitude > .001f;
            if(moving) Facing=sideView?new Vector2(Mathf.Sign(movement.x),0):movement.normalized;
            if (!SprintHeld) sprintNeedsRelease = false;
            if (SprintRecovery > 0) { SprintRecovery = Mathf.Max(0, SprintRecovery-delta); if(SprintRecovery==0)SprintRemaining=sprintDuration; }
            Sprinting = sprintAllowed && SprintHeld && moving && !sprintNeedsRelease && SprintRecovery<=0 && SprintRemaining>0;
            float speed = moveSpeed * (Sprinting ? sprintMultiplier : 1);
            if(Sprinting) { SprintRemaining=Mathf.Max(0,SprintRemaining-delta); if(SprintRemaining<=0){SprintRecovery=sprintCooldown;sprintNeedsRelease=true;} }
            else if(!SprintHeld && SprintRecovery<=0) SprintRemaining=Mathf.Min(sprintDuration,SprintRemaining+delta*sprintDuration/sprintCooldown);
            var velocity = Velocity;
            if (sideView)
            {
                Grounded = IsGrounded();
                coyote = Grounded ? .1f : Mathf.Max(0, coyote - delta);
                jumpBuffer = Mathf.Max(0, jumpBuffer - delta);
                velocity.x = Mathf.Clamp(movement.x, -1, 1) * speed;
                if (jumpBuffer > 0 && coyote > 0)
                {
                    velocity.y = jumpSpeed; jumpBuffer = coyote = 0; Grounded = false;
                }
                if (!jumpHeld && velocity.y > jumpSpeed * .5f) velocity.y = jumpSpeed * .5f;
                velocity.y = Mathf.Max(velocity.y - gravity * delta, -18);
                if (Grounded && velocity.y < 0) velocity.y = -1;
            }
            else velocity = Vector2.ClampMagnitude(movement, 1) * speed;

            var position = (Vector2)transform.position;
            position += Sweep(position, Vector2.right * velocity.x * delta, out bool hitX);
            position += Sweep(position, Vector2.up * velocity.y * delta, out bool hitY);
            if (hitX) velocity.x = 0;
            if (hitY) velocity.y = 0;
            Velocity = velocity;
            rigidBody.position = position;
            transform.position = new Vector3(position.x, position.y, transform.position.z);
            Physics2D.SyncTransforms();
            if (sideView) Grounded = IsGrounded();
        }

        private Vector2 Sweep(Vector2 position, Vector2 displacement, out bool blocked)
        {
            blocked = false;
            float distance = displacement.magnitude;
            if (distance < .00001f) return Vector2.zero;
            var filter = new ContactFilter2D { useLayerMask = true, layerMask = terrainMask, useTriggers = false };
            var size = Vector2.Scale(body.size, body.transform.lossyScale);
            var offset = (Vector2)body.bounds.center - (Vector2)transform.position;
            var direction = displacement / distance;
            int count = Physics2D.BoxCast(position + offset, size, 0, direction, filter, hits, distance + Skin);
            float allowed = distance;
            for (int i = 0; i < count; i++)
            {
                if (Vector2.Dot(direction, hits[i].normal) >= -.05f) continue;
                float hitDistance = Mathf.Max(0, hits[i].distance - Skin);
                if (hitDistance <= allowed) { allowed = hitDistance; blocked = true; }
            }
            return direction * allowed;
        }

        private bool IsGrounded()
        {
            var bounds = body.bounds;
            var filter = new ContactFilter2D { useLayerMask = true, layerMask = terrainMask, useTriggers = false };
            int count = Physics2D.BoxCast(new Vector2(bounds.center.x, bounds.min.y + .04f),
                // Use the same horizontal footprint as movement collision. A narrow support
                // at a platform edge must not stop the fall while leaving Grounded false.
                new Vector2(bounds.size.x, .04f), 0, Vector2.down, filter, hits, .09f);
            for (int i = 0; i < count; i++) if (hits[i].normal.y > .65f) return true;
            return false;
        }

        public void ResetTransient()
        {SprintHeld=Sprinting=sprintNeedsRelease=false;SprintRemaining=sprintDuration;SprintRecovery=0;jumpBuffer=coyote=0;}

        // Used by spawning and deterministic scenario tests, never by world switching.
        public void SetLocalPosition(Vector2 position, Vector2 velocity = default)
        {
            var target = (Vector2)worldRoot.position + position;
            transform.position = new Vector3(target.x, target.y, 0); rigidBody.position = target;
            Velocity = velocity; jumpBuffer = coyote = 0;
            Physics2D.SyncTransforms(); Grounded = sideView && IsGrounded();
        }
    }
}
