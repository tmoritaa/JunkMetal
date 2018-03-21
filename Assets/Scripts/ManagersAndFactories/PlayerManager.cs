using System;
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

    public void UpdateTankSchematic(TankSchematic schematic) {
        TankSchematic = schematic;
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
        
        string playerInfoJson = String.Format("{{\"Tank\":{{\"Hull\":\"{0}\",\"Weapons\":[{1}]}}}}", 
                                    TankSchematic.HullSchematic.Name,
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
        TankSchematic = JSONUtility.LoadTankSchematic(root.Value<JObject>("Tank"));
    }
}
