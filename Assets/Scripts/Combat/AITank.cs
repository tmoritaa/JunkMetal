﻿using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

public partial class Tank
{
    const float MinRatioCutOff = 0.4f;
    const float MaxRatioCutoff = 0.7f; // TODO: later probably make a serialized field for easier tweaking and move to AITankController

    const float StartingBackwardArcAngle = 180f; // TODO: later probably make a serialized field for easier tweaking and move to AITankController
    const float StartingForwardArcAngle = 360f - StartingBackwardArcAngle;

    public void PerformActuation(Vector2 requestDir) {
        if (requestDir.magnitude == 0) {
            this.Wheels.PerformPowerChangeToStop();
            return;
        }

        // First calculate forward and backwards arc angle based on speed
        float sqrMaxVelocityMag = Mathf.Pow(this.TerminalVelocity, 2);
        float sqrCurVelocity = this.Body.velocity.sqrMagnitude;

        float ratio = Mathf.Clamp(1.0f - sqrCurVelocity / sqrMaxVelocityMag, MinRatioCutOff, MaxRatioCutoff);

        float curBackwardArcAngle = ratio * StartingBackwardArcAngle;
        float curForwardArcAngle = ratio * StartingForwardArcAngle;

        Vector2 forwardVec = this.GetForwardVec();
        Vector2 backwardVec = this.GetBackwardVec();

        // Debug stuff
        if (Application.isEditor && DebugManager.Instance.ActuationDebugOn) {
            Debug.DrawLine(this.transform.position, this.transform.position + (Vector3)(forwardVec.Rotate(curForwardArcAngle / 2f) * 50f), Color.blue);
            Debug.DrawLine(this.transform.position, this.transform.position + (Vector3)(forwardVec.Rotate(-curForwardArcAngle / 2f) * 50f), Color.blue);

            Debug.DrawLine(this.transform.position, this.transform.position + (Vector3)(backwardVec.Rotate(curBackwardArcAngle / 2f) * 50f), Color.green);
            Debug.DrawLine(this.transform.position, this.transform.position + (Vector3)(backwardVec.Rotate(-curBackwardArcAngle / 2f) * 50f), Color.green);
        }

        float angleDiffFromFront = Vector2.Angle(forwardVec, requestDir);
        float angleDiffFromBack = Vector2.Angle(backwardVec, requestDir);

        const float sigma = 10f; // TODO: later probably make a serialized field for easier tweaking

        // In this case we want the AI to continue accelerating while going towards the requested direction
        if ((curForwardArcAngle / 2f) >= angleDiffFromFront) {
            float angleToTurn = Vector2.SignedAngle(forwardVec, requestDir);

            if (Mathf.Abs(angleToTurn) > sigma) {
                if (Mathf.Sign(angleToTurn) > 0) {
                    this.Wheels.PerformPowerChange(0, 1);
                } else {
                    this.Wheels.PerformPowerChange(1, 0);
                }
            } else {
                this.Wheels.PerformPowerChange(1, 1);
            }

        // In this case we want the tank to start accelerating backwards
        } else if ((curBackwardArcAngle / 2f) >= angleDiffFromBack) {
            float angleToTurn = Vector2.SignedAngle(backwardVec, requestDir);

            if (Mathf.Abs(angleToTurn) > sigma) {
                if (Mathf.Sign(angleToTurn) > 0) {
                    this.Wheels.PerformPowerChange(-1, 0);
                } else {
                    this.Wheels.PerformPowerChange(0, -1);
                }
            } else {
                this.Wheels.PerformPowerChange(-1, -1);
            }

        // In this case we want the tank to start turning
        } else {
            float angleToTurnFromFront = Vector2.SignedAngle(forwardVec, requestDir);
            float angleToTurnFromBack = Vector2.SignedAngle(backwardVec, requestDir);

            bool turningToFront = Mathf.Abs(angleToTurnFromFront) <= Mathf.Abs(angleToTurnFromBack);
            float angle = turningToFront ? angleToTurnFromFront : angleToTurnFromBack;

            applyRotationPowerChange(angle);
        }

        // Debug
        if (Application.isEditor && DebugManager.Instance.ActuationDebugOn) {
            Vector3 leftWheelPos = this.LeftWheelGO.transform.position;
            Vector3 rightWheelPos = this.RightWheelGO.transform.position;

            Debug.DrawLine(leftWheelPos, leftWheelPos + ((Vector3)forwardVec * 100 * this.Wheels.LeftCurPower), Color.magenta);
            Debug.DrawLine(rightWheelPos, rightWheelPos + ((Vector3)forwardVec * 100 * this.Wheels.RightCurPower), Color.magenta);
        }
    }

    public void PerformRotation(Vector2 alignAngle, Vector2 requestDir) {
        float angle = Vector2.SignedAngle(alignAngle, requestDir);
        applyRotationPowerChange(angle);
    }

    private void applyRotationPowerChange(float angleChange) {
        if (Mathf.Sign(angleChange) >= 0) {
            this.Wheels.PerformPowerChange(-1, 1);
        } else {
            this.Wheels.PerformPowerChange(1, -1);
        }
    }
}