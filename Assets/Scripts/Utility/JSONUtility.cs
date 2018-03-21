using System;
using System.Collections.Generic;
using System.Linq;

using Newtonsoft.Json.Linq;

public class JSONUtility
{
    public static TankSchematic LoadTankSchematic(JObject tankSchemInfo) {
        JObject parts = tankSchemInfo;

        string hullName = parts.Value<string>("Hull");
        HullPartSchematic hull = (HullPartSchematic)PartsManager.Instance.GetPartFromName(PartSchematic.PartType.Hull, hullName);

        JArray weapons = parts.Value<JArray>("Weapons");
        List<WeaponPartSchematic> weaponsList = new List<WeaponPartSchematic>();
        foreach (string str in weapons) {
            if (!str.Equals(string.Empty)) {
                weaponsList.Add((WeaponPartSchematic)PartsManager.Instance.GetPartFromName(PartSchematic.PartType.Weapon, str));
            } else {
                weaponsList.Add(null);
            }
        }

        return new TankSchematic(hull, weaponsList.ToArray());
    }
}
