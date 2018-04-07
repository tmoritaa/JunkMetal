using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

public class MissileClusterBullet : Bullet
{
    private Vector2 firePos = new Vector2();
    private float range = 0;

    private int damage = 0;

    private float hitImpulse = 0;

    private float timeToShootSubmissles = 0;

    private Vector2 forwardVec;

    private int numMissiles = 0;

    private float missileRange = 0;

    private bool applyImpulseNextFrame = false;
    private Vector2 impulseVector;

    private float elapsedTimeSinceShot = 0;

    void FixedUpdate() {
        if (applyImpulseNextFrame) {
            this.body.AddForce(impulseVector, ForceMode2D.Impulse);
            applyImpulseNextFrame = false;
        } else {
            elapsedTimeSinceShot += Time.fixedDeltaTime;
        }

        if (!isBeingDestroyed && elapsedTimeSinceShot >= 0.25f) {
            float angleStep = 45f / Mathf.Round((float)numMissiles / 2);
            for (int i = 0; i < numMissiles; ++i) {
                MissileBullet bullet = (MissileBullet)BulletFactory.Instance.CreateBullet(BulletTypes.Missile);

                int idx = (numMissiles % 2 == 0) ? i + 1 : i;

                float sign = idx % 2 == 0 ? -1 : 1;

                float rotAngle = angleStep * (int)((float)idx / 2f + 0.5f) * sign;

                Vector2 initImpulse = forwardVec.Rotate(rotAngle).normalized * 300f;

                bullet.Init(Owner, this.transform.position, initImpulse, 3000, damage, hitImpulse, missileRange, 0.5f);
            }

            destroySelf();
        }
    }

    public override BulletTypes GetBulletType() {
        return BulletTypes.MissileCluster;
    }

    public override void Fire(Vector2 _forwardVec, Vector2 _firePosOffset, WeaponPartSchematic partSchematic) {
        forwardVec = _forwardVec;
        range = partSchematic.Range;
        damage = partSchematic.Damage;
        hitImpulse = (float)partSchematic.BulletInfos["hit_impulse"];
        firePos = Owner.transform.position + (Vector3)_firePosOffset;
        this.body.position = firePos;
        this.impulseVector = forwardVec.normalized * partSchematic.ShootImpulse;
        applyImpulseNextFrame = true;

        float angle = Vector2.SignedAngle(new Vector2(0, 1).Rotate(this.body.rotation), forwardVec);
        this.body.rotation = angle;

        float recoilImpulse = (float)partSchematic.BulletInfos["recoil_impulse"];
        Vector2 backVec = forwardVec.Rotate(180);
        Owner.Body.AddForce(backVec.normalized * recoilImpulse, ForceMode2D.Impulse);

        numMissiles = (int)partSchematic.BulletInfos["num_missiles"];
        missileRange = (float)partSchematic.BulletInfos["missile_range"];
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
