using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

public class AITankController : TankController
{
    public Vector2 DestPos
    {
        get; set;
    }

    [SerializeField]
    private float sqrDistForDistSigma = 2500;

    private int successiveCollisions = 0;

    private List<Node> path = new List<Node>();
    public List<Node> Path
    {
        get {
            return path;
        }
    }

    public override void Init(Vector2 startPos, HullPart _body, TurretPart _turret, WheelPart _wheels) {
        base.Init(startPos, _body, _turret, _wheels);

        DestPos = Tank.transform.position;
    }

    private void performMovement() {
        // If already at desired position, stop.
        if (((Vector2)Tank.transform.position - DestPos).sqrMagnitude < sqrDistForDistSigma) {
            Tank.Wheels.PerformPowerChange(0, 0);
            return;
        }

        if (path.Count > 0 && ((Vector2)Tank.transform.position - GameManager.Instance.Map.NodeToPosition(path[0])).sqrMagnitude < sqrDistForDistSigma) {
            path.RemoveAt(0);
            smoothPath();
        }

        if (path.Count < 1) {
            path = GameManager.Instance.Map.FindPath(Tank.transform.position, DestPos);
            smoothPath();
        }

        Vector2 target = (path.Count > 0) ? GameManager.Instance.Map.NodeToPosition(path[0]) : DestPos;

        Vector2 requestDir = calcRequestDir(target).normalized;

        if (Application.isEditor) {
            Debug.DrawLine(Tank.transform.position, Tank.transform.position + (Vector3)(requestDir * 50), Color.red);
        }

        performActuation(requestDir);
    }

    private void smoothPath() {
        const int WallBit = 8;
        const int LayerMask = 1 << WallBit;

        int removeCount = 0;
        for (int i = 0; i < path.Count; ++i) {
            Node node = path[i];

            Vector2 leftVec = new Vector2(0, 1).Rotate(Tank.Body.rotation - 90).normalized;
            Vector2 rightVec = new Vector2(0, 1).Rotate(Tank.Body.rotation + 90).normalized;

            Vector2 pos = GameManager.Instance.Map.NodeToPosition(node);
            Vector2 diffVec = pos - (Vector2)Tank.transform.position;

            RaycastHit2D leftHit = Physics2D.Raycast((Vector2)Tank.transform.position + (leftVec * (Tank.Hull.Schematic.Size.x / 2f)), diffVec.normalized, diffVec.magnitude, LayerMask);
            RaycastHit2D rightHit = Physics2D.Raycast((Vector2)Tank.transform.position + (rightVec * (Tank.Hull.Schematic.Size.x / 2f)), diffVec.normalized, diffVec.magnitude, LayerMask);

            // If collision, stop
            if (leftHit.collider != null || rightHit.collider != null) {
                break;
            }

            removeCount = i;
        }

        if (removeCount > 0) {
            path.RemoveRange(0, removeCount);
        }
    }

    private Vector2 calcRequestDir(Vector2 target) {
        // First calculate Seek direction
        Vector2 desiredVec = Seek(target).normalized;
        desiredVec = AvoidWalls(desiredVec).normalized;

        return desiredVec;
    }

    private Vector2 Seek(Vector2 targetPos) {
        Vector2 curPos = Tank.transform.position;
        return targetPos - curPos;
    }

