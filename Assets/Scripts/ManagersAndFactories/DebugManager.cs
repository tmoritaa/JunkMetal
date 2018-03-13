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
    private bool basicTankInfoDebugOn = true;

    [SerializeField]
    private bool actuationDebugOn = true;

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
    private bool maneuverNodesAtSearchTime = true;

    [SerializeField]
    private bool maneuverPathNotObstructedFilter = true;

    [SerializeField]
    private bool maneuverDestNotObstructedFilter = true;

    [SerializeField]
    private bool maneuverDoesNotCrossFireFilter = true;

    [SerializeField]
    private bool maneuverInRangeFilter = true;

    [SerializeField]
    private bool maneuverAngleFromFireFilter = true;

    [SerializeField]
    private bool maneuverAimFilter = true;

    [SerializeField]
    private bool maneuverOptimalRangeFilter = true;

    [SerializeField]
    private bool predictFutureDebugOn = true;

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

            if (basicTankInfoDebugOn) {
                Tank aiTank = CombatManager.Instance.AITankController.SelfTank;
                WeaponPart part = aiTank.Turret.GetAllWeapons()[0];

                Gizmos.color = Color.red;
                Gizmos.DrawLine(
                    part.CalculateFirePos(),
                    part.CalculateFirePos() + part.CalculateFireVec() * 50f);

                Gizmos.color = Color.green;
                Gizmos.DrawLine(
                    aiTank.transform.position,
                    (Vector2)aiTank.transform.position + aiTank.GetForwardVec() * 50f);

                Gizmos.color = Color.blue;
                Gizmos.DrawLine(
                    aiTank.transform.position,
                    (Vector2)aiTank.transform.position + aiTank.GetBackwardVec() * 50f);
            }

            if (moveTestDebugOn) {
                Gizmos.color = Color.green;

                Gizmos.DrawWireSphere(targetPosForMoveTest, 20);
            }

            if (actuationDebugOn) {
                List<Vector2> arcVectors = (List<Vector2>)getRegisterdObj("actuation_arc_vectors");
                Tank aiTank = CombatManager.Instance.AITankController.SelfTank;

                Gizmos.color = Color.green;
                Gizmos.DrawLine(aiTank.transform.position, aiTank.transform.position + (Vector3)arcVectors[0] * 50f);
                Gizmos.DrawLine(aiTank.transform.position, aiTank.transform.position + (Vector3)arcVectors[1] * 50f);

                Gizmos.color = Color.blue;
                Gizmos.DrawLine(aiTank.transform.position, aiTank.transform.position + (Vector3)arcVectors[2] * 50f);
                Gizmos.DrawLine(aiTank.transform.position, aiTank.transform.position + (Vector3)arcVectors[3] * 50f);


                Vector3 leftWheelPos = aiTank.LeftWheelGO.transform.position;
                Vector3 rightWheelPos = aiTank.RightWheelGO.transform.position;
                Vector2 forwardVec = aiTank.GetForwardVec();

                Gizmos.color = Color.magenta;
                Gizmos.DrawLine(leftWheelPos, leftWheelPos + ((Vector3)forwardVec * 100 * aiTank.Hull.LeftCurPower));
                Gizmos.DrawLine(rightWheelPos, rightWheelPos + ((Vector3)forwardVec * 100 * aiTank.Hull.RightCurPower));
            }

            Goal curGoal = (Goal)getRegisterdObj("goal");

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
                if (maneuverNodesAtSearchTime) {
                    object obj = getRegisterdObj("maneuver_nodes_at_searchtime");
                    if (obj != null) {
                        List<LookaheadNode> nodeList = (List<LookaheadNode>)obj;

                        Gizmos.color = Color.green;
                        foreach (LookaheadNode node in nodeList) {
                            Gizmos.DrawWireSphere(node.TankInfo.Pos, 20);
                        }
                    }
                }

                if (maneuverPathNotObstructedFilter) {
                    object obj = getRegisterdObj("maneuver_path_unobstructed_filter");
                    if (obj != null) {
                        List<LookaheadNode> nodeList = (List<LookaheadNode>)obj;

                        Gizmos.color = Color.green;
                        foreach (LookaheadNode node in nodeList) {
                            Gizmos.DrawWireSphere(node.TankInfo.Pos, 20);
                        }
                    }
                }

                if (maneuverDestNotObstructedFilter) {
                    object obj = getRegisterdObj("maneuver_dest_not_obstructed_filter");
                    if (obj != null) {
                        List<LookaheadNode> nodeList = (List<LookaheadNode>)obj;

                        Gizmos.color = Color.green;
                        foreach (LookaheadNode node in nodeList) {
                            Gizmos.DrawWireSphere(node.TankInfo.Pos, 20);
                        }
                    }
                }

                if (maneuverDoesNotCrossFireFilter) {
                    object obj = getRegisterdObj("maneuver_does_not_cross_fire_filter");
                    if (obj != null) {
                        List<LookaheadNode> nodeList = (List<LookaheadNode>)obj;

                        Gizmos.color = Color.green;
                        foreach (LookaheadNode node in nodeList) {
                            Gizmos.DrawWireSphere(node.TankInfo.Pos, 20);
                        }
                    }
                }
        
                if (maneuverAngleFromFireFilter) {
                    object obj = getRegisterdObj("maneuver_angle_from_fire_filter");
                    if (obj != null) {
                        List<LookaheadNode> nodeList = (List<LookaheadNode>)obj;

                        Gizmos.color = Color.green;
                        foreach (LookaheadNode node in nodeList) {
                            Gizmos.DrawWireSphere(node.TankInfo.Pos, 20);
                        }
                    }
                }

                if (maneuverInRangeFilter) {
                    object obj = getRegisterdObj("maneuver_in_range_filter");
                    if (obj != null) {
                        List<LookaheadNode> nodeList = (List<LookaheadNode>)obj;

                        Gizmos.color = Color.green;
                        foreach (LookaheadNode node in nodeList) {
                            Gizmos.DrawWireSphere(node.TankInfo.Pos, 20);
                        }
                    }
                }

                if (maneuverAimFilter) {
                    object obj = getRegisterdObj("maneuver_aim_filter");
                    if (obj != null) {
                        List<LookaheadNode> nodeList = (List<LookaheadNode>)obj;

                        Gizmos.color = Color.green;
                        foreach (LookaheadNode node in nodeList) {
                            Gizmos.DrawWireSphere(node.TankInfo.Pos, 20);
                        }
                    }
                }

                if (maneuverOptimalRangeFilter) {
                    object obj = getRegisterdObj("maneuver_optimal_range_filter");
                    if (obj != null) {
                        List<LookaheadNode> nodeList = (List<LookaheadNode>)obj;

                        Gizmos.color = Color.green;
                        foreach (LookaheadNode node in nodeList) {
                            Gizmos.DrawWireSphere(node.TankInfo.Pos, 20);
                        }
                    }
                }
            }

            if (predictFutureDebugOn) {
                object obj = getRegisterdObj("test_future_pos_lis");
                if (obj != null) {
                    List<Vector2> posList = (List<Vector2>)obj;

                    Gizmos.color = Color.green;
                    foreach (Vector2 pos in posList) {
                        Gizmos.DrawWireSphere(pos, 20);
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
