using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

public class DataPasser : MonoBehaviour 
{
    private static DataPasser instance;
    public static DataPasser Instance
    {
        get {
            return instance;
        }
    }

    private Dictionary<string, object> data = new Dictionary<string, object>();

    void Awake() {
        instance = this;
    }

    public void AddData(string key, object value) {
        data[key] = value;
    }

    public Dictionary<string, object> RetrieveData() {
        Dictionary<string, object> passingDict = new Dictionary<string, object>(data);
        data.Clear();
        return passingDict;
    }
}
