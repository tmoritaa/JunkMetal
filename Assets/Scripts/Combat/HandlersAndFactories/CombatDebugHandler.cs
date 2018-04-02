using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

public class CombatDebugHandler : MonoBehaviour 
{
    private static CombatDebugHandler instance;
    public static CombatDebugHandler Instance
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
    private bool displayAITankRange = true;

    [SerializeField]
    private bool maneuverGoalDebugOn = true;

    [SerializeField]
    private bool maneuverCurMoveDir = true;

    [SerializeField]
    private bool maneuverNodesAtSearchTime = true;

    [SerializeField]
    private bool maneuverPathNotObstructedFilter = true;

    [SerializeField]
    private bool maneuverDestNotObstructedFilter = true;

    [SerializeField]
    private bool maneuverAwayFromWallFilter = true;

    [SerializeField]
    private bool maneuverRunawayNodes = true;

    [SerializeField]
    private bool maneuverGoingforItNode = true;

    [SerializeField]
    private bool maneuverBestNode = true;
    
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
        targetPosForMoveTest = CombatHandler.Instance.AITankController.SelfTank.transform.position;    
    }

    void Update() {
        if (Input.GetMouseButton(0)) {
            targetPosForMoveTest = CombatHandler.Instance.MainCamera.ScreenToWorldPoint(Input.mousePosition);
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
                Tank aiTank = CombatHandler.Instance.AITankController.SelfTank;
                WeaponPart part = aiTank.Hull.GetAllWeapons()[0];

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
                Tank aiTank = CombatHandler.Instance.AITankController.SelfTank;

                object obj = getRegisterdObj("actuation_arc_vectors");
                if (obj != null) {
                    List<Vector2> arcVectors = (List<Vector2>)obj;

                    Gizmos.color = Color.green;
                    Gizmos.DrawLine(aiTank.transform.position, aiTank.transform.position + (Vector3)arcVectors[0] * 50f);
                    Gizmos.DrawLine(aiTank.transform.position, aiTank.transform.position + (Vector3)arcVectors[1] * 50f);

                    Gizmos.color = Color.blue;
                    Gizmos.DrawLine(aiTank.transform.position, aiTank.transform.position + (Vector3)arcVectors[2] * 50f);
                    Gizmos.DrawLine(aiTank.transform.position, aiTank.transform.position + (Vector3)arcVectors[3] * 50f);
                }

                Vector2 size = aiTank.Hull.Schematic.Size;
                Vector2 forwardVec = aiTank.GetForwardVec();
                Vector2 leftVec = forwardVec.Rotate(90);
                Vector2 rightVec = forwardVec.Rotate(-90);

                Vector3 leftWheelPos = (Vector2)aiTank.transform.position + leftVec * size.x / 2f;
                Vector3 rightWheelPos = (Vector2)aiTank.transform.position + rightVec * size.x / 2f;

                Gizmos.color = Color.magenta;
                Gizmos.DrawLine(leftWheelPos, leftWheelPos + ((Vector3)forwardVec * 100 * aiTank.Hull.LeftCurPower));
                Gizmos.DrawLine(rightWheelPos, rightWheelPos + ((Vector3)forwardVec * 100 * aiTank.Hull.RightCurPower));
            }

            Goal curGoal = (Goal)getRegisterdObj("goal");

            if (mapDisplayDebugOn) {
                // Draw Map
                Map map = CombatHandler.Instance.AITankController.Map;
                for (int x = 0; x < map.Cols; ++x) {
                    for (int y = 0; y < map.Rows; ++y) {
                        bool blocked = !map.GetNodeAtIdx(x, y).NodeTraversable();
                        Vector2 pos = map.IdxToPosition(x, y);

                        Gizmos.color = !blocked ? Color.green : Color.red;
                        Gizmos.DrawWireCube(pos, new Vector3(map.TileDim, map.TileDim, map.TileDim));
                    }
                }
            }

            if (maneuverGoalDebugOn && curGoal != null && curGoal.GetType() == typeof(ManeuverGoal)) {
                if (maneuverCurMoveDir) {
                    object obj = getRegisterdObj("maneuver_move_dir");
                    if (obj != null) {
                        Vector2 vec = (Vector2)obj;
                        Tank aiTank = CombatHandler.Instance.AITankController.SelfTank;

                        Gizmos.color = Color.green;
                        Gizmos.DrawLine(aiTank.transform.position, (Vector2)aiTank.transform.position + vec.normalized * 50f);
                    }
                }

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

                if (maneuverAwayFromWallFilter) {
                    object obj = getRegisterdObj("maneuver_away_from_wall_filter");
                    if (obj != null) {
                        List<LookaheadNode> nodeList = (List<LookaheadNode>)obj;

                        Gizmos.color = Color.green;
                        foreach (LookaheadNode node in nodeList) {
                            Gizmos.DrawWireSphere(node.TankInfo.Pos, 20);
                        }
                    }
                }

                if (maneuverRunawayNodes) {
                    object obj = getRegisterdObj("maneuver_runaway_cost_infos");
                    if (obj != null) {
                        List<ManeuverGoal.CostInfo> infos = (List<ManeuverGoal.CostInfo>)obj;

                        float maxCost = infos.Max(c => c.Cost);

                        foreach (ManeuverGoal.CostInfo info in infos) {
                            Gizmos.color = new Color(info.Cost / maxCost, 0, 1f - info.Cost / maxCost);
                            Gizmos.DrawWireSphere(info.Node.TankInfo.Pos, 20);
                        }
                    }
                }

                if (maneuverGoingforItNode) {
                    object obj = getRegisterdObj("maneuver_going_cost_infos");
                    if (obj != null) {
                        List<ManeuverGoal.CostInfo> infos = (List<ManeuverGoal.CostInfo>)obj;

                        float maxCost = infos.Max(c => c.Cost);

                        foreach (ManeuverGoal.CostInfo info in infos) {
                            Gizmos.color = new Color(info.Cost / maxCost, 0, 1f - info.Cost / maxCost);
                            Gizmos.DrawWireSphere(info.Node.TankInfo.Pos, 20);
                        }
                    }
                }

                if (maneuverBestNode) {
                    object obj = getRegisterdObj("maneuver_best_node");
                    if (obj != null) {
                        LookaheadNode node = (LookaheadNode)obj;

                        Gizmos.color = Color.green;
                        Gizmos.DrawWireSphere(node.TankInfo.Pos, 20);
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
                Tank tank = CombatHandler.Instance.AITankController.SelfTank;

                WeaponPart maxRangeWeapon = null;
                foreach (WeaponPart part in tank.Hull.GetAllWeapons()) {
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
