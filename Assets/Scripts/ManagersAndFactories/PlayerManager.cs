﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using Newtonsoft.Json.Linq;

public class PlayerManager : MonoBehaviour 
{
    private const string PlayerInfoKey = "PlayerInfo";

    private static PlayerManager instance;
    public static PlayerManager Instance
    {
        get {
            return instance;
        }
    }

    public TankSchematic TankSchematic
    {
        get; private set;
    }

    void Awake() {
        instance = this;
    }

    public void SavePlayerInfo() {
        string weaponString = string.Empty;
        for (int i = 0; i < TankSchematic.WeaponSchematics.Length; ++i) {
            WeaponPartSchematic weaponSchem = TankSchematic.WeaponSchematics[i];
            weaponString += "\"";

            if (weaponSchem != null) {
                weaponString += weaponSchem.Name;
            }

            weaponString += "\"";

            if (i < TankSchematic.WeaponSchematics.Length - 1) {
                weaponString += ",";
            }
        }
        
        string playerInfoJson = String.Format("{{\"Parts\":{{\"Hull\":\"{0}\",\"Wheels\":\"{1}\",\"Turret\":\"{2}\",\"Weapons\":[{3}]}}}}", 
                                    TankSchematic.HullSchematic.Name,
                                    TankSchematic.WheelSchematic.Name,
                                    TankSchematic.TurretSchematic.Name,
                                    weaponString);

        PlayerPrefs.SetString(PlayerInfoKey, playerInfoJson);
    }

    public void LoadPlayerInfo() {
        string savedPlayerInfo = string.Empty;

        if (PlayerSaveExists()) {
            savedPlayerInfo = PlayerPrefs.GetString(PlayerInfoKey, string.Empty);
        } else {
            savedPlayerInfo = ((TextAsset)Resources.Load("InitialPlayerInfo")).text;
        }

        // Load player info
        JObject root = JObject.Parse(savedPlayerInfo);

        loadPlayerTankSchematic(root);
    }

    public void ClearSavedPlayerInfo() {
        PlayerPrefs.DeleteKey(PlayerInfoKey);
    }

    public bool PlayerSaveExists() {
        string playerInfoStr = PlayerPrefs.GetString(PlayerInfoKey, string.Empty);
        return !playerInfoStr.Equals(string.Empty);
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
