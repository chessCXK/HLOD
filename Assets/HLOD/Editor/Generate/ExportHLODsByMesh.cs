using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityFBXExporter;

namespace Unity.HLOD
{
    public class ExportHLODsByMesh
    {

        public static string s_DirectoryName = "HLODsMesh";
        #region 导出native mesh
        public static void PersistHLODs(Transform hlodRoot, string savePath)
        {
            AssetDatabase.StartAssetEditing();
            if (hlodRoot)
            {
                var mf = hlodRoot.GetComponent<MeshFilter>();
                if (mf != null)
                {
                    SaveUniqueHLODAsset(mf, savePath);
                }
            }

            foreach (Transform child in hlodRoot)
            {
                PersistHLODs(child, savePath);
            }
            AssetDatabase.StopAssetEditing();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        static void SaveUniqueHLODAsset(MeshFilter meshFilter, string savePath)
        {
            var sharedMesh = meshFilter.sharedMesh;
            if (!string.IsNullOrEmpty(savePath))
            {
                var directory = Path.Combine(savePath, s_DirectoryName);
                if (!Directory.Exists(directory))
                    Directory.CreateDirectory(directory);

                //var path = Path.Combine(directory, Path.GetRandomFileName());
                var path = Path.Combine(directory, meshFilter.transform.name);
                path = Path.ChangeExtension(path, "asset");
                path = path.Replace("/", "\\");
                AssetDatabase.CreateAsset(sharedMesh, path);
            }
        }
        #endregion

        #region 导出FBX
        public static void PersistHLODOfFbx(Transform hlodRoot, string savePath)
        {
            if (hlodRoot)
            {
                if (string.IsNullOrEmpty(AssetDatabase.GetAssetPath(hlodRoot)))
                {
                    //SaveUniqueHLODAssetOfFbx(hlodRoot, savePath);
                    MonoBehaviourHelper.StartCoroutine(SaveUniqueHLODAssetOfFbx(hlodRoot, savePath));
                }
            }

            foreach (Transform child in hlodRoot.transform)
            {
                PersistHLODOfFbx(child, savePath);
            }
        }
        static IEnumerator SaveUniqueHLODAssetOfFbx(Transform hlodRoot, string savePath)
        {
            if (!string.IsNullOrEmpty(savePath))
            {
                var directory = Path.Combine(savePath, s_DirectoryName);
                if (!Directory.Exists(directory))
                    Directory.CreateDirectory(directory);

                var path = Path.Combine(directory, hlodRoot.name);
                path = Path.ChangeExtension(path, "fbx");
                path = path.Replace("/", "\\");
                FBXExporter.ExportGameObjToFBX(hlodRoot.gameObject, path, false, false);
                yield return null;
                var assetImporter = AssetImporter.GetAtPath(path);
                var modelImporter = assetImporter as ModelImporter;
                if (modelImporter && !modelImporter.isReadable)
                {
                    modelImporter.isReadable = true;
                }
                modelImporter.importBlendShapes = false;
                modelImporter.importVisibility = false;
                modelImporter.importCameras = false;
                modelImporter.importLights = false;
                modelImporter.meshCompression = ModelImporterMeshCompression.High;
                modelImporter.indexFormat = ModelImporterIndexFormat.Auto;
                modelImporter.animationType = ModelImporterAnimationType.None;
                modelImporter.importAnimation = false;
                //modelImporter.materialImportMode = ModelImporterMaterialImportMode.None;
                modelImporter.SaveAndReimport();
                yield return null;
                var fbx = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                MeshFilter m = fbx.GetComponent<MeshFilter>();
                MeshFilter mh = hlodRoot.GetComponent<MeshFilter>();
                hlodRoot.GetComponent<MeshFilter>().sharedMesh = m.sharedMesh;
            }
        }
        #endregion
    }
}