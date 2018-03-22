using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using Newtonsoft.Json.Linq;

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

    private Dictionary<string, GameObject> nameToPrefabDict = new Dictionary<string, GameObject>();

    void Awake() {
        instance = this;

        loadHullPrefabInfosAndWeaponDict();
    }

    public GameObject GetPrefabViaName(string name) {
        return nameToPrefabDict[name];
    }

    private void loadHullPrefabInfosAndWeaponDict() {
        TextAsset jsonText = Resources.Load("PartPrefabConfs") as TextAsset;

        JObject root = JObject.Parse(jsonText.text);

        foreach (var info in root.Value<JObject>("hulls")) {
            string name = info.Key;
            GameObject prefab = prefabDict[(string)info.Value];
           
            nameToPrefabDict.Add(name, prefab);
        }

        foreach (var info in root.Value<JObject>("weapons")) {
            string name = info.Key;
            GameObject prefab = prefabDict[(string)info.Value];

            nameToPrefabDict.Add(name, prefab);
        }
    }
}