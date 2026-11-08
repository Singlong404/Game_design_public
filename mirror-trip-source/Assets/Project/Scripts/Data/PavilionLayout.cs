using System;
using System.Collections.Generic;
using UnityEngine;

namespace MirrorTrip
{
    [CreateAssetMenu(menuName = "Mirror Trip/Pavilion Layout")]
    public sealed class PavilionLayout : ScriptableObject
    {
        [Serializable]
        public struct Block
        {
            public string id;
            public Vector2 center, size;
            public bool solid;
            public int category; // 0 structural wall, 1 water, 2 glass, 3 column, 4 podium
            public Block(string id, float x, float y, float width, float height, bool solid, int category = 0)
            { this.id = id; center = new Vector2(x, y); size = new Vector2(width, height); this.solid = solid; this.category = category; }
        }
        public List<Block> blocks = new List<Block>();
        public List<Block> doorBlocks = new List<Block>();
        public Vector2 surfaceSpawn = new Vector2(16, 1);
        public Vector2 mirrorSpawn = new Vector2(16, .77f);
        public Vector2 placementStart = new Vector2(13.8f, 2.95f), placementEnd = new Vector2(17.5f, 2.95f);
        public Vector2 switchPosition = new Vector2(17.2f, 1);
        public Vector2 goalPosition = new Vector2(25.3f, 1);
        public Vector2 exitPosition = new Vector2(26.8f, 1);
    }
}
