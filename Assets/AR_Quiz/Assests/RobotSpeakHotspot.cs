using UnityEngine;
using UnityEngine.EventSystems;

public class RobotSpeakerHotspot : MonoBehaviour, IPointerClickHandler
{
    [Header("Refs")]
    public QuizPanelController quiz;   // assign in Inspector
    public bool includeOptions = true; // read options too on repeat

    // Works with EventSystem + PhysicsRaycaster + Collider
    public void OnPointerClick(PointerEventData eventData)
    {
        if (quiz)
        {
            quiz.RepeatSpeakCurrent(includeOptions);
            Debug.Log("[Robot] RepeatSpeakCurrent via IPointerClickHandler");
        }
    }

    // Fallback for cases without EventSystem/PhysicsRaycaster (Editor/mobile taps)
    void OnMouseDown()
    {
        if (quiz)
        {
            quiz.RepeatSpeakCurrent(includeOptions);
            Debug.Log("[Robot] RepeatSpeakCurrent via OnMouseDown");
        }
    }
}
