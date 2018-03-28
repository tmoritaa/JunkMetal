using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

[RequireComponent(typeof(Collider2D), typeof(Rigidbody2D))]
public class Bullet : MonoBehaviour
{
    public enum BulletTypes
    {
        Ballistic,
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
    public Rigidbody2D Body
    {
        get {
            return body;
        }
    }
    
    private bool isBeingDestroyed = false;

    private Vector2 firePos = new Vector2();
    private float range = 0;

    private int damage = 0;

    private float hitImpulse = 0;

    private bool applyImpulseNextFrame = false;
    private Vector2 impulseVector;

    void Awake() {
        this.body = GetComponent<Rigidbody2D>();
    }

    void Update() {
        float travelDistSqr = ((Vector2)this.transform.position - firePos).sqrMagnitude;

        bool travelledRange = travelDistSqr > range * range;

        if (!isBeingDestroyed && travelledRange) {
            destroySelf();
        }
    }

    void FixedUpdate() {
        if (applyImpulseNextFrame) {
            this.body.AddForce(impulseVector, ForceMode2D.Impulse);
            applyImpulseNextFrame = false;
        }
    }

    public void Init(Tank _owner) {
        Owner = _owner;
        this.gameObject.transform.position = Owner.transform.position;
    }

    public void Fire(Vector2 forwardVec, Vector2 firePosOffset, float shootForce, float recoilImpulse, float _hitImpulse, float _range, int _damage) {
        range = _range;
        damage = _damage;
        hitImpulse = _hitImpulse;
        firePos = Owner.transform.position + (Vector3)firePosOffset;
        this.body.position = firePos;
        this.impulseVector = forwardVec.normalized * shootForce;
        applyImpulseNextFrame = true;

        float angle = Vector2.SignedAngle(new Vector2(0, 1).Rotate(this.body.rotation), forwardVec);
        this.body.rotation = angle;

        Vector2 backVec = forwardVec.Rotate(180);
        Owner.Body.AddForce(backVec.normalized * recoilImpulse, ForceMode2D.Impulse);
    }

    private void OnCollisionEnter2D(Collision2D collision) {
        if (!isBeingDestroyed && collision.gameObject != Owner.gameObject) {
            if (collision.collider.GetComponent<Tank>() != null) {
                Tank tank = collision.collider.GetComponent<Tank>();

                Vector2 avgContactPt = new Vector2();
                foreach (ContactPoint2D contactPt in collision.contacts) {
                    avgContactPt += contactPt.point;
                }
                avgContactPt /= collision.contacts.Length;

                Vector2 impulseDir = (avgContactPt - firePos).normalized;
                tank.Body.AddForceAtPosition(impulseDir * hitImpulse, avgContactPt, ForceMode2D.Impulse);

                tank.Damage(damage);
            }
            destroySelf();
        }
    }

    private void destroySelf() {
        GameObject.Destroy(this.gameObject);
        isBeingDestroyed = true;
    }
}
