using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

public class BallisticBullet : Bullet
{
    private Vector2 firePos = new Vector2();
    private float range = 0;

    private int damage = 0;

    private float hitImpulse = 0;

    private bool applyImpulseNextFrame = false;
    private Vector2 impulseVector;

    protected override void Awake() {
        base.Awake();
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

    public override BulletTypes GetBulletType() {
        return BulletTypes.Ballistic;
    }

    public override void Fire(Vector2 forwardVec, Vector2 firePosOffset, WeaponPartSchematic partSchematic) {
        range = partSchematic.Range;
        damage = partSchematic.Damage;
        hitImpulse = partSchematic.HitImpulse;
        firePos = Owner.transform.position + (Vector3)firePosOffset;
        this.body.position = firePos;
        this.impulseVector = forwardVec.normalized * partSchematic.ShootImpulse;
        applyImpulseNextFrame = true;

        float angle = Vector2.SignedAngle(new Vector2(0, 1).Rotate(this.body.rotation), forwardVec);
        this.body.rotation = angle;

        Vector2 backVec = forwardVec.Rotate(180);
        Owner.Body.AddForce(backVec.normalized * partSchematic.RecoilImpulse, ForceMode2D.Impulse);
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

                CombatAnimationHandler.Instance.InstantiatePrefab("spark", avgContactPt, 0);

                tank.Damage(damage);
            }
            destroySelf();
        }
    }
}
