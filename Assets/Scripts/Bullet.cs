using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

[RequireComponent(typeof(Collider2D), typeof(Rigidbody2D))]
public class Bullet : MonoBehaviour
{
    private Rigidbody2D body;
    private Tank owner;

    void Awake() {
        this.body = GetComponent<Rigidbody2D>();
    }

    public void Init(Tank _owner) {
        owner = _owner;
        this.gameObject.transform.position = owner.transform.position;
    }

    public void Fire(Vector2 forwardVec, float shootForce) {
        this.body.AddForce(forwardVec.normalized * shootForce);
    }

    private void OnTriggerEnter2D(Collider2D collision) {
        if (collision.gameObject != owner.gameObject) {
            GameObject.Destroy(this.gameObject);
        }
    }
}
