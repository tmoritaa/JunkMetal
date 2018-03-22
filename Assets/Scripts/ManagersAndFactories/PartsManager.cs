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

    private Dictionary<PartSchematic.PartType, Dictionary<string, PartSchematic>> partSchematicDics = new Dictionary<PartSchematic.PartType, Dictionary<string, PartSchematic>>();

    void Awake() {
        instance = this;
        loadHullParts();
        loadWeaponParts();
    }
    
    public PartSchematic GetPartFromName(PartSchematic.PartType pType, string name) {
        PartSchematic part = null;

        Dictionary<string, PartSchematic> partSchematics = partSchematicDics[pType];

        if (partSchematics.ContainsKey(name)) {
            part = partSchematics[name];
        }

        return part;
    }

    public PartSchematic[] GetPartsOfType(PartSchematic.PartType pType) {
        Dictionary<string, PartSchematic> partSchematics = partSchematicDics[pType];

        List<PartSchematic> parts = new List<PartSchematic>();
        foreach (PartSchematic part in partSchematics.Values) {
            parts.Add(part);
        }

        return parts.ToArray();
    }

    private void loadHullParts() {
        Dictionary<string, PartSchematic> schematics = new Dictionary<string, PartSchematic>();

        TextAsset jsonText = Resources.Load("HullPartList") as TextAsset;

        JObject root = JObject.Parse(jsonText.text);

        foreach (var partInfo in root) {
            string name = partInfo.Key;

            JObject info = (JObject)partInfo.Value;
            int weight = info.Value<int>("weight");
            int armour = info.Value<int>("armor");
            int power = info.Value<int>("engine_pow");

            Vector2 size = new Vector2();
            size.x = info.Value<float>("size_x");
            size.y = info.Value<float>("size_y");

            List<Vector2> weaponDirs = new List<Vector2>();
            List<Vector2> weaponPos = new List<Vector2>();
            List<int> weightRestricts = new List<int>();
            foreach (JObject jo in info.Value<JArray>("weapons")) {
                Vector2 dir = new Vector2();
                dir.x = jo.Value<float>("x_dir");
                dir.y = jo.Value<float>("y_dir");
                weaponDirs.Add(dir);

                Vector2 pos = new Vector2();
                pos.x = jo.Value<float>("x_pos");
                pos.y = jo.Value<float>("y_pos");
                weaponPos.Add(pos);

                int weightRestric = jo.Value<int>("restrict");
                weightRestricts.Add(weightRestric);
            }

            HullPartSchematic part = TankParSchematictFactory.CreateHullPartSchematic(name, armour, power, size, weight, weaponDirs.ToArray(), weaponPos.ToArray(), weightRestricts.ToArray());

            schematics.Add(part.Name, part);
        }

        partSchematicDics.Add(PartSchematic.PartType.Hull, schematics);
    }

    private void loadWeaponParts() {
        Dictionary<string, PartSchematic> schematics = new Dictionary<string, PartSchematic>();

        TextAsset jsonText = Resources.Load("WeaponPartList") as TextAsset;

        JObject root = JObject.Parse(jsonText.text);

        foreach (var partInfo in root) {
            string name = partInfo.Key;

            JObject info = (JObject)partInfo.Value;
            int weight = info.Value<int>("weight");
            float shootImpulse = info.Value<float>("shoot_impulse");
            float recoilImpulse = info.Value<float>("shoot_recoil_impulse");
            float range = info.Value<float>("range");
            float reloadTime = info.Value<float>("reload_time");
            int damage = info.Value<int>("damage");
            Bullet.BulletTypes bType = (Bullet.BulletTypes)Enum.Parse(typeof(Bullet.BulletTypes), info.Value<string>("bullet_type"));

            WeaponPartSchematic part = TankParSchematictFactory.CreateWeaponPartSchematic(name, shootImpulse, recoilImpulse, reloadTime, range, weight, bType, damage);

            schematics.Add(part.Name, part);
        }

        partSchematicDics.Add(PartSchematic.PartType.Weapon, schematics);
    }
}
