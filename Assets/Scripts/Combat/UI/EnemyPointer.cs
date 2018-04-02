using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEngine.UI;

public class EnemyPointer : MonoBehaviour 
{
    [SerializeField]
    private Image arrowImage;

    [SerializeField]
    private Text text;

    void Update()
	{
        Tank aiTank = CombatHandler.Instance.AITankController.SelfTank;
        Vector2 aiTankPos = aiTank.transform.position;
        Vector2 size = aiTank.Hull.Schematic.Size;

        Vector2 RTCorner = aiTankPos + size / 2f;
        Vector2 LTCorner = aiTankPos + new Vector2(-size.x/2f, size.y/2f);
        Vector2 LBCorner = aiTankPos - size / 2f;
        Vector2 RBCorner = aiTankPos + new Vector2(size.x / 2f, -size.y / 2f);

        Vector2[] corners = new Vector2[] { RTCorner, LTCorner, LBCorner, RBCorner };
        bool allCornersHidden = true;
        foreach (Vector2 corner in corners) {
            Vector2 screenPos = CombatHandler.Instance.MainCamera.WorldToScreenPoint(corner);

            if (!(screenPos.x < 0 || screenPos.x > Screen.width || screenPos.y < 0 || screenPos.y > Screen.height)) {
                allCornersHidden = false;
                break;
            }
        }

        if (allCornersHidden) {
            arrowImage.gameObject.SetActive(true);

            Tank playerTank = CombatHandler.Instance.HumanTankController.SelfTank;

            Vector2 diffVec = aiTank.transform.position - playerTank.transform.position;

            Vector2 offsetVec = diffVec.normalized * (Screen.width / 5f);
            arrowImage.transform.position = offsetVec + (Vector2)playerTank.transform.position;

            float angle = Vector2.SignedAngle(new Vector2(0, 1).Rotate(arrowImage.transform.rotation.eulerAngles.z), diffVec);
            arrowImage.transform.Rotate(new Vector3(0, 0, angle));

            text.text = diffVec.magnitude.ToString("0.00");
        } else {
            arrowImage.gameObject.SetActive(false);
        }

    }
}
