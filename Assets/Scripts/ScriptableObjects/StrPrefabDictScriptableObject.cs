using System.Collections.Generic;

using UnityEngine;

public class StrPrefabDictScriptableObject : ScriptableObject
{
    [SerializeField]
    private StringGODictionary stringGOStore = StringGODictionary.New<StringGODictionary>();
    public Dictionary<string, GameObject> StrPrefabsDict
    {
        get { return stringGOStore.dictionary; }
    }
}