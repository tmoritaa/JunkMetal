﻿using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

public partial class Tank : MonoBehaviour
{
    [SerializeField]
    private Rigidbody2D body;
    public Rigidbody2D Body
    {
        get {
            return body;
        }
    }

    [SerializeField]
    private GameObject leftWheelGO;
    public GameObject LeftWheelGO
    {
        get {
            return leftWheelGO;
        }
    }

    [SerializeField]
    private GameObject rightWheelGO;
    public GameObject RightWheelGO
    {
        get {
            return rightWheelGO;
        }
    }

    [SerializeField]
    private BoxCollider2D boxCollider;

    [SerializeField]
    private GameObject turretGO;
    public GameObject TurretGO
    {
        get {
            return turretGO;
        }
    }

    [SerializeField]
    private GameObject hullGO;

    public HullPart Hull
    {
        get; private set;
    }

    public TurretPart Turret
    {
        get; private set;
    }

    public int MaxArmour
    {
        get; private set;
    }

    public int CurArmour
    {
        get; private set;
    }

    private bool initialized = false;

    private void FixedUpdate() {
        if (initialized) {
            handleMovement();
            Turret.PerformFixedUpdate();
        }
    }

    public void Init(TankSchematic tankSchematic) {
        Hull = new HullPart(tankSchematic.HullSchematic);
        Turret = new TurretPart(tankSchematic.TurretSchematic);

        int count = 0;
        foreach(WeaponPartSchematic weaponSchematic in tankSchematic.WeaponSchematics) {
            if (weaponSchematic != null) {
                WeaponPart part = new WeaponPart(weaponSchematic);
                Turret.AddWeaponAtIdx(part, count);
            }
            count += 1;
        }

        Turret.SetOwner(this);

        Vector2 hullSize = Hull.Schematic.Size;
        boxCollider.size = hullSize;
        hullGO.GetComponent<RectTransform>().sizeDelta = new Vector2(hullSize.x, hullSize.y);

        leftWheelGO.transform.localPosition = new Vector3(-hullSize.x / 2f, 0, 0);
        rightWheelGO.transform.localPosition = new Vector3(hullSize.x / 2f, 0, 0);

        float totalWeight = calculateTotalWeight() / 10f;
        this.body.mass = totalWeight;

        MaxArmour = calculateTotalArmour();

        ResetState();

        initialized = true;
    }

    public void ResetState() {
        CurArmour = MaxArmour;
        ResetMovement();
    }

    public void ResetMovement() {
        Hull.Reset();
        Turret.Reset();
    }

    public void Damage(int damage) {
        CurArmour -= damage;
        CurArmour = Mathf.Max(0, CurArmour);

        if (CurArmour <= 0) {
            Dead();
        }
    }

    public void Dead() {
        Debug.Log("Tank is dead");
        CombatManager.Instance.DeathOccurred(this);
    }

    public Vector2 GetForwardVec() {
        return new Vector2(0, 1).Rotate(body.rotation);
    }

    public Vector2 GetBackwardVec() {
        return new Vector2(0, -1).Rotate(body.rotation);
    }

    private int calculateTotalArmour() {
        int armour = 0;

        armour = Hull.Schematic.Armour;
        armour += Turret.Schematic.Armour;

        return armour;
    }

    private float calculateTotalWeight() {
        float weight = 0;

        weight += Hull.Schematic.Weight;
        weight += Turret.Schematic.Weight;

        return weight;
    }

    private Vector2 calcAppliedLinearForce(Vector2 forwardVec, float leftCurPower, float rightCurPower) {
        Vector3 leftForceVec = forwardVec * leftCurPower * Hull.Schematic.EnergyPower / 2f;
        Vector3 rightForceVec = forwardVec * rightCurPower * Hull.Schematic.EnergyPower / 2f;

        Vector2 linearForce = rightForceVec + leftForceVec;

        return linearForce;
    }

    private float calcAppliedTorque(Vector2 forwardVec, float leftCurPower, float rightCurPower) {
        Vector3 leftForceVec = forwardVec * leftCurPower * Hull.Schematic.EnergyPower / 2f;
        Vector3 rightForceVec = forwardVec * rightCurPower * Hull.Schematic.EnergyPower / 2f;

        float width = Hull.Schematic.Size.x;
        Vector3 rightR = new Vector2(width / 2f, 0).Rotate(body.rotation);
        Vector3 rightTorque = Vector3.Cross(rightR, rightForceVec);

        Vector3 leftR = new Vector2(-width / 2f, 0).Rotate(body.rotation);
        Vector3 leftTorque = Vector3.Cross(leftR, leftForceVec);

        return rightTorque.z + leftTorque.z;
    }

    private void handleMovement() {
        Vector2 linearForce = calcAppliedLinearForce(GetForwardVec(), Hull.LeftCurPower, Hull.RightCurPower);
        body.AddForce(linearForce);

        float torque = calcAppliedTorque(GetForwardVec(), Hull.LeftCurPower, Hull.RightCurPower);
        body.AddTorque(torque, ForceMode2D.Force);
    }
}
