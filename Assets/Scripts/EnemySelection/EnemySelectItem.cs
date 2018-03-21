using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEngine.UI;

public class EnemySelectItem : MonoBehaviour 
{
    [SerializeField]
    private Text text;

    private EnemyInfo enemyInfo;

    public void Init(EnemyInfo _enemyInfo) {
        enemyInfo = _enemyInfo;

        updateUI();
    }

    private void updateUI() {
        text.text = enemyInfo.Name;
    }

    public void ItemPressed() {
        EnemySelectionHandler.Instance.GotoCombatWithEnemy(enemyInfo);
    }
}
