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

    private List<Goal> goals = new List<Goal>();

    private Goal curGoal = null;
    // NOTE: only for debugging.
    public Goal CurGoal
    {
        get { return curGoal; }
    }

    void Awake() {
        // For 1v1 matches, this will always be true. Maybe later we'll have to change the logic, but for now this is fine.
        TargetTank = GameManager.Instance.HumanTankController.Tank;
    }

    void Update() {
        updateGoalsAndPerformActions();
    }

    public override void Init(Vector2 startPos, HullPart _body, TurretPart _turret, WheelPart _wheels) {
        base.Init(startPos, _body, _turret, _wheels);

        // TODO: for now, just manually fill up goals list.
        //goals.Add(new SearchGoal(this));
        //goals.Add(new AttackGoal(this));
        goals.Add(new DodgeGoal(this));
        curGoal = null;
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
            curGoal.Init();
        }

        AIAction[] actions = curGoal.CalcActionsToPerform();

        foreach(AIAction action in actions) {
            action.Perform();
        }
    }

    public void SmoothPath(List<Node> path) {
        const int WallBit = 8;
        const int LayerMask = 1 << WallBit;

        int removeCount = 0;
        for (int i = 0; i < path.Count; ++i) {
            Node node = path[i];

            Vector2 leftVec = Tank.GetForwardVec().Rotate(-90).normalized;
            Vector2 rightVec = Tank.GetForwardVec().Rotate(90).normalized;

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

    public Vector2 CalcRequestDir(Vector2 target) {
        // First calculate Seek direction
        Vector2 desiredVec = seek(target).normalized;
        desiredVec = AvoidWalls(desiredVec).normalized;

        return desiredVec;
    }

    private Vector2 seek(Vector2 targetPos) {
        Vector2 curPos = Tank.transform.position;
        return targetPos - curPos;
    }

    public Vector2 AvoidWalls(Vector2 desiredDir) {
        Vector2 forwardVec = Tank.GetForwardVec();
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

    // Used http://danikgames.com/blog/how-to-intersect-a-moving-target-in-2d/ as a reference.
    public static Vector2 CalculateTargetPos(Tank tank, WeaponPart part, Tank targetTank) {
        // NOTE: Since bullet mass is always 1, shoot impulse is directly the terminal velocity of the bullet
        float weaponTerminalVel = part.Schematic.ShootImpulse;

        Vector2 diffVec = (Vector2)targetTank.transform.position - part.CalculateFirePos();
        Vector2 abVec = diffVec.normalized;

        Vector2 targetVel = targetTank.Body.velocity;
        Vector2 uj = (Vector2.Dot(targetVel, abVec) / abVec.magnitude) * abVec;
        Vector2 ui = targetVel - uj;

        Vector2 vi = ui;

        float timeToHit = 999;
        Vector2 v;
        // Corner case: if it turns out the weapons is too slow to ever catch the target, we just kind of try.
        if (ui.magnitude > weaponTerminalVel) {
            v = targetVel.normalized * weaponTerminalVel;
        } else {
            Vector2 vj = abVec * Mathf.Sqrt(weaponTerminalVel * weaponTerminalVel - vi.sqrMagnitude);
            v = vi + vj;
            timeToHit = diffVec.magnitude / (vj.magnitude - uj.magnitude);
        }

        Vector2 targetPos = (Vector2)tank.transform.position + v * timeToHit;

        return targetPos;
    }
}
