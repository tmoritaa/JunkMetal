using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

public class GlobalManager : MonoBehaviour 
{
    [SerializeField]
    private bool debugOn;

    void Start() {
        if (debugOn) {
            PlayerManager.Instance.LoadPlayerInfo();
        }
    }
}
