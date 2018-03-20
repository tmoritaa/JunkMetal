using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using Newtonsoft.Json.Linq;

public class HullPrefabInfo
{
    public GameObject HullPrefab
    {
        get; private set;
    }

    public GameObject WheelPrefab
    {
        get; private set;
    }

    public HullPrefabInfo(GameObject hullPrefab, GameObject wheelPrefab) {
        HullPrefab = hullPrefab;
        WheelPrefab = wheelPrefab;
    }
}

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

    private Dictionary<string, HullPrefabInfo> hullNameToInfoDict = new Dictionary<string, HullPrefabInfo>();

    void Awake() {
        instance = this;

        loadHullPrefabInfos();
    }
    
    public HullPrefabInfo GetPrefabInfoViaHullName(string hullName) {
        return hullNameToInfoDict[hullName];
    }

    private void loadHullPrefabInfos() {
        TextAsset jsonText = Resources.Load("PartPrefabConfs") as TextAsset;

        JObject root = JObject.Parse(jsonText.text);

        JObject hullConfs = root.Value<JObject>("Hulls");

        foreach (var info in hullConfs) {
            string name = info.Key;
            JObject obj = (JObject)info.Value;

            GameObject hullPrefab = prefabDict[obj.Value<string>("Hull")];
            GameObject wheelPrefab = prefabDict[obj.Value<string>("Wheel")];

            hullNameToInfoDict.Add(name, new HullPrefabInfo(hullPrefab, wheelPrefab));
        }
    }
}