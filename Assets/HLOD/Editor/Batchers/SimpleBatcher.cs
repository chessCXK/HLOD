using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Unity.HLOD
{
    /// <summary>
    /// A simple batcher that combines textures into an atlas and meshes (non material-preserving)
    /// </summary>
    class SimpleBatcher : IBatcher
    {
        Texture2D whiteTexture
        {
            get
            {
                if (!m_WhiteTexture)
                {
                    var path = "Assets/HLODResources/Generated/Atlases/white.asset";
                    m_WhiteTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                    if (!m_WhiteTexture)
                    {
                        m_WhiteTexture = UnityEngine.Object.Instantiate(Texture2D.whiteTexture);
                        var directory = Path.GetDirectoryName(path);
                        if (!Directory.Exists(directory))
                            Directory.CreateDirectory(directory);

                        AssetDatabase.CreateAsset(m_WhiteTexture, path);
                    }
                }

                return m_WhiteTexture;
            }
        }

        Texture2D m_WhiteTexture;

        public IEnumerator Batch(HLODGenerate hg, LODVolume lodVolume)
        {
            GameObject go = lodVolume.combined._hlodRoot;
            var renderers = go.GetComponentsInChildren<Renderer>();
            var materials = new HashSet<Material>(renderers.SelectMany(r => r.sharedMaterials));

            List<Texture2D> textures = new List<Texture2D>();
            List<Texture2D> normals = new List<Texture2D>();

            
            textures = new HashSet<Texture2D>(materials.Select(m =>
            {
                if (m)
                    return m.mainTexture as Texture2D;

                return null;
            }).Where(t => t != null)).ToList();
            textures.Add(whiteTexture);

            /*
            foreach(var t2d in textures)
            {
                foreach(var rd in renderers)
                {
                    if(rd.sharedMaterial.mainTexture == t2d)
                    {
                        Texture2D t = rd.sharedMaterial.GetTexture("_BumpMap") as Texture2D;
                        if (t == null)
                        {
                            var texture = new Texture2D(rd.sharedMaterial.mainTexture.width, rd.sharedMaterial.mainTexture.height, TextureFormat.RGB24, false, PlayerSettings.colorSpace == ColorSpace.Linear);

                            t = texture;
                            //texture.Apply();
                        }
                        else if(t.width != rd.sharedMaterial.mainTexture.width || t.height != rd.sharedMaterial.mainTexture.height)
                        {
                            int width = rd.sharedMaterial.mainTexture.width;
                            int height = rd.sharedMaterial.mainTexture.height;
                            var texture = new Texture2D(rd.sharedMaterial.mainTexture.width, rd.sharedMaterial.mainTexture.height, t.format, false, PlayerSettings.colorSpace == ColorSpace.Linear);

                            
                            for(int i = 0; i < height; i++)
                            {
                                for(int j = 0; j <width; j++)
                                {
                                    //EditorUtility.DisplayProgressBar("fsdfasd", (height * width).ToString(), (float)(i * j) / (height * width));
                                   // Color newColor = t.GetPixelBilinear(j / width, i / height);
                                    texture.SetPixel(j, i, Color.white);
                                }
                            }
                            //EditorUtility.ClearProgressBar();
                            texture.Apply();
                            t = texture;
                        }
                        normals.Add(t);
                        break;
                    }
                }
            }
            normals.Add(whiteTexture);
            */

            TextureAtlasData atlasData = null;
            yield return TextureAtlasModule.instance.GetTextureAtlas(hg, lodVolume, textures.ToArray(), normals.ToArray(), a => atlasData = a);

            var mainAtlasLookup = new Dictionary<Texture2D, Rect>();
            //var normalAtlasLookup = new Dictionary<Texture2D, Rect>();
            var atlasTextures = atlasData.m_Atlases.textures;
            for (int i = 0; i < atlasTextures.Length; i++)
            {
                mainAtlasLookup[atlasTextures[i]] = atlasData.m_Atlases.uvs[i];
            }

            MeshFilter[] meshFilters = go.GetComponentsInChildren<MeshFilter>();
            var combine = new List<CombineInstance>();
            for (int i = 0; i < meshFilters.Length; i++)
             {
                var mf = meshFilters[i];
                var sharedMesh = mf.sharedMesh;

                if (!sharedMesh)
                    continue;

                if (!sharedMesh.isReadable)
                {
                    var assetPath = AssetDatabase.GetAssetPath(sharedMesh);
                    if (!string.IsNullOrEmpty(assetPath))
                    {
                        var importer = AssetImporter.GetAtPath(assetPath) as ModelImporter;
                        if (importer)
                        {
                            importer.isReadable = true;
                            importer.SaveAndReimport();
                        }
                    }
                }

                var ci = new CombineInstance();

                var mesh = UnityEngine.Object.Instantiate(sharedMesh);

                var mr = mf.GetComponent<MeshRenderer>();
                var sharedMaterials = mr.sharedMaterials;
                var uv = mesh.uv;
                if(uv == null)
                {
                    uv = mesh.uv2;
                }
                var colors = mesh.colors;
                if (colors == null || colors.Length == 0)
                    colors = new Color[uv.Length];
                var updated = new bool[uv.Length];
                var triangles = new List<int>();

                // Some meshes have submeshes that either aren't expected to render or are missing a material, so go ahead and skip
                var subMeshCount = Mathf.Min(mesh.subMeshCount, sharedMaterials.Length);
                for (int j = 0; j < subMeshCount; j++)
                {
                    var sharedMaterial = sharedMaterials[Mathf.Min(j, sharedMaterials.Length - 1)];
                    var mainTexture = whiteTexture;
                    var materialColor = Color.white;

                    if (sharedMaterial)
                    {
                        var texture = sharedMaterial.mainTexture as Texture2D;
                        //sharedMaterial.texture
                        if (texture)
                            mainTexture = texture;

                        if (sharedMaterial.HasProperty("_Color"))
                            materialColor = sharedMaterial.color;
                    }

                    if (mesh.GetTopology(j) != MeshTopology.Triangles)
                    {
                        Debug.LogWarning("Mesh must have triangles", mf);
                        continue;
                    }

                    triangles.Clear();
                    mesh.GetTriangles(triangles, j);
                    var uvOffset = mainAtlasLookup[mainTexture];
                    foreach (var t in triangles)
                    {
                        if (!updated[t])
                        {
                            var uvCoord = uv[t];
                            if (mainTexture == whiteTexture)
                            {
                                // Sample at center of white texture to avoid sampling edge colors incorrectly
                                uvCoord.x = 0.5f;
                                uvCoord.y = 0.5f;
                            }

                            while (uvCoord.x < 0)
                                uvCoord.x += 1;
                            while (uvCoord.y < 0)
                                uvCoord.y += 1;
                            while (uvCoord.x > 1)
                                uvCoord.x -= 1;
                            while (uvCoord.y > 1)
                                uvCoord.y -= 1;

                            uvCoord.x = Mathf.Lerp(uvOffset.xMin, uvOffset.xMax, uvCoord.x);
                            uvCoord.y = Mathf.Lerp(uvOffset.yMin, uvOffset.yMax, uvCoord.y);
                            uv[t] = uvCoord;

                            if (mainTexture == whiteTexture)
                                colors[t] = materialColor;
                            else
                                colors[t] = Color.white;

                            updated[t] = true;
                        }
                    }

                    yield return null;
                }
                mesh.uv = uv;
                mesh.uv2 = null;
                mesh.colors = colors;
                
                ci.mesh = mesh;
                ci.transform = mf.transform.localToWorldMatrix;
                combine.Add(ci);

                mf.gameObject.SetActive(false);

                yield return null;
            }
            var combinedMesh = new Mesh();
#if UNITY_2017_3_OR_NEWER
            combinedMesh.indexFormat = IndexFormat.UInt32;
#endif
            combinedMesh.CombineMeshes(combine.ToArray(), true, true);
            combinedMesh.RecalculateBounds();
            var meshFilter = go.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = combinedMesh;

            for (int i = 0; i < meshFilters.Length; i++)
            {
                UnityEngine.Object.DestroyImmediate(meshFilters[i].gameObject);
            }

            var meshRenderer = go.AddComponent<MeshRenderer>();
            
            meshRenderer.sharedMaterial = atlasData.m_Material;

            atlasData.m_MeshRenderers.Add(meshRenderer);
        }

    }
}
