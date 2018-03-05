﻿using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

public class SearchGoal : Goal
{
    private SearchMap searchQuadrants;
    // NOTE: only for debugging.
    public SearchMap SearchQuadrants
    {
        get {
            return searchQuadrants;
        }
    }

    private SearchNode curDestQuad;

    private List<Node> path = new List<Node>();
    // NOTE: only for debugging.
    public List<Node> Path
    {
        get { return path; }
    }

    public SearchGoal(AITankController tankController) : base(tankController) 
    {}

    public override void ReInit() {
        Map map = CombatManager.Instance.Map;

        float tileDim = map.MapWidth / (float)5;
        searchQuadrants = new SearchMap(map.MapWidth, map.MapHeight, tileDim);

        curDestQuad = (SearchNode)searchQuadrants.PositionToNode(controller.SelfTank.transform.position);
        curDestQuad.searched = true;
    }

    public override void UpdateInsistence() {
        // Increases as player is not visible
        Insistence = 0;
    }

    public override AIAction[] CalcActionsToPerform() {
        List<AIAction> actions = new List<AIAction>();

        AITankController aiTankController = (AITankController)controller;

        Vector2 curPos = controller.SelfTank.transform.position;
        Vector2 destPos = searchQuadrants.NodeToPosition(curDestQuad);
        float sqrDistSigma = aiTankController.SqrDistForDistSigma;

        if (path.Count > 0 && (curPos- CombatManager.Instance.Map.NodeToPosition(path[0])).sqrMagnitude < sqrDistSigma) {
            path.RemoveAt(0);
            AIUtility.SmoothPath(path, controller.SelfTank);
        }

        // Check if destination reached, if so, pick next quadrant to go to, and plan path.
        if (path.Count == 0) {
            List<Connection> connections = searchQuadrants.FindConnectedNodes(curDestQuad);

            // If no connections, reset values in quadMap and do again.
            if (connections.Count == 0) {
                searchQuadrants.ResetNodeValues();
                connections = searchQuadrants.FindConnectedNodes(curDestQuad);
            }

            curDestQuad.searched = true;

            // Pick random node for next destination
            curDestQuad = (SearchNode)connections[GlobalRandom.GetRandomNumber(0, connections.Count)].targetNode;
            destPos = searchQuadrants.NodeToPosition(curDestQuad);

            path = CombatManager.Instance.Map.FindPath(controller.SelfTank.transform.position, destPos);
            AIUtility.SmoothPath(path, controller.SelfTank);
        }

        Vector2 target = (path.Count > 0) ? CombatManager.Instance.Map.NodeToPosition(path[0]) : destPos;

        Vector2 requestDir = aiTankController.CalcRequestDir(target).normalized;
        actions.Add(new GoInDirAction(requestDir, controller));

        // Also aim in direction of movement
        actions.Add(new AimAction(requestDir.normalized, controller));

        return actions.ToArray();
    }
}