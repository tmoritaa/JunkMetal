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

    public Rigidbody2D LeftWheelBody
    {
        get; private set;
    }

    public Rigidbody2D RightWheelBody
    {
        get; private set;
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

    public WheelPart Wheels
    {
        get; private set;
    }

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

    // Used link for reference https://answers.unity.com/questions/151724/calculating-rigidbody-top-speed.html
    // TODO: Right now we're assuming that going to terminal velocity is instantaneous for most calculations. We might have to alleviate this assumption if things are wonky.
    public float TerminalVelocity
    {
        get {
            float unityDrag = LeftWheelBody.drag;
            float mass = LeftWheelBody.mass;
            float addedForce = Hull.Schematic.EnergyPower / 2f;
            return ((addedForce / unityDrag)) / mass;
        }
    }

    // We need a different value for this because angular drag is different from linear drag.
    // TODO: Not completely accurate, but relatively accurate. Maybe enough for our purposes.
    public float TerminalVelocityForRotation
    {
        get {
            float unityDrag = Body.angularDrag;
            float mass = LeftWheelBody.mass;
            float addedForce = Hull.Schematic.EnergyPower / 2f;
            return ((addedForce / unityDrag)) / mass;
        }
    }

    private bool initialized = false;

    private void Awake() {
        LeftWheelBody = leftWheelGO.GetComponent<Rigidbody2D>();
        RightWheelBody = rightWheelGO.GetComponent<Rigidbody2D>();
    }

    private void FixedUpdate() {
        if (initialized) {
            handleMovement();
            Turret.PerformFixedUpdate();
        }
    }

    public void Init(TankSchematic tankSchematic) {
        Hull = new HullPart(tankSchematic.HullSchematic);
        Wheels = new WheelPart(tankSchematic.WheelSchematic);
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
        leftWheelGO.GetComponent<FixedJoint2D>().connectedAnchor = new Vector2(-hullSize.x / 2f, 0);

        rightWheelGO.transform.localPosition = new Vector3(hullSize.x / 2f, 0, 0);
        rightWheelGO.GetComponent<FixedJoint2D>().connectedAnchor = new Vector2(hullSize.x / 2f, 0);

        float totalWeight = calculateTotalWeight() / 10f;
        this.LeftWheelBody.mass = totalWeight / 2f;
        this.RightWheelBody.mass = totalWeight / 2f;

        MaxArmour = calculateTotalArmour();

        ResetState();

        initialized = true;
    }

    public void ResetState() {
        CurArmour = MaxArmour;
        ResetMovement();
    }

    public void ResetMovement() {
        Wheels.Reset();
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

    // TODO: Not completely accurate, but relatively accurate. Maybe enough for our purposes.
    public float CalcTimeToRotate(Vector2 from, Vector2 to) {
        float rotationAngle = Vector2.Angle(from, to);
        float circumference = (Hull.Schematic.Size.x / 2f) * Mathf.PI;
        float timeToDoOneFullRot = circumference / TerminalVelocityForRotation;
        return rotationAngle / 360f * timeToDoOneFullRot;
    }

    public float CalcTimeToReachPosWithNoRot(Vector2 targetPos) {
        Vector2 desiredDir = targetPos - (Vector2)this.transform.position;

        float curVel = Body.velocity.magnitude;
        float angle = Vector2.Angle(Body.velocity, desiredDir);
        if (angle >= 90) {
            curVel *= -1;
        }

        float f = Hull.Schematic.EnergyPower / 2f;
        float m = LeftWheelBody.mass;
        float drag = LeftWheelBody.drag;
        float a = f / m;

        float newVel = curVel;
        float dt = Time.fixedDeltaTime;
        float totalDt = 0;

        float distToTarget = desiredDir.magnitude;
        while (distToTarget > 0) {
            totalDt += dt;
            newVel = (newVel + a * dt) * (1f / (1f + drag * dt));
            distToTarget -= newVel * dt;
        }

        return totalDt;
    }

    // This only accounts for reaching terminal vel in forward or backwards. Doesn't take into account rotation.
    public float CalcTimeToReachTerminalVelInDir(Vector2 desiredDir) {
        float maxVel = TerminalVelocity - 0.1f;
        float curVel = Body.velocity.magnitude;

        float angle = Vector2.Angle(Body.velocity, desiredDir);

        bool goingInDir = angle < 90;

        if (!goingInDir) {
            curVel *= -1;
        }

        float f = Hull.Schematic.EnergyPower / 2f;
        float m =  LeftWheelBody.mass;
        float drag = LeftWheelBody.drag;
        float a = f / m;

        float newVel = curVel;
        float dt = Time.fixedDeltaTime;
        float totalDt = 0;
        while (newVel < maxVel && totalDt < 10) {
            totalDt += dt;
            newVel = (newVel + a * dt) * (1f / (1f + drag*dt));
        }

        return totalDt;
    }

    public float CalcAvgOptimalRange() {
        float totalRange = 0;
        int count = 0;
        foreach (WeaponPart part in Turret.GetAllWeapons()) {
            totalRange += part.Schematic.OptimalRange;
            count += 1;
        }

        return totalRange / count;
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
        weight += Wheels.Schematic.Weight;

        return weight;
    }

    private void handleMovement() {
        Vector2 forwardVec = this.GetForwardVec();
        this.LeftWheelBody.AddForce(forwardVec * Wheels.LeftCurPower * (Hull.Schematic.EnergyPower / 2f));
        this.RightWheelBody.AddForce(forwardVec * Wheels.RightCurPower * (Hull.Schematic.EnergyPower / 2f));
    }
}
