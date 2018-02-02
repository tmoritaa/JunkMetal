using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEditor;


public class CreateBulletPrefabScriptableObject : MonoBehaviour 
{
    [MenuItem("Assets/Create/Bullet Prefab Scriptable Object")]
    public static void CreateAsset() {
        BulletPrefabScriptableObject asset = ScriptableObject.CreateInstance<BulletPrefabScriptableObject>();

        AssetDatabase.CreateAsset(asset, "Assets/NewBulletPrefab.asset");
        AssetDatabase.SaveAssets();

        EditorUtility.FocusProjectWindow();

        Selection.activeObject = asset;
    }
}
