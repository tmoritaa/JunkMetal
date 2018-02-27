﻿using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

public class TankController : MonoBehaviour
{
    public enum PlayerTypes
    {
        Human,
        AI,
    }

    public PlayerTypes PlayerType
    {
        get; protected set;
    }

    [SerializeField]
    protected Tank tankPrefab;

    public Tank SelfTank {
        get; private set;
    }

    void Awake() {
        SelfTank = null;
    }

    public virtual void Init(Vector2 startPos, TankSchematic tankSchematic) {
        if (SelfTank != null) {
            Destroy(SelfTank.gameObject);
        }

        SelfTank = Instantiate(tankPrefab, this.transform, false);
        SelfTank.transform.position = startPos;

        SelfTank.Init(tankSchematic);
    }
}
