//using UnityEngine;
//using UnityEngine.UI;
//using System.Collections.Generic;
//using TMPro;

//public class AssemblyTutorialManager : MonoBehaviour
//{
//    [System.Serializable]
//    public class AssemblyStep
//    {
//        public GameObject component;       // The AR model
//        public Transform snapTarget;       // Where it snaps
//        public string instructionText;     // What to show in the UI
//        public GameObject highlight;
//    }

//    public List<AssemblyStep> steps = new List<AssemblyStep>();
//    public Button startButton;
//    public TextMeshProUGUI tutorialText;
//    public TextMeshProUGUI correctMessage;


//    private int currentStep = -1;

//    void Start()
//    {
//        // Disable all components at the start
//        foreach (var step in steps)
//        {
//            step.component.SetActive(false);
//        }

//        startButton.onClick.AddListener(StartTutorial);
//        correctMessage.gameObject.SetActive(false);
//    }

//    public void StartTutorial()
//    {
//        startButton.gameObject.SetActive(false);
//        NextStep();
//    }

//    public void NextStep()
//    {
//        currentStep++;
//        if (currentStep >= steps.Count)
//        {
//            tutorialText.text = "🎉 Computer Setup Complete!";
//            return;
//        }

//        var step = steps[currentStep];
//        step.component.SetActive(true);
//        tutorialText.text = step.instructionText;

//        // Enable drag and snapping
//        var drag = step.component.GetComponent<DragAndSnapStep>();
//        if (drag != null)
//        {
//            drag.enabled = true;
//            drag.Initialize(this, step.snapTarget);
//        }

//        if (step.highlight != null)
//            step.highlight.SetActive(true);

//    }

//    public void ShowCorrectAndContinue()
//    {
//        StartCoroutine(ShowCorrectRoutine());

//        if (steps[currentStep].highlight != null)
//            steps[currentStep].highlight.SetActive(false);

//    }

//    private IEnumerator<WaitForSeconds> ShowCorrectRoutine()
//    {
//        correctMessage.gameObject.SetActive(true);
//        correctMessage.text = "Correct!";
//        yield return new WaitForSeconds(1.5f);
//        correctMessage.gameObject.SetActive(false);
//        NextStep();
//    }
//}
