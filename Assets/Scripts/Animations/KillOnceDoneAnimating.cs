using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

public class KillOnceDoneAnimating : MonoBehaviour 
{
	public void KillSelf() {
        Destroy(this.gameObject);
    }
}
