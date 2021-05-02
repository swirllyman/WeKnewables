using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Tutorial : MonoBehaviour
{
    public static bool tutorialFinished;
    public float textSpeed = .05f;
    public float waitTimeBetweenDialogue = 3.5f;
    [TextArea(3, 5)]
    public string[] dialogueStrings;

    public GameObject tutorialObject;
    public GameObject clickToContinueObject;
    public GameObject startTutorialButton;

    public TMP_Text dialogueText;
    public Image nextTextFillImage;
    public Animator tutorialAnimator;

    int currentDialogueID = 0;
    float currentWaitTime = 0.0f;

    bool waiting = false;
    bool playingDialogue = false;

    bool waitingOnExternalInput = false;
    Coroutine currentDialogueRoutine;

    public Toggle[] allToggles;
    public Button[] allButtons;
    public Button gasButton;
    public Button solarButton;
    public Button beeFarmButton;
    public Button sellButton;
    public Button FFButton;
    public Toggle farmToggle;
    // Start is called before the first frame update
    void Awake()
    {
        if (!PlayerPrefs.HasKey("TutorialFinished"))
        {
            PlayerPrefs.SetInt("TutorialFinished", 0);
        }

        tutorialFinished = PlayerPrefs.GetInt("TutorialFinished") == 0 ? false : true;

        startTutorialButton.SetActive(tutorialFinished);
        tutorialObject.SetActive(false);
    }

    private void Update()
    {
        if (Input.anyKeyDown &! waitingOnExternalInput)
        {
            if (playingDialogue)
            {
                playingDialogue = false;
                StopCoroutine(currentDialogueRoutine);
                FinishDialogue();
            }
            else if (waiting)
            {
                currentWaitTime = waitTimeBetweenDialogue;
            }
        }

        if (waiting &! waitingOnExternalInput)
        {
            currentWaitTime += Time.deltaTime;
            nextTextFillImage.fillAmount = currentWaitTime / waitTimeBetweenDialogue;
            if (currentWaitTime >= waitTimeBetweenDialogue)
            {
                StartNextEquence();

            }
        }
    }

    public void ReplayTutorial()
    {
        tutorialFinished = false;
        GameManager.singleton.cameraController.PlayIntialGameStart();
    }

    internal void StartTutorial()
    {
        if (!tutorialFinished)
        {
            tutorialAnimator.SetTrigger("PlayTutorial");
            tutorialObject.SetActive(true);
            LeanTween.scale(clickToContinueObject, clickToContinueObject.transform.localScale * 1.05f, .5f).setLoopPingPong().setEaseInSine();
            StartNextEquence();

            foreach(Button b in allButtons)
            {
                b.interactable = false;
            }

            foreach(Toggle t in allToggles)
            {
                t.interactable = false;
            }
        }
    }

    public void PlayNext()
    {
        if (!tutorialFinished)
        {
            if(currentDialogueID - 1 == 3)
            {
                gasButton.interactable = false;
            }

            if (currentDialogueID - 1 == 6)
            {
                sellButton.interactable = false;
            }

            if (currentDialogueID - 1 == 7)
            {
                solarButton.interactable = false;
            }

            if (currentDialogueID - 1 == 10)
            {
                farmToggle.interactable = false;
            }

            if (currentDialogueID - 1 == 11)
            {
                beeFarmButton.interactable = false;
            }

            //if (currentDialogueID - 1 == 13)
            //{
            //    FFButton.interactable = false;
            //}

            if (playingDialogue)
            {
                playingDialogue = false;
                StopCoroutine(currentDialogueRoutine);
                FinishDialogue();
            }

            StartNextEquence();
        }
    }

    void StartNextEquence()
    {
        waiting = false;
        currentWaitTime = 0.0f;
        nextTextFillImage.fillAmount = 0.0f;
        clickToContinueObject.SetActive(false);
        if (playingDialogue)
        {
            playingDialogue = false;
            StopCoroutine(currentDialogueRoutine);
        }

        playingDialogue = true;
        if (currentDialogueID >= dialogueStrings.Length)
        {
            playingDialogue = false;
            StartCoroutine(FinishTutorialDialogue());
        }
        else
        {
            currentDialogueRoutine = StartCoroutine(PlayNextDialogue());
        }
    }

    bool CheckForDialogueAnim()
    {
        return currentDialogueID != 0 && currentDialogueID != 1 && currentDialogueID != 2 && currentDialogueID != 8 && currentDialogueID != 14/*&& currentDialogueID != 5*/ /*&& currentDialogueID != 2*/;
    }

    bool CheckForRequiredExternalInput()
    {
        return currentDialogueID != 0 && currentDialogueID != 1 && currentDialogueID != 2 && currentDialogueID != 5 && currentDialogueID != 9 && currentDialogueID != 14;
    }

    IEnumerator PlayNextDialogue()
    {
        if(currentDialogueID == 1)
        {
            GameManager.singleton.cameraController.MoveToOverview();
        }

        if(currentDialogueID == 3)
        {
            GameManager.singleton.cameraController.MoveToDestination(0);
            gasButton.interactable = true;
        }

        if (currentDialogueID == 6)
        {
            sellButton.interactable = true;
        }

        if (currentDialogueID == 7)
        {
            solarButton.interactable = true;
        }

        if (currentDialogueID == 10)
        {
            farmToggle.interactable = true;
        }

        if (currentDialogueID == 11)
        {
            beeFarmButton.interactable = true;
        }

        if (currentDialogueID == 13)
        {
            FFButton.interactable = true;
        }

        if (CheckForDialogueAnim())
        {
            tutorialAnimator.SetTrigger("PlayNext");
        }

        waitingOnExternalInput = CheckForRequiredExternalInput();

        int totalTextCount = dialogueStrings[currentDialogueID].Length;
        int currentCount = 0;
        dialogueText.text = dialogueStrings[currentDialogueID];

        currentDialogueID++;
        while (currentCount < totalTextCount)
        {
            currentCount = Mathf.Clamp(currentCount + 1, 0, totalTextCount);
            dialogueText.maxVisibleCharacters = currentCount;
            yield return new WaitForSeconds(textSpeed);
        }

        FinishDialogue();
    }

    void FinishDialogue()
    {
        dialogueText.maxVisibleCharacters = dialogueText.text.Length;
        playingDialogue = false;
        waiting = true;
        clickToContinueObject.SetActive(true);
        if (currentDialogueID >= dialogueStrings.Length)
        {
            playingDialogue = false;
            StartCoroutine(FinishTutorialDialogue());
        }
    }

    IEnumerator FinishTutorialDialogue()
    {
        clickToContinueObject.SetActive(false);
        yield return new WaitForSeconds(2.0f);
        tutorialObject.SetActive(false);
    }

    
    internal void TutorialFinished()
    {
        PlayerPrefs.SetInt("TutorialFinished", 1);
        tutorialFinished = true;
        StartCoroutine(FinishTutorialSequence());
    }

    IEnumerator FinishTutorialSequence()
    {
        GameManager.singleton.PlayNotification("Tutorial Complete!");
        yield return new WaitForSeconds(2.0f);
        GameManager.singleton.PlayNotification("Game Restarting in 1 Second!");
        yield return new WaitForSeconds(1.0f);
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
