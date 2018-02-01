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

    private Rigidbody2D leftWheelBody;

    [SerializeField]
    private GameObject rightWheelGO;

    private Rigidbody2D rightWheelBody;

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

    private float totalMass = 0;

    private float totalDrag = 0;

    private bool initialized = false;

    private void Awake() {
        leftWheelBody = leftWheelGO.GetComponent<Rigidbody2D>();
        rightWheelBody = rightWheelGO.GetComponent<Rigidbody2D>();
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

        boxCollider.size = Hull.Size;
        hullGO.GetComponent<RectTransform>().sizeDelta = new Vector2(Hull.Size.x, Hull.Size.y);

        leftWheelGO.transform.localPosition = leftWheelGO.transform.localPosition + new Vector3(-Hull.Size.x / 2f, 0, 0);
        leftWheelGO.GetComponent<FixedJoint2D>().connectedAnchor = new Vector2(-Hull.Size.x / 2f, 0);

        rightWheelGO.transform.localPosition = rightWheelGO.transform.localPosition + new Vector3(Hull.Size.x / 2f, 0, 0);
        rightWheelGO.GetComponent<FixedJoint2D>().connectedAnchor = new Vector2(Hull.Size.x / 2f, 0);

        totalMass = calculateMass();
        totalDrag = calculateDrag();

        ResetState();

        initAI();

        initialized = true;
    }

    public void ResetState() {
        curArmour = Hull.Armour;
        curArmour += Turret.Armour;
    }

    private float calculateMass() {
        float mass = 0;

        mass += body.mass;
        mass += leftWheelBody.mass;
        mass += rightWheelBody.mass;

        return mass;
    }

    private float calculateDrag() {
        float drag = 0;

        drag += body.drag;
        drag += leftWheelBody.drag;
        drag += rightWheelBody.drag;

        return drag;
    }

    private void handleMovement() {
        Vector2 forwardVec = new Vector2(0, 1).Rotate(this.body.rotation);
        this.leftWheelBody.AddForce(forwardVec * Wheels.LeftCurPower * (Hull.MoveForce / 2f));
        this.rightWheelBody.AddForce(forwardVec * Wheels.RightCurPower * (Hull.MoveForce / 2f));
    }
}
