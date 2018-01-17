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
    private GameObject cannonGO;
    public GameObject CannonGO
    {
        get {
            return cannonGO;
        }
    }

    public WheelPart LeftWheel
    {
        get; private set;
    }

    public WheelPart RightWheel
    {
        get; private set;
    }

    public BodyPart BodyPart
    {
        get; private set;
    }

    public MainWeaponPart MainWeapon
    {
        get; private set;
    }

    public EnginePart EnginePart
    {
        get; private set;
    }

    private int curArmour;

    private bool initialized = false;

    void FixedUpdate() {
        if (initialized) {
            handleMovement();
            MainWeapon.PerformFixedUpdate();
        }
    }

    void Update() {
        if (initialized) {
            LeftWheel.HandleInput();
            RightWheel.HandleInput();
            MainWeapon.HandleInput();
        }
    }

    public void Init(BodyPart _body, EnginePart _enginePart, MainWeaponPart _mainWeapon, WheelPart _leftWheel, WheelPart _rightWheel) {

        BodyPart = _body;
        LeftWheel = _leftWheel;
        RightWheel = _rightWheel;
        MainWeapon = _mainWeapon;
        EnginePart = _enginePart;

        boxCollider.size = BodyPart.Size;

        ResetState();

        initialized = true;
    }

    public void ResetState() {
        curArmour = BodyPart.Armour;
    }

    private void handleMovement() {
        float width = BodyPart.Size.x;

        // Calculate rotation
        Vector2 leftVec = (new Vector2(-width / 2, LeftWheel.CurPower)).Rotate(this.body.rotation);
        Vector2 rightVec = (new Vector2(width / 2, RightWheel.CurPower)).Rotate(this.body.rotation);

        Vector2 diffVec = rightVec - leftVec;
        Vector2 perpUnitVec = new Vector2(-diffVec.y, diffVec.x).normalized;

        float finalAngle = Vector2.SignedAngle((new Vector2(0, 1.0f)).Rotate(this.body.rotation), perpUnitVec);
        this.body.rotation += finalAngle;

        // Calculate forward velocity
        Vector2 forwardVec = new Vector2(0, 1).Rotate(this.body.rotation);
        float finalVel = (LeftWheel.CurPower + RightWheel.CurPower) / 2f * EnginePart.MoveForce;
        this.body.AddForce(forwardVec.normalized * finalVel);
    }
}
