using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

public class MissileBullet : Bullet
{
    private Vector2 initPos;
    private Vector2 initImpulse;
    private int damage = 0;
    private float hitImpulse = 0;
    private float fireImpulseMag = 0;
    private float range = 0;

    private bool doInitImpulse = false;

    private bool doFireImpulse = false;

    private float timeToStart = 0;

    private float elapsedTime = 0;

    private float origRotation = 0;

    void Update() {
        float travelDistSqr = ((Vector2)this.transform.position - initPos).sqrMagnitude;

        bool travelledRange = travelDistSqr > range * range;

        if (!isBeingDestroyed && travelledRange) {
            destroySelf();
        }
    }

    void FixedUpdate() {
        if (!isBeingDestroyed) {
            if (doInitImpulse) {
                this.body.AddForce(initImpulse, ForceMode2D.Impulse);
                origRotation = this.body.rotation;
                doInitImpulse = false;
                doFireImpulse = true;
            } else {
                elapsedTime += Time.fixedDeltaTime;
            }

            if (doFireImpulse) {
                Tank opposingTank = CombatHandler.Instance.GetOpposingTank(Owner);
                Vector2 toOpp = (opposingTank.transform.position - this.transform.position).normalized;
                float angle = Vector2.SignedAngle(new Vector2(0, 1).Rotate(this.body.rotation), toOpp);
                this.body.rotation += angle;

                if (elapsedTime > timeToStart) {
                    Vector2 impulse = (opposingTank.transform.position - this.transform.position).normalized * fireImpulseMag;
                    this.body.AddForce(impulse, ForceMode2D.Impulse);

                    doFireImpulse = false;
                }
            }
        }
    }

    public override void Fire(Vector2 forwardVec, Vector2 firePosOffset, WeaponPartSchematic partSchematic) {
        // Not used.
        Debug.Assert(false, "Fire should never be called for MissileBullet");
    }

    public override BulletTypes GetBulletType() {
        return BulletTypes.Missile;
    }

    public void Init(Tank owner, Vector2 _initPos, Vector2 _initImpulse, float _fireImpulseMag, int _damage, float _hitImpulse, float _range, float _timeToStart) {
        Owner = owner;
        damage = _damage;
        hitImpulse = _hitImpulse;
        range = _range;
        initImpulse = _initImpulse;
        initPos = _initPos;
        fireImpulseMag = _fireImpulseMag;
        timeToStart = _timeToStart;

        this.body.transform.position = initPos;

        doInitImpulse = true;
    }

    private void OnCollisionEnter2D(Collision2D collision) {
        if (!isBeingDestroyed) {
            if (collision.collider.GetComponent<Tank>() != null) {
                Tank tank = collision.collider.GetComponent<Tank>();

                Vector2 avgContactPt = new Vector2();
                foreach (ContactPoint2D contactPt in collision.contacts) {
                    avgContactPt += contactPt.point;
                }
                avgContactPt /= collision.contacts.Length;

                Vector2 impulseDir = ((Vector2)tank.transform.position - avgContactPt).normalized;
                tank.Body.AddForceAtPosition(impulseDir * hitImpulse, avgContactPt, ForceMode2D.Impulse);

                CombatAnimationHandler.Instance.InstantiatePrefab("spark", avgContactPt, 0);

                tank.Damage(damage);
            }
            destroySelf();
        }
    }
}
