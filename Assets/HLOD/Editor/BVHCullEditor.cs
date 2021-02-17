using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Unity.HLOD
{
    public static class BVHCullEditor
	{
#if UNITY_EDITOR
		/*剔除*/
		public static bool CullByMesh(this BVHDivide m_BVHDivide, Transform parent)
		{
			bool result = false;
			result = CullByMeshOfBound(m_BVHDivide, parent);

			return result;
		}
		/*根据包围盒大小，将物体从ScenenLod剔除计算*/
		public static bool CullByMeshOfBound(BVHDivide m_BVHDivide, Transform parent)
		{
			Bounds bounds = new Bounds();
			if (m_BVHDivide.m_CalboundOfChild)
			{
				Vector3 center = Vector3.zero;
				Renderer[] renders = parent.GetComponentsInChildren<Renderer>();
				foreach (Renderer child in renders)
				{
					center += child.bounds.center;
				}
				center /= renders.Length;
				bounds = new Bounds(center, Vector3.zero);
				foreach (Renderer child in renders)
				{
					bounds.Encapsulate(child.bounds);
				}
			}
			else
			{
				Renderer render = parent.GetComponent<Renderer>();
				bounds = render.bounds;
			}
			
			float size = Mathf.Max(Mathf.Max(bounds.size.x, bounds.size.y), bounds.size.z);
			return size >= m_BVHDivide.m_BoundCondtionDia;
		}
#endif
	}
}
