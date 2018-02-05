using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEngine.UI;

public abstract class BarUI : MonoBehaviour 
{
    [SerializeField]
    Image fillImage;
	
	protected virtual void Update()
	{
        RectTransform rectTrans = fillImage.GetComponent<RectTransform>();
        Vector2 anchorMax = rectTrans.anchorMax;

        anchorMax.x = Mathf.Clamp01(getFillPercentage());

        rectTrans.anchorMax = anchorMax;
	}

    protected abstract float getFillPercentage();
}
