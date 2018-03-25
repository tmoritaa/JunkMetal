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
    private Transform animationRoot;

    void Awake() {
        instance = this;
    }

    public GameObject InstantiatePrefab(string key, Vector2 pos, float angle) {
        GameObject go = Instantiate(animationPrefabs.dictionary[key], animationRoot, false);
        go.transform.position = pos;
        go.transform.Rotate(new Vector3(0, 0, angle));

        return go;
    }
}
