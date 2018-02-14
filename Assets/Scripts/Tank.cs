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

    public float TerminalVelocity
    {
        get {
            return Mathf.Sqrt(Hull.Schematic.EnergyPower / totalDrag);
        }
    }

    private float totalDrag = 0;

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

    public void Init(HullPart _body, TurretPart _turret, WheelPart _wheels) {
        Hull = _body;
        Wheels = _wheels;
        Turret = _turret;

        Turret.SetOwner(this);

        Vector2 hullSize = Hull.Schematic.Size;
        boxCollider.size = hullSize;
        hullGO.GetComponent<RectTransform>().sizeDelta = new Vector2(hullSize.x, hullSize.y);

        leftWheelGO.transform.localPosition = leftWheelGO.transform.localPosition + new Vector3(-hullSize.x / 2f, 0, 0);
        leftWheelGO.GetComponent<FixedJoint2D>().connectedAnchor = new Vector2(-hullSize.x / 2f, 0);

        rightWheelGO.transform.localPosition = rightWheelGO.transform.localPosition + new Vector3(hullSize.x / 2f, 0, 0);
        rightWheelGO.GetComponent<FixedJoint2D>().connectedAnchor = new Vector2(hullSize.x / 2f, 0);

        totalDrag = calculateDrag();

        float totalWeight = calculateTotalWeight() / 10f;
        this.body.mass = totalWeight;
        this.LeftWheelBody.mass = totalWeight / 2f;
        this.RightWheelBody.mass = totalWeight / 2f;

        MaxArmour = calculateTotalArmour();

        ResetState();

        initialized = true;
    }

    public void ResetState() {
        CurArmour = MaxArmour;
    }

    public void Damage(int damage) {
        Debug.Log("Tank has taken damage");

        CurArmour -= damage;
        CurArmour = Mathf.Max(0, CurArmour);

        if (CurArmour <= 0) {
            Dead();
        }
    }

    public void Dead() {
        Debug.Log("Tank is dead");
    }

    public float GetAvgRangeOfWeapons() {
        float val = 0;
        int count = 0;
        foreach (WeaponPart part in Turret.Weapons) {
            if (part == null) {
                continue;
            }

            val = part.Schematic.Range;
            count += 1;
        }

        return val / count;
    }

    public Vector2 GetForwardVec() {
        return new Vector2(0, 1).Rotate(body.rotation);
    }

    public Vector2 GetBackwardVec() {
        return new Vector2(0, -1).Rotate(body.rotation);
    }

    public float CalcTimeToRotate(Vector2 from, Vector2 to) {
        float rotationAngle = Vector2.Angle(from, to);
        float circumference = Hull.Schematic.Size.x * Mathf.PI;
        float timeToDoOneFullRot = circumference / TerminalVelocity;
        return rotationAngle / 360f * timeToDoOneFullRot;
    }

    private int calculateTotalArmour() {
        int armour = 0;

        armour = Hull.Schematic.Armour;
        armour += Turret.Schematic.Armour;

        return armour;
    }

    private float calculateDrag() {
        float drag = 0;

        drag += body.drag;
        drag += LeftWheelBody.drag;
        drag += RightWheelBody.drag;

        return drag;
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
