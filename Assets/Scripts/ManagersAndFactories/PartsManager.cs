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
            int enginePower = info.Value<int>("engine_pow");

            Vector2 size = new Vector2();
            size.x = info.Value<float>("size_x");
            size.y = info.Value<float>("size_y");

            float angularDrag = info.Value<float>("angular_drag");

            float energy = info.Value<float>("energy");
            float energyRefresh = info.Value<float>("energy_refresh");
            float jetImpulse = info.Value<float>("jet_impulse");
            float jetEnergyUsage = info.Value<float>("jet_energy_usage");

            List<Vector2> weaponDirs = new List<Vector2>();
            List<Vector2> weaponPos = new List<Vector2>();
            List<PartSchematic.WeaponTier> tierRestrictions = new List<PartSchematic.WeaponTier>();
            foreach (JObject jo in info.Value<JArray>("weapons")) {
                Vector2 dir = new Vector2();
                dir.x = jo.Value<float>("x_dir");
                dir.y = jo.Value<float>("y_dir");
                weaponDirs.Add(dir);

                Vector2 pos = new Vector2();
                pos.x = jo.Value<float>("x_pos");
                pos.y = jo.Value<float>("y_pos");
                weaponPos.Add(pos);

                string weaponTier = jo.Value<string>("tier");

                PartSchematic.WeaponTier tier = PartSchematic.WeaponTier.Light;
                if (weaponTier.Equals("L")) {
                    tier = PartSchematic.WeaponTier.Light;
                } else if (weaponTier.Equals("M")) {
                    tier = PartSchematic.WeaponTier.Medium;
                } else if (weaponTier.Equals("H")) {
                    tier = PartSchematic.WeaponTier.Heavy;
                }
                tierRestrictions.Add(tier);
            }

            HullPartSchematic part = TankParSchematictFactory.CreateHullPartSchematic(name, armour, enginePower, size, weight, angularDrag, energy, energyRefresh, jetImpulse, jetEnergyUsage, weaponDirs.ToArray(), weaponPos.ToArray(), tierRestrictions.ToArray());

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
            
            float reloadTime = info.Value<float>("reload_time");
            int energyUsage = info.Value<int>("energy_usage");
            string tierStr = info.Value<string>("tier");
            PartSchematic.WeaponTier tier = PartSchematic.WeaponTier.Light;
            if (tierStr.Equals("L")) {
                tier = PartSchematic.WeaponTier.Light;
            } else if (tierStr.Equals("M")) {
                tier = PartSchematic.WeaponTier.Medium;
            } else if (tierStr.Equals("H")) {
                tier = PartSchematic.WeaponTier.Heavy;
            }

            Bullet.BulletTypes bType = (Bullet.BulletTypes)Enum.Parse(typeof(Bullet.BulletTypes), info.Value<string>("bullet_type"));

            Dictionary<string, object> bulletInfoDict = parseBulletInfoJson(bType, info.Value<JObject>("bullet_info"));

            WeaponPartSchematic part = TankParSchematictFactory.CreateWeaponPartSchematic(name, reloadTime, energyUsage, tier, bType, bulletInfoDict);

            schematics.Add(part.Name, part);
        }

        partSchematicDics.Add(PartSchematic.PartType.Weapon, schematics);
    }

    private Dictionary<string, object> parseBulletInfoJson(Bullet.BulletTypes bType, JObject info) {
        Dictionary<string, object> bulletInfos = new Dictionary<string, object>();

        bulletInfos.Add("shoot_impulse", info.Value<float>("shoot_impulse"));
        bulletInfos.Add("recoil_impulse", info.Value<float>("shoot_recoil_impulse"));
        bulletInfos.Add("hit_impulse", info.Value<float>("hit_impulse"));
        bulletInfos.Add("damage", info.Value<int>("damage"));
        bulletInfos.Add("range", info.Value<float>("range"));

        if (bType == Bullet.BulletTypes.MissileCluster) {
            bulletInfos.Add("num_missiles", info.Value<int>("num_missiles"));
            bulletInfos.Add("missile_range", info.Value<float>("missile_range"));
            bulletInfos.Add("missile_shoot_time", info.Value<float>("missile_shoot_time"));
            bulletInfos.Add("missile_init_impulse", info.Value<float>("missile_init_impulse"));
            bulletInfos.Add("missile_fire_impulse", info.Value<float>("missile_fire_impulse"));
            bulletInfos.Add("missile_trigger_time", info.Value<float>("missile_trigger_time"));
        }

        return bulletInfos;
    }
}
