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

    [SerializeField]
    RectTransform frameRect;

    protected virtual void Update()
	{
        float frameMaxXAnchor = frameRect.anchorMax.x;

        RectTransform rectTrans = fillImage.GetComponent<RectTransform>();
        Vector2 anchorMax = rectTrans.anchorMax;

        float fillPercentage = Mathf.Clamp01(getFillPercentage());
        float xAnchorVal = frameMaxXAnchor + (1.0f - frameMaxXAnchor) * fillPercentage;

        anchorMax.x = xAnchorVal;

        rectTrans.anchorMax = anchorMax;
	}

    protected abstract float getFillPercentage();
}
