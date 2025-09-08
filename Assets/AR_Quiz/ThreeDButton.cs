using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public class ThreeDButton : MonoBehaviour, IPointerClickHandler, IPointerDownHandler, IPointerUpHandler
{
    [Header("Events")]
    public UnityEvent onClick;

    [Header("Feedback")]
    public float pressedScale = 0.9f;
    public AudioSource audioSource;  // optional
    public AudioClip clickClip;      // optional

    private Vector3 _defaultScale;

    void Awake() { _defaultScale = transform.localScale; }

    public void OnPointerDown(PointerEventData eventData)
    {
        transform.localScale = _defaultScale * pressedScale;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        transform.localScale = _defaultScale;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (audioSource && clickClip) audioSource.PlayOneShot(clickClip);
        onClick?.Invoke();
    }
}
