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

    void Awake() {
        instance = this;
    }

    void Update() {
        if (Input.GetMouseButton(0)) {
            GameManager.Instance.AiTank.TargetPos = GameManager.Instance.MainCamera.ScreenToWorldPoint(Input.mousePosition);
        }
    }

    void OnDrawGizmos() {
        if (Application.isPlaying) {
            Color color = Gizmos.color;

            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(GameManager.Instance.AiTank.TargetPos, 30);

            Gizmos.color = color;

            // Draw Map
            TileMap map = GameManager.Instance.Map;
            for (int x = 0; x < map.Cols; ++x) {
                for (int y = 0; y < map.Rows; ++y) {
                    char value = map.GetValueAtIdx(x, y);
                    Vector2 pos = map.IdxToPosition(x, y);

                    Color tmp = Gizmos.color;

                    Gizmos.color = value == 0 ? Color.green : Color.red;
                    Gizmos.DrawWireCube(pos, new Vector3(map.TileDim, map.TileDim, map.TileDim));

                    Gizmos.color = tmp;
                }
            }
        }
    }
}
