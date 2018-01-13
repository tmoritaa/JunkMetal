using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEngine.UI;

public class Tank : MonoBehaviour
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
    private float powerIncPerTS = 0.1f;
    public float PowerIncPerTS
    {
        get { return powerIncPerTS; }
    }

    [SerializeField]
    private float powerDeterPerTS = 0.05f;
    public float PowerDeterPerTS
    {
        get { return powerDeterPerTS; }
    }
    
    [SerializeField]
    private float maxForcePerTS = 10000f;

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

    private bool initialized = false;

    void FixedUpdate() {
        if (initialized) {
            handleMovement();
            mainWeapon.PerformFixedUpdate();
        }
    }

    void Update() {
        if (initialized) {
            leftWheel.HandleInput();
            rightWheel.HandleInput();
            mainWeapon.HandleInput();
        }
    }

    public void Init(BodyPart _body, MainWeaponPart _mainWeapon, WheelPart _leftWheel, WheelPart _rightWheel) {

        bodyPart = _body;
        boxCollider.size = bodyPart.Size;

        leftWheel = _leftWheel;
        rightWheel = _rightWheel;
        mainWeapon = _mainWeapon;

        initialized = true;
    }

    private void handleMovement() {
        float width = bodyPart.Size.x;

        // Calculate rotation
        Vector2 leftVec = (new Vector2(-width / 2, leftWheel.CurPower)).Rotate(this.body.rotation);
        Vector2 rightVec = (new Vector2(width / 2, rightWheel.CurPower)).Rotate(this.body.rotation);

        Vector2 diffVec = rightVec - leftVec;
        Vector2 perpUnitVec = new Vector2(-diffVec.y, diffVec.x).normalized;

        float finalAngle = Vector2.SignedAngle((new Vector2(0, 1.0f)).Rotate(this.body.rotation), perpUnitVec);
        this.body.rotation += finalAngle;

        // Calculate forward velocity
        Vector2 forwardVec = new Vector2(0, 1).Rotate(this.body.rotation);
        float finalVel = (leftWheel.CurPower + rightWheel.CurPower) / 2f * maxForcePerTS;
        this.body.AddForce(forwardVec.normalized * finalVel);
    }
}
