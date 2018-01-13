using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEngine.UI;

public class Tank : MonoBehaviour
{
    public Rigidbody2D Body
    {
        get; private set;
    }

    private BoxCollider2D boxCollider;

    [SerializeField]
    private float powerIncPerTS = 0.1f;
    public float PowerIncPerTS
    {
        get { return powerIncPerTS; }
    }

    [SerializeField]
    private float powerDeterPerTS = 0.01f;
    public float PowerDeterPerTS
    {
        get { return powerDeterPerTS; }
    }
    
    [SerializeField]
    private float maxForcePerTS = 10000f;

    [SerializeField]
    private float shootingForce = 25000f;

    [SerializeField]
    private float reloadTime = 1;

    [SerializeField]
    private float rotPerTS = 0.1f;

    [SerializeField]
    private GameObject cannonGO;
    public GameObject CannonGO
    {
        get {
            return cannonGO;
        }
    }

    private WheelPart leftWheel;
    private WheelPart rightWheel;
    private BodyPart bodyPart;
    private MainWeaponPart mainWeapon;

    void Awake() {
        Body = this.GetComponent<Rigidbody2D>();
        boxCollider = this.GetComponent<BoxCollider2D>();

        leftWheel = TankPartFactory.CreateWheelPart(this, KeyCode.W, KeyCode.S);
        rightWheel = TankPartFactory.CreateWheelPart(this, KeyCode.I, KeyCode.K);

        bodyPart = TankPartFactory.CreateBodyPart(new Vector2(50, 50));
        boxCollider.size = bodyPart.Size;

        mainWeapon = TankPartFactory.CreateMainWeaponPart(this, shootingForce, reloadTime, rotPerTS, KeyCode.P, KeyCode.T, KeyCode.Y);
    }

    void FixedUpdate() {
        handleMovement();

        mainWeapon.PerformFixedUpdate();
    }

    void Update() {
        leftWheel.HandleInput();
        rightWheel.HandleInput();
        mainWeapon.HandleInput();
    }

    private void handleMovement() {
        float width = bodyPart.Size.x;

        // Calculate rotation
        Vector2 leftVec = (new Vector2(-width / 2, leftWheel.CurPower)).Rotate(this.Body.rotation);
        Vector2 rightVec = (new Vector2(width / 2, rightWheel.CurPower)).Rotate(this.Body.rotation);

        Vector2 diffVec = rightVec - leftVec;
        Vector2 perpUnitVec = new Vector2(-diffVec.y, diffVec.x).normalized;

        float finalAngle = Vector2.SignedAngle((new Vector2(0, 1.0f)).Rotate(this.Body.rotation), perpUnitVec);
        this.Body.rotation += finalAngle;

        // Calculate forward velocity
        Vector2 forwardVec = new Vector2(0, 1).Rotate(this.Body.rotation);
        float finalVel = (leftWheel.CurPower + rightWheel.CurPower) / 2f * maxForcePerTS;
        this.Body.AddForce(forwardVec.normalized * finalVel);
    }
}
