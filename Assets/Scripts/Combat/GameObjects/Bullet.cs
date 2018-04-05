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

    public abstract void Fire(Vector2 forwardVec, Vector2 firePosOffset, WeaponPartSchematic partSchematic);

    public abstract BulletTypes GetBulletType();

    public void Init(Tank _owner) {
        Owner = _owner;
        this.gameObject.transform.position = Owner.transform.position;
    }

    protected virtual void Awake() {
        this.body = GetComponent<Rigidbody2D>();
    }

    protected void destroySelf() {
        GameObject.Destroy(this.gameObject);
        isBeingDestroyed = true;
    }
}
