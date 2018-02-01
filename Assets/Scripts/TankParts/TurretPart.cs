using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

public class TurretPart
{
    public float Weight
    {
        get; private set;
    }

    public int Armour
    {
        get; private set;
    }

    private Tank owningTank;

    private float rotPerTimeStep;

    private KeyCode leftTurnKey;
    private KeyCode rightTurnKey;

    private float rotDir = 0;

    public Vector2 ForwardVec
    {
        get; private set;
    }

    private List<WeaponPart> weapons = new List<WeaponPart>();

    public TurretPart(Tank _tank, float _rotPerTimestep, float _weight, KeyCode _leftTurnKey, KeyCode _rightTurnKey) {
        owningTank = _tank;
        rotPerTimeStep = _rotPerTimestep;
        leftTurnKey = _leftTurnKey;
        rightTurnKey = _rightTurnKey;
        Weight = _weight;
        ForwardVec = new Vector2(0, 1);
    }

    public void AddWeapon(WeaponPart weapon) {
        weapons.Add(weapon);
    }

    public float GetWeightOfWeapons() {
        float weight = 0;

        foreach (WeaponPart weapon in weapons) {
            weight += weapon.Weight;
        }

        return weight;
    }

    public void HandleInput() {
        if (Input.GetKey(leftTurnKey)) {
            rotDir = 1.0f;
        } else if (Input.GetKey(rightTurnKey)) {
            rotDir = -1.0f;
        } else {
            rotDir = 0;
        }

        foreach (WeaponPart weapon in weapons) {
            weapon.HandleInput();
        }
    }

    public void PerformFixedUpdate() {
        if (Mathf.Abs(rotPerTimeStep) > 0 && Math.Abs(rotDir) > 0) {
            float angle = rotDir * rotPerTimeStep;
            owningTank.TurretGO.transform.Rotate(new Vector3(0, 0, angle));
            ForwardVec = ForwardVec.Rotate(angle);
        }

        foreach (WeaponPart weapon in weapons) {
            weapon.PerformFixedUpdate();
        }
    }
}
