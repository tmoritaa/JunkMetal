﻿using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

public partial class Tank : MonoBehaviour
{
    public Vector2 TargetPos
    {
        get; set;
    }

    private int successiveCollisions = 0;
    private float prevCollisionTime = 0;

    private void performMovement() {
        Vector2 requestDir = calcRequestDir().normalized;

        if (Application.isEditor) {
            Debug.DrawLine(this.transform.position, this.transform.position + (Vector3)(requestDir * 50), Color.red);
        }

        performActuation(requestDir);
    }

    private void performActuation(Vector2 requestDir) {
        // First calculate forward and backwards arc angle based on speed
        float sqrMaxVelocityMag = this.EnginePart.MoveForce / this.totalDrag;
        float sqrCurVelocity = this.body.velocity.sqrMagnitude;

        const float minRatioCutOff = 0.2f;
        const float maxRatioCutoff = 0.6f; // TODO: later probably make a serialized field for easier tweaking
        float ratio = Mathf.Clamp(1.0f - sqrCurVelocity / sqrMaxVelocityMag, minRatioCutOff, maxRatioCutoff);

        const float startingBackwardArcAngle = 135f; // TODO: later probably make a serialized field for easier tweaking
        const float startingForwardArcAngle = 360f - startingBackwardArcAngle;

        float curBackwardArcAngle = ratio * startingBackwardArcAngle;
        float curForwardArcAngle = ratio * startingForwardArcAngle;

        // Debug stuff
        if (Application.isEditor && GameManager.Instance.ActuationDebugOn)
        {
            Vector2 forwardVec = (new Vector2(0, 1)).Rotate(this.body.rotation);
            Debug.DrawLine(this.transform.position, this.transform.position + (Vector3)(forwardVec.Rotate(curForwardArcAngle / 2f) * 50f), Color.blue);
            Debug.DrawLine(this.transform.position, this.transform.position + (Vector3)(forwardVec.Rotate(-curForwardArcAngle / 2f) * 50f), Color.blue);

            Vector2 backwardVec = (new Vector2(0, -1)).Rotate(this.body.rotation);
            Debug.DrawLine(this.transform.position, this.transform.position + (Vector3)(backwardVec.Rotate(curBackwardArcAngle / 2f) * 50f), Color.green);
            Debug.DrawLine(this.transform.position, this.transform.position + (Vector3)(backwardVec.Rotate(-curBackwardArcAngle / 2f) * 50f), Color.green);
        }
        
        float angleDiffFromFront = Vector2.Angle((new Vector2(0, 1)).Rotate(this.body.rotation), requestDir);
        float angleDiffFromBack = Vector2.Angle((new Vector2(0, -1)).Rotate(this.body.rotation), requestDir);

        const float sigma = 10f; // TODO: later probably make a serialized field for easier tweaking

        // In this case we want the AI to continue accelerating while going towards the requested direction
        if ((curForwardArcAngle / 2f) >= angleDiffFromFront) {
            float angleToTurn = Vector2.SignedAngle((new Vector2(0, 1)).Rotate(this.body.rotation), requestDir);

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

            bool turningToFront = Mathf.Abs(angleToTurnFromFront) <= Mathf.Abs(angleToTurnFromBack);
            float angle = turningToFront ? angleToTurnFromFront : angleToTurnFromBack;

            if (Mathf.Sign(angle) >= 0) {
                LeftWheel.PerformPowerChange(-1);
                RightWheel.PerformPowerChange(1);
            } else {
                LeftWheel.PerformPowerChange(1);
                RightWheel.PerformPowerChange(-1);
            }
        }

        // Debug
        if (Application.isEditor && GameManager.Instance.ActuationDebugOn) {
            Vector3 leftWheelPos = this.leftWheelGO.transform.position;
            Vector3 rightWheelPos = this.rightWheelGO.transform.position;

            Vector3 forwardVec = (new Vector2(0, 1)).Rotate(this.body.rotation);

            Debug.DrawLine(leftWheelPos, leftWheelPos + (forwardVec * 100 * LeftWheel.CurPower), Color.magenta);
            Debug.DrawLine(rightWheelPos, rightWheelPos + (forwardVec * 100 * RightWheel.CurPower), Color.magenta);
        }
    }

    private Vector2 calcRequestDir() {
        // First calculate Seek direction
        Vector2 desiredVec = Seek(TargetPos).normalized;
        desiredVec = AvoidWalls(desiredVec).normalized;

        return desiredVec;
    }

    private Vector2 Seek(Vector2 targetPos) {
        Vector2 curPos = this.transform.position;
        return targetPos - curPos;
    }

