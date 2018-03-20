using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEditor;

public class CreateStrPrefabDictScriptableObject : MonoBehaviour 
{
    [MenuItem("Assets/Create/String Prefab Dictionary Scriptable Object")]
    public static void CreateAsset() {
        StrPrefabDictScriptableObject asset = ScriptableObject.CreateInstance<StrPrefabDictScriptableObject>();

        AssetDatabase.CreateAsset(asset, "Assets/NewStrPrefabDictScriptableObject.asset");
        AssetDatabase.SaveAssets();

        EditorUtility.FocusProjectWindow();

        Selection.activeObject = asset;
    }
}
