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
    private Bullet bulletPrefab;

    private static BulletFactory instance;
    public static BulletFactory Instance {
        get {
            return instance;
        }
    }

    void Awake() {
        instance = this;    
    }

    public Bullet CreateBullet(Tank owningTank) {
        Bullet bullet = GameObject.Instantiate(bulletPrefab);
        bullet.transform.SetParent(bulletsRoot, false);
        bullet.Init(owningTank);
        return bullet;
    }
}