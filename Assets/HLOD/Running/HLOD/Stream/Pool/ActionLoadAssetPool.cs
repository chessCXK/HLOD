using System.Collections;
using System.Collections.Generic;
using Unity.HLOD;
using UnityEngine;

public class ActionLoadAssetPool : MonoBehaviour
{
    // Start is called before the first frame update
    void Awake()
    {
        PoolBaseLoadAsset.ActionPoolBaseLoadAsset();
    }
}
