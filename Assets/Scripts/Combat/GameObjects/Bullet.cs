using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

[RequireComponent(typeof(Collider2D), typeof(Rigidbody2D))]
public abstract class Bullet : MonoBehaviour
{
    public enum BulletTypes
    {
        Ballistic,
        Energy,
        MissileCluster,
        Missile,
    }

    public Tank Owner
    {
        get; protected set;
    }

    protected Rigidbody2D body;
    public Rigidbody2D Body
    {
        get {
            return body;
        }
    }

    protected bool isBeingDestroyed = false;

    protected virtual void Awake() {
        this.body = GetComponent<Rigidbody2D>();
    }

    public abstract void Fire(Vector2 forwardVec, Vector2 firePosOffset, WeaponPartSchematic partSchematic);

    public abstract BulletTypes GetBulletType();

    public void Init(Tank _owner) {
        Owner = _owner;
        this.gameObject.transform.position = Owner.transform.position;
    }

    protected void destroySelf() {
        GameObject.Destroy(this.gameObject);
        isBeingDestroyed = true;
    }
}
