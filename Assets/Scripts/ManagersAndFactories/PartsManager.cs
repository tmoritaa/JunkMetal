using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using Newtonsoft.Json.Linq;

public class PartsManager : MonoBehaviour 
{
    private static PartsManager instance;
    public static PartsManager Instance
    {
        get {
            return instance;
        }
    }

    private Dictionary<string, PartSchematic> partSchematics = new Dictionary<string, PartSchematic>();

    void Awake() {
        instance = this;
        loadHullParts();
        loadWheelParts();
        loadTurretParts();
        loadWeaponParts();
    }
    
    public T GetPartFromName<T>(string name) where T : PartSchematic {
        PartSchematic part = null;

        if (partSchematics.ContainsKey(name)) {
            part = partSchematics[name];
        }

        return (T)part;
    }

    private void loadHullParts() {
        TextAsset jsonText = Resources.Load("HullPartList") as TextAsset;

        JObject root = JObject.Parse(jsonText.text);

        foreach (var partInfo in root) {
            string name = partInfo.Key;

            JObject info = (JObject)partInfo.Value;
            float weight = info.Value<int>("weight");
            int armour = info.Value<int>("armor");
            Vector2 size = new Vector2();
            size.x = info.Value<int>("width");
            size.y = info.Value<int>("height");
            float power = info.Value<float>("engine_pow");

            HullPartSchematic part = TankParSchematictFactory.CreateHullPartSchematic(name, armour, size, power, weight);

            partSchematics.Add(part.Name, part);
        }
    }

    private void loadWheelParts() {
        TextAsset jsonText = Resources.Load("WheelPartList") as TextAsset;

        JObject root = JObject.Parse(jsonText.text);

        foreach (var partInfo in root) {
            string name = partInfo.Key;

            JObject info = (JObject)partInfo.Value;
            float weight = info.Value<int>("weight");
            float energyInc = info.Value<float>("energy_inc");
            float energyDec = info.Value<float>("energy_dec");

            // TODO: keycodes are for now. Should later be done differently using keyboard schematics
            WheelPartSchematic part = TankParSchematictFactory.CreateWheelPartSchematic(name, energyInc, energyDec, weight, KeyCode.W, KeyCode.S, KeyCode.I, KeyCode.K);

            partSchematics.Add(part.Name, part);
        }
    }

    private void loadTurretParts() {
        TextAsset jsonText = Resources.Load("TurretPartList") as TextAsset;

        JObject root = JObject.Parse(jsonText.text);

        foreach (var partInfo in root) {
            string name = partInfo.Key;

            JObject info = (JObject)partInfo.Value;
            float weight = info.Value<int>("weight");
            int armour = info.Value<int>("armor");
            float rotSpeed = info.Value<int>("rot_speed");

            List<Vector2> weaponDirs = new List<Vector2>();
            List<float> weightRestricts = new List<float>();
            foreach (JObject jo in info.Value<JArray>("weapons")) {
                Vector2 dir = new Vector2();
                dir.x = jo.Value<float>("x_dir");
                dir.y = jo.Value<float>("y_dir");
                weaponDirs.Add(dir);

                float weightRestric = jo.Value<float>("restrict");
                weightRestricts.Add(weightRestric);
            }

            // TODO: keycodes are for now. Should later be done differently using keyboard schematics
            TurretPartSchematic part = TankParSchematictFactory.CreateTurretPartSchematic(name, armour, rotSpeed, weight, weaponDirs.ToArray(), weightRestricts.ToArray(), KeyCode.T, KeyCode.Y);

            partSchematics.Add(part.Name, part);
        }
    }

    private void loadWeaponParts() {
        TextAsset jsonText = Resources.Load("WeaponPartList") as TextAsset;

        JObject root = JObject.Parse(jsonText.text);

        foreach (var partInfo in root) {
            string name = partInfo.Key;

            JObject info = (JObject)partInfo.Value;
            float weight = info.Value<int>("weight");
            float force = info.Value<int>("force");
            float range = info.Value<int>("range");
            float reloadTime = info.Value<int>("reload_time");
            int damage = info.Value<int>("damage");
            Bullet.BulletTypes bType = (Bullet.BulletTypes)Enum.Parse(typeof(Bullet.BulletTypes), info.Value<string>("bullet_type"));

            // TODO: keycodes are for now. Should later be done differently using keyboard schematics
            KeyCode code = KeyCode.P;
            if (name.Equals("Basic Weapon2")) {
                code = KeyCode.O;
            }

            WeaponPartSchematic part = TankParSchematictFactory.CreateWeaponPartSchematic(name, force, reloadTime, range, weight, bType, damage, code);

            partSchematics.Add(part.Name, part);
        }
    }
}
