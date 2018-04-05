using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEngine.UI;

public class EnergyBullet : Bullet
{
    private RectTransform rectTrans;

    private BoxCollider2D boxCollider;

    private Image image;

    private Vector2 firePos = new Vector2();
    private int damage = 0;

    private float hitImpulse = 0;

    private float shootForce = 0;

    private Vector2 shotForwardVec;

    private float timeHit = 9999;

    private bool hitboxEnabled = true;

    protected override void Awake() {
        base.Awake();
        boxCollider = this.GetComponent<BoxCollider2D>();
        rectTrans = this.GetComponent<RectTransform>();
        image = this.GetComponent<Image>();
    }

    void Update() {
        float durationDiff = Time.time - timeHit;
        float alpha = 1;

        if (durationDiff > 0) {
            Color color = image.color;

            alpha = Mathf.Clamp01(0.75f - durationDiff / 0.75f);
            color.a = alpha;

            image.color = color;
        }

        if (alpha <= 0) {
            destroySelf();
        }
    }

    void FixedUpdate() {
        if (hitboxEnabled) {
            float extendLength = shootForce * Time.fixedDeltaTime;

            RaycastHit2D hit = Physics2D.Raycast(this.transform.position, shotForwardVec, extendLength);

            if (hit.collider != null && hit.collider.gameObject == Owner.gameObject) {
                extendLength = hit.distance;
            }

            float curHeight = rectTrans.sizeDelta.y;

            float newHeight = curHeight + extendLength;

            Vector2 newSize = new Vector2(rectTrans.sizeDelta.x, newHeight);

            rectTrans.sizeDelta = newSize;

            boxCollider.size = newSize;
            boxCollider.offset = new Vector2(0, newSize.y / 2f);
        }
    }

    public override BulletTypes GetBulletType() {
        return BulletTypes.Energy;
    }

    public override void Fire(Vector2 forwardVec, Vector2 firePosOffset, WeaponPartSchematic partSchematic) {
        damage = partSchematic.Damage;
        hitImpulse = partSchematic.HitImpulse;
        shootForce = partSchematic.ShootImpulse;
        firePos = Owner.transform.position + (Vector3)firePosOffset;
        this.shotForwardVec = forwardVec;
        this.body.position = firePos;

        float angle = Vector2.SignedAngle(new Vector2(0, 1).Rotate(this.body.rotation), this.shotForwardVec);
        this.body.rotation = angle;

        Vector2 backVec = this.shotForwardVec.Rotate(180);
        Owner.Body.AddForce(backVec.normalized * partSchematic.RecoilImpulse, ForceMode2D.Impulse);
    }

    private void disableHitbox() {
        hitboxEnabled = false;

        timeHit = Time.time;
        this.boxCollider.enabled = false;
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

            disableHitbox();
        }
    }
}
