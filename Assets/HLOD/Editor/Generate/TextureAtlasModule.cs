using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityObject = UnityEngine.Object;

namespace Unity.HLOD
{
    //[System.Serializable]
    public class TextureAtlasData
    {
        public TextureAtlasData(TextureAtlas atlases, State state)
        {
            m_Atlases = atlases;
            m_State = state;
        }
        public enum State//贴图状态
        {
            None,  
            Delete,//删除
            Add,   //更新
        }
        public GameObject m_Root;//子树跟节点
        public List<MeshRenderer> m_MeshRenderers = new List<MeshRenderer>();//引用的renderer
        public TextureAtlas m_Atlases;   //大贴图
        public Material m_Material;      
        public State m_State = State.None;
    }
    //[CreateAssetMenu(fileName = "TextureAtlasModule", menuName = "AutoLOD/TextureAtlasModule")]
    public class TextureAtlasModule : ScriptableSingleton<TextureAtlasModule>
    {
        //没有HLODGenerate的图集钥匙
        //[SerializeField]
        private HLODGenerate s_CommonKey = new HLODGenerate();
        private Dictionary<HLODGenerate, List<TextureAtlasData>> m_AllAtlases = new Dictionary<HLODGenerate, List<TextureAtlasData>>();

        public static string s_TexDirectoryName = "Atlases";
        public static string s_MatDirectoryName = "Materias";

        public Dictionary<HLODGenerate, List<TextureAtlasData>> AllAtlases
        {
            get
            {
                /*删除空key*/
                List<HLODGenerate> keyList = new List<HLODGenerate>();
                keyList.Clear();
                var keys = m_AllAtlases.Keys;
                foreach (var item in keys)
                {
                    if (item.Equals(null))
                    {
                        keyList.Add(item);
                    }
                }
                foreach (var key in keyList)
                {
                    m_AllAtlases.Remove(key);
                }
                return m_AllAtlases;
            }
        }
        static void SaveMaterialsAsset(Material mat, string paths, string name)
        {
            string directory = "";
            directory = Path.Combine(paths, s_MatDirectoryName);
            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            string path = null;
            if (name == "")
            {
                path = directory + Path.GetRandomFileName();
            }
            else
            {
                path = Path.Combine(directory, name); 
            }
            path = Path.ChangeExtension(path, ".mat");
            AssetDatabase.CreateAsset(mat, path);
        }
        static T SaveUniqueAtlasAsset<T>(T asset, string paths = null, string name = "")where T: UnityObject
        {
            string directory = "";
            if (paths == null)
            {
                directory = "Assets/AutoLOD/Generated/Atlases/" + s_TexDirectoryName + "/";
            }
            else
            {
                directory = Path.Combine(paths, s_TexDirectoryName) + "\\";
            }
            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            string path = null;
            if (name == "")
            {
                path = directory + Path.GetRandomFileName();
            }
            else
            {
                path = directory + name;
            }
            path = Path.ChangeExtension(path, "asset");
            Texture2D tex2D = asset as Texture2D;

            
            var assetImporter = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(tex2D));
            var textureImporter = assetImporter as TextureImporter;
            if (textureImporter && !textureImporter.isReadable)
            {
                textureImporter.isReadable = true;
                textureImporter.SaveAndReimport();
            }
            
            if (tex2D != null)
            {
                tex2D.Apply();
                path = Path.ChangeExtension(path, "jpg");
                byte[] datas = tex2D.EncodeToJPG();
                System.IO.File.WriteAllBytes(path, datas);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                return AssetDatabase.LoadAssetAtPath<T>(path);
            }
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }
       public void ClearTextureAtlasList(HLODGenerate key)
       {
            List<TextureAtlasData> atlasesData = null;
            if (m_AllAtlases.TryGetValue(key, out atlasesData))
            {
                atlasesData.Clear();
            }
       }
        /*是否重新生成*/
        bool IsReGenerate(LODVolume lodVolume, Texture2D[] textures, TextureAtlas atlas)
        {
            //父节点有使用不删除
            Transform currParent = lodVolume.transform.parent;
            LODVolume parentVolume = currParent.GetComponent<LODVolume>();
            //自己是root，贴图数量不一样更新
            //父节点没有合并，说明自己是子树的父节点，从自己开始
            if (parentVolume == null || parentVolume.combined == null)
            {
                IEnumerable<Texture2D> residueOld = atlas.textures.Except(textures);
                IEnumerable<Texture2D> residueNew = textures.Except(atlas.textures);

                return residueOld.Any() | residueNew.Any();
            }

            return false;
        }
        void GetTextureAtlasList(HLODGenerate hg, out List<TextureAtlasData> atlasesData)
        {
            HLODGenerate hlodGenerate = hg;
            HLODGenerate key = hlodGenerate ? hlodGenerate : s_CommonKey;
            if (!m_AllAtlases.TryGetValue(key, out atlasesData))
            {
                atlasesData = new List<TextureAtlasData>();
                m_AllAtlases.Add(key, atlasesData);
                return;
            }
            // Clear out any atlases that were removed
            atlasesData.RemoveAll(a => a.m_Atlases == null);
        }

