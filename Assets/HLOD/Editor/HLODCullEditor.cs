
using UnityEditor;
namespace Unity.HLOD
{
    [CustomEditor(typeof(HLODCull))]
    [CanEditMultipleObjects]
    public class HLODCullEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            HLODCull hlodCull = (HLODCull)target;

            if (hlodCull.m_RootVolume == null)
            {
                hlodCull.m_RootVolume = hlodCull.GetComponent<LODVolume>();
            }

        }
    }
}