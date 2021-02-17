using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Unity.HLOD
{
    public class LODVolume : MonoBehaviour
    {
        // HLOD运行相关
        [SerializeField]
        public List<UnityLODGroupFast> lodGroups = new List<UnityLODGroupFast>();

        public UnityLODGroupFast combined;

        public List<LODVolume> childVolumes = new List<LODVolume>();

        //所在子树root节点
        public ChildTreeRoot childTreeRoot;

        //相对于大树的深度
        public int deep = 0;

#if UNITY_EDITOR
        static string k_DefaultName = "LODVolumeNode";
        static int k_Splits = 2;

        [HideInInspector]
        public bool deleteDirty;

        public bool dirty;

        public Bounds bounds;

        /*当前正在维护的HLODGenerate*/
        [HideInInspector]
        public HLODGenerate m_CurrHLODGenerate;

        static readonly Color[] k_DepthColors = new Color[]
        {
        Color.red,
        Color.green,
        Color.blue,
        Color.magenta,
        Color.yellow,
        Color.cyan,
        Color.grey,
        };

        public static LODVolume Create(HLODGenerate hlodGenerate)
        {
            int volumeCount = hlodGenerate.m_LODVolumeCount++;
            GameObject go = new GameObject(k_DefaultName + volumeCount, typeof(LODVolume));
            LODVolume volume = go.GetComponent<LODVolume>();
            volume.m_CurrHLODGenerate = hlodGenerate ? hlodGenerate : volume.m_CurrHLODGenerate;
            return volume;
        }

        public IEnumerator AddLodGroup(UnityLODGroupFast lodGroup)
        {
            if (!this)
                yield break;

            if (!lodGroup)
                yield break;

            if (lodGroups.Count == 0 && Mathf.Approximately(bounds.size.magnitude, 0f))
            {
                Renderer[] rds = lodGroup.GetLODs()[0].renderers;
                var targetBounds = bounds;
                foreach (var r in rds)
                {
                    targetBounds.Encapsulate(r.bounds);
                }
                bounds = GetCuboidBounds(targetBounds);

                transform.position = bounds.min;
            }

            // Each LODVolume maintains it's own list of renderers, which includes renderers in children nodes
            if (WithinBounds(lodGroup._lodGroup, bounds))
            {
                var lodGroupsRef = lodGroups.Select(l => l._lodGroup);
                if (!lodGroupsRef.Contains(lodGroup._lodGroup))
                    lodGroups.Add(lodGroup);

                /*当自己维护的节点大于k_VolumeSplitRendererCount个时，就开始分割子节点*/
                if (transform.childCount == 0)
                {

                    if (GetRenderersCount() > m_CurrHLODGenerate.m_BVHDivide.m_VolumeSplitRendererCount)
                        yield return SplitLodGroup();
                    else
                        yield return SetDirty();
                }
                else
                {
                    foreach (Transform child in transform)
                    {
                        if (!lodGroup)
                            yield break;

                        var lodVolume = child.GetComponent<LODVolume>();
                        if (WithinBounds(lodGroup._lodGroup, lodVolume.bounds))
                        {
                            yield return lodVolume.AddLodGroup(lodGroup);
                            break;
                        }
                        yield return null;
                    }
                }
            }
            else if (!transform.parent || !transform.parent.GetComponent<LODVolume>())
            {
                if (transform.childCount == 0 && lodGroups.Count < m_CurrHLODGenerate.m_BVHDivide.m_VolumeSplitRendererCount)
                {
                    bounds.Encapsulate(lodGroup.bounds);
                    bounds = GetCuboidBounds(bounds);

                    var lodGroupsRef = lodGroups.Select(l => l._lodGroup);
                    if (!lodGroupsRef.Contains(lodGroup._lodGroup))
                        lodGroups.Add(lodGroup);

                    yield return SetDirty();
                }
                else
                {
                    // Expand and then try to add at the larger bounds
                    var targetBounds = bounds;
                    targetBounds.Encapsulate(lodGroup.bounds);
                    targetBounds = GetCuboidBounds(targetBounds);
                    yield return Grow(targetBounds);
                    yield return transform.parent.GetComponent<LODVolume>().AddLodGroup(lodGroup);
                }
            }

        }

        public IEnumerator RemoveLodGroup(LODGroup lodGroup)
        {
            var lodGroupsRef = lodGroups.Select(l => l._lodGroup);
            if (lodGroups.RemoveAll(l => { return l._lodGroup == lodGroup; }) > 0)
            {
                if (combined != null)
                    deleteDirty = true;

                foreach (Transform child in transform)
                {
                    var lodVolume = child.GetComponent<LODVolume>();
                    if (lodVolume)
                        yield return lodVolume.RemoveLodGroup(lodGroup);

                    yield return null;
                }

                if (!transform.parent)
                    yield return ShrinkByLodGroup();

                if (!this)
                    yield break;


                if (transform.childCount == 0)
                    yield return SetDirty();
            }
        }

        IEnumerator SplitLodGroup()
        {
            Vector3 size = bounds.size;
            size.x /= k_Splits;
            size.y /= k_Splits;
            size.z /= k_Splits;

            //List<LODGroup> useRenderer = new List<LODGroup>();
            for (int i = 0; i < k_Splits; i++)
            {
                for (int j = 0; j < k_Splits; j++)
                {
                    for (int k = 0; k < k_Splits; k++)
                    {
                        var lodVolume = Create(m_CurrHLODGenerate);
                        var lodVolumeTransform = lodVolume.transform;
                        lodVolumeTransform.parent = transform;
                        var center = bounds.min + size * 0.5f + Vector3.Scale(size, new Vector3(i, j, k));
                        lodVolumeTransform.position = center;
                        lodVolume.bounds = new Bounds(center, size);

                        foreach (var l in lodGroups)
                        {

                            if (l && WithinBounds(l._lodGroup, lodVolume.bounds))
                            {
                                lodVolume.lodGroups.Add(l);
                                //useRenderer.Add(lodg);
                                yield return lodVolume.SetDirty();
                            }
                        }

                        yield return null;
                    }
                }
            }
        }
        IEnumerator Grow(Bounds targetBounds)
        {
            var direction = Vector3.Normalize(targetBounds.center - bounds.center);
            Vector3 size = bounds.size;
            size.x *= k_Splits;
            size.y *= k_Splits;
            size.z *= k_Splits;

            var corners = new Vector3[]
            {
            bounds.min,
            bounds.min + Vector3.right * bounds.size.x,
            bounds.min + Vector3.forward * bounds.size.z,
            bounds.min + Vector3.up * bounds.size.y,
            bounds.min + Vector3.right * bounds.size.x + Vector3.forward * bounds.size.z,
            bounds.min + Vector3.right * bounds.size.x + Vector3.up * bounds.size.y,
            bounds.min + Vector3.forward * bounds.size.x + Vector3.up * bounds.size.y,
            bounds.min + Vector3.right * bounds.size.x + Vector3.forward * bounds.size.z + Vector3.up * bounds.size.y
            };

            // Determine where the current volume is situated in the new expanded volume
            var best = 0f;
            var expandedVolumeCenter = bounds.min;
            foreach (var c in corners)
            {
                var dot = Vector3.Dot(c, direction);
                if (dot > best)
                {
                    best = dot;
                    expandedVolumeCenter = c;
                }
                yield return null;
            }

            var expandedVolume = Create(m_CurrHLODGenerate);
            var expandedVolumeTransform = expandedVolume.transform;
            expandedVolumeTransform.position = expandedVolumeCenter;
            expandedVolume.bounds = new Bounds(expandedVolumeCenter, size);
            expandedVolume.lodGroups = new List<UnityLODGroupFast>(lodGroups);
            var expandedBounds = expandedVolume.bounds;

            var rootHlod = transform.parent;
            transform.parent = expandedVolumeTransform;
            expandedVolumeTransform.parent = rootHlod;
            m_CurrHLODGenerate.m_RootLODVolume = expandedVolume.gameObject;

            var splitSize = bounds.size;
            var currentCenter = bounds.center;
            for (int i = 0; i < k_Splits; i++)
            {
                for (int j = 0; j < k_Splits; j++)
                {
                    for (int k = 0; k < k_Splits; k++)
                    {
                        var center = expandedBounds.min + splitSize * 0.5f + Vector3.Scale(splitSize, new Vector3(i, j, k));
                        if (Mathf.Approximately(Vector3.Distance(center, currentCenter), 0f))
                            continue; // Skip the existing LODVolume we are growing from

                        var lodVolume = Create(m_CurrHLODGenerate);
                        var lodVolumeTransform = lodVolume.transform;
                        lodVolumeTransform.parent = expandedVolumeTransform;
                        lodVolumeTransform.position = center;
                        lodVolume.bounds = new Bounds(center, splitSize);
                    }
                }
            }
        }

        IEnumerator ShrinkByLodGroup()
        {
            var populatedChildrenNodes = 0;
            foreach (Transform child in transform)
            {
                var lodVolume = child.GetComponent<LODVolume>();
                if (lodGroups != null && lodGroups.Count > 0 && lodGroups.Count(r => r != null) > 0)
                    populatedChildrenNodes++;

                yield return null;
            }

            if (populatedChildrenNodes <= 1)
            {
                var lodVolumes = GetComponentsInChildren<LODVolume>();
                LODVolume newRootVolume = null;
                if (lodVolumes.Length > 0)
                {
                    newRootVolume = lodVolumes[lodVolumes.Length - 1];
                    newRootVolume.transform.parent = null;
                }

                // Clean up child HLODs before destroying the GameObject; Otherwise, we'd leak into the scene
                foreach (var lodVolume in lodVolumes)
                {
                    if (lodVolume != newRootVolume)
                        lodVolume.CleanupHLOD();
                }
                DestroyImmediate(gameObject);

                if (newRootVolume)
                    yield return newRootVolume.ShrinkByLodGroup();
            }
        }
        IEnumerator SetDirty()
        {
            dirty = true;

            childVolumes.Clear();
            foreach (Transform child in transform)
            {
                var cv = child.GetComponent<LODVolume>();
                if (cv)
                    childVolumes.Add(cv);
            }

            yield return null;

            var lodVolumeParent = transform.parent;
            var parentLODVolume = lodVolumeParent ? lodVolumeParent.GetComponentInParent<LODVolume>() : null;
            if (parentLODVolume)
                yield return parentLODVolume.SetDirty();
        }

        static Bounds GetCuboidBounds(Bounds bounds)
        {
            // Expand bounds side lengths to maintain a cube
            var maxSize = Mathf.Max(Mathf.Max(bounds.size.x, bounds.size.y), bounds.size.z);
            var extents = Vector3.one * maxSize * 0.5f;
            bounds.center = bounds.min + extents;
            bounds.extents = extents;

            return bounds;
        }

        void OnDrawGizmos()
        {
            var depth = GetDepth(transform);
            DrawGizmos(Mathf.Max(1f - Mathf.Pow(0.9f, depth), 0.2f), GetDepthColor(depth));
        }

        void OnDrawGizmosSelected()
        {
            if (Selection.activeGameObject == gameObject)
                DrawGizmos(1f, Color.magenta);
        }

        void DrawGizmos(float alpha, Color color)
        {
            color.a = alpha;
            Gizmos.color = color;
            Gizmos.DrawWireCube(bounds.center, bounds.size);
        }

        public int ChildDeep()
        {
            int childDeep = 0;
            foreach (Transform child in transform)
            {
                var childLODVolume = child.GetComponent<LODVolume>();
                if (childLODVolume)
                {
                    int deep = childLODVolume.ChildDeep();
                    if (deep > childDeep)
                        childDeep = deep;
                }
            }
            return childDeep + 1;
        }

        /*树高，根节点，生成贴图和网格回调传回去, 是否为更新*/
        public IEnumerator UpdateHLODs(int treeHeight, GameObject hlods)
        {
            EditorUtility.DisplayProgressBar("更新HLODs", name, 0.5f);
            if (dirty)
            {
                yield return GenerateHLODInstancing(false, treeHeight, hlods);
                dirty = false;
            }
            // Process children first, since we are now combining children HLODs to make parent HLODs
            foreach (Transform child in transform)
            {
                var childLODVolume = child.GetComponent<LODVolume>();
                if (childLODVolume)
                    yield return childLODVolume.UpdateHLODs(treeHeight, hlods);

                if (!this)
                    yield break;
            }
        }

        /*获得当前节点所有renderer*/
        public Renderer[] GetAllRendererByLodGroups()
        {
            List<Renderer> renderers = new List<Renderer>();
            foreach (var lod in lodGroups)
            {
                renderers.AddRange(lod._lodGroup.GetLODs()[lod._lodGroup.lodCount - 1].renderers);
            }
            return renderers.ToArray();
        }

        /*获得当前节点renderer数量*/
        public int GetRenderersCount()
        {
            int count = 0;
            foreach (var lod in lodGroups)
            {
                count += lod.GetLODs()[lod._lodGroup.lodCount - 1].renderers.Count();
            }
            return count;
        }
        public IEnumerator GenerateHLODInstancing(bool propagateUpwards = true, int treeHeight = 2, GameObject hlods = null)
        {
            if (ChildDeep() <= treeHeight)
            {
                HashSet<Renderer> hlodRenderers = new HashSet<Renderer>();
                Renderer[] renderers = GetAllRendererByLodGroups();

                foreach (var r in renderers)
                {
                    var mr = r as MeshRenderer;
                    if (mr)
                    {
                        //hlodRenderers.Add(mr);

                        // Use the coarsest LOD if it exists
                        var mrLODGroup = mr.GetComponentInParent<LODGroup>();
                        if (mrLODGroup)
                        {
                            var mrLODs = mrLODGroup.GetLODs();
                            var maxLOD = mrLODGroup.lodCount - 1;
                            var mrLOD = mrLODs[maxLOD];
                            foreach (var lr in mrLOD.renderers)
                            {
                                if (lr && lr.GetComponent<MeshFilter>())
                                    hlodRenderers.Add(lr);
                            }
                        }
                        else if (mr.GetComponent<MeshFilter>())
                        {
                            hlodRenderers.Add(mr);
                        }
                    }

                    yield return null;
                }

                var lodRenderers = new List<Renderer>();
                CleanupHLOD();

                GameObject hlodRoot = new GameObject(transform.name);
                hlodRoot.transform.parent = hlods.transform;

                var parent = hlodRoot.transform;
                foreach (var r in hlodRenderers)
                {
                    var rendererTransform = r.transform;

                    var child = new GameObject(r.name, typeof(MeshFilter), typeof(MeshRenderer));
                    var childTransform = child.transform;
                    childTransform.SetPositionAndRotation(rendererTransform.position, rendererTransform.rotation);
                    childTransform.localScale = rendererTransform.lossyScale;
                    childTransform.SetParent(parent, true);

                    var mr = child.GetComponent<MeshRenderer>();
                    EditorUtility.CopySerialized(r.GetComponent<MeshFilter>(), child.GetComponent<MeshFilter>());
                    EditorUtility.CopySerialized(r.GetComponent<MeshRenderer>(), mr);

                    lodRenderers.Add(mr);
                }

                LOD lod = new LOD();

                var lodGroup = hlodRoot.GetComponent<LODGroup>();
                if (!lodGroup)
                    lodGroup = hlodRoot.AddComponent<LODGroup>();

                lodGroup.enabled = false;
                UnityLODGroupFast lodGroupFast = ScriptableObject.CreateInstance<UnityLODGroupFast>();
                lodGroupFast.Init(lodGroup, hlodRoot);
                this.combined = lodGroupFast;

                EditorUtility.DisplayProgressBar("合并网格和贴图", name, 0.8f);

                var batcher = (IBatcher)Activator.CreateInstance(ObjectUtils.batcherType);
                
                yield return batcher.Batch(m_CurrHLODGenerate, this);
    
                lod.renderers = hlodRoot.GetComponentsInChildren<Renderer>(false);
                lodGroup.SetLODs(new LOD[] { lod });
                //lodGroup.size += 5;//lod屏幕占比偏差

                if (propagateUpwards)
                {
                    var lodVolumeParent = transform.parent;
                    var parentLODVolume = lodVolumeParent ? lodVolumeParent.GetComponentInParent<LODVolume>() : null;
                    if (parentLODVolume)
                        yield return parentLODVolume.GenerateHLODInstancing();
                }

                hlodRoot.GetComponent<MeshRenderer>().enabled = false;
            }
        }

        void SetChildDirty(LODVolume lv)
        {
            Queue<LODVolume> queue = new Queue<LODVolume>();
            queue.Enqueue(lv);
            while (queue.Count > 0)
            {
                LODVolume lodVolume = queue.Dequeue();
                foreach (LODVolume l in lodVolume.childVolumes)
                {
                    if (l.lodGroups.Count <= 1)
                    {
                        continue;
                    }
                    l.dirty = true;
                    EditorUtility.DisplayProgressBar("设置需要更改的节点", l.name, 0.8f);
                    queue.Enqueue(l);
                }
            }
        }

        /*更新BVH后一些检查操作*/
        public IEnumerator CheckAfterBVHUpdata(int treeHeight)
        {
            Queue<LODVolume> queue = new Queue<LODVolume>();
            //将没用合批的父节点dirty设置成false
            queue.Enqueue(this);
            while (queue.Count > 0)
            {
                LODVolume lodVolume = queue.Dequeue();
                int deep = lodVolume.ChildDeep();
                if (lodVolume.combined != null || deep <= treeHeight)
                {
                    continue;
                }
                lodVolume.dirty = false;
                foreach (LODVolume lv in lodVolume.childVolumes)
                {
                    queue.Enqueue(lv);
                }
                EditorUtility.DisplayProgressBar("剔除不必要更改的节点", lodVolume.name, 0.3f);
            }

            yield return null;

            /*剔除空引用,广度遍历*/
            queue.Clear();
            queue = new Queue<LODVolume>();
            queue.Enqueue(this);
            while (queue.Count > 0)
            {
                LODVolume lodVolume = queue.Dequeue();

                lodVolume.lodGroups.RemoveAll(r =>
                {//有物体删除了
                    if (r == null)
                    {
                        if (lodVolume.combined != null)
                            lodVolume.deleteDirty = true;

                        return true;
                    }

                    return false;
                });

                if (lodVolume.deleteDirty || lodVolume.dirty)
                {
                    EditorUtility.DisplayProgressBar("设置需要更改的节点", lodVolume.name, 0.5f);
                    lodVolume.dirty = true;
                    SetChildDirty(lodVolume);
                }
                if (lodVolume.lodGroups.Count <= 1 && !lodVolume.deleteDirty)
                {
                    lodVolume.dirty = false;
                    continue;
                }
                lodVolume.deleteDirty = false;
                foreach (LODVolume lv in lodVolume.childVolumes)
                {
                    queue.Enqueue(lv);
                }
            }
        }

        /*更新BVH，对生成的BVH做后处理，广度遍历*/
        public IEnumerator UpdateRootVolume(Action<List<LODVolume>> action)
        {
            Queue<LODVolume> queue = new Queue<LODVolume>();

            //给每个子树添加ChildTreeRoot
            queue.Enqueue(this);
            while (queue.Count > 0)
            {
                LODVolume lodVolume = queue.Dequeue();
                if (lodVolume.combined != null)
                {
                    ChildTreeRoot treeRoot = lodVolume.gameObject.AddComponent<ChildTreeRoot>();
                    continue;
                }
                foreach (LODVolume lv in lodVolume.childVolumes)
                {
                    queue.Enqueue(lv);
                }
                EditorUtility.DisplayProgressBar("添加ChildTreeRoot", lodVolume.name, 0.3f);
            }

            //叶子节点，给每个UnityLODGroup做引用，同时所有子树的节点引用ChildTreeRoot
            queue.Enqueue(this);
            while (queue.Count > 0)
            {
                LODVolume lodVolume = queue.Dequeue();
                foreach (LODVolume lv in lodVolume.childVolumes)
                {
                    queue.Enqueue(lv);
                }
                if (lodVolume.childVolumes.Count < 1)
                {
                    foreach (var fast in lodVolume.lodGroups)
                    {
                        fast._lodVolume = lodVolume;
                    }
                }
                lodVolume.childTreeRoot = lodVolume.GetComponentInParent<ChildTreeRoot>();
                EditorUtility.DisplayProgressBar("引用ChildTreeRoot", lodVolume.name, 0.6f);
            }

            //设置节点深度
            int curLevelNum = 0;//当前层的节点数
            int curLevel = 1;//层数
            queue.Enqueue(this);
            while (queue.Count > 0)
            {
                curLevelNum = queue.Count;
                while(curLevelNum-- > 0)
                {
                    LODVolume lodVolume = queue.Dequeue();
                    lodVolume.deep = curLevel;

                    foreach (LODVolume lv in lodVolume.childVolumes)
                    {
                        queue.Enqueue(lv);
                    }
                    EditorUtility.DisplayProgressBar("深度设置", lodVolume.name, 0.8f);
                }
                curLevel++;
            }

            //构造后续遍历列表
            List<LODVolume> backSort = new List<LODVolume>();
            Stack<LODVolume> stack = new Stack<LODVolume>();
            stack.Push(this);
            while(stack.Count > 0)
            {
                LODVolume lodVolume = stack.Pop();
                backSort.Add(lodVolume);
                foreach (LODVolume lv in lodVolume.childVolumes)
                {
                    stack.Push(lv);
                }
                EditorUtility.DisplayProgressBar("构造后续遍历列表", lodVolume.name, 0.9f);
            }
            backSort.Reverse();

            if (action != null)
            {
                action.Invoke(backSort);
            }

            yield return null;
        }

        void CleanupHLOD()
        {
            if (combined) // Clean up old HLOD
            {
                if (combined._hlodRoot)
                {
                    var mf = combined._hlodRoot.GetComponent<MeshFilter>();
                    if (mf)
                        DestroyImmediate(mf.sharedMesh, true); // Clean up file on 

                    DestroyImmediate(combined._hlodRoot);
                }
                DestroyImmediate(combined);
            }
        }
        public static int GetDepth(Transform transform)
        {
            int count = 0;
            Transform parent = transform.parent;
            while (parent)
            {
                count++;
                parent = parent.parent;
            }

            return count;
        }

        public static Color GetDepthColor(int depth)
        {
            return k_DepthColors[depth % k_DepthColors.Length];
        }

        static bool WithinBounds(LODGroup lodGroup, Bounds bounds)
        {
            // Use this approach if we are not going to split meshes and simply put the object in one volume or another
            Vector3 v = lodGroup.transform.TransformPoint(lodGroup.localReferencePoint);
            return Mathf.Approximately(bounds.size.magnitude, 0f) || bounds.Contains(lodGroup.transform.TransformPoint(lodGroup.localReferencePoint));
        }
#endif
    }
}