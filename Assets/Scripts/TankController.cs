using System;
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

    public Tank Tank {
        get; private set;
    }

    void Awake() {
        Tank = null;
    }

    public virtual void Init(Vector2 startPos, HullPart _body, TurretPart _turret, WheelPart _wheels) {
        if (Tank != null) {
            Destroy(Tank.gameObject);
        }

        Tank = Instantiate(tankPrefab, this.transform, false);
        Tank.transform.position = startPos;

        Tank.Init(_body, _turret, _wheels);
    }
}
