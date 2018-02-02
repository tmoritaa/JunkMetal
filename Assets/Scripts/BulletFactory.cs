using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;

public class BulletFactory : MonoBehaviour
{
    [SerializeField]
    private Transform bulletsRoot;

    [SerializeField]
    private PrefabManager prefabManager;

    private static BulletFactory instance;
    public static BulletFactory Instance {
        get {
            return instance;
        }
    }

    void Awake() {
        instance = this;    
    }

    public Bullet CreateBullet(Tank owningTank, Bullet.BulletTypes bType) {
        Bullet prefab = prefabManager.GetBulletPrefabOfType(bType);

        Bullet bullet = GameObject.Instantiate(prefab);
        bullet.transform.SetParent(bulletsRoot, false);
        bullet.Init(owningTank);
        return bullet;
    }
}