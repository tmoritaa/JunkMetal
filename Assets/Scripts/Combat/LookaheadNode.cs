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

    public bool IncomingWasJet
    {
        get; private set;
    }

    public List<Node> PassedNodes
    {
        get; private set;
    }
    
    public TankStateInfo TankInfo
    {
        get; private set;
    }

    public LookaheadNode(LookaheadNode parentNode, Vector2 incomingDir, bool incomingWasJet, TankStateInfo stateInfo, float elapsedTimeFromParent, List<Node> passedNodes) {
        ParentNode = parentNode;
        IncomingDir = incomingDir;
        IncomingWasJet = incomingWasJet;
        TankInfo = stateInfo;
        ElapsedTimeFromParent = elapsedTimeFromParent;
        PassedNodes = passedNodes;
        ChildNodes = new List<LookaheadNode>();
    }

    public void PopulateChildren(Map map, float searchStepTime, List<TreeSearchMoveInfo> possibleMoves) {
        // TODO: if we ever want to do searches more than 1 layer, we should re-implement opposite direction filtering

        foreach (TreeSearchMoveInfo moveInfo in possibleMoves) {
            Vector2 vec = moveInfo.BaseDir.Rotate(TankInfo.Rot);

            List<Vector2> passedPos = new List<Vector2>();

            TankStateInfo newStateInfo;
            if (!moveInfo.IsJet) {
                newStateInfo = AIUtility.CalcPosInFutureWithRequestedDir(vec, searchStepTime, TankInfo, out passedPos);
            } else {
                newStateInfo = AIUtility.CalcPosInFutureWithRequestedDirJets(vec, searchStepTime, TankInfo, out passedPos);
            }

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
                ChildNodes.Add(new LookaheadNode(this, vec, moveInfo.IsJet, newStateInfo, searchStepTime, passedNodes.ToList()));
            }
        }
    }

    public bool PathNotObstructed() {
        bool obstructed = false;
        foreach (Node node in PassedNodes) {
            if (!node.NodeTraversable()) {
                obstructed = true;
                break;
            }
        }

        return !obstructed;
    }

    public bool HasOverlappedTargetWithWeapon(Vector2 targetPos, WeaponPart part) {
        LookaheadNode curSearchNode = this;

        bool crossedTarget = false;
        while (curSearchNode != null && curSearchNode.ParentNode != null) {
            Vector2 originFireVec = part.OwningTank.Hull.Schematic.OrigWeaponDirs[part.EquipIdx];
            float range = part.Schematic.Range;

            Vector2 curFireVec = originFireVec.Rotate(curSearchNode.TankInfo.Rot);
            Vector2 parentFireVec = originFireVec.Rotate(curSearchNode.ParentNode.TankInfo.Rot);

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

    public LookaheadNode GetNodeOneStepAfterRoot() {
        LookaheadNode curNode = this;

        while (curNode.ParentNode.ParentNode != null) {
            curNode = curNode.ParentNode;
        }

        return curNode;
    }
}
