using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class TutorialStep
{
    [TextArea]
    public string stepText;
    public List<GameObject> objectsToActivate = new List<GameObject>();
    public List<GameObject> objectsToDeactivate = new List<GameObject>();
}

public class TutorialUI : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private GameObject tutorialPanel;
    [SerializeField] private TextMeshProUGUI tutorialText;
    [SerializeField] private Button nextButton;
    [SerializeField] private TextMeshProUGUI nextButtonText;

    [Header("Tutorial Content")]
    [SerializeField] private List<TutorialStep> tutorialSteps = new List<TutorialStep>();

    private int currentStep = 0;
    private const string TutorialShownKey = "TutorialShown";

    private void Start()
    {
        if (PlayerPrefs.GetInt(TutorialShownKey, 0) == 1)
        {
            tutorialPanel.SetActive(false);
            return;
        }

        tutorialPanel.SetActive(true);
        currentStep = 0;
        ShowStep(currentStep);

        nextButton.onClick.AddListener(OnNextClicked);
    }

    private void ShowStep(int stepIndex)
    {
        if (stepIndex < 0 || stepIndex >= tutorialSteps.Count) return;

        TutorialStep step = tutorialSteps[stepIndex];
        tutorialText.text = step.stepText;

        foreach (var obj in step.objectsToActivate)
        {
            if (obj != null) obj.SetActive(true);
        }

        foreach (var obj in step.objectsToDeactivate)
        {
            if (obj != null) obj.SetActive(false);
        }

        nextButtonText.text = stepIndex == tutorialSteps.Count - 1 ? "Finish" : "Next";
    }

    private void OnNextClicked()
    {
        MusicController.Instance.PlayClickSound();

        currentStep++;

        if (currentStep < tutorialSteps.Count)
        {
            ShowStep(currentStep);
        }
        else
        {
            tutorialPanel.SetActive(false);
            PlayerPrefs.SetInt(TutorialShownKey, 1);
            PlayerPrefs.Save();
        }
    }
}