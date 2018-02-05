using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEngine.UI;

public class PlayerHealthBarUI : BarUI 
{
    [SerializeField]
    Tank.PlayerTypes DisplayType;

    [SerializeField]
    Text armourText;

    [SerializeField]
    Text nameText;

    protected override void Update() {
        base.Update();

        armourText.text = getTankOfType().CurArmour + "/" + getTankOfType().MaxArmour;

        nameText.text = (DisplayType == Tank.PlayerTypes.Human) ? "Player" : "AI";
    }

    protected override float getFillPercentage() {
        Tank tank = getTankOfType();

        return (float)tank.CurArmour / tank.MaxArmour;
    }

    private Tank getTankOfType() {
        return (DisplayType == Tank.PlayerTypes.Human) ? GameManager.Instance.PlayerTank : GameManager.Instance.AiTank;
    }
}
