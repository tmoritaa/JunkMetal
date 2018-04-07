using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

public class BulletInstanceHandler : MonoBehaviour
{
    [SerializeField]
    private BulletFactory bulletFactory;

    private static BulletInstanceHandler instance;
    public static BulletInstanceHandler Instance
    {
        get {
            return instance;
        }
    }

    public List<Bullet> BulletInstances
    {
        get; private set;
    }

    void Awake() {
        instance = this;

        BulletInstances = new List<Bullet>();
    }

    public Bullet CreateBullet(Bullet.BulletTypes bType) {
        Bullet bullet = bulletFactory.CreateBullet(bType);

        BulletInstances.Add(bullet);

        return bullet;
    }

    public void DestroyBullet(Bullet bullet) {
        BulletInstances.Remove(bullet);
        GameObject.Destroy(bullet.gameObject);
    }
}
