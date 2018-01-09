using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

public class PlayerTank : MonoBehaviour
{
    private Rigidbody2D body;

    private float leftVel = 0f;
    private float rightVel = 0f;

    [SerializeField]
    private float accel = 0.1f;

    [SerializeField]
    private float maxSpeed = 500f;

    [SerializeField]
    private float frictionDeterioration = 0.01f;

    [SerializeField]
    private float width = 50f;

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
        // Apply slow down due to friction.
        if (Mathf.Abs(leftVel) > 0) {
            leftVel = Mathf.Sign(leftVel) * (Mathf.Abs(leftVel) - frictionDeterioration);
        }
        if (Mathf.Abs(rightVel) > 0) {
            rightVel = Mathf.Sign(rightVel) * (Mathf.Abs(rightVel) - frictionDeterioration);
        }

        // Force 0 if below sigma.
        if (Mathf.Abs(leftVel) < 0.001f) {
            leftVel = 0;
        }
        if (Mathf.Abs(rightVel) < 0.001f) {
            rightVel = 0;
        }

        // Add acceleration and clamp based on key input.
        if (Input.GetKey(KeyCode.W)) {
            leftVel += accel;
        }
        if (Input.GetKey(KeyCode.S)) {
            leftVel -= accel;
        }
        leftVel = Mathf.Clamp(leftVel, -1.0f, 1.0f);

        if (Input.GetKey(KeyCode.I)) {
            rightVel += accel;
        }
        if (Input.GetKey(KeyCode.K)) {
            rightVel -= accel;
        }
        rightVel = Mathf.Clamp(rightVel, -1.0f, 1.0f);
    }

    private void handleMovement() {
        // Calculate rotation
        Vector2 leftVec = new Vector2(-width / 2, leftVel);
        Vector2 rightVec = new Vector2(width / 2, rightVel);

        Vector2 diffVec = new Vector2(rightVec.x - leftVec.x, rightVec.y - leftVec.y);
        Vector2 perpUnitVec = new Vector2(-diffVec.y, diffVec.x).normalized;

        float finalAngle = Vector2.SignedAngle(new Vector2(0, 1.0f), perpUnitVec);
        this.body.rotation += finalAngle;

        // Calculate forward velocity
        float finalVel = (leftVel + rightVel) / 2f * maxSpeed;
        float radAngle = this.body.rotation * Mathf.PI / 180f;
        Vector2 forwardVec = new Vector2(-Mathf.Sin(radAngle), Mathf.Cos(radAngle));

        this.body.velocity = forwardVec.normalized * finalVel;
    }
}
