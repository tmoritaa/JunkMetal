using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

public class AITankController : TankController
{
    public Tank TargetTank
    {
        get; private set;
    }

    [SerializeField]
    private float sqrDistForDistSigma = 2500;
    public float SqrDistForDistSigma
    {
        get {
            return sqrDistForDistSigma;
        }
    }

    public ThreatMap ThreatMap
    {
        get; private set;
    }

    private List<Goal> goals = new List<Goal>();

    private Goal curGoal = null;

    private int successiveCollisions = 0;

    void Awake() {
        // For 1v1 matches, this will always be true. Maybe later we'll have to change the logic, but for now this is fine.
        TargetTank = CombatManager.Instance.HumanTankController.SelfTank;

        ThreatMap = new ThreatMap(CombatManager.Instance.Map);
    }

    protected override void Update() {
        base.Update();

        if (DebugManager.Instance.MoveTestDebugOn) {
            Vector2 targetPos = DebugManager.Instance.TargetPosForMoveTest;

            Vector2 requestDir = targetPos - (Vector2)SelfTank.transform.position;
            requestDir = AvoidWalls(requestDir);
            SelfTank.PerformActuation(requestDir.normalized);
        } else {
            if (!CombatManager.Instance.DisableMovement) {
                TargetTank.MarkCurPositionAsBlockedOnMap(CombatManager.Instance.Map);
                TargetTank.MarkCurPositionAsBlockedOnMap(ThreatMap);

                updateThreatMap();
                updateGoalsAndPerformActions();
            }

            DebugManager.Instance.RegisterObject("goal", curGoal);
        }
    }

    //protected void FixedUpdate() {
    //    // TODO: for testing only. Remove once done.
    //    //rotationTest();
    //    //velocityTest();
    //    //reachPosTest();
    //    //futurePredicTest();
    //}

    public override void Init(Vector2 startPos, TankSchematic tankSchematic) {
        base.Init(startPos, tankSchematic);

        // TODO: for now, just manually fill up goals list.
        //goals.Add(new SearchGoal(this));
        //goals.Add(new AttackGoal(this));
        goals.Add(new ManeuverGoal(this));
        curGoal = null;
    }

    private void updateThreatMap() {
        ThreatMap.ResetNodeValues();
        ThreatMap.UpdateTimeForTankToHitNode(TargetTank);
        ThreatMap.UpdateTimeToHitTargetFromNode(SelfTank, TargetTank);
        ThreatMap.MarkDangerousNodes();
    }

    private void updateGoalsAndPerformActions() {
        goals.ForEach(g => g.UpdateInsistence());

        bool goalChanged = false;

        // curGoal is null when first run. Placed here since goal init performs game operations and shouldn't actually happen during initialization.
        if (curGoal == null) {
            curGoal = goals[0];
            goalChanged = true;
        } else {
            foreach (Goal goal in goals) {
                if (goal != curGoal && goal.Insistence > curGoal.Insistence) {
                    curGoal = goal;
                    goalChanged = true;
                }
            }
        }
        
        if (goalChanged) {
            curGoal.ReInit();
        }

        AIAction[] actions = curGoal.CalcActionsToPerform();

        foreach(AIAction action in actions) {
            action.Perform();
        }
    }

    public Vector2 CalcRequestDir(Vector2 target) {
        // First calculate Seek direction
        Vector2 desiredVec = seek(target).normalized;
        desiredVec = AvoidWalls(desiredVec).normalized;

        return desiredVec;
    }

    private Vector2 seek(Vector2 targetPos) {
        Vector2 curPos = SelfTank.transform.position;
        return targetPos - curPos;
    }

