using UnityEngine;
namespace MirrorTrip
{
    public enum MirrorKind { Portal, Storage, Copy, Refraction, OneWayGate }

    // Greybox behaviors adapted to the dual-world clock and pooled card simulation.
    public sealed class MechanismMirror : MonoBehaviour
    {
        public string persistentId;
        public MirrorKind kind;
        public WorldSession session;
        public bool mirrorSide = true, initiallyEnabled = true;
        public bool exitOnly, hideWhenDisabled;
        public MechanismMirror partner;
        public Vector2 localFront = Vector2.right;
        [Range(-150,150)] public float bendDegrees = 35;
        public float exitDistance = .8f;
        [Header("Player exit override (does not change card direction)")]
        public bool overridePlayerExit;
        public Vector2 localPlayerExit=Vector2.right;
        public float playerExitDistance=1.1f;
        public Vector2 PlayerExitDirection=>overridePlayerExit?((Vector2)transform.TransformDirection(localPlayerExit)).normalized:Front;
        public Collider2D contact;
        public SpriteRenderer[] visuals;
        public bool Enabled { get; private set; }
        public bool Stored { get; private set; }
        public bool Used { get; private set; }
        public int StoredReflections { get; private set; }
        public bool StoredChild { get; private set; }
        private float playerCooldown;
        public Vector2 Front => ((Vector2)transform.TransformDirection(localFront)).normalized;
        private void Awake()
        {
            if(!session)session=FindAnyObjectByType<WorldSession>();
            if(!contact)contact=GetComponent<Collider2D>();
            SetEnabled(initiallyEnabled);
        }
        private void LateUpdate()
        {
            Color c=MuchaPalette.State(kind,Enabled,Used,Stored);
            if(session&&session.MirrorActive!=mirrorSide)c=Color.Lerp(c,new Color(c.grayscale,c.grayscale,c.grayscale),.25f);
            foreach(var r in visuals)if(r)r.color=c;
        }
        public void SetEnabled(bool value){Enabled=value;if(contact)contact.enabled=value&&!Used; if(hideWhenDisabled)foreach(var r in visuals)if(r)r.enabled=value;}
        public void Restore(bool enabled,bool stored,bool used,int reflections,bool child)
        {Stored=stored;Used=used;StoredReflections=reflections;StoredChild=child;playerCooldown=.3f;SetEnabled(enabled);}
        public void Tick(float delta)
        {
            playerCooldown=Mathf.Max(0,playerCooldown-delta);
            if(!session||!session.CanInteract||session.MirrorActive!=mirrorSide||!contact)return;
            var actor=mirrorSide?session.mirrorPlayer:session.surfacePlayer;
            if(contact.bounds.Intersects(actor.Bounds))TryTeleport(actor);
        }
        public bool TryTeleport(WorldPlayerMotor actor)
        {
            if(exitOnly||!Enabled||Used||playerCooldown>0||!partner||!partner.isActiveAndEnabled||!partner.Enabled||partner.mirrorSide!=mirrorSide
                ||(kind!=MirrorKind.Portal&&kind!=MirrorKind.OneWayGate))return false;
            Vector2 direction=partner.PlayerExitDirection;
            float clearance=partner.overridePlayerExit?partner.playerExitDistance:partner.exitDistance;
            var bodyOffset=(Vector2)actor.Bounds.center-(Vector2)actor.transform.position;
            if(partner.overridePlayerExit)
            {
                // The motor pivot is near the feet; clearing just the pivot can leave the head in the slab.
                var e=actor.Bounds.extents;float half=Mathf.Abs(direction.x)*e.x+Mathf.Abs(direction.y)*e.y;
                var pe=partner.contact?partner.contact.bounds.extents:Vector3.zero;
                float portalHalf=Mathf.Abs(direction.x)*pe.x+Mathf.Abs(direction.y)*pe.y;
                clearance=Mathf.Max(clearance,half-Vector2.Dot(bodyOffset,direction)+portalHalf+.12f);
            }
            Vector2 destination=(Vector2)partner.transform.position+direction*clearance;
            var filter=new ContactFilter2D{useLayerMask=true,layerMask=actor.terrainMask,useTriggers=false};
            var overlap=new Collider2D[8];
            Vector2 querySize=(Vector2)actor.Bounds.size+Vector2.one*.02f;
            if(Physics2D.OverlapBox(destination+bodyOffset,querySize,0,filter,overlap)>0)
            {
                if(!partner.overridePlayerExit)return false;
                bool found=false;
                // A mirror may hang slightly below a thin ceiling. Search only on the chosen
                // side for a whole-body clearance, never fall back onto the platform above.
                for(int i=1;i<=30&&!found;i++)foreach(float sign in new[]{-1f,1f})
                {
                    float distance=clearance+sign*i*.04f;
                    if(distance+Vector2.Dot(bodyOffset,direction)<=.1f)continue;
                    Vector2 candidate=(Vector2)partner.transform.position+direction*distance;
                    if(Physics2D.OverlapBox(candidate+bodyOffset,querySize,0,filter,overlap)==0){destination=candidate;found=true;break;}
                }
                if(!found)return false;
            }
            if(session.feedback)session.feedback.Play(FeedbackKind.Teleport,actor.Bounds.center,mirrorSide);
            actor.SetLocalPosition(destination-(Vector2)actor.worldRoot.position,direction*Mathf.Max(1,actor.Velocity.magnitude));
            if(session.feedback)session.feedback.Play(FeedbackKind.Teleport,actor.Bounds.center,mirrorSide);
            if(mirrorSide&&session.expansion)session.expansion.ResetFall();
            playerCooldown=partner.playerCooldown=.35f;
            if(kind==MirrorKind.OneWayGate){Used=true;if(contact)contact.enabled=false;}
            (mirrorSide?session.mirrorCamera:session.surfaceCamera).GetComponent<PlayerFollowCamera>()?.SnapToTarget();
            return true;
        }
        // Return false when the incoming card is consumed (including successful storage).
        public bool Process(CardProjectile card,CardSystem system)
        {
            if(!Enabled||Used)return false;
            if(exitOnly){card.Position+=card.Direction*.3f;card.LastMirror=contact;return true;}
            if(kind==MirrorKind.Storage)
            {
                if(Stored)return false;
                Stored=true;StoredReflections=card.Reflections;StoredChild=card.IsCopy;
                session?.ShowMessage("卡牌已封存。里世界按 E，朝旅人所在方向释放。",5);return false;
            }
            if(kind==MirrorKind.Portal||kind==MirrorKind.OneWayGate)
            {
                if(!partner||!partner.isActiveAndEnabled||!partner.Enabled||partner.mirrorSide!=mirrorSide)return false;
                card.Direction=partner.Front;
                card.Position=(Vector2)partner.transform.position+partner.Front*partner.exitDistance;
                card.LastMirror=partner.contact;card.Reflections++;return true;
            }
            if(kind==MirrorKind.Refraction)
            {
                card.Direction=(Vector2)(Quaternion.Euler(0,0,bendDegrees)*card.Direction);
                card.Reflections++;
            }
            else if(kind==MirrorKind.Copy&&!card.IsCopy)
            {
                // Original passes through; a single reflected child cannot multiply again.
                var reflected=Vector2.Reflect(card.Direction,Front).normalized;
                system.SpawnDerived(card.Position+reflected*.28f,reflected,card.RemainingLife,card.Reflections+1,true,contact);
            }
            card.Position+=card.Direction*.3f;card.LastMirror=contact;
            return card.Reflections<=8;
        }
        public bool Release()
        {
            if(!Stored||!Enabled||!session||!session.CanShoot)return false;
            Vector2 dir=((Vector2)session.mirrorPlayer.transform.position-(Vector2)transform.position).normalized;
            if(dir.sqrMagnitude<.01f)dir=Front;
            if(!session.cards.SpawnDerived((Vector2)transform.position+dir*.3f,dir,8,StoredReflections,StoredChild,contact))return false;
            Stored=false;return true;
        }
    }
}
