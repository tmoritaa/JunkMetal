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

    private int successiveCollisions = 0;

    private ThreatMap threatMap;
    // TODO: only for debugging.
    public ThreatMap ThreatMap {
        get {
            return threatMap;
        }
    }

    private List<Goal> goals = new List<Goal>();

    private Goal curGoal = null;
    // NOTE: only for debugging.
    public Goal CurGoal
    {
        get { return curGoal; }
    }

    void Awake() {
        // For 1v1 matches, this will always be true. Maybe later we'll have to change the logic, but for now this is fine.
        TargetTank = CombatManager.Instance.HumanTankController.SelfTank;

        threatMap = new ThreatMap(CombatManager.Instance.Map);
    }

    protected override void Update() {
        base.Update();

        if (!CombatManager.Instance.DisableMovement) {
            TargetTank.MarkCurPositionAsBlockedOnMap(CombatManager.Instance.Map);
            TargetTank.MarkCurPositionAsBlockedOnMap(threatMap);

            updateThreatMap();
            updateGoalsAndPerformActions();
        }

        // TODO: for testing only. Remove once done.
        //rotationTest();
        //velocityTest();
        //reachPosTest();
    }

    public override void Init(Vector2 startPos, TankSchematic tankSchematic) {
        base.Init(startPos, tankSchematic);

        // TODO: for now, just manually fill up goals list.
        //goals.Add(new SearchGoal(this));
        goals.Add(new AttackGoal(this));
        goals.Add(new ManeuverGoal(this));
        curGoal = null;
    }

    private void updateThreatMap() {
        threatMap.ResetNodeValues();
        threatMap.UpdateTimeForTankToHitNode(TargetTank);
        threatMap.UpdateTimeToHitTargetFromNode(SelfTank, TargetTank);
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
        const float SideRatio = 1.0f;
        const float DiagRatio = 0.6f;

        const float ForwardFanRatio = 0.7f;
        const float BackwardFanRatio = 1.0f - ForwardFanRatio;

        float maxDistance = Mathf.Max(SelfTank.Body.velocity.magnitude, 150f);

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


    //bool first = false;
    //Vector2 targetdir;
    //float startTime = 0;
    //// TODO: for testing only. Remove once done.
    //private void rotationTest() {
    //    SelfTank.PerformRotation(SelfTank.GetForwardVec(), SelfTank.GetBackwardVec());
    //    if (!first) {
    //        targetdir = SelfTank.GetBackwardVec();
    //        startTime = Time.time;
    //        first = true;
    //    } else {
    //        if ((targetdir - SelfTank.GetForwardVec()).magnitude < 0.05f) {
    //            Debug.Log("Time for full rot=" + (2f * (Time.time - startTime)));

    //            float circumference = (SelfTank.Hull.Schematic.Size.x / 2f) * Mathf.PI;
    //            float timeToDoOneFullRot = circumference / SelfTank.TerminalVelocityForRotation;

    //            Debug.Log("Calculated time for full rot=" + timeToDoOneFullRot);
    //        }
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
    //Vector2 testTargetPos = new Vector2(300, 0);
    //private void reachPosTest() {
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
    //        }
    //    }
    //}
}
