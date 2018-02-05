using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

public partial class Tank : MonoBehaviour
{
    public enum PlayerTypes
    {
        Human,
        AI,
    }

    public PlayerTypes PlayerType
    {
        get; protected set;
    }

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

    public Rigidbody2D LeftWheelBody
    {
        get; private set;
    }

    [SerializeField]
    private GameObject rightWheelGO;

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

    private int curArmour = 0;

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

    private void Update() {
        if (initialized) {
            if (PlayerType == PlayerTypes.Human) {
                Wheels.HandleInput();
                Turret.HandleInput();
            } else if (PlayerType == PlayerTypes.AI) {
                performMovement();
            }
        }
    }

    public void Init(PlayerTypes _playerType, HullPart _body, TurretPart _turret, WheelPart _wheels) {
        PlayerType = _playerType;

        if (PlayerType == PlayerTypes.Human) {
            this.gameObject.layer = 9; // Player layer
        }

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

        ResetState();

        initAI();

        initialized = true;
    }

    public void ResetState() {
        curArmour = Hull.Schematic.Armour;
        curArmour += Turret.Schematic.Armour;
    }

    public void Damage(int damage) {
        Debug.Log("Tank has taken damage");

        curArmour -= damage;

        if (curArmour <= 0) {
            Dead();
        }
    }

    public void Dead() {
        Debug.Log("Tank is dead");
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
        Vector2 forwardVec = new Vector2(0, 1).Rotate(this.body.rotation);
        this.LeftWheelBody.AddForce(forwardVec * Wheels.LeftCurPower * (Hull.Schematic.EnergyPower / 2f));
        this.RightWheelBody.AddForce(forwardVec * Wheels.RightCurPower * (Hull.Schematic.EnergyPower / 2f));
    }
}
