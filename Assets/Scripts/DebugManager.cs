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
    private float debugMoveForce = 15000f;
    public float DebugMoveForce
    {
        get {
            return debugMoveForce;
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
        }
    }
}