    public Vector2 AvoidWalls(Vector2 desiredDir) {
        Vector2 forwardVec = SelfTank.GetForwardVec();
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

        float xAdd = SelfTank.Hull.Schematic.Size.x / 2f;
        float yAdd = SelfTank.Hull.Schematic.Size.y / 2f;
        Vector2 TopCenter = (Vector2)SelfTank.transform.position + forwardVec * yAdd;
        Vector2 TLCorner = (Vector2)SelfTank.transform.position + forwardVec * yAdd + leftVec * xAdd;
        Vector2 TRCorner = (Vector2)SelfTank.transform.position + forwardVec * yAdd + rightVec * xAdd;

        // If Collision, then take into account desired Dir and see if risk of collision
        const int WallBit = 8;
        const int PlayerBit = 9;
        const int LayerMask = 1 << WallBit | 1 << PlayerBit;

        const float DiagRatio = 0.8f;
        float fanRatio = Mathf.Clamp01((float)successiveCollisions / 100f);

        float maxDistance = Mathf.Max(SelfTank.Body.velocity.magnitude, 150f);

        RaycastHit2D[] hitResult = new RaycastHit2D[5];
        hitResult[0] = Physics2D.Raycast(TopCenter, forwardVec, maxDistance, LayerMask);
        hitResult[1] = Physics2D.Raycast(TLCorner, forwardVec, maxDistance, LayerMask);
        hitResult[2] = Physics2D.Raycast(TRCorner, forwardVec, maxDistance, LayerMask);
        hitResult[3] = Physics2D.Raycast(TLCorner, (forwardVec * (1f - fanRatio) + leftVec * fanRatio).normalized, maxDistance * DiagRatio, LayerMask);
        hitResult[4] = Physics2D.Raycast(TRCorner, (forwardVec * (1f - fanRatio) + rightVec * fanRatio).normalized, maxDistance * DiagRatio, LayerMask);

        if (Application.isEditor && DebugManager.Instance.AvoidWallsDebugOn) {
            Debug.DrawLine(TopCenter, TopCenter + forwardVec.normalized * maxDistance, Color.blue);
            Debug.DrawLine(TLCorner, TLCorner + forwardVec.normalized * maxDistance, Color.blue);
            Debug.DrawLine(TRCorner, TRCorner + forwardVec.normalized * maxDistance, Color.blue);
            Debug.DrawLine(TLCorner, TLCorner + (forwardVec * (1f - fanRatio) + leftVec * fanRatio).normalized * maxDistance * DiagRatio, Color.blue);
            Debug.DrawLine(TRCorner, TRCorner + (forwardVec * (1f - fanRatio) + rightVec * fanRatio).normalized * maxDistance * DiagRatio, Color.blue);
        }

        bool centerHit = hitResult[0].collider != null;
        bool leftFordHit = hitResult[1].collider != null;
        bool rightFordHit= hitResult[2].collider != null;
        bool leftDiagHit = hitResult[3].collider != null;
        bool rightDiagHit = hitResult[4].collider != null;
        bool leftHit = leftFordHit || leftDiagHit;
        bool rightHit = rightFordHit || rightDiagHit;

        Vector2 newDesiredDir = desiredDir;

        const float MinBlend = 0.05f;
        const float MaxBlend = 1.0f;

        successiveCollisions += (centerHit || leftHit || rightHit) ? 1 : 0;

        if (leftHit && !rightHit) {
            float minHitDist = 9999;
            if (leftFordHit) {
                minHitDist = Mathf.Min(hitResult[1].distance, minHitDist);
            }
            if (leftDiagHit) {
                minHitDist = Mathf.Min(hitResult[3].distance, minHitDist);
            }

            float blendRatio = Mathf.Clamp(minHitDist / maxDistance, MinBlend, MaxBlend);
            Vector2 dodgeVec = desiredDir.Rotate(90);
            newDesiredDir = (blendRatio * desiredDir + (1.0f - blendRatio) * dodgeVec).normalized;
        } else if (rightHit && !leftHit) {
            float minHitDist = 9999;
            if (rightFordHit) {
                minHitDist = Mathf.Min(hitResult[2].distance, minHitDist);
            }
            if (rightDiagHit) {
                minHitDist = Mathf.Min(hitResult[4].distance, minHitDist);
            }

            float blendRatio = Mathf.Clamp(minHitDist / maxDistance, MinBlend, MaxBlend);
            Vector2 dodgeVec = desiredDir.Rotate(-90);
            newDesiredDir = (blendRatio * desiredDir + (1.0f - blendRatio) * dodgeVec).normalized;
        } else if (centerHit) {
            Vector2 obstCenterPos = hitResult[0].collider.transform.position;
            Vector2 diffVec = obstCenterPos - (Vector2)SelfTank.transform.position;

            float angle = Vector2.SignedAngle(forwardVec, diffVec);

            float minHitDist = hitResult[0].distance;
            if (angle > 0) {
                float blendRatio = Mathf.Clamp(minHitDist / maxDistance, MinBlend, MaxBlend);
                Vector2 dodgeVec = desiredDir.Rotate(-90);
                newDesiredDir = (blendRatio * desiredDir + (1.0f - blendRatio) * dodgeVec).normalized;
            } else {
                float blendRatio = Mathf.Clamp(minHitDist / maxDistance, MinBlend, MaxBlend);
                Vector2 dodgeVec = desiredDir.Rotate(90);
                newDesiredDir = (blendRatio * desiredDir + (1.0f - blendRatio) * dodgeVec).normalized;
            }
        } else {
            successiveCollisions = 0;
        }

        return newDesiredDir;
    }

    //bool first = false;
    //bool second = false;
    //Vector2 targetdir;
    //Vector2 startDir;
    //float startTime = 0;
    //float expectedTime = 0;
    //bool finished = false;
    //// TODO: for testing only. Remove once done.
    //private void rotationTest() {
    //    if (finished) {
    //        return;
    //    }

    //    if (!first) {
    //        startDir = SelfTank.GetForwardVec();
    //        targetdir = SelfTank.GetBackwardVec();
    //        expectedTime = SelfTank.CalcTimeToRotate(startDir, targetdir);
    //        startTime = Time.time;
    //        first = true;
    //    } else if (first && !second) {
    //        if ((targetdir - SelfTank.GetForwardVec()).magnitude < 0.05f) {
    //            Debug.Log("Time for first half rot=" + (Time.time - startTime));
    //            Debug.Log("Calculated time for first half rot=" + expectedTime);

