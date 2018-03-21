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
    private Dictionary<string, GameObject> weaponNameToPrefabDict = new Dictionary<string, GameObject>();

    void Awake() {
        instance = this;

        loadHullPrefabInfosAndWeaponDict();
    }
    
    public HullPrefabInfo GetHullPrefabInfoViaHullName(string hullName) {
        return hullNameToInfoDict[hullName];
    }

    public GameObject GetWeaponPrefabViaWeaponName(string weaponName) {
        return weaponNameToPrefabDict[weaponName];
    }

    private void loadHullPrefabInfosAndWeaponDict() {
        TextAsset jsonText = Resources.Load("PartPrefabConfs") as TextAsset;

        JObject root = JObject.Parse(jsonText.text);

        foreach (var info in root.Value<JObject>("hulls")) {
            string name = info.Key;
            JObject obj = (JObject)info.Value;

            GameObject hullPrefab = prefabDict[obj.Value<string>("hull")];
            GameObject wheelPrefab = prefabDict[obj.Value<string>("wheel")];

            hullNameToInfoDict.Add(name, new HullPrefabInfo(hullPrefab, wheelPrefab));
        }

        foreach (var info in root.Value<JObject>("weapons")) {
            string name = info.Key;
            GameObject prefab = prefabDict[(string)info.Value];

            weaponNameToPrefabDict.Add(name, prefab);
        }
    }
}