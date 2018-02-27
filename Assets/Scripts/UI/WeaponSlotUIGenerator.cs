using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

public class WeaponSlotUIGenerator : MonoBehaviour 
{
    [SerializeField]
    private WeaponSlotUI weaponSlotUIPrefab;

    [SerializeField]
    private int totalNumWeapons = 4;

	void Start() {
        Tank playerTank = CombatManager.Instance.HumanTankController.SelfTank;

        for (int i = 0; i < totalNumWeapons; ++i) {
            WeaponSlotUI slotUI = Instantiate<WeaponSlotUI>(weaponSlotUIPrefab, this.transform, false);

            float minYAnchor = 1f - ((float)(i + 1) / totalNumWeapons);
            float maxYAnchor = 1f - ((float)i / totalNumWeapons);

            RectTransform rectTrans = slotUI.GetComponent<RectTransform>();

            Vector2 anchorMin = rectTrans.anchorMin;
            anchorMin.y = minYAnchor;

            Vector2 anchorMax = rectTrans.anchorMax;
            anchorMax.y = maxYAnchor;

            rectTrans.anchorMin = anchorMin;
            rectTrans.anchorMax = anchorMax;

            WeaponPart part = playerTank.Turret.GetWeaponAtIdx(i);
            slotUI.Init(part);
        }
	}
}