    //            targetdir = new Vector2(0, 1);
    //            expectedTime = SelfTank.CalcTimeToRotate(SelfTank.GetForwardVec(), targetdir);
    //            startTime = Time.time;
    //            second = true;
    //        }
    //    } else if (first && second) {
    //        if ((targetdir - SelfTank.GetForwardVec()).magnitude < 0.05f) {
    //            Debug.Log("Time for second half rot=" + (Time.time - startTime));
    //            Debug.Log("Calculated time for second half rot=" + expectedTime);
    //            finished = true;
    //        }
    //    }

    //    if (first && !second) {
    //        SelfTank.Wheels.PerformPowerChange(-1, 1);
    //    } else if (first && second) {
    //        SelfTank.Wheels.PerformPowerChange(1, -1);
    //    }
    //}

    //bool first = false;
    //float startTime = 0;
    //float expectedTimeToReachTerminal = 0;
    //private void velocityTest() {
    //    SelfTank.Wheels.PerformPowerChange(1, 1);

    //    if (!first) {
    //        Debug.Log("Start Vel=" + SelfTank.Body.velocity.magnitude);
    //        Debug.Log("Terminal Velocity=" + SelfTank.TerminalVelocity);
    //        expectedTimeToReachTerminal = SelfTank.CalcTimeToReachTerminalVelInDir(new Vector2(0, 1));
    //        Debug.Log("Calculated time to reach max vel=" + expectedTimeToReachTerminal);
    //        startTime = Time.time;
    //        first = true;
    //    } else {
    //        if (SelfTank.Body.velocity.magnitude >= SelfTank.TerminalVelocity - 0.1f) {
    //            Debug.Log("CurVel=" + SelfTank.Body.velocity.magnitude + " MaxVel=" + SelfTank.TerminalVelocity);
    //            Debug.Log("Time to reach max vel=" + (Time.time - startTime));
    //            Debug.Log("Calculated time to reach max vel=" + expectedTimeToReachTerminal);
    //        }
    //    }
    //}

    //bool first = false;
    //bool second = false;
    //float startTime = 0;
    //float expectedTime = 0;
    //bool finished = false;
    //Vector2 testTargetPos = new Vector2(300, 0);
    //private void reachPosTest() {
    //    if (finished) {
    //        return;
    //    }

    //    if (!second) {
    //        SelfTank.Wheels.PerformPowerChange(1, 1);
    //    } else {
    //        SelfTank.Wheels.PerformPowerChange(-1, -1);
    //    }

    //    if (!first) {
    //        expectedTime = SelfTank.CalcTimeToReachPosWithNoRot(testTargetPos);
    //        startTime = Time.time;
    //        first = true;
    //    } else if (!second) {
    //        float diffMag = ((Vector2)SelfTank.transform.position - testTargetPos).magnitude;
    //        if (diffMag < 5) {
    //            Debug.Log("Time to reach first target=" + (Time.time - startTime));
    //            Debug.Log("Calculated time to reach first target=" + expectedTime);

    //            second = true;
    //            testTargetPos = new Vector2(300, -600);
    //            expectedTime = SelfTank.CalcTimeToReachPosWithNoRot(testTargetPos);
    //            startTime = Time.time;
    //        }
    //    } else {
    //        float diffMag = ((Vector2)SelfTank.transform.position - testTargetPos).magnitude;
    //        if (diffMag < 5) {
    //            Debug.Log("Time to reach second target=" + (Time.time - startTime));
    //            Debug.Log("Calculated time to reach second target=" + expectedTime);
    //            finished = true;
    //        }
    //    }
    //}

    //float startTime = 0;
    //bool first = false;
    //List<bool> reached = new List<bool>();
    //List<float> angles = new List<float>();
    //float curAngle = 0;
    //private void futurePredicTest() {
    //    if (!first) {
    //        angles = new List<float> { -45f, 0, 0, -45f, 0, 0, 45f, 45f, 0, -45f, 135f, 180f, 180f, 45f, 0, 0};
    //        curAngle = angles[0];

    //        List<Vector2> futurePos = new List<Vector2>();
    //        TankStateInfo stateInfo = new TankStateInfo(SelfTank);
    //        foreach (float angle in angles) {
    //            Vector2 vec = stateInfo.ForwardVec.Rotate(angle);
    //            stateInfo = AIUtility.CalcPosInFutureWithRequestedDir(vec, 0.5f, stateInfo);
    //            futurePos.Add(stateInfo.Pos);
    //        }

    //        DebugManager.Instance.RegisterObject("test_future_pos_lis", futurePos);

    //        startTime = Time.time;
    //        first = true;
    //    } else if (reached.Count + 1 < angles.Count) {
    //        int count = reached.Count + 1;
    //        float time = count * 0.5f;

    //        if (Time.time - startTime >= time) {
    //            reached.Add(true);
    //            curAngle = angles[reached.Count];
    //        }
    //    }

    //    SelfTank.PerformActuation(SelfTank.GetForwardVec().Rotate(curAngle));
    //}
}