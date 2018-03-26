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

    public float WheelXOffset
    {
        get; private set;
    }

    public HullPrefabInfo(GameObject hullPrefab, GameObject wheelPrefab, float xOffset) {
        HullPrefab = hullPrefab;
        WheelPrefab = wheelPrefab;
        WheelXOffset = xOffset;
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

    private Dictionary<string, GameObject> nameToPrefabDict = new Dictionary<string, GameObject>();

    private Dictionary<string, HullPrefabInfo> nameToHullPrefabInfo = new Dictionary<string, HullPrefabInfo>();

    void Awake() {
        instance = this;

        loadHullPrefabInfosAndWeaponDict();
    }

    public GameObject GetPrefabViaName(string name) {
        return nameToPrefabDict[name];
    }

    public HullPrefabInfo GetHullPrefabInfoViaName(string name) {
        return nameToHullPrefabInfo[name];
    }

    private void loadHullPrefabInfosAndWeaponDict() {
        TextAsset jsonText = Resources.Load("PartPrefabConfs") as TextAsset;

        JObject root = JObject.Parse(jsonText.text);

        foreach (var info in root.Value<JObject>("hulls")) {
            string name = info.Key;

            JObject jObj = (JObject)info.Value;

            GameObject hullPrefab = prefabDict[jObj.Value<string>("hull")];
            GameObject wheelPrefab = prefabDict[jObj.Value<string>("wheels")];
            float offset = jObj.Value<float>("wheel_offset");
            
            nameToHullPrefabInfo.Add(name, new HullPrefabInfo(hullPrefab, wheelPrefab, offset));
        }

        foreach (var info in root.Value<JObject>("weapons")) {
            string name = info.Key;
            GameObject prefab = prefabDict[(string)info.Value];

            nameToPrefabDict.Add(name, prefab);
        }
    }
}