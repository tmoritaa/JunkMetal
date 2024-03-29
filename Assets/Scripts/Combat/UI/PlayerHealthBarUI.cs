﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEngine.UI;

public class PlayerHealthBarUI : BarUI 
{
    [SerializeField]
    TankController.PlayerTypes DisplayType;

    [SerializeField]
    Text armourText;

    [SerializeField]
    Text nameText;

    protected override void Update() {
        base.Update();

        armourText.text = getTankOfType().CurArmour + "/" + getTankOfType().MaxArmour;

        nameText.text = (DisplayType == TankController.PlayerTypes.Human) ? "You" : "Them";
    }

    protected override float getFillPercentage() {
        Tank tank = getTankOfType();

        return (float)tank.CurArmour / tank.MaxArmour;
    }

    private Tank getTankOfType() {
        return (DisplayType == TankController.PlayerTypes.Human) ? CombatHandler.Instance.HumanTankController.SelfTank : CombatHandler.Instance.AITankController.SelfTank;
    }
}