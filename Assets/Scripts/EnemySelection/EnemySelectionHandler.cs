using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class EnemySelectionHandler : MonoBehaviour 
{
    [SerializeField]
    private EnemySelectItem enemySelectItemPrefab;

    [SerializeField]
    private Transform enemySelectItemRoot;

    private static EnemySelectionHandler instance;
    public static EnemySelectionHandler Instance
    {
        get {
            return instance;
        }
    }

    void Awake() {
        instance = this;
    }

    void Start() {
        initScreen();
    }

    void Update() {
        if (InputManager.Instance.IsKeyTypeDown(InputManager.KeyType.Cancel, true)) {
            SceneManager.LoadScene("Main");
        }
    }

    public void GotoCombatWithEnemy(EnemyInfo info) {
        DataPasser.Instance.AddData("Opponent", info);
        SceneManager.LoadScene("Combat");
    }

    private void initScreen() {
        List<EnemyInfo> infos = EnemyInfoManager.Instance.GetAllEnemyInfos();

        const float itemHeight = 50f;
        float startY = itemHeight * infos.Count / 2;

        EnemySelectItem highlightItem = null;
        int count = 0;
        foreach (EnemyInfo info in infos) {
            EnemySelectItem item = Instantiate(enemySelectItemPrefab, enemySelectItemRoot, false);
            item.transform.localPosition = new Vector3(0, startY - count * itemHeight, 0);

            item.Init(info);

            count += 1;

            if (highlightItem == null) {
                highlightItem = item;
            }
        }

        highlightItem.GetComponent<Button>().Select();
    }
}
