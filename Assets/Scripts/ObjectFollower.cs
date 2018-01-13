using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

public class ObjectFollower : MonoBehaviour 
{
    GameObject objToFollow = null;

    void Update() {
        if (objToFollow != null) {
            Vector3 newPos = objToFollow.transform.position;
            newPos.z = this.transform.position.z;
            this.transform.position = newPos;
        }    
    }

    public void SetObjToFollow(GameObject obj) {
        objToFollow = obj;
    }
}
