﻿using System;
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
    private GameObject leftWheelGO;

    private Rigidbody2D leftWheelBody;

    [SerializeField]
    private GameObject rightWheelGO;

    private Rigidbody2D rightWheelBody;

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

    [SerializeField]
    private GameObject bodyGO;

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

    void Awake() {
        leftWheelBody = leftWheelGO.GetComponent<Rigidbody2D>();
        rightWheelBody = rightWheelGO.GetComponent<Rigidbody2D>();
    }

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
        bodyGO.GetComponent<RectTransform>().sizeDelta = new Vector2(BodyPart.Size.x, BodyPart.Size.y);

        leftWheelGO.transform.localPosition = leftWheelGO.transform.localPosition + new Vector3(-BodyPart.Size.x / 2f, 0, 0);
        leftWheelGO.GetComponent<FixedJoint2D>().connectedAnchor = new Vector2(-BodyPart.Size.x / 2f, 0);

        rightWheelGO.transform.localPosition = rightWheelGO.transform.localPosition + new Vector3(BodyPart.Size.x / 2f, 0, 0);
        rightWheelGO.GetComponent<FixedJoint2D>().connectedAnchor = new Vector2(BodyPart.Size.x / 2f, 0);

        ResetState();

        initialized = true;
    }

    public void ResetState() {
        curArmour = BodyPart.Armour;
    }

    private void handleMovement() {
        Vector2 forwardVec = new Vector2(0, 1).Rotate(this.body.rotation);
        this.leftWheelBody.AddForce(forwardVec * LeftWheel.CurPower * (EnginePart.MoveForce / 2f)) ;
        this.rightWheelBody.AddForce(forwardVec * RightWheel.CurPower * (EnginePart.MoveForce / 2f));
    }
}
