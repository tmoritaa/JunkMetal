using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

public class TankGOConstructor : MonoBehaviour 
{
    public GameObject HullGO
    {
        get; private set;
    }

    public List<GameObject> weaponGOs
    {
        get; private set;
    }

    private bool initialized = false;

    public void Init(TankSchematic tankSchematic) {
        HullGO = Instantiate(PartPrefabManager.Instance.GetPrefabViaName(tankSchematic.HullSchematic.Name), this.transform, false);

        weaponGOs = new List<GameObject>();
        int count = 0;
        foreach (WeaponPartSchematic weaponSchematic in tankSchematic.WeaponSchematics) {
            if (weaponSchematic != null) {
                // First initialize GO
                GameObject instance = Instantiate(PartPrefabManager.Instance.GetPrefabViaName(weaponSchematic.Name), this.transform, false);

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

        initialized = true;
    }

    public void Clear() {
        if (initialized) {
            GameObject.Destroy(HullGO);
            foreach (GameObject go in weaponGOs) {
                GameObject.Destroy(go);
            }
            weaponGOs.Clear();

            initialized = false;
        }
    }
}
