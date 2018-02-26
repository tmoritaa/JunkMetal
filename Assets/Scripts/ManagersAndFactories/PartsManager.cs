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
            int weight = info.Value<int>("weight");
            int armour = info.Value<int>("armor");
            Vector2 size = new Vector2();
            size.x = info.Value<float>("width");
            size.y = info.Value<float>("height");
            int power = info.Value<int>("engine_pow");

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
            int weight = info.Value<int>("weight");
            float energyInc = info.Value<float>("energy_inc");
            float energyDec = info.Value<float>("energy_dec");

            // TODO: keycodes are for now. Should later be done differently using keyboard schematics
            WheelPartSchematic part = TankParSchematictFactory.CreateWheelPartSchematic(name, energyInc, energyDec, weight);

            partSchematics.Add(part.Name, part);
        }
    }

    private void loadTurretParts() {
        TextAsset jsonText = Resources.Load("TurretPartList") as TextAsset;

        JObject root = JObject.Parse(jsonText.text);

        foreach (var partInfo in root) {
            string name = partInfo.Key;

            JObject info = (JObject)partInfo.Value;
            int weight = info.Value<int>("weight");
            int armour = info.Value<int>("armor");
            float rotSpeed = info.Value<int>("rot_speed");

            List<Vector2> weaponDirs = new List<Vector2>();
            List<Vector2> weaponFireOffset = new List<Vector2>();
            List<int> weightRestricts = new List<int>();
            foreach (JObject jo in info.Value<JArray>("weapons")) {
                Vector2 dir = new Vector2();
                dir.x = jo.Value<float>("x_dir");
                dir.y = jo.Value<float>("y_dir");
                weaponDirs.Add(dir);

                Vector2 posOffset = new Vector2();
                posOffset.x = jo.Value<float>("fire_x_offset");
                posOffset.y = jo.Value<float>("fire_y_offset");
                weaponFireOffset.Add(posOffset);

                int weightRestric = jo.Value<int>("restrict");
                weightRestricts.Add(weightRestric);
            }

            TurretPartSchematic part = TankParSchematictFactory.CreateTurretPartSchematic(name, armour, rotSpeed, weight, weaponDirs.ToArray(), weaponFireOffset.ToArray(), weightRestricts.ToArray());

            partSchematics.Add(part.Name, part);
        }
    }

    private void loadWeaponParts() {
        TextAsset jsonText = Resources.Load("WeaponPartList") as TextAsset;

        JObject root = JObject.Parse(jsonText.text);

        foreach (var partInfo in root) {
            string name = partInfo.Key;

            JObject info = (JObject)partInfo.Value;
            int weight = info.Value<int>("weight");
            float shootImpulse = info.Value<float>("shoot_impulse");
            float backforce = info.Value<float>("back_force");
            float range = info.Value<float>("range");
            float reloadTime = info.Value<float>("reload_time");
            int damage = info.Value<int>("damage");
            Bullet.BulletTypes bType = (Bullet.BulletTypes)Enum.Parse(typeof(Bullet.BulletTypes), info.Value<string>("bullet_type"));

            WeaponPartSchematic part = TankParSchematictFactory.CreateWeaponPartSchematic(name, shootImpulse, backforce, reloadTime, range, weight, bType, damage);

            partSchematics.Add(part.Name, part);
        }
    }
}
