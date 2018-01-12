using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

[RequireComponent(typeof(Collider2D), typeof(Rigidbody2D))]
public class Bullet : MonoBehaviour
{
    private Rigidbody2D body;
    private Tank owner;

    private bool isBeingDestroyed = false;

    void Awake() {
        this.body = GetComponent<Rigidbody2D>();
    }

    void Update() {
        // TODO: replace Camera.main with something that doesn't trigger search every time.
        Vector3 screenPos = Camera.main.WorldToScreenPoint(this.transform.position);
        
        if (!isBeingDestroyed && 
            (screenPos.x < 0 || screenPos.x > Screen.width) &&
            (screenPos.y < 0 || screenPos.y > Screen.height))
        {
            GameObject.Destroy(this.gameObject, 0.25f);
            isBeingDestroyed = true;
        }
    }

    public void Init(Tank _owner) {
        owner = _owner;
        this.gameObject.transform.position = owner.transform.position;
    }

    public void Fire(Vector2 forwardVec, float shootForce) {
        this.body.AddForce(forwardVec.normalized * shootForce);
    }

    private void OnTriggerEnter2D(Collider2D collision) {
        if (!isBeingDestroyed && collision.gameObject != owner.gameObject) {
            GameObject.Destroy(this.gameObject);
            isBeingDestroyed = true;
        }
    }
}
