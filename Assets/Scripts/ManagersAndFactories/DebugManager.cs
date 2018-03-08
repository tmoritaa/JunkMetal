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
    private bool moveTestDebugOn = true;
    public bool MoveTestDebugOn
    {
        get {
            return moveTestDebugOn;
        }
    }

    [SerializeField]
    private bool mapDisplayDebugOn = true;

    [SerializeField]
    private bool threatMapDebugOn = true;

    [SerializeField]
    private bool threatMapTargetToHitPosDebugOn = true;

    [SerializeField]
    private bool threatMapPosToHitTargetDebugOn = true;

    [SerializeField]
    private bool threatMapDiffMapDebugOn = true;

    [SerializeField]
    private bool threatMapTargetToHitPosNoReloadDebugOn = true;

    [SerializeField]
    private bool threatMapTimeDiffDangerousNodesDebugOn = true;

    [SerializeField]
    private bool threatMapStrictlyDangerousNodesDebugOn = true;

    [SerializeField]
    private bool displayAITankRange = true;

    [SerializeField]
    private bool maneuverGoalDebugOn = true;

    [SerializeField]
    private bool maneuverPathDebugOn = true;

    [SerializeField]
    private bool maneuverDisplayDiffNodesDebugOn = true;

    private Vector2 targetPosForMoveTest = new Vector2();
    public Vector2 TargetPosForMoveTest
    {
        get {
            return targetPosForMoveTest;
        }
    }

    void Awake() {
        instance = this;
    }

    void Start() {
        targetPosForMoveTest = CombatManager.Instance.AITankController.SelfTank.transform.position;    
    }

    void Update() {
        if (Input.GetMouseButton(0)) {
            targetPosForMoveTest = CombatManager.Instance.MainCamera.ScreenToWorldPoint(Input.mousePosition);
        }    
    }

    void OnDrawGizmos() {
        if (Application.isPlaying) {
            Color origColor = Gizmos.color;

            if (moveTestDebugOn) {
                Gizmos.color = Color.green;

                Gizmos.DrawWireSphere(targetPosForMoveTest, 20);
            }

            if (mapDisplayDebugOn) {
                // Draw Map
                Map map = CombatManager.Instance.Map;
                for (int x = 0; x < map.Cols; ++x) {
                    for (int y = 0; y < map.Rows; ++y) {
                        bool blocked = !map.GetNodeAtIdx(x, y).NodeTraversable();
                        Vector2 pos = map.IdxToPosition(x, y);

                        Gizmos.color = !blocked ? Color.green : Color.red;
                        Gizmos.DrawWireCube(pos, new Vector3(map.TileDim, map.TileDim, map.TileDim));
                    }
                }

                if (CombatManager.Instance.AITankController.CurGoal != null 
                    && CombatManager.Instance.AITankController.CurGoal.GetType() == typeof(SearchGoal)) {
                    Gizmos.color = Color.blue;

                    SearchGoal goal = (SearchGoal)CombatManager.Instance.AITankController.CurGoal;

                    // TODO: a bit hacky right now. Maybe we can clean this up once we have blackboards.
                    List<Node> path = goal.Path;

                    foreach (Node node in path) {
                        Vector2 pos = CombatManager.Instance.Map.NodeToPosition(node);
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
                ThreatMap map = CombatManager.Instance.AITankController.ThreatMap;

                foreach (Node _node in map.MapArray) {
                    ThreatNode node = (ThreatNode)_node;

                    if (threatMapTargetToHitPosDebugOn) {
                        if (node.TimeForTargetToHitNode <= ThreatMap.MaxTimeInSecs) {
                            Color color = new Color((ThreatMap.MaxTimeInSecs - node.TimeForTargetToHitNode)/ThreatMap.MaxTimeInSecs, 0, node.TimeForTargetToHitNode/ThreatMap.MaxTimeInSecs);
                            Gizmos.color = color;

                            Gizmos.DrawWireSphere(map.NodeToPosition(node), 10);
                        }
                    }

                    if (threatMapPosToHitTargetDebugOn) {
                        if (node.TimeToHitTargetFromNode <= ThreatMap.MaxTimeInSecs) {
                            Color color = new Color((ThreatMap.MaxTimeInSecs - node.TimeToHitTargetFromNode)/ThreatMap.MaxTimeInSecs, 0, node.TimeToHitTargetFromNode/ThreatMap.MaxTimeInSecs);
                            Gizmos.color = color;

                            Gizmos.DrawWireSphere(map.NodeToPosition(node), 10);
                        }
                    }

                    if (threatMapDiffMapDebugOn) {
                        float diffVal = node.GetTimeDiffForHittingTarget();
                        if (diffVal > 0) {
                            Color color = new Color(Mathf.Clamp01(diffVal / ThreatMap.MaxTimeInSecs), 0, Mathf.Clamp01((ThreatMap.MaxTimeInSecs - diffVal)/ ThreatMap.MaxTimeInSecs));
                            Gizmos.color = color;

                            Gizmos.DrawWireSphere(map.NodeToPosition(node), 10);
                        }
                    }

                    if (threatMapTargetToHitPosNoReloadDebugOn) {
                        if (node.TimeForTargetToHitNodeNoReload <= ThreatMap.MaxTimeInSecs) {
                            Color color = new Color((ThreatMap.MaxTimeInSecs - node.TimeForTargetToHitNodeNoReload) / ThreatMap.MaxTimeInSecs, 0, node.TimeForTargetToHitNodeNoReload / ThreatMap.MaxTimeInSecs);
                            Gizmos.color = color;

                            Gizmos.DrawWireSphere(map.NodeToPosition(node), 10);
                        }
                    }

                    if (threatMapTimeDiffDangerousNodesDebugOn && node.Marked) {
                        if (node.TimeDiffDangerous) {
                            Gizmos.color = Color.red;
                        } else {
                            Gizmos.color = Color.blue;
                        }

                        Gizmos.DrawSphere(map.NodeToPosition(node), 10);
                    }

                    if (threatMapStrictlyDangerousNodesDebugOn && node.Marked) {
                        if (node.StrictlyDangerous) {
                            Gizmos.color = Color.red;
                        } else {
                            Gizmos.color = Color.blue;
                        }

                        Gizmos.DrawSphere(map.NodeToPosition(node), 10);
                    }
                }
            }

            if (maneuverGoalDebugOn && CombatManager.Instance.AITankController.CurGoal != null && CombatManager.Instance.AITankController.CurGoal.GetType() == typeof(ManeuverGoal)) {
                ManeuverGoal goal = (ManeuverGoal)CombatManager.Instance.AITankController.CurGoal;

                if (maneuverPathDebugOn) {
                    // TODO: a bit hacky right now. Maybe we can clean this up once we have blackboards.
                    List<Node> path = goal.Path;
                    Gizmos.color = Color.magenta;

                    foreach (Node node in path) {
                        Gizmos.DrawWireSphere(CombatManager.Instance.Map.NodeToPosition(node), 15);
                    }
                }

                if (maneuverDisplayDiffNodesDebugOn) {
                    // TODO: a bit hacky right now. Maybe we can clean this up once we have blackboards.
                    List<ThreatNode> nodes = goal.DebugDiffNodes;

                    foreach (ThreatNode node in nodes) {
                        Color color = new Color((ThreatMap.MaxTimeInSecs - node.GetTimeDiffForHittingTarget()) / ThreatMap.MaxTimeInSecs, 0, node.GetTimeDiffForHittingTarget() / ThreatMap.MaxTimeInSecs);
                        Gizmos.color = color;

                        Gizmos.DrawWireSphere(CombatManager.Instance.Map.NodeToPosition(node), 10);
                    }
                }
            }

            if (displayAITankRange) {
                Tank tank = CombatManager.Instance.AITankController.SelfTank;

                WeaponPart maxRangeWeapon = null;
                foreach (WeaponPart part in tank.Turret.GetAllWeapons()) {
                    if (maxRangeWeapon == null || maxRangeWeapon.Schematic.Range < part.Schematic.Range) {
                        maxRangeWeapon = part;
                    }
                }

                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(tank.transform.position, maxRangeWeapon.Schematic.Range);
            }

            Gizmos.color = origColor;
        }
    }
}
