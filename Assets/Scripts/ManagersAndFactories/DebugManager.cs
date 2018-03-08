﻿using System;
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
    private bool ThreatMapDebugOn = true;

    [SerializeField]
    private bool ThreatMapTargetToHitPosDebugOn = true;

    [SerializeField]
    private bool ThreatMapPosToHitTargetDebugOn = true;

    [SerializeField]
    private bool ThreatMapDiffMapDebugOn = true;

    [SerializeField]
    private bool ThreatMapTargetToHitPosNoReloadDebugOn = true;

    [SerializeField]
    private bool ThreatMapTimeDiffDangerousNodesDebugOn = true;

    [SerializeField]
    private bool ThreatMapStrictlyDangerousNodesDebugOn = true;

    [SerializeField]
    private bool displayAITankRange = true;

    [SerializeField]
    private bool ManeuverGoalDebugOn = true;

    [SerializeField]
    private bool ManeuverPathDebugOn = true;

    [SerializeField]
    private bool ManeuverDisplayDiffNodesDebugOn = true;

    void Awake() {
        instance = this;
    }

    void OnDrawGizmos() {
        if (Application.isPlaying) {
            Color origColor = Gizmos.color;

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

            if (ThreatMapDebugOn) {
                ThreatMap map = CombatManager.Instance.AITankController.ThreatMap;

                foreach (Node _node in map.MapArray) {
                    ThreatNode node = (ThreatNode)_node;

                    if (ThreatMapTargetToHitPosDebugOn) {
                        if (node.TimeForTargetToHitNode <= ThreatMap.MaxTimeInSecs) {
                            Color color = new Color((ThreatMap.MaxTimeInSecs - node.TimeForTargetToHitNode)/ThreatMap.MaxTimeInSecs, 0, node.TimeForTargetToHitNode/ThreatMap.MaxTimeInSecs);
                            Gizmos.color = color;

                            Gizmos.DrawWireSphere(map.NodeToPosition(node), 10);
                        }
                    }

                    if (ThreatMapPosToHitTargetDebugOn) {
                        if (node.TimeToHitTargetFromNode <= ThreatMap.MaxTimeInSecs) {
                            Color color = new Color((ThreatMap.MaxTimeInSecs - node.TimeToHitTargetFromNode)/ThreatMap.MaxTimeInSecs, 0, node.TimeToHitTargetFromNode/ThreatMap.MaxTimeInSecs);
                            Gizmos.color = color;

                            Gizmos.DrawWireSphere(map.NodeToPosition(node), 10);
                        }
                    }

                    if (ThreatMapDiffMapDebugOn) {
                        float diffVal = node.GetTimeDiffForHittingTarget();
                        if (diffVal > 0) {
                            Color color = new Color(Mathf.Clamp01(diffVal / ThreatMap.MaxTimeInSecs), 0, Mathf.Clamp01((ThreatMap.MaxTimeInSecs - diffVal)/ ThreatMap.MaxTimeInSecs));
                            Gizmos.color = color;

                            Gizmos.DrawWireSphere(map.NodeToPosition(node), 10);
                        }
                    }

                    if (ThreatMapTargetToHitPosNoReloadDebugOn) {
                        if (node.TimeForTargetToHitNodeNoReload <= ThreatMap.MaxTimeInSecs) {
                            Color color = new Color((ThreatMap.MaxTimeInSecs - node.TimeForTargetToHitNodeNoReload) / ThreatMap.MaxTimeInSecs, 0, node.TimeForTargetToHitNodeNoReload / ThreatMap.MaxTimeInSecs);
                            Gizmos.color = color;

                            Gizmos.DrawWireSphere(map.NodeToPosition(node), 10);
                        }
                    }

                    if (ThreatMapTimeDiffDangerousNodesDebugOn && node.Marked) {
                        if (node.TimeDiffDangerous) {
                            Gizmos.color = Color.red;
                        } else {
                            Gizmos.color = Color.blue;
                        }

                        Gizmos.DrawSphere(map.NodeToPosition(node), 10);
                    }

                    if (ThreatMapStrictlyDangerousNodesDebugOn && node.Marked) {
                        if (node.StrictlyDangerous) {
                            Gizmos.color = Color.red;
                        } else {
                            Gizmos.color = Color.blue;
                        }

                        Gizmos.DrawSphere(map.NodeToPosition(node), 10);
                    }
                }
            }

            if (ManeuverGoalDebugOn && CombatManager.Instance.AITankController.CurGoal != null && CombatManager.Instance.AITankController.CurGoal.GetType() == typeof(ManeuverGoal)) {
                ManeuverGoal goal = (ManeuverGoal)CombatManager.Instance.AITankController.CurGoal;

                Gizmos.color = Color.magenta;

                Gizmos.DrawWireSphere(goal.CenterOfZone, 20);

                if (ManeuverPathDebugOn) {
                    // TODO: a bit hacky right now. Maybe we can clean this up once we have blackboards.
                    List<Node> path = goal.Path;

                    foreach (Node node in path) {
                        Gizmos.DrawWireSphere(CombatManager.Instance.Map.NodeToPosition(node), 15);
                    }
                }

                if (ManeuverDisplayDiffNodesDebugOn) {
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