    private Vector2 AvoidWalls(Vector2 desiredDir) {
        Vector2 forwardVec = (new Vector2(0, 1)).Rotate(this.body.rotation);
        Vector2 leftVec = forwardVec.Rotate(-90);
        Vector2 rightVec = forwardVec.Rotate(90);
        Vector2 backVec = forwardVec.Rotate(180);

        // If below 90, ray starting points should be in front of tank. Otherwise, behind tank.
        if (Mathf.Abs(Vector2.SignedAngle(forwardVec, desiredDir.normalized)) > 90) {
            Vector2 tmpVec = leftVec;
            leftVec = rightVec;
            rightVec = tmpVec;

            tmpVec = forwardVec;
            forwardVec = backVec;
            backVec = tmpVec;
        }

        float xAdd = this.BodyPart.Size.x / 2f;
        float yAdd = this.BodyPart.Size.y / 2f;
        Vector2 TopCenter = (Vector2)this.transform.position + forwardVec * yAdd;
        Vector2 TLCorner = (Vector2)this.transform.position + forwardVec * yAdd + leftVec * xAdd;
        Vector2 TRCorner = (Vector2)this.transform.position + forwardVec * yAdd + rightVec * xAdd;

        // If Collision, then take into account desired Dir and see if risk of collision
        float maxDistance = Mathf.Max(this.body.velocity.magnitude, 50f);

        const int WallBit = 8;
        const int LayerMask = 1 << WallBit;
        const float SideRatio = 0.8f;
        float ratio = Mathf.Min((float)successiveCollisions / 250f, 0.8f);

        const float ForwardFanRatio = 0.8f;
        const float BackwardFanRatio = 1.0f - ForwardFanRatio;

        RaycastHit2D[] hitResult = new RaycastHit2D[3];
        hitResult[0] = Physics2D.Raycast(TopCenter, forwardVec, maxDistance, LayerMask);
        //hitResult[1] = Physics2D.Raycast(TLCorner, (forwardVec * (ForwardFanRatio - ForwardFanRatio * ratio) + leftVec * (BackwardFanRatio + ForwardFanRatio * ratio)).normalized, maxDistance * SideRatio, LayerMask);
        //hitResult[2] = Physics2D.Raycast(TRCorner, (forwardVec * (ForwardFanRatio - ForwardFanRatio * ratio) + rightVec * (BackwardFanRatio + ForwardFanRatio * ratio)).normalized, maxDistance * SideRatio, LayerMask);
        hitResult[1] = Physics2D.Raycast(TopCenter, (forwardVec * (ForwardFanRatio - ForwardFanRatio * ratio) + leftVec * (BackwardFanRatio + ForwardFanRatio * ratio)).normalized, maxDistance * SideRatio, LayerMask);
        hitResult[2] = Physics2D.Raycast(TopCenter, (forwardVec * (ForwardFanRatio - ForwardFanRatio * ratio) + rightVec * (BackwardFanRatio + ForwardFanRatio * ratio)).normalized, maxDistance * SideRatio, LayerMask);

        if (Application.isEditor && GameManager.Instance.AvoidWallsDebugOn) {
            Debug.DrawLine(TopCenter, TopCenter + forwardVec * maxDistance, Color.blue);
            //Debug.DrawLine(TLCorner, TLCorner + (forwardVec * (ForwardFanRatio - ForwardFanRatio * ratio) + leftVec * (BackwardFanRatio + ForwardFanRatio * ratio)).normalized * maxDistance * SideRatio, Color.blue);
            //Debug.DrawLine(TRCorner, TRCorner + (forwardVec * (ForwardFanRatio - ForwardFanRatio * ratio) + rightVec * (BackwardFanRatio + ForwardFanRatio * ratio)).normalized * maxDistance * SideRatio, Color.blue);
            Debug.DrawLine(TopCenter, TLCorner + (forwardVec * (ForwardFanRatio - ForwardFanRatio * ratio) + leftVec * (BackwardFanRatio + ForwardFanRatio * ratio)).normalized * maxDistance * SideRatio, Color.blue);
            Debug.DrawLine(TopCenter, TRCorner + (forwardVec * (ForwardFanRatio - ForwardFanRatio * ratio) + rightVec * (BackwardFanRatio + ForwardFanRatio * ratio)).normalized * maxDistance * SideRatio, Color.blue);
        }

        bool centerHit = hitResult[0].collider != null;
        bool leftHit = hitResult[1].collider != null;
        bool rightHit = hitResult[2].collider != null;

        if (centerHit || leftHit || rightHit) {
            successiveCollisions += 1;
            prevCollisionTime = Time.time;
        }

        Vector2 newDesiredDir = new Vector2();

        const float MinBlend = 0.1f;
        const float MaxBlend = 0.5f;

        if (leftHit && !rightHit) {
            float minHitDist = hitResult[1].distance;

            float blendRatio = Mathf.Clamp(minHitDist / maxDistance, MinBlend, MaxBlend);

            newDesiredDir = (blendRatio * desiredDir + (1.0f - blendRatio) * rightVec).normalized;
        } else if (rightHit && !leftHit) {
            float minHitDist = hitResult[2].distance;

            float blendRatio = Mathf.Clamp(minHitDist / maxDistance, MinBlend, MaxBlend);
            newDesiredDir = (blendRatio * desiredDir + (1.0f - blendRatio) * leftVec).normalized;
        //} else if (centerHit) {
        //    float minHitDist = 99999;

        //    if (leftHit) {
        //        minHitDist = Mathf.Min(minHitDist, hitResult[1].distance);
        //    }
        //    if (rightHit) {
        //        minHitDist = Mathf.Min(minHitDist, hitResult[2].distance);
        //    }

        //    float blendRatio = Mathf.Clamp(minHitDist / maxDistance, MinBlend, MaxBlend);
        //    float angle = Vector2.SignedAngle(forwardVec, desiredDir);
        //    if (Mathf.Sign(angle) > 0) {
        //        newDesiredDir = (blendRatio * desiredDir + (1.0f - blendRatio) * rightVec).normalized;
        //    } else {
        //        newDesiredDir = (blendRatio * desiredDir + (1.0f - blendRatio) * leftVec).normalized;
        //    }
        } else if (!centerHit && !rightHit && !leftHit){            
            if (Time.time - prevCollisionTime >= 0.5f) {
                successiveCollisions = 0;
            }

            newDesiredDir = desiredDir;
        }

        return newDesiredDir;
    }
}