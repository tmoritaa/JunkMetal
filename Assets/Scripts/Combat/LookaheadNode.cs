using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

public class LookaheadNode
{
    public float ElapsedTimeFromParent
    {
        get; private set;
    }

    public List<LookaheadNode> ChildNodes
    {
        get; private set;
    }

    public LookaheadNode ParentNode
    {
        get; private set;
    }

    public Vector2 IncomingDir
    {
        get; private set;
    }

    private List<Node> passedNodes = new List<Node>();
    
    public TankStateInfo TankInfo
    {
        get; private set;
    }

    public LookaheadNode(LookaheadNode _parentNode, Vector2 _incomingDir, TankStateInfo _stateInfo, float _elapsedTimeFromParent, List<Node> _passedNodes) {
        ParentNode = _parentNode;
        IncomingDir = _incomingDir;
        TankInfo = _stateInfo;
        ElapsedTimeFromParent = _elapsedTimeFromParent;
        passedNodes = _passedNodes;
        ChildNodes = new List<LookaheadNode>();
    }

    public void PopulateChildren(Map map, float searchStepTime) {
        List<Vector2> possibleDirs = new List<Vector2>() {
            TankInfo.ForwardVec,
            TankInfo.ForwardVec.Rotate(180f),
            TankInfo.ForwardVec.Rotate(45f),
            TankInfo.ForwardVec.Rotate(135f),
            TankInfo.ForwardVec.Rotate(-45f),
            TankInfo.ForwardVec.Rotate(-135f)
        };

        // Remove the largest angle difference which should correspond to the opposite direction of the incoming direction
        if (IncomingDir.magnitude > 0) {
            float biggestAngDiff = 0;
            int biggestVecIdx = -1;

            for (int i = 0; i < possibleDirs.Count; ++i) {
                Vector2 vec = possibleDirs[i];

                float angle = Vector2.Angle(IncomingDir, vec);
                if (angle > biggestAngDiff) {
                    biggestAngDiff = angle;
                    biggestVecIdx = i;
                }
            }

            possibleDirs.RemoveAt(biggestVecIdx);
        }

        foreach (Vector2 vec in possibleDirs) {
            List<Vector2> passedPos = new List<Vector2>();
            TankStateInfo newStateInfo = AIUtility.CalcPosInFutureWithRequestedDir(vec, searchStepTime, TankInfo, out passedPos);

            HashSet<Node> passedNodes = new HashSet<Node>();
            bool passedOutOfBounds = false;
            foreach (Vector2 pos in passedPos) {
                if (map.IsPositionWithinBounds(pos)) {
                    passedNodes.Add(map.PositionToNode(pos));
                } else {
                    passedOutOfBounds = true;
                    break;
                }
            }

            if (!passedOutOfBounds) {
                ChildNodes.Add(new LookaheadNode(this, vec, newStateInfo, searchStepTime, passedNodes.ToList()));
            }
        }
    }

    public bool PathNotObstructed() {
        bool obstructed = false;
        foreach (Node node in passedNodes) {
            if (!node.NodeTraversable()) {
                obstructed = true;
                break;
            }
        }

        return !obstructed;
    }

    public bool PathFromRootDoesNotCrossDangerNode() {
        LookaheadNode curSearchNode = this;

        bool crossedDanger = false;
        while (curSearchNode != null) {
            foreach (Node node in curSearchNode.passedNodes) {
                ThreatNode tNode = (ThreatNode)node;
                if (tNode.TimeDiffDangerLevel == 0) {
                    crossedDanger = true;
                    break;
                }
            }

            if (crossedDanger) {
                break;
            }

            curSearchNode = curSearchNode.ParentNode;
        }

        return !crossedDanger;
    }

    public bool HasOverlappedTargetWithWeapon(Vector2 targetPos, WeaponPart part) {
        LookaheadNode curSearchNode = this;

        bool crossedTarget = false;
        while (curSearchNode != null && curSearchNode.ParentNode != null) {
            Vector2 originFireVec = part.OwningTank.Turret.Schematic.OrigWeaponDirs[part.TurretIdx];
            float range = part.Schematic.Range;

            Vector2 curFireVec = originFireVec.Rotate(curSearchNode.TankInfo.Rot); // TODO: this is assuming turret loses rotation functionality. If we keep it in the end, don't forget to adjust this.
            Vector2 parentFireVec = originFireVec.Rotate(curSearchNode.ParentNode.TankInfo.Rot); // TODO: this is assuming turret loses rotation functionality. If we keep it in the end, don't forget to adjust this.

            Vector2 curToTargetVec = targetPos - curSearchNode.TankInfo.Pos;
            Vector2 parentToTargetVec = targetPos - curSearchNode.ParentNode.TankInfo.Pos;

            float curAngleDiff = Vector2.SignedAngle(curFireVec, curToTargetVec);
            float parentAngleDiff = Vector2.SignedAngle(parentFireVec, parentToTargetVec);

            bool curFacing = Mathf.Abs(curAngleDiff) < 90f;

            if (curFacing && Mathf.Sign(curAngleDiff) != Mathf.Sign(parentAngleDiff) && curToTargetVec.magnitude < range && parentToTargetVec.magnitude < range) {
                crossedTarget = true;
                break;
            }

            curSearchNode = curSearchNode.ParentNode;
        }

        return crossedTarget;
    }
}
