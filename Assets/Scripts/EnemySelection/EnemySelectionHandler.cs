using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
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
        if (Input.GetKeyUp(KeyCode.Joystick1Button1) || Input.GetKeyUp(KeyCode.Mouse1) || Input.GetKeyUp(KeyCode.Escape)) {
            SceneManager.LoadScene("Main");
        }
    }

    public void GotoCombatWithEnemy(EnemyInfo info) {
        Debug.Log("Going to enemy " + info.Name);
    }

    private void initScreen() {
        List<EnemyInfo> infos = EnemyInfoManager.Instance.GetAllEnemyInfos();

        int count = 0;
        foreach (EnemyInfo info in infos) {
            EnemySelectItem item = Instantiate(enemySelectItemPrefab, enemySelectItemRoot, false);
            item.transform.localPosition = new Vector3(0, count * 50, 0);

            item.Init(info);

            count += 1;
        }
    }
}
