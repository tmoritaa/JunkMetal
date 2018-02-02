using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

public class PrefabManager : MonoBehaviour 
{
    [SerializeField]
    BulletPrefabScriptableObject bulletPrefabs;

    private Dictionary<Bullet.BulletTypes, Bullet> typeToBulletDict = new Dictionary<Bullet.BulletTypes, Bullet>();

    void Awake() {
        foreach (Bullet bullet in bulletPrefabs.bulletPrefabs) {
            typeToBulletDict.Add(bullet.BulletType, bullet);
        }
    }
    
    public Bullet GetBulletPrefabOfType(Bullet.BulletTypes bType) {
        return typeToBulletDict[bType];
    }
}
