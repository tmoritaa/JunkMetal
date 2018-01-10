using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

public class Tank : MonoBehaviour
{
    private Rigidbody2D body;

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
    private float width = 50f;

    [SerializeField]
    private float maxForcePerTS = 10000f;

    private WheelPart leftWheel;
    private WheelPart rightWheel;

    void Awake() {
        body = this.GetComponent<Rigidbody2D>();

        Debug.Log("LeftWheel");
        leftWheel = TankPartFactory.CreateWheelPart(this);

        Debug.Log("RightWheel");
        rightWheel = TankPartFactory.CreateWheelPart(this);
    }

    void FixedUpdate() {
        handleMovement();
    }

    void Update() {
        leftWheel.HandleInput();
        rightWheel.HandleInput();
    }

    private void handleMovement() {
        // Calculate rotation
        Vector2 leftVec = new Vector2(-width / 2, leftWheel.CurPower);
        Vector2 rightVec = new Vector2(width / 2, rightWheel.CurPower);

        Vector2 diffVec = rightVec - leftVec;
        Vector2 perpUnitVec = new Vector2(-diffVec.y, diffVec.x).normalized;

        float finalAngle = Vector2.SignedAngle(new Vector2(0, 1.0f), perpUnitVec);
        this.body.rotation += finalAngle;

        // Calculate forward velocity
        Vector2 forwardVec = new Vector2(0, 1).Rotate(this.body.rotation);
        float finalVel = (leftWheel.CurPower + rightWheel.CurPower) / 2f * maxForcePerTS;
        this.body.AddForce(forwardVec.normalized * finalVel, ForceMode2D.Force);
    }
}
