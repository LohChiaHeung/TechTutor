using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Vuforia;

[RequireComponent(typeof(ObserverBehaviour))]
public class VuforiaImageSwipeSelector_ExistingChildren : MonoBehaviour
{
    [Header("Existing child models (NO prefabs)")]
    public List<GameObject> existingModels = new List<GameObject>();

    [Header("Swipe")]
    [Range(20f, 200f)] public float swipeThresholdPixels = 80f;
    [Range(0f, 1f)] public float swipeCooldownSeconds = 0.25f;

    [Header("Behaviour")]
    [Tooltip("If true, hide the current model when target is lost.")]
    public bool hideWhenLost = true;

    [Header("Start")]
    [Tooltip("Which model shows first when the app starts.")]
    [SerializeField] private int startIndex = 0;

    [Header("Locking")]
    [Tooltip("If true, swiping is disabled after Choose until Unchoose is pressed.")]
    public bool lockAfterChoose = true;

    [Header("Events")]
    public UnityEvent<string> OnModelChanged;
    public UnityEvent<string> OnModelChosen;
    public UnityEvent<string> OnModelUnchosen;

    private ObserverBehaviour _observer;
    [SerializeField] private int _currentIndex = 0;
    private Vector2 _touchStartPos;
    private bool _trackingTouch = false;
    private float _lastSwipeTime = -10f;

    private bool _isChosen = false;
    private string _chosenName = null;
    private bool _lockedByChoice = false;

    public int CurrentIndex => _currentIndex;
    public string CurrentModelName
    {
        get
        {
            if (existingModels == null || existingModels.Count == 0) return "(none)";
            var go = existingModels[Mathf.Clamp(_currentIndex, 0, existingModels.Count - 1)];
            return go ? go.name : "(unnamed)";
        }
    }
    public bool IsChosen => _isChosen;
    public string ChosenName => _chosenName;
    public bool IsSwipeLocked => _lockedByChoice;

    private void Awake()
    {
        _observer = GetComponent<ObserverBehaviour>();
        if (_observer != null)
            _observer.OnTargetStatusChanged += OnTargetStatusChanged;
    }
    private void OnDestroy()
    {
        if (_observer != null)
            _observer.OnTargetStatusChanged -= OnTargetStatusChanged;
    }

    private void Start()
    {
        _currentIndex = Mathf.Clamp(startIndex, 0, Mathf.Max(0, existingModels.Count - 1));
        ApplyActiveIndex(_currentIndex, fireEvent: true);

        if (_observer != null && IsTracked(_observer.TargetStatus))
            SetActiveSafe(_currentIndex, true);

        Debug.Log($"[SwipeSelectorExisting] Start index = {_currentIndex} ({CurrentModelName})");
    }

    private void Update()
    {
#if UNITY_EDITOR || UNITY_STANDALONE
        if (Input.GetMouseButtonDown(0))
        {
            _touchStartPos = Input.mousePosition;
            _trackingTouch = true;
        }
        else if (Input.GetMouseButtonUp(0) && _trackingTouch)
        {
            var delta = (Vector2)Input.mousePosition - _touchStartPos;
            _trackingTouch = false;
            TryHandleSwipe(delta);
        }
#else
        if (Input.touchCount == 1)
        {
            var t = Input.GetTouch(0);
            if (t.phase == TouchPhase.Began)
            {
                _touchStartPos = t.position;
                _trackingTouch = true;
            }
            else if ((t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled) && _trackingTouch)
            {
                var delta = t.position - _touchStartPos;
                _trackingTouch = false;
                TryHandleSwipe(delta);
            }
        }
#endif
    }

    private void OnTargetStatusChanged(ObserverBehaviour behaviour, TargetStatus status)
    {
        bool tracked = IsTracked(status);
        if (hideWhenLost)
        {
            for (int i = 0; i < existingModels.Count; i++)
                SetActiveSafe(i, tracked && i == _currentIndex);
        }
        else
        {
            for (int i = 0; i < existingModels.Count; i++)
                SetActiveSafe(i, i == _currentIndex);
        }
    }

    private static bool IsTracked(TargetStatus st)
    {
        var s = st.Status;
        return s == Status.TRACKED || s == Status.EXTENDED_TRACKED || s == Status.LIMITED;
    }

    private void TryHandleSwipe(Vector2 delta)
    {
        if (_lockedByChoice) return; // 🔒 locked after choose
        if (Time.time - _lastSwipeTime < swipeCooldownSeconds) return;
        if (existingModels == null || existingModels.Count == 0) return;

        if (Mathf.Abs(delta.x) > swipeThresholdPixels && Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
        {
            _lastSwipeTime = Time.time;
            if (delta.x < 0f) NextModel();
            else PrevModel();
        }
    }

    private void NextModel()
    {
        if (existingModels.Count == 0) return;
        _currentIndex = (_currentIndex + 1) % existingModels.Count;
        ClearChoiceState();
        ApplyActiveIndex(_currentIndex, fireEvent: true);
    }

    private void PrevModel()
    {
        if (existingModels.Count == 0) return;
        _currentIndex = (_currentIndex - 1 + existingModels.Count) % existingModels.Count;
        ClearChoiceState();
        ApplyActiveIndex(_currentIndex, fireEvent: true);
    }

    private void ApplyActiveIndex(int idx, bool fireEvent)
    {
        for (int i = 0; i < existingModels.Count; i++)
            SetActiveSafe(i, i == idx && (!hideWhenLost || (_observer != null && IsTracked(_observer.TargetStatus))));

        if (fireEvent)
            OnModelChanged?.Invoke(CurrentModelName);
    }

    private void SetActiveSafe(int i, bool active)
    {
        if (i < 0 || i >= existingModels.Count) return;
        var go = existingModels[i];
        if (go && go.activeSelf != active) go.SetActive(active);
    }

    // Add inside the class
    public GameObject CurrentModelGO =>
        (existingModels != null && existingModels.Count > 0)
            ? existingModels[Mathf.Clamp(_currentIndex, 0, existingModels.Count - 1)]
            : null;


    private void ClearChoiceState()
    {
        _isChosen = false;
        _chosenName = null;
        _lockedByChoice = false;
    }

    // === UI hooks ===
    public void ChooseCurrentModel()
    {
        string name = CurrentModelName;
        _isChosen = true;
        _chosenName = name;
        if (lockAfterChoose) _lockedByChoice = true; // 🔒 stop swiping
        Debug.Log($"[SwipeSelectorExisting] '{name}' is chosen. Swipe locked = {_lockedByChoice}");
        OnModelChosen?.Invoke(name);
    }

    public void UnchooseCurrentModel()
    {
        if (!_isChosen) return;
        Debug.Log($"[SwipeSelectorExisting] Unchosen '{_chosenName}'. Swipe unlocked.");
        OnModelUnchosen?.Invoke(_chosenName);
        _isChosen = false;
        _chosenName = null;
        _lockedByChoice = false; // 🔓 allow swiping again
    }

    // ——— add inside VuforiaImageSwipeSelector_ExistingChildren ———
    public void UI_Next() { /* call your internal next */  NextModel(); }
    public void UI_Prev() { /* call your internal prev */  PrevModel(); }
}
