using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEngine.UI;

public class PlayerEnergyBarUI : BarUI
{
    [SerializeField]
    TankController.PlayerTypes DisplayType;

    [SerializeField]
    Text energyText;

    protected override void Update() {
        base.Update();

        energyText.text = ((int)getTankOfType().Hull.CurEnergy).ToString();
    }

    protected override float getFillPercentage() {
        Tank tank = getTankOfType();

        return tank.Hull.CurEnergy / tank.Hull.Schematic.Energy;
    }

    private Tank getTankOfType() {
        return (DisplayType == TankController.PlayerTypes.Human) ? CombatHandler.Instance.HumanTankController.SelfTank : CombatHandler.Instance.AITankController.SelfTank;
    }
}
