using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using Newtonsoft.Json.Linq;

public class EnemyInfoManager : MonoBehaviour
{
    private static EnemyInfoManager instance;
    public static EnemyInfoManager Instance
    {
        get {
            return instance;
        }
    }

    private Dictionary<string, EnemyInfo> enemyInfoDict = new Dictionary<string, EnemyInfo>();

    void Awake() {
        instance = this;

        loadEnemyInfo();
    }

    private void loadEnemyInfo() {
        TextAsset jsonText = Resources.Load("EnemyConf") as TextAsset;

        JObject root = JObject.Parse(jsonText.text);

        foreach (var info in root) {
            string name = info.Key;
            JObject jObj = (JObject)info.Value;

            TankSchematic schem = JSONUtility.LoadTankSchematic(jObj.Value<JObject>("Tank"));

            enemyInfoDict.Add(name, new EnemyInfo(name, schem));
        }
    }
}
