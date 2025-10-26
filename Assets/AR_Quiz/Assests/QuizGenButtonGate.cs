using System;
using System.Reflection;
using System.Collections;
using UnityEngine;

public class QuizGenButtonGate : MonoBehaviour
{
    [Header("What to show/hide")]
    public GameObject generateButton;   // The parent GameObject holding your Generate & Save UI

    [Header("Where to read lock state from")]
    public Component lockSource;        // Drag your lock script component here
    public string boolFieldName = "isLocked"; // The bool property/field name in your lock script

    [Header("Polling (if no event)")]
    public float checkInterval = 0.25f; // Polling interval seconds

    FieldInfo _field;
    PropertyInfo _prop;

    void Start()
    {
        if (!generateButton) { Debug.LogWarning("[GenGate] No generateButton assigned."); return; }
        if (!lockSource) { Debug.LogWarning("[GenGate] No lockSource assigned."); generateButton.SetActive(false); return; }

        var t = lockSource.GetType();
        _field = t.GetField(boolFieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        _prop = t.GetProperty(boolFieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

        if (_field == null && _prop == null)
            Debug.LogWarning($"[GenGate] '{boolFieldName}' not found on {t.Name}. Button will stay hidden until you set it correctly.");

        generateButton.SetActive(false);
        StartCoroutine(CheckLoop());
    }

    IEnumerator CheckLoop()
    {
        while (true)
        {
            bool locked = ReadLocked();
            if (generateButton) generateButton.SetActive(locked);
            yield return new WaitForSeconds(checkInterval);
        }
    }

    bool ReadLocked()
    {
        if (!lockSource) return false;
        if (_field != null && _field.FieldType == typeof(bool))
            return (bool)_field.GetValue(lockSource);
        if (_prop != null && _prop.PropertyType == typeof(bool) && _prop.CanRead)
            return (bool)_prop.GetValue(lockSource, null);
        return false;
    }
}
