using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

public partial class Tank
{
    const float MinRatioCutOff = 0.4f;
    const float MaxRatioCutoff = 0.7f; // TODO: later probably make a serialized field for easier tweaking and move to AITankController

    const float StartingBackwardArcAngle = 180f; // TODO: later probably make a serialized field for easier tweaking and move to AITankController
    const float StartingForwardArcAngle = 360f - StartingBackwardArcAngle;

    // Used link for reference https://answers.unity.com/questions/151724/calculating-rigidbody-top-speed.html
    public float TerminalVelocity
    {
        get {
            float unityDrag = body.drag;
            float mass = body.mass;
            float addedForce = Hull.Schematic.EnergyPower;
            return ((addedForce / unityDrag)) / mass;
        }
    }

    public void PerformActuation(Vector2 requestDir) {
        int[] powerChange = calcPowerChangeBasedOnRequestDir(requestDir);

        this.Hull.PerformPowerChange(powerChange[0], powerChange[1]);
    }
    
    public void PerformRotation(Vector2 alignAngle, Vector2 requestDir) {
        float angle = Vector2.SignedAngle(alignAngle, requestDir);

        int[] powerChange = calcPowerChangeForRotation(angle);
        Hull.PerformPowerChange(powerChange[0], powerChange[1]);
    }

    // Returns Vector3 in the form (Vector2, curRot)
    public Vector3 CalcPosInFutureWithRequestedDir(Vector2 requestDir, float timeInSecs) {
        float elapsedTime = 0;
        Vector2 tankPos = this.transform.position;

        int[] powerChange = calcPowerChangeBasedOnRequestDir(requestDir);
        
        Vector2 curVel = Body.velocity;

        float curAngVel = body.angularVelocity;

        float curRot = Body.rotation;
        float dt = Time.fixedDeltaTime;

        List<Vector2> allPos = new List<Vector2>();

        while (elapsedTime < timeInSecs) {
            elapsedTime += dt;

            Vector2 forwardVec = new Vector2(0, 1).Rotate(curRot);
            Vector2 linearForce = calcAppliedLinearForce(forwardVec, powerChange[0], powerChange[1]);
            float torque = calcAppliedTorque(forwardVec, powerChange[0], powerChange[1]);

            // Pos update
            {
                Vector2 f = linearForce;
                float m = body.mass;
                float drag = body.drag;
                Vector2 a = f / m;
                curVel = (curVel + a * dt) * (1f / (1f + drag * dt));                
                tankPos += curVel * dt;
            }

            allPos.Add(tankPos);

            // rot update
            {
                float angularDrag = body.angularDrag;
                float angularAccel = torque / body.inertia * Mathf.Rad2Deg;

                curAngVel = (curAngVel + angularAccel * dt) * (1f / (1f + angularDrag * dt));
                curRot += curAngVel * dt;
            }
        }

        return new Vector3(tankPos.x, tankPos.y, curRot);
    }

    public float CalcTimeToRotate(Vector2 from, Vector2 to) {
        float rotationAngle = Vector2.Angle(from, to);

        float r = Hull.Schematic.Size.x / 2f;
        float f = Hull.Schematic.EnergyPower;
        float torque = r * f;
        float angularDrag = body.angularDrag;

        float angularAccel = torque / body.inertia * Mathf.Rad2Deg;

        float newVel = body.angularVelocity;

        float angle = Vector2.SignedAngle(GetForwardVec(), to);

        newVel *= Mathf.Sign(angle);

        float dt = Time.fixedDeltaTime;
        float totalDt = 0;

        float angleToCover = rotationAngle;
        while (angleToCover > 0) {
            totalDt += dt;
            newVel = (newVel + angularAccel * dt) * (1f / (1f + angularDrag * dt));
            angleToCover -= newVel * dt;
        }

        return totalDt;
    }

    public float CalcTimeToReachPosWithNoRot(Vector2 targetPos) {
        Vector2 desiredDir = targetPos - (Vector2)this.transform.position;

        float curVel = Body.velocity.magnitude;
        float angle = Vector2.Angle(Body.velocity, desiredDir);
        if (angle >= 90) {
            curVel *= -1;
        }

        float f = Hull.Schematic.EnergyPower;
        float m = body.mass;
        float drag = body.drag;
        float a = f / m;

        float newVel = curVel;
        float dt = Time.fixedDeltaTime;
        float totalDt = 0;

        float distToTarget = desiredDir.magnitude;
        while (distToTarget > 0) {
            totalDt += dt;
            newVel = (newVel + a * dt) * (1f / (1f + drag * dt));
            distToTarget -= newVel * dt;
        }

        return totalDt;
    }

