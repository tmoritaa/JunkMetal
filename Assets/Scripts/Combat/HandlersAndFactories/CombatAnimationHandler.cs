using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

public class CombatAnimationHandler : MonoBehaviour 
{
    private static CombatAnimationHandler instance;
    public static CombatAnimationHandler Instance
    {
        get {
            return instance;
        }
    }

    [SerializeField]
    private StringGODictionary animationPrefabs;

    [SerializeField]
    private Transform defaultAnimRoot;

    void Awake() {
        instance = this;
    }

    public GameObject InstantiatePrefab(string key, Vector2 pos, float angle, Transform root=null, bool local=false) {
        Transform animRoot = (root != null) ? root : defaultAnimRoot;

        GameObject go = Instantiate(animationPrefabs.dictionary[key], animRoot, false);

        go.transform.Rotate(new Vector3(0, 0, angle));

        if (local) {
            go.transform.localPosition = pos;
        } else {
            go.transform.position = pos;
        }

        return go;
    }
}