    private Vector2 AvoidWalls(Vector2 desiredDir) {
        Vector2 forwardVec = (new Vector2(0, 1)).Rotate(Tank.Body.rotation);
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

        float xAdd = Tank.Hull.Schematic.Size.x / 2f;
        float yAdd = Tank.Hull.Schematic.Size.y / 2f;
        Vector2 TopCenter = (Vector2)Tank.transform.position + forwardVec * yAdd;
        Vector2 TLCorner = (Vector2)Tank.transform.position + forwardVec * yAdd + leftVec * xAdd;
        Vector2 TRCorner = (Vector2)Tank.transform.position + forwardVec * yAdd + rightVec * xAdd;

        // If Collision, then take into account desired Dir and see if risk of collision
        const int WallBit = 8;
        const int PlayerBit = 9;
        const int LayerMask = 1 << WallBit | 1 << PlayerBit;
        const float SideRatio = 1.0f;
        const float DiagRatio = 0.6f;

        const float ForwardFanRatio = 0.7f;
        const float BackwardFanRatio = 1.0f - ForwardFanRatio;

        float maxDistance = Mathf.Max(Tank.Body.velocity.magnitude, 150f);

        float fanRatio = Mathf.Min((float)successiveCollisions / 250f, 0.8f);
        RaycastHit2D[] hitResult = new RaycastHit2D[4];
        hitResult[0] = Physics2D.Raycast(TopCenter, (forwardVec * (ForwardFanRatio - ForwardFanRatio * fanRatio) + leftVec * (BackwardFanRatio + ForwardFanRatio * fanRatio)).normalized, maxDistance * SideRatio, LayerMask);
        hitResult[1] = Physics2D.Raycast(TopCenter, (forwardVec * (ForwardFanRatio - ForwardFanRatio * fanRatio) + rightVec * (BackwardFanRatio + ForwardFanRatio * fanRatio)).normalized, maxDistance * SideRatio, LayerMask);
        hitResult[2] = Physics2D.Raycast(TLCorner, (forwardVec * (ForwardFanRatio - ForwardFanRatio * fanRatio) + leftVec * (BackwardFanRatio + ForwardFanRatio * fanRatio)).normalized, maxDistance * DiagRatio, LayerMask);
        hitResult[3] = Physics2D.Raycast(TRCorner, (forwardVec * (ForwardFanRatio - ForwardFanRatio * fanRatio) + rightVec * (BackwardFanRatio + ForwardFanRatio * fanRatio)).normalized, maxDistance * DiagRatio, LayerMask);

        if (Application.isEditor && DebugManager.Instance.AvoidWallsDebugOn) {
            Debug.DrawLine(TopCenter, TopCenter + (forwardVec * (ForwardFanRatio - ForwardFanRatio * fanRatio) + leftVec * (BackwardFanRatio + ForwardFanRatio * fanRatio)).normalized * maxDistance * SideRatio, Color.blue);
            Debug.DrawLine(TopCenter, TopCenter + (forwardVec * (ForwardFanRatio - ForwardFanRatio * fanRatio) + rightVec * (BackwardFanRatio + ForwardFanRatio * fanRatio)).normalized * maxDistance * SideRatio, Color.blue);
            Debug.DrawLine(TLCorner, TLCorner + (forwardVec * (ForwardFanRatio - ForwardFanRatio * fanRatio) + leftVec * (BackwardFanRatio + ForwardFanRatio * fanRatio)).normalized * maxDistance * DiagRatio, Color.blue);
            Debug.DrawLine(TRCorner, TRCorner + (forwardVec * (ForwardFanRatio - ForwardFanRatio * fanRatio) + rightVec * (BackwardFanRatio + ForwardFanRatio * fanRatio)).normalized * maxDistance * DiagRatio, Color.blue);
        }

        bool leftSideHit = hitResult[0].collider != null;
        bool rightSideHit = hitResult[1].collider != null;
        bool leftCornerHit = hitResult[2].collider != null;
        bool rightCornerHit = hitResult[3].collider != null;
        bool leftHit = leftSideHit || leftCornerHit;
        bool rightHit = rightSideHit || rightCornerHit;

        if (leftHit || rightHit) {
            successiveCollisions += 1;
        }

        Vector2 newDesiredDir = new Vector2();

        const float MinBlend = 0.05f;
        const float MaxBlend = 0.4f;

        if (leftHit) {
            float minHitDist = 9999;
            if (leftSideHit) {
                minHitDist = Mathf.Min(hitResult[0].distance, minHitDist);
            } else {
                minHitDist = Mathf.Min(hitResult[2].distance, minHitDist);
            }

            float blendRatio = Mathf.Clamp(minHitDist / maxDistance, MinBlend, MaxBlend);
            newDesiredDir = (blendRatio * desiredDir + (1.0f - blendRatio) * rightVec).normalized;
        } else if (rightHit) {
            float minHitDist = 9999;
            if (rightSideHit) {
                minHitDist = Mathf.Min(hitResult[1].distance, minHitDist);
            } else {
                minHitDist = Mathf.Min(hitResult[3].distance, minHitDist);
            }

            float blendRatio = Mathf.Clamp(minHitDist / maxDistance, MinBlend, MaxBlend);
            newDesiredDir = (blendRatio * desiredDir + (1.0f - blendRatio) * leftVec).normalized;
        } else if (!rightHit && !leftHit) {
            successiveCollisions = 0;

            newDesiredDir = desiredDir;
        }

        return newDesiredDir;
    }

