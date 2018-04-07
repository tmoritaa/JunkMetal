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
    BulletPrefabScriptableObject bulletPrefabs;

    private Dictionary<Bullet.BulletTypes, Bullet> typeToBulletDict = new Dictionary<Bullet.BulletTypes, Bullet>();

    void Awake() {
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