using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

[RequireComponent(typeof(Collider2D), typeof(Rigidbody2D))]
public class Bullet : MonoBehaviour
{
    public enum BulletTypes
    {
        Normal,
    }

    [SerializeField]
    private BulletTypes bulletType;
    public BulletTypes BulletType
    {
        get; private set;
    }

    public Tank Owner
    {
        get; private set;
    }

    private Rigidbody2D body;
    
    private bool isBeingDestroyed = false;

    private Vector2 firePos = new Vector2();
    private float range = 0;

    private int damage = 0;

    void Awake() {
        this.body = GetComponent<Rigidbody2D>();
    }

    void Update() {
        float travelDistSqr = ((Vector2)this.transform.position - firePos).sqrMagnitude;

        bool travelledRange = travelDistSqr > range * range;

        if (!isBeingDestroyed && travelledRange)
        {
            destroySelf();
        }
    }

    public void Init(Tank _owner) {
        Owner = _owner;
        this.gameObject.transform.position = Owner.transform.position;
    }

    public void Fire(Vector2 forwardVec, float shootForce, float shootBackForce, float _range, int _damage) {
        range = _range;
        damage = _damage;
        firePos = this.transform.position;
        this.body.AddForce(forwardVec.normalized * shootForce);

        Vector2 backVec = forwardVec.Rotate(180);
        Owner.LeftWheelBody.AddForce(backVec.normalized * shootBackForce);
        Owner.RightWheelBody.AddForce(backVec.normalized * shootBackForce);
    }

    private void OnTriggerEnter2D(Collider2D collision) {
        if (!isBeingDestroyed && collision.gameObject != Owner.gameObject && collision.GetComponent<Tank>() != null) {
            Tank tank = collision.GetComponent<Tank>();
            tank.Damage(damage);
            destroySelf();
        }
    }

    private void destroySelf() {
        GameObject.Destroy(this.gameObject);
        isBeingDestroyed = true;
    }
}
