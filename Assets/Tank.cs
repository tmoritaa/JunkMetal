using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEngine.UI;

public class Tank : MonoBehaviour
{
    private Rigidbody2D body;
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

    private WheelPart leftWheel;
    private WheelPart rightWheel;
    private BodyPart bodyPart;

    void Awake() {
        body = this.GetComponent<Rigidbody2D>();
        boxCollider = this.GetComponent<BoxCollider2D>();

        Debug.Log("LeftWheel");
        leftWheel = TankPartFactory.CreateWheelPart(this, KeyCode.W, KeyCode.S);

        Debug.Log("RightWheel");
        rightWheel = TankPartFactory.CreateWheelPart(this, KeyCode.I, KeyCode.K);

        bodyPart = TankPartFactory.CreateBodyPart(new Vector2(50, 50));
        boxCollider.size = bodyPart.Size;
    }

    void FixedUpdate() {
        handleMovement();
    }

    void Update() {
        leftWheel.HandleInput();
        rightWheel.HandleInput();
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
