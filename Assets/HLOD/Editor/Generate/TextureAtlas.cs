using UnityEngine;

namespace Unity.HLOD
{
    [CreateAssetMenu(fileName = "TextureAtlas", menuName = "AutoLOD/Texture Atlas")]
    public class TextureAtlas : ScriptableObject
    {
        public Texture2D textureAtlas;
        public Texture2D[] textures;
        public Rect[] uvs;

        public Texture2D textureAtlas_N;
    }
}