        //检查是否为所在树的贴图集
        bool CheckInCurrLittleOctree(LODVolume lodVolume, TextureAtlasData rootData)
        {
            LODVolume[] parents = lodVolume.GetComponentsInParent<LODVolume>();
            foreach(LODVolume p in parents)
            {
                if(p.gameObject == rootData.m_Root)//找到我是该大贴图的孩子
                {
                    return true;
                }
            }
            return false;
        }
        public IEnumerator GetTextureAtlas(HLODGenerate hg, LODVolume lodVolume, Texture2D[] textures, Texture2D[] normals, Action<TextureAtlasData> callback)
        {
            TextureAtlasData atlasData = null;

            List<TextureAtlasData> atlasesData = null;
            GetTextureAtlasList(hg, out atlasesData);

            yield return null;

            foreach (var a in atlasesData)
            {
                if (a.m_State == TextureAtlasData.State.Delete)
                    continue;
                //检查是否为所在树的贴图集
                if(!CheckInCurrLittleOctree(lodVolume, a))
                {
                    continue;
                }
                //是否重新生成
                if (IsReGenerate(lodVolume, textures, a.m_Atlases))
                {
                    a.m_State = TextureAtlasData.State.Delete;
                    break;
                }
                // At a minimum the atlas should have all of the textures requested, but can be a superset
                if (!textures.Except(a.m_Atlases.textures).Any())
                {
                    atlasData = a;
                    break;
                }

                yield return null;
            }
            //m_Atlases.Remove(deleteAtlas);
            if(atlasData == null)//没有找到图集就创建一个
            {
                atlasData = new TextureAtlasData(null, TextureAtlasData.State.Add);
                atlasData.m_Material = new Material(Shader.Find("Standard"));
                /*先序遍历，所有根节点就是第一个创建的对象，子节点会找到这个图集而不会继续创建*/
                atlasData.m_Root = lodVolume.gameObject;
            }
            if (!atlasData.m_Atlases)
            {
                atlasData.m_Atlases = ScriptableObject.CreateInstance<TextureAtlas>();

                TextureReadableSetting(textures);
                //TextureReadableSetting(normals);

                CombineTexture(textures, atlasData.m_Atlases, false, callback);
                //CombineTexture(normals, atlasData.m_Atlases, true, callback);

                atlasData.m_Material.mainTexture = atlasData.m_Atlases.textureAtlas;
                //material.SetTexture("_NormalMap", atlasData.m_Atlases.textureAtlas_N);
                atlasesData.Add(atlasData);
                yield return null;
            }

            if (callback != null)
                callback(atlasData);
        }
        /*贴图合并操作
         * 要合并的、哪个hlod的图集、图集数据、回调
         */
        void CombineTexture(Texture2D[] textures, TextureAtlas atlas, bool isNormal = false, Action<TextureAtlasData> callback = null)
        {
            var textureAtlas = new Texture2D(0, 0, TextureFormat.RGB24, true, PlayerSettings.colorSpace == ColorSpace.Linear);
            var uvs = textureAtlas.PackTextures(textures.ToArray(), 0, 1024, false);
            if (uvs != null)
            {
                Texture2D t2d = null;
                if (callback == null)
                {
                    t2d = SaveUniqueAtlasAsset<Texture2D>(textureAtlas);
                }
                else
                {
                    t2d = textureAtlas;
                }
                
                if(isNormal)
                {
                    atlas.textureAtlas_N = t2d;
                }
                else
                {
                    atlas.textureAtlas = t2d;
                    atlas.uvs = uvs;
                    atlas.textures = textures;
                }
                if (callback == null)
                {
                    SaveUniqueAtlasAsset(atlas);
                }
            }
        }
        //贴图权限设置
        void TextureReadableSetting(Texture2D[] textures)
        {
            foreach (var t in textures)
            {
                var assetImporter = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(t));
                var textureImporter = assetImporter as TextureImporter;
                if (textureImporter && !textureImporter.isReadable)
                {
                    textureImporter.isReadable = true;
                    textureImporter.SaveAndReimport();
                }
            }
        }

        /*导出单独一个TextureAtlas*/
        public static void ExportOnlyOneTextureAtlasData(TextureAtlasData data, string atlasPath, string materialPath, string name)
        {
            string paths = AssetDatabase.GetAssetPath(data.m_Atlases);
            if (paths != "")
            {
                return;
            }
            data.m_Atlases.textureAtlas = SaveUniqueAtlasAsset<Texture2D>(data.m_Atlases.textureAtlas, atlasPath, name);
            if(data.m_Atlases.textureAtlas_N)//有法线贴图就保存
            {
                data.m_Atlases.textureAtlas_N = SaveUniqueAtlasAsset<Texture2D>(data.m_Atlases.textureAtlas_N, atlasPath, name + "_N");
            }
            data.m_Material.mainTexture = data.m_Atlases.textureAtlas;
            SaveMaterialsAsset(data.m_Material, materialPath, name);

            SaveUniqueAtlasAsset(data.m_Atlases, atlasPath, name);
        }

        /*删除单独一个TextureAtlas*/
        public static void DeleteOnlyOneTextureAtlasData(TextureAtlasData data)
        {
            string paths = AssetDatabase.GetAssetPath(data.m_Atlases.textureAtlas);
            if (paths != "")
            {
                AssetDatabase.DeleteAsset(paths);
            }
            paths = AssetDatabase.GetAssetPath(data.m_Atlases.textureAtlas_N);
            if (paths != "")
            {
                AssetDatabase.DeleteAsset(paths);
            }
            paths = AssetDatabase.GetAssetPath(data.m_Atlases);
            if (paths != "")
            {
                AssetDatabase.DeleteAsset(paths);
            }
            paths = AssetDatabase.GetAssetPath(data.m_Material);
            if (paths != "")
            {
                AssetDatabase.DeleteAsset(paths);
            }
        }
    }
}
