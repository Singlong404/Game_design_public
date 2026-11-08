using UnityEngine;
namespace MirrorTrip
{
    // Small, muted accents against the existing monochrome drawing.
    public static class MuchaPalette
    {
        public static readonly Color Ordinary=new Color32(80,139,177,255); // slate blue
        public static readonly Color Portal=new Color32(112,163,119,255);  // sage green
        public static readonly Color Storage=new Color32(205,164,67,255);  // antique gold
        public static readonly Color Refraction=new Color32(166,115,185,255);// dusty violet
        public static readonly Color Copy=new Color32(206,114,83,255);     // terracotta
        public static readonly Color Gate=new Color32(174,84,108,255);     // muted rose
        public static readonly Color Switch=new Color32(164,104,92,255);
        public static readonly Color Placement=new Color32(190,177,133,255);
        public static readonly Color Locked=new Color32(128,128,128,255);
        public static Color Mirror(MirrorKind kind)
        {
            switch(kind)
            {
                case MirrorKind.Portal:return Portal;
                case MirrorKind.Storage:return Storage;
                case MirrorKind.Refraction:return Refraction;
                case MirrorKind.Copy:return Copy;
                default:return Gate;
            }
        }
        public static Color State(MirrorKind kind,bool enabled,bool used,bool stored)
        {
            Color c=Mirror(kind);
            if(used)return Locked;
            if(!enabled)return Color.Lerp(c,Locked,.5f);
            return stored?Color.Lerp(c,new Color(.2f,.18f,.14f),.32f):c;
        }
    }
}
