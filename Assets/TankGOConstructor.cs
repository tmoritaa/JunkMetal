using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

public class TankGOConstructor : MonoBehaviour 
{
    public GameObject LeftWheelGO
    {
        get; private set;
    }

    public GameObject RightWheelGO
    {
        get; private set;
    }

    public GameObject HullGO
    {
        get; private set;
    }

    public List<GameObject> weaponGOs
    {
        get; private set;
    }

    public void Init(TankSchematic tankSchematic) {
        HullPrefabInfo info = PartPrefabManager.Instance.GetHullPrefabInfoViaHullName(tankSchematic.HullSchematic.Name);

        LeftWheelGO = Instantiate(info.WheelPrefab, this.transform, false);
        RightWheelGO = Instantiate(info.WheelPrefab, this.transform, false);
        HullGO = Instantiate(info.HullPrefab, this.transform, false);

        weaponGOs = new List<GameObject>();
        int count = 0;
        foreach (WeaponPartSchematic weaponSchematic in tankSchematic.WeaponSchematics) {
            if (weaponSchematic != null) {
                // First initialize GO
                GameObject instance = Instantiate(PartPrefabManager.Instance.GetWeaponPrefabViaWeaponName(weaponSchematic.Name), this.transform, false);

                RectTransform rect = instance.GetComponent<RectTransform>();
                rect.pivot = new Vector2(0.5f, 0);
                rect.transform.localPosition = tankSchematic.HullSchematic.OrigWeaponPos[count];

                Vector2 dir = tankSchematic.HullSchematic.OrigWeaponDirs[count];
                float angle = Vector2.Angle(new Vector2(0, 1), dir);
                rect.transform.Rotate(new Vector3(0, 0, angle));

                weaponGOs.Add(instance);
            }
            count += 1;
        }

        Vector2 hullSize = HullGO.GetComponent<RectTransform>().sizeDelta;
        LeftWheelGO.transform.localPosition = new Vector3(-hullSize.x / 2f, 0, 0);
        RightWheelGO.transform.localPosition = new Vector3(hullSize.x / 2f, 0, 0);
    }
}