    // This only accounts for reaching terminal vel in forward or backwards. Doesn't take into account rotation.
    public float CalcTimeToReachTerminalVelInDir(Vector2 desiredDir) {
        float maxVel = TerminalVelocity - 0.1f;
        float curVel = Body.velocity.magnitude;

        float angle = Vector2.Angle(Body.velocity, desiredDir);

        bool goingInDir = angle < 90;

        if (!goingInDir) {
            curVel *= -1;
        }

        float f = Hull.Schematic.EnergyPower;
        float m = body.mass;
        float drag = body.drag;
        float a = f / m;

        float newVel = curVel;
        float dt = Time.fixedDeltaTime;
        float totalDt = 0;
        while (newVel < maxVel && totalDt < 10) {
            totalDt += dt;
            newVel = (newVel + a * dt) * (1f / (1f + drag * dt));
        }

        return totalDt;
    }

    public float CalcAvgOptimalRange() {
        float totalRange = 0;
        int count = 0;
        foreach (WeaponPart part in Turret.GetAllWeapons()) {
            totalRange += part.Schematic.OptimalRange;
            count += 1;
        }

        return totalRange / count;
    }

    private int[] calcPowerChangeBasedOnRequestDir(Vector2 requestDir) {
        int[] powerChange = new int[2];

        if (requestDir.magnitude == 0) {
            powerChange[0] = Mathf.Sign(Hull.LeftCurPower) > 0 ? -1 : 1;
            powerChange[1] = Mathf.Sign(Hull.RightCurPower) > 0 ? -1 : 1;
            return powerChange;
        }

        // First calculate forward and backwards arc angle based on speed
        float sqrMaxVelocityMag = Mathf.Pow(this.TerminalVelocity, 2);
        float sqrCurVelocity = this.Body.velocity.sqrMagnitude;

        float ratio = Mathf.Clamp(1.0f - sqrCurVelocity / sqrMaxVelocityMag, MinRatioCutOff, MaxRatioCutoff);

        float curBackwardArcAngle = ratio * StartingBackwardArcAngle;
        float curForwardArcAngle = ratio * StartingForwardArcAngle;

        Vector2 forwardVec = this.GetForwardVec();
        Vector2 backwardVec = this.GetBackwardVec();

        List<Vector2> arcVectors = new List<Vector2> {
            forwardVec.Rotate(curForwardArcAngle / 2f),
            forwardVec.Rotate(-curForwardArcAngle / 2f),
            backwardVec.Rotate(curBackwardArcAngle / 2f),
            backwardVec.Rotate(-curBackwardArcAngle / 2f)
        };
        DebugManager.Instance.RegisterObject("actuation_arc_vectors", arcVectors);

        float angleDiffFromFront = Vector2.Angle(forwardVec, requestDir);
        float angleDiffFromBack = Vector2.Angle(backwardVec, requestDir);

        const float sigma = 10f; // TODO: later probably make a serialized field for easier tweaking

        // In this case we want the AI to continue accelerating while going towards the requested direction
        if ((curForwardArcAngle / 2f) >= angleDiffFromFront) {
            float angleToTurn = Vector2.SignedAngle(forwardVec, requestDir);

            if (Mathf.Abs(angleToTurn) > sigma) {
                if (Mathf.Sign(angleToTurn) > 0) {
                    powerChange[0] = 0;
                    powerChange[1] = 1;
                } else {
                    powerChange[0] = 1;
                    powerChange[1] = 0;
                }
            } else {
                powerChange[0] = 1;
                powerChange[1] = 1;
            }

            // In this case we want the tank to start accelerating backwards
        } else if ((curBackwardArcAngle / 2f) >= angleDiffFromBack) {
            float angleToTurn = Vector2.SignedAngle(backwardVec, requestDir);

            if (Mathf.Abs(angleToTurn) > sigma) {
                if (Mathf.Sign(angleToTurn) > 0) {
                    powerChange[0] = -1;
                    powerChange[1] = 0;
                } else {
                    powerChange[0] = 0;
                    powerChange[1] = -1;
                }
            } else {
                powerChange[0] = -1;
                powerChange[1] = -1;
            }

            // In this case we want the tank to start turning
        } else {
            float angleToTurnFromFront = Vector2.SignedAngle(forwardVec, requestDir);
            float angleToTurnFromBack = Vector2.SignedAngle(backwardVec, requestDir);

            bool turningToFront = Mathf.Abs(angleToTurnFromFront) <= Mathf.Abs(angleToTurnFromBack);
            float angle = turningToFront ? angleToTurnFromFront : angleToTurnFromBack;

            powerChange = calcPowerChangeForRotation(angle);
        }

        return powerChange;
    }

    private int[] calcPowerChangeForRotation(float angleChange) {
        int[] powerChange = new int[2];

        if (Mathf.Sign(angleChange) >= 0) {
            powerChange[0] = -1;
            powerChange[1] = 1;
        } else {
            powerChange[0] = 1;
            powerChange[1] = -1;
        }

        return powerChange;
    }
}
