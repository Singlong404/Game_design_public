using System;
using UnityEngine;
namespace MirrorTrip
{
    [CreateAssetMenu(menuName = "Mirror Trip/Character Animation Set")]
    public sealed class CharacterAnimationSet : ScriptableObject
    {
        [Serializable] public sealed class Sequence
        {
            public string name;
            public Sprite[] frames;
            public float fps = 10;
            public bool loop = true;
            [Tooltip("Optional per-frame visual size correction. Does not affect the motor or collider.")]
            public float[] visualScales;
        }
        public Sequence[] sequences;
        public Sequence Find(string key) => Array.Find(sequences, s => s.name == key);
    }
}
