using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEngine.UI;

public class EnergyBullet : Bullet
{
    private RectTransform rectTrans;

    private Image image;

    private int damage = 0;

    private float hitImpulse = 0;

    private float shootForce = 0;

    private Vector2 shotForwardVec;

    private float timeHit = 9999;

    private float origAngle = 0;

    protected override void Awake() {
        base.Awake();
        rectTrans = this.GetComponent<RectTransform>();
        image = this.GetComponent<Image>();
    }

    void Update() {
        float durationDiff = Time.time - timeHit;
        float alpha = 1;

        if (durationDiff > 0) {
            Color color = image.color;

            alpha = Mathf.Clamp01(0.5f - durationDiff / 0.5f);
            color.a = alpha;

            image.color = color;
        }

        if (alpha <= 0) {
            destroySelf();
        }
    }

    void FixedUpdate() {
        this.Body.rotation = origAngle;

        float extendLength = shootForce * Time.fixedDeltaTime;

        float curHeight = rectTrans.sizeDelta.y;

        float newHeight = curHeight + extendLength;

        Vector2 newSize = new Vector2(rectTrans.sizeDelta.x, newHeight);

        rectTrans.sizeDelta = newSize;

        Collider.size = newSize;
        Collider.offset = new Vector2(0, newSize.y / 2f);
    }

    public override BulletTypes GetBulletType() {
        return BulletTypes.Energy;
    }

    public override void Fire(Vector2 forwardVec, Vector2 firePosOffset, WeaponPartSchematic partSchematic) {
        damage = partSchematic.Damage;
        hitImpulse = (float)partSchematic.BulletInfos["hit_impulse"];
        shootForce = partSchematic.ShootImpulse;
        this.shotForwardVec = forwardVec;
        this.Body.position = Owner.transform.position + (Vector3)firePosOffset;

        origAngle = Vector2.SignedAngle(new Vector2(0, 1).Rotate(this.Body.rotation), this.shotForwardVec);
        this.Body.rotation = origAngle;

        float recoilImpulse = (float)partSchematic.BulletInfos["recoil_impulse"];
        Vector2 backVec = this.shotForwardVec.Rotate(180);
        Owner.Body.AddForce(backVec.normalized * recoilImpulse, ForceMode2D.Impulse);
    }

    private void disableHitbox() {
        timeHit = Time.time;
        this.Collider.enabled = false;
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

                Vector2 impulseDir = ((Vector2)tank.transform.position - avgContactPt).normalized;
                tank.Body.AddForceAtPosition(impulseDir * hitImpulse, avgContactPt, ForceMode2D.Impulse);

                CombatAnimationHandler.Instance.InstantiatePrefab("spark", avgContactPt, 0);

                tank.Damage(damage);
            }

            disableHitbox();
        }
    }
}
