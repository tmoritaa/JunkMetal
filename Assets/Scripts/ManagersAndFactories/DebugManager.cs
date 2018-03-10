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
    private bool maneuverDirDebugOn = true;

    [SerializeField]
    private bool maneuverDodgeFilterDebugOn = true;

    [SerializeField]
    private bool maneuverAimFilterDebugOn = true;

    [SerializeField]
    private bool maneuverClusterFilterDebugOn = true;

    [SerializeField]
    private bool maneuverDistFilterDebugOn = true;

    [SerializeField]
    private bool maneuverPrevDirFilterDebugOn = true;

    private Vector2 targetPosForMoveTest = new Vector2();
    public Vector2 TargetPosForMoveTest
    {
        get {
            return targetPosForMoveTest;
        }
    }

    private Dictionary<string, object> debugObjectDict = new Dictionary<string, object>();

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

    public void RegisterObject(string key, object obj) {
        if (Application.isEditor) {
            debugObjectDict[key] = obj;
        }
    }

    private object getRegisterdObj(string key) {
        object retObj = null;
        if (debugObjectDict.ContainsKey(key)) {
            retObj = debugObjectDict[key];
        }

        return retObj;
    }

    void OnDrawGizmos() {
        if (Application.isPlaying && Application.isEditor) {
            Color origColor = Gizmos.color;

            Goal curGoal = (Goal)getRegisterdObj("goal");

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
                
                if (curGoal != null && curGoal.GetType()  == typeof(SearchGoal)) {
                    Gizmos.color = Color.blue;

                    SearchGoal goal = (SearchGoal)curGoal;

                    List<Node> path = (List<Node>)getRegisterdObj("search_path");

                    foreach (Node node in path) {
                        Vector2 pos = CombatManager.Instance.Map.NodeToPosition(node);
                        Gizmos.DrawWireSphere(pos, 15);
                    }

                    SearchMap searchQuads = (SearchMap)getRegisterdObj("search_quads");

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

                    if (threatMapTimeDiffDangerousNodesDebugOn && node.Marked && node.TimeDiffDangerLevel >= 0) {
                        float ratio = (float)node.TimeDiffDangerLevel / 2;
                        Gizmos.color = new Color((1f - ratio), 0, ratio);
                        Gizmos.DrawSphere(map.NodeToPosition(node), 10);
                    }

                    if (threatMapStrictlyDangerousNodesDebugOn && node.Marked && node.TankToNodeDangerLevel >= 0) {
                        float ratio = (float)node.TankToNodeDangerLevel / 2;
                        Gizmos.color = new Color((1f - ratio), 0, ratio);
                        Gizmos.DrawSphere(map.NodeToPosition(node), 10);
                    }
                }
            }

            if (maneuverGoalDebugOn && curGoal != null && curGoal.GetType() == typeof(ManeuverGoal)) {
                ManeuverGoal goal = (ManeuverGoal)curGoal;

                if (maneuverDirDebugOn) {
                    object obj = getRegisterdObj("maneuver_move_dir");

                    if (obj != null) {
                        Vector2 moveDir = (Vector2)obj;

                        Tank aiTank = CombatManager.Instance.AITankController.SelfTank;

                        Gizmos.color = Color.green;
                        Gizmos.DrawLine(aiTank.transform.position, aiTank.transform.position + (Vector3)moveDir * 50f);
                    }
                }

                if (maneuverDodgeFilterDebugOn) {
                    object obj = getRegisterdObj("maneuver_dodge_filter_vecs");
                    if (obj != null) {
                        List<Vector2> vecs = (List<Vector2>)obj;

                        Tank aiTank = CombatManager.Instance.AITankController.SelfTank;
                        Vector2 tankPos = aiTank.transform.position;
                        Gizmos.color = Color.blue;
                        foreach (Vector2 vec in vecs) {
                            Gizmos.DrawLine(tankPos, tankPos + vec * 50f);
                        }
                    }
                }

                if (maneuverAimFilterDebugOn) {
                    object obj = getRegisterdObj("maneuver_aim_filter_vecs");
                    if (obj != null) {
                        List<Vector2> vecs = (List<Vector2>)obj;

                        Tank aiTank = CombatManager.Instance.AITankController.SelfTank;
                        Vector2 tankPos = aiTank.transform.position;
                        Gizmos.color = Color.blue;
                        foreach (Vector2 vec in vecs) {
                            Gizmos.DrawLine(tankPos, tankPos + vec * 50f);
                        }
                    }

                    // TODO: delete once done
                    //List<Vector2> alignedVecs = (List<Vector2>)getRegisterdObj("maneuver_aligned_vecs");
                    //Gizmos.color = Color.green;
                    //foreach (Vector2 vec in alignedVecs) {
                    //    Gizmos.DrawLine(tankPos, tankPos + vec * 50f);
                    //}
                }

                if (maneuverClusterFilterDebugOn) {
                    object obj = getRegisterdObj("maneuver_cluster_filter_vecs");
                    if (obj != null) {
                        List<Vector2> vecs = (List<Vector2>)obj;

                        Tank aiTank = CombatManager.Instance.AITankController.SelfTank;
                        Vector2 tankPos = aiTank.transform.position;
                        Gizmos.color = Color.blue;
                        foreach (Vector2 vec in vecs) {
                            Gizmos.DrawLine(tankPos, tankPos + vec * 50f);
                        }
                    }

                    ThreatMap map = CombatManager.Instance.AITankController.ThreatMap;
                    var cluster = (ThreatMap.Cluster)getRegisterdObj("maneuver_picked_cluster");
                    foreach (ThreatNode node in cluster.Nodes) {
                        float ratio = (float)node.TimeDiffDangerLevel / 2;
                        Gizmos.color = new Color((1f - ratio), 0, ratio);
                        Gizmos.DrawSphere(map.NodeToPosition(node), 10);
                    }

                    Gizmos.color = Color.magenta;
                    Gizmos.DrawWireSphere(cluster.CalcCenterPos(map), 20);
                }

                if (maneuverDistFilterDebugOn) {
                    object obj = getRegisterdObj("maneuver_dist_filter_vecs");
                    if (obj != null) {
                        List<Vector2> vecs = (List<Vector2>)obj;

                        Tank aiTank = CombatManager.Instance.AITankController.SelfTank;
                        Vector2 tankPos = aiTank.transform.position;
                        Gizmos.color = Color.blue;
                        foreach (Vector2 vec in vecs) {
                            Gizmos.DrawLine(tankPos, tankPos + vec * 50f);
                        }

                        Gizmos.color = Color.green;
                        Gizmos.DrawWireSphere(aiTank.transform.position, aiTank.Turret.GetAllWeapons()[0].Schematic.Range / 2f);
                    }
                }

                if (maneuverPrevDirFilterDebugOn) {
                    object obj = getRegisterdObj("maneuver_prev_dir_filter_vecs");
                    if (obj != null) {
                        List<Vector2> vecs = (List<Vector2>)obj;

                        Tank aiTank = CombatManager.Instance.AITankController.SelfTank;
                        Vector2 tankPos = aiTank.transform.position;
                        Gizmos.color = Color.blue;
                        foreach (Vector2 vec in vecs) {
                            Gizmos.DrawLine(tankPos, tankPos + vec * 50f);
                        }
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
