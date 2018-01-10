using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

public class PlayerTank : MonoBehaviour
{
    private Rigidbody2D body;

    private float leftWheelPower = 0f;
    private float rightWheelPower = 0f;

    [SerializeField]
    private float powerIncPerTS = 0.1f;

    [SerializeField]
    private float powerDeterPerTS = 0.01f;

    [SerializeField]
    private float width = 50f;

    [SerializeField]
    private float maxForcePerTS = 10000f;

    void Awake() {
        body = this.GetComponent<Rigidbody2D>();
    }

    void FixedUpdate() {
        handleMovement();
    }

    void Update() {
        handleInput();
    }

    private void handleInput() {
        // Add power increase and clamp based on key input.
        if (Input.GetKey(KeyCode.W)) {
            leftWheelPower += powerIncPerTS;
        } else if (Input.GetKey(KeyCode.S)) {
            leftWheelPower -= powerIncPerTS;
        } else if (Mathf.Abs(leftWheelPower) > 0) {
            leftWheelPower = Mathf.Sign(leftWheelPower) * (Mathf.Abs(leftWheelPower) - powerDeterPerTS);
        }
        leftWheelPower = Mathf.Clamp(leftWheelPower, -1.0f, 1.0f);

        if (Input.GetKey(KeyCode.I)) {
            rightWheelPower += powerIncPerTS;
        } else if (Input.GetKey(KeyCode.K)) {
            rightWheelPower -= powerIncPerTS;
        } else if (Mathf.Abs(rightWheelPower) > 0) {
            rightWheelPower = Mathf.Sign(rightWheelPower) * (Mathf.Abs(rightWheelPower) - powerDeterPerTS);
        }
        rightWheelPower = Mathf.Clamp(rightWheelPower, -1.0f, 1.0f);

        // Force 0 if below sigma.
        if (Mathf.Abs(leftWheelPower) < 0.001f) {
            leftWheelPower = 0;
        }
        if (Mathf.Abs(rightWheelPower) < 0.001f) {
            rightWheelPower = 0;
        }
    }

    private void handleMovement() {
        // Calculate rotation
        Vector2 leftVec = new Vector2(-width / 2, leftWheelPower);
        Vector2 rightVec = new Vector2(width / 2, rightWheelPower);

        Vector2 diffVec = rightVec - leftVec;
        Vector2 perpUnitVec = new Vector2(-diffVec.y, diffVec.x).normalized;

        float finalAngle = Vector2.SignedAngle(new Vector2(0, 1.0f), perpUnitVec);
        this.body.rotation += finalAngle;

        // Calculate forward velocity
        Vector2 forwardVec = new Vector2(0, 1).Rotate(this.body.rotation);
        float finalVel = (leftWheelPower + rightWheelPower) / 2f * maxForcePerTS;
        this.body.AddForce(forwardVec.normalized * finalVel, ForceMode2D.Force);
    }
}
