using UnityEngine;
using UnityEngine.EventSystems;

public class EventSystemKeeper : MonoBehaviour
{
    static EventSystemKeeper inst;
    void Awake()
    {
        if (inst && inst != this) { Destroy(gameObject); return; }
        inst = this;
        DontDestroyOnLoad(gameObject);
    }
}
