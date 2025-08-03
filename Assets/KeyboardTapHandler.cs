using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class KeyboardTapHandler : MonoBehaviour
{
    [Header("UI & Visuals")]
    public GameObject canvasObject;
    public GameObject backgroundPanelObject; // Should be the "BackgroundPanel" GameObject
    public GameObject keyHighlightGroup;
    public Button closeButton;

    [Header("Audio")]
    public AudioClip descriptionAudio;

    private AudioSource audioSource;
    private TextMeshProUGUI textMesh;
    private Image backgroundImage;

    private bool isDeactivated = false;

    void Start()
    {
        // Setup audio
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;

        // Validation checks
        if (canvasObject == null)
            Debug.LogError("[KeyboardTapHandler] ❌ canvasObject is NULL — not assigned!");
        else
            Debug.Log("[KeyboardTapHandler] ✅ canvasObject found: " + canvasObject.name + " | activeSelf: " + canvasObject.activeSelf);

        if (backgroundPanelObject == null)
            Debug.LogError("[KeyboardTapHandler] ❌ backgroundPanelObject is NULL!");
        else
            Debug.Log("[KeyboardTapHandler] ✅ backgroundPanel found: " + backgroundPanelObject.name);

        if (backgroundPanelObject != null)
        {
            textMesh = backgroundPanelObject.GetComponentInChildren<TextMeshProUGUI>();
            backgroundImage = backgroundPanelObject.GetComponent<Image>();
        }

        // Hide canvas initially
        if (canvasObject != null)
            canvasObject.SetActive(false);

        if (closeButton != null)
            closeButton.onClick.AddListener(OnCloseButtonTapped);

        // Make background and text transparent
        if (backgroundImage != null)
            backgroundImage.color = new Color(backgroundImage.color.r, backgroundImage.color.g, backgroundImage.color.b, 0f);

        if (textMesh != null)
            textMesh.color = new Color(textMesh.color.r, textMesh.color.g, textMesh.color.b, 0f);

        if (backgroundPanelObject != null)
            backgroundPanelObject.SetActive(true); // ensure visible when needed
    }

    void Update()
    {
        if (isDeactivated) return;

        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
        {
            Touch touch = Input.GetTouch(0);
            Ray ray = Camera.main.ScreenPointToRay(touch.position);

            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                if (hit.transform == transform)
                {
                    Debug.Log("[KeyboardTapHandler] Keyboard tapped!");

                    if (descriptionAudio != null)
                    {
                        audioSource.clip = descriptionAudio;
                        audioSource.Play();
                    }

                    ShowDescriptionCard();
                }
            }
        }
    }

    public void OnCloseButtonTapped()
    {
        isDeactivated = true;
        Debug.Log("[KeyboardTapHandler] Close button tapped.");

        if (canvasObject != null)
            canvasObject.SetActive(false);

        if (audioSource != null && audioSource.isPlaying)
            audioSource.Stop();

        if (keyHighlightGroup != null)
        {
            keyHighlightGroup.SetActive(true);
            Debug.Log("[KeyboardTapHandler] keyHighlightGroup is now active!");
        }
        else
        {
            Debug.LogWarning("[KeyboardTapHandler] keyHighlightGroup is null!");
        }

        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.enabled = false;
            Debug.Log("[KeyboardTapHandler] Root collider disabled.");
        }
    }

    void ShowDescriptionCard()
    {
        Debug.Log("[KeyboardTapHandler] ShowDescriptionCard called");

        if (canvasObject != null)
        {
            canvasObject.SetActive(true);
            Debug.Log("[KeyboardTapHandler] canvasObject activated: " + canvasObject.name);
        }

        if (backgroundPanelObject != null)
        {
            backgroundPanelObject.SetActive(true);
            Debug.Log("[KeyboardTapHandler] backgroundPanelObject active? " + backgroundPanelObject.activeSelf);
        }

        if (backgroundImage != null)
            backgroundImage.color = new Color(1f, 1f, 1f, 1f); // white opaque

        if (textMesh != null)
            textMesh.color = new Color(1f, 1f, 1f, 1f); // black opaque

    }
}
