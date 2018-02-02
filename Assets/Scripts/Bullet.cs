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

    private Rigidbody2D body;
    private Tank owner;

    private bool isBeingDestroyed = false;


    private Vector2 firePos = new Vector2();
    private float range = 0;

    void Awake() {
        this.body = GetComponent<Rigidbody2D>();
    }

    void Update() {
        // TODO: replace Camera.main with something that doesn't trigger search every time.
        Vector3 screenPos = GameManager.Instance.MainCamera.WorldToScreenPoint(this.transform.position);

        float travelDistSqr = ((Vector2)this.transform.position - firePos).sqrMagnitude;

        bool travelledRange = travelDistSqr > range * range;

        if (!isBeingDestroyed && travelledRange)
        {
            destroySelf();
        }
    }

    public void Init(Tank _owner) {
        owner = _owner;
        this.gameObject.transform.position = owner.transform.position;
    }

    public void Fire(Vector2 forwardVec, float shootForce, float _range) {
        range = _range;
        firePos = this.transform.position;
        this.body.AddForce(forwardVec.normalized * shootForce);
    }

    private void OnTriggerEnter2D(Collider2D collision) {
        if (!isBeingDestroyed && collision.gameObject != owner.gameObject) {
            destroySelf();
        }
    }

    private void destroySelf() {
        GameObject.Destroy(this.gameObject);
        isBeingDestroyed = true;
    }
}
