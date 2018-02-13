using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

public class SearchGoal : Goal
{
    private TileMap searchQuadrants;
    // NOTE: only for debugging.
    public TileMap SearchQuadrants
    {
        get {
            return searchQuadrants;
        }
    }

    private Node curDestQuad;

    private List<Node> path = new List<Node>();
    // NOTE: only for debugging.
    public List<Node> Path
    {
        get { return path; }
    }

    public SearchGoal(AITankController tankController) : base(tankController) 
    {}

    public override void Init() {
        TileMap map = GameManager.Instance.Map;

        searchQuadrants = new TileMap(map.MapWidth, map.MapHeight, 5);

        curDestQuad = searchQuadrants.PositionToNode(controller.Tank.transform.position);
        curDestQuad.value = (char)1;
    }

    public override void UpdateInsistence() {
        // Increases as player is not visible

        // TODO: for now just set it to 100.
        Insistence = 100;
    }

    public override AIAction[] CalcActionsToPerform() {
        List<AIAction> actions = new List<AIAction>();

        AITankController aiTankController = (AITankController)controller;

        Vector2 curPos = controller.Tank.transform.position;
        Vector2 destPos = searchQuadrants.NodeToPosition(curDestQuad);
        float sqrDistSigma = aiTankController.SqrDistForDistSigma;

        if (path.Count > 0 && (curPos- GameManager.Instance.Map.NodeToPosition(path[0])).sqrMagnitude < sqrDistSigma) {
            path.RemoveAt(0);
            aiTankController.SmoothPath(path);
        }

        // Check if destination reached, if so, pick next quadrant to go to, and plan path.
        if (path.Count == 0) {
            List<Connection> connections = searchQuadrants.FindConnectedNodes(curDestQuad);

            // If no connections, reset values in quadMap and do again.
            if (connections.Count == 0) {
                searchQuadrants.ClearNodeValues();
                connections = searchQuadrants.FindConnectedNodes(curDestQuad);
            }

            curDestQuad.value = (char)1;

            // Pick random node for next destination
            curDestQuad = connections[GlobalRandom.GetRandomNumber(0, connections.Count)].targetNode;
            destPos = searchQuadrants.NodeToPosition(curDestQuad);

            path = GameManager.Instance.Map.FindPath(controller.Tank.transform.position, destPos);
            aiTankController.SmoothPath(path);
        }

        Vector2 target = (path.Count > 0) ? GameManager.Instance.Map.NodeToPosition(path[0]) : destPos;

        Vector2 requestDir = aiTankController.CalcRequestDir(target).normalized;
        actions.Add(new GoInDirAction(requestDir, controller));

        // Also aim in direction of movement
        actions.Add(new AimAction(requestDir.normalized, controller));

        return actions.ToArray();
    }
}
