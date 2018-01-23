using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

public partial class Tank : MonoBehaviour
{
    public Vector2 TargetPos
    {
        get; set;
    }

    private void performMovement() {
        Vector2 requestDir = getRequestDir().normalized;

        performActuation(requestDir);
    }

    private void performActuation(Vector2 requestDir) {
        if (Application.isEditor) {
            Debug.DrawLine(this.transform.position, this.transform.position + (Vector3)(requestDir * 50), Color.green);
        }

        // First calculate forward and backwards arc angle based on speed
        float sqrMaxVelocityMag = this.EnginePart.MoveForce / this.totalDrag;
        float sqrCurVelocity = this.body.velocity.sqrMagnitude;

        float ratio = 1.0f - sqrCurVelocity / sqrMaxVelocityMag;

        const float minRatioCutoff = 0.6f; // TODO: later probably make a serialized field for easier tweaking

        ratio = Mathf.Max(minRatioCutoff, ratio);

        const float startingBackwardArcAngle = 135f; // TODO: later probably make a serialized field for easier tweaking
        const float startingForwardArcAngle = 360f - startingBackwardArcAngle;

        float curBackwardArcAngle = ratio * startingBackwardArcAngle;
        float curForwardArcAngle = ratio * startingForwardArcAngle;

        // Debug stuff
        if (Application.isEditor)
        {
            Vector2 forwardVec = (new Vector2(0, 1)).Rotate(this.body.rotation);
            Debug.DrawLine(this.transform.position, this.transform.position + (Vector3)(forwardVec.Rotate(curForwardArcAngle / 2f) * 50f), Color.blue);
            Debug.DrawLine(this.transform.position, this.transform.position + (Vector3)(forwardVec.Rotate(-curForwardArcAngle / 2f) * 50f), Color.blue);

            Vector2 backwardVec = (new Vector2(0, -1)).Rotate(this.body.rotation);
            Debug.DrawLine(this.transform.position, this.transform.position + (Vector3)(backwardVec.Rotate(curBackwardArcAngle / 2f) * 50f), Color.red);
            Debug.DrawLine(this.transform.position, this.transform.position + (Vector3)(backwardVec.Rotate(-curBackwardArcAngle / 2f) * 50f), Color.red);
        }

        Vector2 targetVec = (TargetPos - (Vector2)this.transform.position).normalized;

        float angleDiffFromFront = Vector2.Angle((new Vector2(0, 1)).Rotate(this.body.rotation), targetVec);
        float angleDiffFromBack = Vector2.Angle((new Vector2(0, -1)).Rotate(this.body.rotation), targetVec);

        // In this case we want the AI to continue accelerating while going towards the requested direction
        if ((curForwardArcAngle / 2f) >= angleDiffFromFront) {
            float angleToTurn = Vector2.SignedAngle((new Vector2(0, 1)).Rotate(this.body.rotation), requestDir);

            const float sigma = 10f; // TODO: later probably make a serialized field for easier tweaking

            if (Mathf.Abs(angleToTurn) > sigma) {
                if (Mathf.Sign(angleToTurn) > 0) {
                    LeftWheel.PerformPowerChange(0);
                    RightWheel.PerformPowerChange(1);
                } else {
                    LeftWheel.PerformPowerChange(1);
                    RightWheel.PerformPowerChange(0);
                }
            } else {
                LeftWheel.PerformPowerChange(1);
                RightWheel.PerformPowerChange(1);
            }

        // In this case we want the tank to start accelerating backwards
        } else if ((curBackwardArcAngle / 2f) >= angleDiffFromBack) {
            float angleToTurn = Vector2.SignedAngle((new Vector2(0, -1)).Rotate(this.body.rotation), requestDir);

            const float sigma = 10f; // TODO: later probably make a serialized field for easier tweaking

            if (Mathf.Abs(angleToTurn) > sigma) {
                if (Mathf.Sign(angleToTurn) > 0) {
                    LeftWheel.PerformPowerChange(-1);
                    RightWheel.PerformPowerChange(0);
                } else {
                    LeftWheel.PerformPowerChange(0);
                    RightWheel.PerformPowerChange(-1);
                }
            } else {
                LeftWheel.PerformPowerChange(-1);
                RightWheel.PerformPowerChange(-1);
            }

        // In this case we want the tank to start turning
        } else {
            float angleToTurnFromFront = Vector2.SignedAngle((new Vector2(0, 1)).Rotate(this.body.rotation), requestDir);
            float angleToTurnFromBack = Vector2.SignedAngle((new Vector2(0, -1)).Rotate(this.body.rotation), requestDir);

            float angle = Mathf.Abs(angleToTurnFromFront) <= Mathf.Abs(angleToTurnFromBack) ? angleToTurnFromFront : angleToTurnFromBack;

            if (Mathf.Sign(angle) > 0) {
                LeftWheel.PerformPowerChange(-1);
                RightWheel.PerformPowerChange(1);
            } else {
                LeftWheel.PerformPowerChange(1);
                RightWheel.PerformPowerChange(-1);
            }
        }

        // Debug
        if (Application.isEditor) {
            Vector3 leftWheelPos = this.leftWheelGO.transform.position;
            Vector3 rightWheelPos = this.rightWheelGO.transform.position;

            Vector3 forwardVec = (new Vector2(0, 1)).Rotate(this.body.rotation);

            Debug.DrawLine(leftWheelPos, leftWheelPos + (forwardVec * 50 * Mathf.Sign(LeftWheel.CurPower)), Color.magenta);
            Debug.DrawLine(rightWheelPos, rightWheelPos + (forwardVec * 50 * Mathf.Sign(RightWheel.CurPower)), Color.magenta);
        }
    }

    private Vector2 getRequestDir() {
        // First calculate Seek direction
        Vector2 seekVec = Seek(TargetPos);

        return seekVec;
    }

    private Vector2 Seek(Vector2 targetPos) {
        Vector2 curPos = this.transform.position;
        return targetPos - curPos;
    }
}
