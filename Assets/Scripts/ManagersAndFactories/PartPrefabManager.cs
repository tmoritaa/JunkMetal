using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

public class PartPrefabManager : MonoBehaviour 
{
    private static PartPrefabManager instance;
    public static PartPrefabManager Instance
    {
        get {
            return instance;
        }
    }

    [SerializeField]
    private StrPrefabDictScriptableObject strPrefabDict;

    private Dictionary<string, GameObject> prefabDict
    {
        get {
            return strPrefabDict.StrPrefabsDict;
        }
    }

    void Awake() {
        instance = this;
    }
    
    public GameObject GetPrefabViaKey(string key) {
        return prefabDict[key];
    }
}