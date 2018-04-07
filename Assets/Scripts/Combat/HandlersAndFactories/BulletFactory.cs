﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;

public class BulletFactory : MonoBehaviour
{
    [SerializeField]
    private Transform bulletsRoot;
    
    [SerializeField]
    BulletPrefabScriptableObject bulletPrefabs;

    private Dictionary<Bullet.BulletTypes, Bullet> typeToBulletDict = new Dictionary<Bullet.BulletTypes, Bullet>();

    private static BulletFactory instance;
    public static BulletFactory Instance {
        get {
            return instance;
        }
    }

    void Awake() {
        instance = this;

        foreach (Bullet bullet in bulletPrefabs.bulletPrefabs) {
            typeToBulletDict.Add(bullet.GetBulletType(), bullet);
        }
    }

    public Bullet CreateBullet(Bullet.BulletTypes bType) {
        Bullet prefab = typeToBulletDict[bType];

        Bullet bullet = GameObject.Instantiate(prefab);
        bullet.transform.SetParent(bulletsRoot, false);
        return bullet;
    }
}