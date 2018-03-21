using UnityEngine;
using UnityEngine.UI;

using UnityEditor;

[UnityEditor.CustomPropertyDrawer(typeof(StringGODictionary))]
public class StringGODictionaryDrawer : SerializableDictionaryDrawer<string, GameObject>
{
    protected override SerializableKeyValueTemplate<string, GameObject> GetTemplate() {
        return GetGenericTemplate<SerializableStringGOTemplate>();
    }
}
internal class SerializableStringGOTemplate : SerializableKeyValueTemplate<string, GameObject> { }