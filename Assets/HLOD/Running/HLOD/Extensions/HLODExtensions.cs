using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Unity.HLOD
{
    public static class HLODExtensions
    {
        public static float GetWorldSpaceScale(Transform t)
        {
            var scale = t.lossyScale;
            float largestAxis = Mathf.Abs(scale.x);
            largestAxis = Mathf.Max(largestAxis, Mathf.Abs(scale.y));
            largestAxis = Mathf.Max(largestAxis, Mathf.Abs(scale.z));
            return largestAxis;
        }
        public static float DistanceToRelativeHeight(Camera camera, float distance, float size)
        {
            if (camera.orthographic)
                return size * 0.5F / camera.orthographicSize * UnityEngine.QualitySettings.lodBias;

            var halfAngle = Mathf.Tan(Mathf.Deg2Rad * camera.fieldOfView * 0.5F);
            var relativeHeight = size * 0.5F / (distance * halfAngle);
            return relativeHeight * UnityEngine.QualitySettings.lodBias;
        }
        public static float GetWorldSpaceSize(this LODGroup lodGroup)
        {
            return GetWorldSpaceScale(lodGroup.transform) * lodGroup.size;
        }
        public static Vector3 GetWorldSpaceCenter(this LODGroup lodGroup)
        {
            return lodGroup.transform.TransformPoint(lodGroup.localReferencePoint);
        }
        // 计算所有Renderer的包围盒
        public static Bounds GetBounds(Transform transform)
        {
            var b = new Bounds(transform.position, Vector3.zero);
            var renderers = transform.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                var r = renderers[i];
                if (r.bounds.size != Vector3.zero)
                    b.Encapsulate(r.bounds);
            }

            // As a fallback when there are no bounds, collect all transform positions
            if (b.size == Vector3.zero)
            {
                var transforms = transform.GetComponentsInChildren<Transform>();
                foreach (var t in transforms)
                    b.Encapsulate(t.position);
            }

            return b;
        }
        public static Bounds GetBounds(this LODGroup lodGroup)
        {
            return GetBounds(lodGroup.transform);
        }
    }
}