    private void performActuation(Vector2 requestDir) {
        // First calculate forward and backwards arc angle based on speed
        float sqrMaxVelocityMag = Tank.Hull.Schematic.EnergyPower / Tank.TotalDrag;
        float sqrCurVelocity = Tank.Body.velocity.sqrMagnitude;

        const float minRatioCutOff = 0.4f;
        const float maxRatioCutoff = 0.7f; // TODO: later probably make a serialized field for easier tweaking
        float ratio = Mathf.Clamp(1.0f - sqrCurVelocity / sqrMaxVelocityMag, minRatioCutOff, maxRatioCutoff);

        const float startingBackwardArcAngle = 180f; // TODO: later probably make a serialized field for easier tweaking
        const float startingForwardArcAngle = 360f - startingBackwardArcAngle;

        float curBackwardArcAngle = ratio * startingBackwardArcAngle;
        float curForwardArcAngle = ratio * startingForwardArcAngle;

        // Debug stuff
        if (Application.isEditor && DebugManager.Instance.ActuationDebugOn) {
            Vector2 forwardVec = (new Vector2(0, 1)).Rotate(Tank.Body.rotation);
            Debug.DrawLine(Tank.transform.position, Tank.transform.position + (Vector3)(forwardVec.Rotate(curForwardArcAngle / 2f) * 50f), Color.blue);
            Debug.DrawLine(Tank.transform.position, Tank.transform.position + (Vector3)(forwardVec.Rotate(-curForwardArcAngle / 2f) * 50f), Color.blue);

            Vector2 backwardVec = (new Vector2(0, -1)).Rotate(Tank.Body.rotation);
            Debug.DrawLine(Tank.transform.position, Tank.transform.position + (Vector3)(backwardVec.Rotate(curBackwardArcAngle / 2f) * 50f), Color.green);
            Debug.DrawLine(Tank.transform.position, Tank.transform.position + (Vector3)(backwardVec.Rotate(-curBackwardArcAngle / 2f) * 50f), Color.green);
        }

        float angleDiffFromFront = Vector2.Angle((new Vector2(0, 1)).Rotate(Tank.Body.rotation), requestDir);
        float angleDiffFromBack = Vector2.Angle((new Vector2(0, -1)).Rotate(Tank.Body.rotation), requestDir);

        const float sigma = 10f; // TODO: later probably make a serialized field for easier tweaking

        // In this case we want the AI to continue accelerating while going towards the requested direction
        if ((curForwardArcAngle / 2f) >= angleDiffFromFront) {
            float angleToTurn = Vector2.SignedAngle((new Vector2(0, 1)).Rotate(Tank.Body.rotation), requestDir);

            if (Mathf.Abs(angleToTurn) > sigma) {
                if (Mathf.Sign(angleToTurn) > 0) {
                    Tank.Wheels.PerformPowerChange(0, 1);
                } else {
                    Tank.Wheels.PerformPowerChange(1, 0);
                }
            } else {
                Tank.Wheels.PerformPowerChange(1, 1);
            }

            // In this case we want the tank to start accelerating backwards
        } else if ((curBackwardArcAngle / 2f) >= angleDiffFromBack) {
            float angleToTurn = Vector2.SignedAngle((new Vector2(0, -1)).Rotate(Tank.Body.rotation), requestDir);

            if (Mathf.Abs(angleToTurn) > sigma) {
                if (Mathf.Sign(angleToTurn) > 0) {
                    Tank.Wheels.PerformPowerChange(-1, 0);
                } else {
                    Tank.Wheels.PerformPowerChange(0, -1);
                }
            } else {
                Tank.Wheels.PerformPowerChange(-1, -1);
            }

            // In this case we want the tank to start turning
        } else {
            float angleToTurnFromFront = Vector2.SignedAngle((new Vector2(0, 1)).Rotate(Tank.Body.rotation), requestDir);
            float angleToTurnFromBack = Vector2.SignedAngle((new Vector2(0, -1)).Rotate(Tank.Body.rotation), requestDir);

            bool turningToFront = Mathf.Abs(angleToTurnFromFront) <= Mathf.Abs(angleToTurnFromBack);
            float angle = turningToFront ? angleToTurnFromFront : angleToTurnFromBack;

            if (Mathf.Sign(angle) >= 0) {
                Tank.Wheels.PerformPowerChange(-1, 1);
            } else {
                Tank.Wheels.PerformPowerChange(1, -1);
            }
        }

        // Debug
        if (Application.isEditor && DebugManager.Instance.ActuationDebugOn) {
            Vector3 leftWheelPos = Tank.LeftWheelGO.transform.position;
            Vector3 rightWheelPos = Tank.RightWheelGO.transform.position;

            Vector3 forwardVec = (new Vector2(0, 1)).Rotate(Tank.Body.rotation);

            Debug.DrawLine(leftWheelPos, leftWheelPos + (forwardVec * 100 * Tank.Wheels.LeftCurPower), Color.magenta);
            Debug.DrawLine(rightWheelPos, rightWheelPos + (forwardVec * 100 * Tank.Wheels.RightCurPower), Color.magenta);
        }
    }

    void Update() {
        performMovement();
    }
}
