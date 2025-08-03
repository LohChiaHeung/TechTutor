using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SpeakerTapHandler : MonoBehaviour
{
    [Header("UI & Visuals")]
    public GameObject canvasObject;
    public GameObject backgroundPanelObject;
    public GameObject speakerHighlightGroup;
    public Button closeButton;

    [Header("Audio")]
    public AudioClip descriptionAudio;

    private AudioSource audioSource;
    private TextMeshProUGUI textMesh;
    private Image backgroundImage;
    private bool isDeactivated = false;

    void Start()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;

        if (canvasObject != null) canvasObject.SetActive(false);

        if (backgroundPanelObject != null)
        {
            textMesh = backgroundPanelObject.GetComponentInChildren<TextMeshProUGUI>();
            backgroundImage = backgroundPanelObject.GetComponent<Image>();
        }

        if (backgroundImage != null)
            backgroundImage.color = new Color(1f, 1f, 1f, 0f);

        if (textMesh != null)
            textMesh.color = new Color(1f, 1f, 1f, 0f);

        if (closeButton != null)
            closeButton.onClick.AddListener(OnCloseButtonTapped);
    }

    void Update()
    {
        if (isDeactivated) return;

        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.GetTouch(0).position);

            if (Physics.Raycast(ray, out RaycastHit hit) && hit.transform == transform)
            {
                Debug.Log("[SpeakerTapHandler] Speaker tapped!");
                if (descriptionAudio != null)
                {
                    audioSource.clip = descriptionAudio;
                    audioSource.Play();
                }
                ShowDescriptionCard();
            }
        }
    }

    public void OnCloseButtonTapped()
    {
        isDeactivated = true;
        canvasObject?.SetActive(false);
        if (audioSource.isPlaying) audioSource.Stop();

        if (speakerHighlightGroup != null)
            speakerHighlightGroup.SetActive(true);

        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;
    }

    void ShowDescriptionCard()
    {
        canvasObject?.SetActive(true);
        backgroundPanelObject?.SetActive(true);

        if (backgroundImage != null)
            backgroundImage.color = new Color(1f, 1f, 1f, 1f);
        if (textMesh != null)
            textMesh.color = new Color(1f, 1f, 1f, 1f);
    }
}
