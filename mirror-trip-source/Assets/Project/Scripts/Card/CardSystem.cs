using System.Collections.Generic;
using UnityEngine;

namespace MirrorTrip
{
    public sealed class CardSystem : MonoBehaviour
    {
        public GameObject prefab;
        public Transform container;
        public LayerMask collisionMask;
        public int maximumCards = 3;
        public float speed = 12;
        public float lifetime = 5;
        public float radius = .08f;
        public float cooldown = .25f;
        public IReadOnlyList<CardProjectile> ActiveCards => active;
        public int TotalReflections { get; private set; }
        public int GroundHits { get; private set; }
        private readonly List<CardProjectile> active = new List<CardProjectile>();
        private readonly Stack<CardProjectile> pool = new Stack<CardProjectile>();
        private readonly RaycastHit2D[] hits = new RaycastHit2D[48];
        private float fireTimer;
        public GameplayFeedback feedback;

        public bool Fire(Vector2 origin, Vector2 direction)
        {
            if (fireTimer > 0 || active.Count >= maximumCards || direction.sqrMagnitude < .001f) return false;
            CardProjectile card;
            if (pool.Count > 0) card = pool.Pop();
            else card = Instantiate(prefab, container).GetComponent<CardProjectile>();
            card.Position = origin; card.Direction = direction.normalized;
            card.RemainingLife = lifetime; card.Reflections = 0; card.LastMirror = null; card.IsCopy=false;
            card.gameObject.SetActive(true); active.Add(card); fireTimer = cooldown;
            return true;
        }

        public bool SpawnDerived(Vector2 origin,Vector2 direction,float life,int reflections,bool child,Collider2D source)
        {
            if(active.Count>=maximumCards || direction.sqrMagnitude<.001f)return false;
            var card=pool.Count>0?pool.Pop():Instantiate(prefab,container).GetComponent<CardProjectile>();
            card.Position=origin;card.Direction=direction.normalized;card.RemainingLife=life;card.Reflections=reflections;
            card.IsCopy=child;card.LastMirror=source;card.gameObject.SetActive(true);active.Add(card);return true;
        }
        public void Tick(float delta)
        {
            fireTimer = Mathf.Max(0, fireTimer - delta);
            for (int i = active.Count - 1; i >= 0; i--)
            {
                var card = active[i]; card.RemainingLife -= delta;
                if (card.RemainingLife <= 0 || !Travel(card, speed * delta)) RecycleAt(i);
            }
        }

        private bool Travel(CardProjectile card, float distance)
        {
            var filter = new ContactFilter2D { useLayerMask = true, layerMask = collisionMask, useTriggers = true };
            for (int iteration = 0; iteration < 8 && distance > .0001f; iteration++)
            {
                int count = Physics2D.CircleCast(card.Position, radius, card.Direction, filter, hits, distance);
                RaycastHit2D closest = default; float closestDistance = float.MaxValue;
                for (int j = 0; j < count; j++)
                {
                    var hit = hits[j];
                    if (hit.collider == card.LastMirror && hit.distance < .03f) continue;
                    // Goal/exit/kill triggers never block a card or act as switches.
                    if (hit.collider.isTrigger && hit.collider.GetComponentInParent<CardReflector>() == null
                        && hit.collider.GetComponentInParent<CardSwitch>() == null && hit.collider.GetComponentInParent<MechanismMirror>() == null) continue;
                    if (hit.distance < closestDistance) { closest = hit; closestDistance = hit.distance; }
                }
                if (!closest.collider) { card.Position += card.Direction * distance; return true; }
                card.Position += card.Direction * closest.distance;
                if(feedback)feedback.Play(FeedbackKind.Hit,card.Position,true,.7f);
                distance -= closest.distance;
                var target = closest.collider.GetComponentInParent<CardSwitch>();
                if (target != null)
                {
                    if (card.Reflections >= target.minimumReflections) target.Hit();
                    else target.session?.ShowMessage("Reflect the card off a mirror before hitting the switch.");
                    return false;
                }
                var mechanism=closest.collider.GetComponentInParent<MechanismMirror>();
                if(mechanism)
                {
                    if(card.Reflections>=8||!mechanism.Process(card,this))return false;
                    distance=Mathf.Max(0,distance-.3f);continue;
                }
                var reflector = closest.collider.GetComponentInParent<CardReflector>();
                if (reflector == null) { GroundHits++; return false; }
                if (++card.Reflections > 8 || !reflector.Reflect(card.Direction, out var outgoing)) return false;
                card.Direction = outgoing; TotalReflections++; card.LastMirror = closest.collider;
                card.Position += outgoing * .012f + reflector.Normal * .004f;
                distance = Mathf.Max(0, distance - .012f);
            }
            return distance <= .0001f; // A pathological corner cannot loop forever in one physics step.
        }

        private void RecycleAt(int index)
        {
            var card = active[index]; active.RemoveAt(index);
            card.gameObject.SetActive(false); pool.Push(card);
        }

        public void Clear()
        {
            for (int i = active.Count - 1; i >= 0; i--) RecycleAt(i);
            fireTimer = 0;
        }

        public bool Overlaps(Vector2 center, Vector2 size, float angle)
        {
            var inverse = Quaternion.Euler(0, 0, -angle);
            foreach (var card in active)
            {
                Vector2 point = inverse * (card.Position - center);
                Vector2 nearest = new Vector2(Mathf.Clamp(point.x, -size.x * .5f, size.x * .5f), Mathf.Clamp(point.y, -size.y * .5f, size.y * .5f));
                if (Vector2.Distance(point, nearest) < radius + .02f) return true;
            }
            return false;
        }
    }
}
