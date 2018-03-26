using System;
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
    private BoxCollider2D boxCollider;

    [SerializeField]
    private TankGOConstructor tankGOConstructor;
    public TankGOConstructor TankGOConstructor
    {
        get {
            return tankGOConstructor;
        }
    }

    public HullPart Hull
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

    public TankStateInfo StateInfo
    {
        get {
            return new TankStateInfo(this);
        }
    }

    private bool initialized = false;

    private void FixedUpdate() {
        if (initialized) {
            handleMovement();
            Hull.PerformFixedUpdate();
        }
    }

    public void Init(TankSchematic tankSchematic) {
        tankGOConstructor.Init(tankSchematic);

        Hull = new HullPart(tankSchematic.HullSchematic);
        
        int count = 0;
        int validCount = 0;
        foreach(WeaponPartSchematic weaponSchematic in tankSchematic.WeaponSchematics) {
            if (weaponSchematic != null) {
                RectTransform rect = tankGOConstructor.weaponGOs[validCount].GetComponent<RectTransform>();
                Vector2 dir = tankSchematic.HullSchematic.OrigWeaponDirs[count];

                Vector2 weaponFireOffset = dir.normalized * rect.sizeDelta.y;

                // Then create weapon representation and add to hull
                WeaponPart part = new WeaponPart(weaponSchematic, weaponFireOffset, this);
                Hull.AddWeaponAtIdx(part, count);

                validCount += 1;
            }
            count += 1;
        }

        boxCollider.size = tankSchematic.HullSchematic.Size;

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
        CombatHandler.Instance.DeathOccurred(this);
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

        return armour;
    }

    private float calculateTotalWeight() {
        float weight = 0;

        weight += Hull.Schematic.Weight;

        return weight;
    }

    private void handleMovement() {
        TankStateInfo stateInfo = StateInfo;

        Vector2 linearForce = TankUtility.CalcAppliedLinearForce(stateInfo);
        body.AddForce(linearForce);

        float torque = TankUtility.CalcAppliedTorque(stateInfo);
        body.AddTorque(torque, ForceMode2D.Force);

        performAnimations(stateInfo);
    }

    private void performAnimations(TankStateInfo tankStateInfo) {
        string leftWheelStateName = Mathf.Sign(tankStateInfo.LeftCurPower) > 0 ? "WheelForward" : "WheelBackward";
        if (tankStateInfo.LeftCurPower == 0) {
            leftWheelStateName = "WheelIdle";
        }
        Animator leftAnimator = tankGOConstructor.LeftWheelGO.GetComponent<Animator>();
        if (!leftAnimator.GetCurrentAnimatorStateInfo(0).IsName(leftWheelStateName)) {
            leftAnimator.Play(leftWheelStateName);
        }

        string rightWheelStateName = Mathf.Sign(tankStateInfo.RightCurPower) > 0 ? "WheelForward" : "WheelBackward";
        if (tankStateInfo.RightCurPower == 0) {
            rightWheelStateName = "WheelIdle";
        }
        Animator rightAnimator = tankGOConstructor.RightWheelGO.GetComponent<Animator>();
        if (!rightAnimator.GetCurrentAnimatorStateInfo(0).IsName(rightWheelStateName)) {
            rightAnimator.Play(leftWheelStateName);
        }
    }
}
