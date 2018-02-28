using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEngine.SceneManagement;

public class CustomizationHandler : MonoBehaviour 
{
    public enum StateType
    {
        EquippedItemSelect,
        OtherItemSelect,
    }

    [SerializeField]
    private SimpleObjectPool equippedPartsItemPool;
    public SimpleObjectPool EquippedPartsItemPool
    {
        get {
            return equippedPartsItemPool;
        }
    }

    [SerializeField]
    private Transform equippedPartsItemRoot;
    public Transform EquippedPartsItemRoot
    {
        get {
            return equippedPartsItemRoot;
        }
    }

    [SerializeField]
    private GameObject otherPartsFrame;
    public GameObject OtherPartsFrame
    {
        get {
            return otherPartsFrame;
        }
    }

    [SerializeField]
    private Transform otherPartsItemsRoot;
    public Transform OtherPartsItemsRoot
    {
        get {
            return otherPartsItemsRoot;
        }
    }

    [HideInInspector]
    public EquippedPartsItem PickedPartsItem;

    private Dictionary<StateType, State> states = new Dictionary<StateType, State>();
    private State curState;
    private StateType curStateType;

	void Start()
	{
        states[StateType.EquippedItemSelect] = new SelectEquippedItemsState(this);
        states[StateType.OtherItemSelect] = new SelectOtherItemsState(this);

        curStateType = StateType.EquippedItemSelect;
        curState = states[curStateType];

        curState.Start();
    }

    void Update() {
        curState.PerformUpdate();
    }

    public void GotoState(StateType stateType) {
        curState.End();

        curStateType = stateType;
        curState = states[curStateType];

        curState.Start();
    }

    public void BackToMain() {
        SceneManager.LoadScene("Main");
    }
}
