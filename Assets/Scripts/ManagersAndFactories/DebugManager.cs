using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

public class DebugManager : MonoBehaviour 
{
    private static DebugManager instance;
    public static DebugManager Instance
    {
        get {
            return instance;
        }
    }

    [SerializeField]
    private bool actuationDebugOn = true;
    public bool ActuationDebugOn
    {
        get {
            return actuationDebugOn;
        }
    }

    [SerializeField]
    private bool avoidWallsDebugOn = true;
    public bool AvoidWallsDebugOn
    {
        get {
            return avoidWallsDebugOn;
        }
    }

    [SerializeField]
    private bool mapDisplayDebugOn = true;

    [SerializeField]
    private bool threatMapDebugOn = true;

    void Awake() {
        instance = this;
    }

    void OnDrawGizmos() {
        if (Application.isPlaying) {
            Color origColor = Gizmos.color;

            if (mapDisplayDebugOn) {
                // Draw Map
                Map map = GameManager.Instance.Map;
                for (int x = 0; x < map.Cols; ++x) {
                    for (int y = 0; y < map.Rows; ++y) {
                        bool blocked = map.GetNodeAtIdx(x, y).blocked;
                        Vector2 pos = map.IdxToPosition(x, y);

                        Gizmos.color = !blocked ? Color.green : Color.red;
                        Gizmos.DrawWireCube(pos, new Vector3(map.TileDim, map.TileDim, map.TileDim));
                    }
                }

                if (GameManager.Instance.AITankController.CurGoal.GetType() == typeof(SearchGoal)) {
                    Gizmos.color = Color.blue;

                    SearchGoal goal = (SearchGoal)GameManager.Instance.AITankController.CurGoal;

                    // TODO: a bit hacky right now. Maybe we can clean this up once we have blackboards.
                    List<Node> path = goal.Path;

                    foreach (Node node in path) {
                        Vector2 pos = GameManager.Instance.Map.NodeToPosition(node);
                        Gizmos.DrawWireSphere(pos, 15);
                    }

                    SearchMap searchQuads = goal.SearchQuadrants;

                    foreach (Node _node in searchQuads.MapArray) {
                        SearchNode node = (SearchNode)_node;
                        if (!node.searched) {
                            Gizmos.color = Color.blue;
                        } else {
                            Gizmos.color = Color.red;
                        }

                        Gizmos.DrawWireCube(searchQuads.NodeToPosition(node), new Vector3(searchQuads.TileDim, searchQuads.TileDim, searchQuads.TileDim));
                    }
                }
            }

            if (threatMapDebugOn) {
                ThreatMap map = GameManager.Instance.AITankController.ThreatMap;

                foreach (Node _node in map.MapArray) {
                    ThreatNode node = (ThreatNode)_node;
                    if (node.ThreatValue > 0) {
                        Color color = new Color(node.ThreatValue, 0, 1f - node.ThreatValue);
                        Gizmos.color = color;

                        Gizmos.DrawWireSphere(map.NodeToPosition(node), 10);
                    }
                }
            }

            Gizmos.color = origColor;
        }
    }
}
