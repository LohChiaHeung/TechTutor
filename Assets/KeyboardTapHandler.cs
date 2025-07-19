using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class KeyboardTapHandler : MonoBehaviour
{
    public AudioClip descriptionAudio;
    public string descriptionText;
    public GameObject canvasObject;
    public GameObject backgroundPanelObject; // Should be the "BackgroundPanel" GameObject
    public GameObject keyHighlightGroup;
    public Button closeButton;
    private bool isDeactivated = false;


    private AudioSource audioSource;
    private TextMeshProUGUI textMesh;
    private Image backgroundImage;

    void Start()
    {
        Debug.Log("[KeyboardTapHandler] backgroundImage component: " + backgroundImage);


        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;

        // Get references
        if (backgroundPanelObject != null)
        {
            textMesh = backgroundPanelObject.GetComponentInChildren<TextMeshProUGUI>();
            backgroundImage = backgroundPanelObject.GetComponent<Image>();
        }

        // Hide everything initially
        if (canvasObject != null)
            canvasObject.SetActive(false);

        if (closeButton != null)
            closeButton.onClick.AddListener(OnCloseButtonTapped);

        if (backgroundImage != null)
            backgroundImage.color = new Color(backgroundImage.color.r, backgroundImage.color.g, backgroundImage.color.b, 0f);

        if (textMesh != null)
            textMesh.color = new Color(textMesh.color.r, textMesh.color.g, textMesh.color.b, 0f);

        if (backgroundPanelObject != null)
        {
            backgroundPanelObject.SetActive(true); // ensure it's not accidentally disabled
            Debug.Log("[KeyboardTapHandler] Background panel set active manually at Start");
        }

    }

    void Update()
    {
        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
        {
            if (isDeactivated) return;

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
        isDeactivated = true; // 🔐 Disallow all future taps
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

        // ✅ Disable this object's collider so it doesn't block future clicks
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
            backgroundPanelObject.SetActive(true); // just in case
            Debug.Log("[KeyboardTapHandler] backgroundPanelObject active? " + backgroundPanelObject.activeSelf);
        }
        else
        {
            Debug.LogWarning("[KeyboardTapHandler] backgroundPanelObject is null!");
        }

        if (backgroundImage != null)
            backgroundImage.color = new Color(0f, 1f, 0f, 1f); // green, fully opaque

        if (textMesh != null)
        {
            textMesh.color = new Color(1f, 1f, 1f, 1f); // white, fully opaque
        }
    }
}
