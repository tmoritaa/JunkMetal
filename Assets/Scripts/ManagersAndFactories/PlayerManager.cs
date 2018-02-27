using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using Newtonsoft.Json.Linq;

public class PlayerManager : MonoBehaviour 
{
    private static PlayerManager instance;
    public static PlayerManager Instance
    {
        get {
            return instance;
        }
    }

    private const string PlayerInfoKey = "PlayerInfo";

    public TankSchematic TankSchematic
    {
        get; private set;
    }

    void Awake() {
        instance = this;

        loadPlayerInfo();
    }

    private void loadPlayerInfo() {
        string savedPlayerInfo = PlayerPrefs.GetString(
            PlayerInfoKey,
            string.Empty);

        if (savedPlayerInfo.Equals(string.Empty)) {
            savedPlayerInfo = ((TextAsset)Resources.Load("InitialPlayerInfo")).text;
        }

        // Load player info
        JObject root = JObject.Parse(savedPlayerInfo);

        loadPlayerTankSchematic(root);
    }

    private void loadPlayerTankSchematic(JObject root) {
        JObject parts = root.Value<JObject>("Parts");

        string hullName = parts.Value<string>("Hull");
        HullPartSchematic hull = PartsManager.Instance.GetPartFromName<HullPartSchematic>(hullName);
        
        string turretName = parts.Value<string>("Turret");
        TurretPartSchematic turret = PartsManager.Instance.GetPartFromName<TurretPartSchematic>(turretName);

        string wheelsName = parts.Value<string>("Wheels");
        WheelPartSchematic wheels = PartsManager.Instance.GetPartFromName<WheelPartSchematic>(wheelsName);

        JArray weapons = parts.Value<JArray>("Weapons");
        List<WeaponPartSchematic> weaponsList = new List<WeaponPartSchematic>();
        foreach (string str in weapons) {
            if (!str.Equals(string.Empty)) {
                weaponsList.Add(PartsManager.Instance.GetPartFromName<WeaponPartSchematic>(str));
            } else {
                weaponsList.Add(null);
            }
        }

        TankSchematic = new TankSchematic(hull, turret, wheels, weaponsList.ToArray());
    }
}
