using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;


#if UNITY_EDITOR
using UnityEditor;
#endif


public class GameManager : MonoBehaviour
{
    public static GameManager singleton;
    public static StructurePropertyScriptableObject currentStructure;
    public static GenerationScriptableObject currentGeneration;
    public static int waveNum = 0;
    public static int currentGen = 0;
    public static int pollutionLevel = 0;
    public static bool failed = false;
    public static bool cheating = false;

    public CameraController cameraController;
    public Tutorial myTutorial;
    public Image[] UICostImages;
    public StructurePropertyScriptableObject[] structureProperties;
    public GenerationScriptableObject[] generationProperties;

    [Header("Structs")]
    public MainMenuUI mainMenuUI;
    public FactualTooltip factualTooltip;
    public MouseTooltip mouseTooltip;
    public StructureSelectedUI selectedStructure;
    public GameNotificationUI gameNotification;
    public WaveRecapUI waveRecap;
    public Pollution pollution;
    public FastForward fastForward;
    public PauseButton pauseButton;
    public GameObject restartMenu;

    [Header("Classes")]
    public Placeholder placeholder;
    public PathManager pathManager;
    public Economy economy;
    public TopWaveLeaderboard m_Leaderboard;


    internal int currentStructureID;
    internal int nextPathID = 0;
    internal int totalSets = 0;
    internal int totalWaves = 0;

    bool recap = false;
    bool buildPhase = true;
    bool paused = false;
    float buildPhaseTimer = 0.0f;

    Cell currentStructureCell;

    Coroutine pathRoutine;
    Coroutine currentNotificationRoutine;
    Coroutine pollutionFillRoutine;
    Coroutine moneyAddedRoutine;
    Coroutine endOfRoundRecap;

    public delegate void PollutionCallback();
    public event PollutionCallback onPollutionChange;

    private void Awake()
    {
        StopFF();
        singleton = this;
        factualTooltip.uiObject.SetActive(false);
        selectedStructure.uiObject.SetActive(false);
        restartMenu.SetActive(false);
        HideMouseTooltip();
        economy.currentMoneyText.text = "$ " + economy.currentMoney;

        currentGeneration = generationProperties[0];
        waveNum = 0;
        currentGen = 0;
        pollutionLevel = 0;
        failed = false;

        LeanTween.scale(mainMenuUI.gameLogo, mainMenuUI.gameLogo.transform.localScale * 1.05f, .45f).setLoopPingPong().setEaseInExpo();
        ShowNextPath();
        AddPollution(0);
        UpdateButtons();
        UpdateUIPanel(0);
        pathManager.UpdateNextWaveText();
    }

    private void OnDestroy()
    {
        singleton = null;
    }

    private void Update()
    {
        // if (Input.GetKeyDown(KeyCode.Space))
        // {
        //     DisplayMouseTooltip();
        // }

        if (placeholder.show)
        {
            if (GridInteraction.singleton.currentCell == null)
            {
                placeholder.HidePlaceholder();
            }
            else
            {
                placeholder.UpdatePlaceholderPosition(GridInteraction.singleton.GetCurrentPlacementPosition(), currentStructure.powerStructure);
            }
        }

        if (!cheating && buildPhase & !FirstWave())
        {
            if (buildPhaseTimer >= 0.0f)
            {
                buildPhaseTimer -= Time.deltaTime;
                pathManager.nextWaveTimer.text = "Starting in " + buildPhaseTimer.ToString("F1");
                if (buildPhaseTimer <= 10.0f & !pathManager.warning)
                {
                    pathManager.warning = true;
                    PlayNotification("Wave Starting In 10 Seconds!");
                }

                if (buildPhaseTimer <= 0.0f)
                {
                    buildPhase = false;
                    SendWave();
                }
            }
        }
    }

    bool FirstWave()
    {
        return (totalWaves == 0 && totalSets == 0 && currentGen == 0);
    }

    public bool WaveActive()
    {
        if (pathManager != null && pathManager.currentPath != null)
            return pathManager.currentPath.roundActive;
        else
            return false;
    }

    public static int GetCurrentGenSpawnAmount()
    {
        var totalSpawnCount = currentGeneration.unitsPerWave;

        if (singleton.totalSets == 1)
            totalSpawnCount += 40;
        else if (singleton.totalSets >= 2)
            totalSpawnCount += 50 * (singleton.totalSets + 1) / 2;

        return totalSpawnCount;
    }
    public static float CurrentGenHappiness()
    {
        return CalculateHappiness(currentGeneration.unitHappiness);
    }

    public static float CurrentGenFullHappiness()
    {
        return CalculateHappiness(currentGeneration.unitFullHappiness);
    }

    static float CalculateHappiness(float currentHappinessRequired)
    {
        if (singleton.totalSets > 5)
            currentHappinessRequired *= 100 * singleton.totalSets;
        else if (singleton.totalSets > 3)
            currentHappinessRequired *= 15 * singleton.totalSets;
        // else if (singleton.totalSets > 1)
        //     currentHappinessRequired *= 5 * singleton.totalSets;
        else if (singleton.totalSets > 0)
            currentHappinessRequired *= 5 * singleton.totalSets;

        return currentHappinessRequired;
    }

    public static float GetCurrentGenSpeed()
    {
        return singleton.totalSets >= 1 ? (singleton.totalSets + 1) / 2 : 1;
    }

    public static int GetCurrentGenMoney()
    {
        if (singleton.totalSets == 0 && currentGen == 0)
            return 25;
            
        var money = currentGeneration.unitMoney;
        if (singleton.totalSets > 5)
            money += singleton.totalSets * 10;
        else if (singleton.totalSets > 3)
            money += singleton.totalSets * 5;
        else
            money += singleton.totalSets * 2;

        return money;
    }

    public static int GetWaveEndBonus()
    {
        return currentGeneration.waveEndBonus + singleton.totalSets * 100;
    }

    public static int GetGenerationEndBonus()
    {
        return currentGeneration.generationEndBonus + singleton.totalSets * 100;
    }

    public void StopFF()
    {
        fastForward.ffState = FastForward.FF_State.Normal;
        Time.timeScale = 1.0f;
        fastForward.speedChangeImage.sprite = fastForward.ffIcons[1];
    }

    #region FastForward
    void CycleNextFFState()
    {
        int currentState = (int)fastForward.ffState;

        currentState = (currentState + 1) % Enum.GetValues(typeof(FastForward.FF_State)).Length;
        fastForward.ffState = (FastForward.FF_State)currentState;

        switch (fastForward.ffState)
        {
            case FastForward.FF_State.Normal:
                StopFF();
                break;

            case FastForward.FF_State.Medium:
                Time.timeScale = 5.0f;
                fastForward.speedChangeImage.sprite = fastForward.ffIcons[2];
                break;

            case FastForward.FF_State.Maximum:
                Time.timeScale = 10.0f;
                fastForward.speedChangeImage.sprite = fastForward.ffIcons[0];
                break;
        }
    }
    #endregion


    #region Notification
    internal void PlayNotification(string notification, float holdTime = 2.0f)
    {
        gameNotification.notificationText.text = notification;
        gameNotification.notificationText.transform.localScale = Vector3.zero;
        LeanTween.scale(gameNotification.notificationText.rectTransform, Vector3.one, .25f).setEaseInExpo();

        if (currentNotificationRoutine != null) StopCoroutine(currentNotificationRoutine);
        currentNotificationRoutine = StartCoroutine(ResetNotification(holdTime));
    }

    IEnumerator ResetNotification(float holdTime)
    {
        yield return new WaitForSecondsRealtime(holdTime);
        gameNotification.notificationText.transform.localScale = Vector3.one;
        LeanTween.scale(gameNotification.notificationText.rectTransform, Vector3.zero, .25f).setEaseInExpo();
    }
    #endregion

    #region UI Buttons
    public void RestartGameButtonPressed()
    {
        restartMenu.SetActive(true);
        SetPause(true);
    }

    public void SetPause(bool pause)
    {
        if (pause)
        {
            paused = true;
            Time.timeScale = 0.0f;
            pauseButton.pauseImage.sprite = pauseButton.pauseButtonIcons[0];
            fastForward.FFButton.interactable = false;
        }
        else
        {
            paused = false;
            pauseButton.pauseImage.sprite = pauseButton.pauseButtonIcons[1];
            Time.timeScale = 1.0f;
            fastForward.FFButton.interactable = true;
        }
    }

    public void TogglePause()
    {
        SetPause(!paused);
    }
    public void MuteToggle()
    {
        MusicManager.singleton.ToggleMute();
    }

    public void ChangeSpeed()
    {
        if (buildPhase)
        {
            SendWave();
            StopFF();
        }
        else
        {
            if (recap)
            {
                StopCoroutine(endOfRoundRecap);
                endOfRoundRecap = null;
                StartCoroutine(ShowNextPath(.5f));
                waveRecap.nextWaveText.text = "";
                waveRecap.uiObject.SetActive(false);
            }
            else if (!buildPhase)
            {
                CycleNextFFState();
            }
        }
    }

    public void SellCurrentTower()
    {
        if (currentStructureCell != null)
        {
            currentStructureCell.SellStructure();
        }
    }

    public void RestartGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void EndWaveEarly()
    {
        pathManager.EndWaveEarly();
    }

    //Called Fom UI Button
    public void SendWave()
    {
        StopCoroutine(pathRoutine);
        pathManager.StartPath();
        totalWaves++;
        waveRecap.currentText.text = $"Wave {totalWaves} / Gen {currentGen + 1} - {totalSets}";
        pathManager.currentWaveText.text = "Wave " + totalWaves;
        PlayNotification("Wave " + totalWaves + " Has Started!");
        buildPhase = false;
    }

    //Called From UI Buttons
    public void UpdateUIPanel(int panelID)
    {
        for (int i = 0; i < mainMenuUI.uiPanels.Length; i++)
        {
            mainMenuUI.uiPanels[i].SetActive(i == panelID);
        }

    }

    //Called From UI Buttons
    public void StructureButtonPressed(int newID)
    {
        currentStructureID = newID;
        placeholder.SetupPlaceholder(structureProperties[newID]);
        currentStructure = structureProperties[newID];
        GridInteraction.singleton.ActivateBuildMode();
        ShowStructureInfo(currentStructure);
    }
    #endregion

    #region Waves
    internal void StartNewGame()
    {
        myTutorial.StartTutorial();
        pathManager.totalLeaks = 0;
        failed = false;
    }

    public void Fail()
    {
        failed = true;
        if (fastForward.ffState != FastForward.FF_State.Normal)
            StopFF();

        StartCoroutine(FailedGameRoutine());
    }

    IEnumerator FailedGameRoutine()
    {
        PlayNotification("Game Failed. Too Many Unfulfilled People.", 3.0f);
        yield return new WaitForSecondsRealtime(3.0f);
        PlayNotification($"Wave {totalWaves} / Gen {currentGen + 1} - {totalSets}", 60.0f);

        if (!cheating)
        {
            if (m_Leaderboard == null)
                m_Leaderboard = FindFirstObjectByType<TopWaveLeaderboard>();

            m_Leaderboard.CheckScoreAndSubmit(totalWaves);
        }

        yield return new WaitForSecondsRealtime(1.0f);
        mainMenuUI.restartButton.SetActive(true);
        yield return new WaitForSecondsRealtime(60.0f);
        PlayNotification("Game Restarting in 1 second.");
        yield return new WaitForSecondsRealtime(1.0f);
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    internal void WaveFinished()
    {
        if (fastForward.ffState != FastForward.FF_State.Normal)
            StopFF();


        if (!Tutorial.tutorialFinished)
        {
            myTutorial.TutorialFinished();
            return;
        }

        // AddMoney((int)(GetCurrentGenMoney() / 2) * pathManager.currentPath.fullySatisfiedCount);
        // AddMoney((int)GetCurrentGenMoney() * pathManager.currentPath.satisfiedCount);
        // AddMoney(currentGeneration.waveEndBonus);

        if (endOfRoundRecap != null) StopCoroutine(endOfRoundRecap);
        endOfRoundRecap = StartCoroutine(PostRoundDisplay());
    }

    IEnumerator PostRoundDisplay()
    {
        recap = true;
        PlayNotification("Wave " + totalWaves + " Complete!", 1.5f);
        AddMoney(GetWaveEndBonus());
        yield return new WaitForSecondsRealtime(.5f);
        cameraController.MoveToOverview();

        yield return new WaitForSecondsRealtime(1.0f);
        PlayNotification("Wave Bonus: " + currentGeneration.waveEndBonus, 1.5f);
        waveRecap.uiObject.SetActive(true);
        waveRecap.playersHappyRecap.text = "";
        waveRecap.playersReallyHappyRecap.text = "";
        float lerpTime;

        float satisfied, fullySatisfied;
        int totalSpawnCount = pathManager.currentPath.totalSpawnCount;

        lerpTime = Mathf.Lerp(.25f, 1.0f, pathManager.currentPath.satisfiedCount / totalSpawnCount);
        for (float i = 0.0f; i <= lerpTime; i += Time.deltaTime)
        {
            float perc = Mathf.Clamp01(i / lerpTime);
            satisfied = Mathf.Lerp(0, pathManager.currentPath.satisfiedCount, perc);

            waveRecap.playersHappyRecap.text = "Happy Players\n" + (int)satisfied + " / " + totalSpawnCount + "\t<color=yellow>+" + (int)(satisfied * GetCurrentGenMoney());
            yield return null;
        }

        waveRecap.playersHappyRecap.text = "Happy Players\n" + pathManager.currentPath.satisfiedCount + " / " + pathManager.currentPath.totalSpawnCount +
           "\t<color=yellow>+" + pathManager.currentPath.satisfiedCount * GetCurrentGenMoney();

        lerpTime = Mathf.Lerp(.25f, 1.0f, pathManager.currentPath.fullySatisfiedCount / totalSpawnCount);
        for (float i = 0.0f; i <= lerpTime; i += Time.deltaTime)
        {
            float perc = Mathf.Clamp01(i / lerpTime);
            fullySatisfied = Mathf.Lerp(0, pathManager.currentPath.fullySatisfiedCount, perc);
            waveRecap.playersReallyHappyRecap.text = "Very Happy Players<\n" + (int)fullySatisfied + " / " + totalSpawnCount + "\t<color=yellow>+" + (int)(fullySatisfied * (GetCurrentGenMoney() * .5f));
            yield return null;
        }

        waveRecap.playersReallyHappyRecap.text = "Very Happy Players\n" + pathManager.currentPath.fullySatisfiedCount + " / " + pathManager.currentPath.totalSpawnCount +
            "\t<color=yellow>+" + (int)(pathManager.currentPath.fullySatisfiedCount * (GetCurrentGenMoney() * .5f));
        for (int i = 3; i > 0; i--)
        {
            waveRecap.nextWaveText.text = "Next Wave Starting In: " + i;
            yield return new WaitForSecondsRealtime(1.0f);
        }

        waveRecap.nextWaveText.text = "";
        waveRecap.uiObject.SetActive(false);

        yield return new WaitForSecondsRealtime(.25f);

        StartCoroutine(ShowNextPath(.5f));
    }

    IEnumerator ShowNextPath(float waitTime = .5f)
    {
        recap = false;
        buildPhase = true;
        buildPhaseTimer = currentGeneration.buildTime;
        if (endOfRoundRecap != null) StopCoroutine(endOfRoundRecap);
        endOfRoundRecap = null;

        if (nextPathID == 0)
        {
            AddMoney(GetGenerationEndBonus());
            PlayNotification($"Generation {currentGen} - {totalSets} Finished! <color=green> +" + GetGenerationEndBonus() + "</color>");
            currentGen = (currentGen + 1) % generationProperties.Length;
            if (currentGen == 0)
            {
                totalSets++;
            }
            yield return new WaitForSecondsRealtime(1.5f);
        }

        int pathID = nextPathID;
        ShowNextPath();

        yield return new WaitForSecondsRealtime(1.0f);

        //PlayNotification("Wave " + totalWaves + " Starting");
        waveNum = (waveNum + 1) % pathManager.gamePaths.Length;
        yield return new WaitForSecondsRealtime(waitTime);
        cameraController.MoveToDestination(pathID);
        yield return new WaitForSecondsRealtime(waitTime);
        PlayNotification("Build Phase!");
        if (fastForward.ffState != FastForward.FF_State.Normal)
            StopFF();

        if (!cheating)
        {
            yield return new WaitForSecondsRealtime(1.5f);
            PlayNotification("Wave " + totalWaves + " Starting In " + currentGeneration.buildTime.ToString("F0") + " Seconds!");
        }
    }

    internal void ShowNextPath()
    {
        recap = false;
        currentGeneration = generationProperties[currentGen];
        pathManager.SetupNextPath(nextPathID);

        if (pathRoutine != null) StopCoroutine(pathRoutine);
        pathRoutine = StartCoroutine(ShowPath());

        nextPathID = (nextPathID + 1) % pathManager.gamePaths.Length;
    }

    IEnumerator ShowPath()
    {
        while (true)
        {
            for (int i = 0; i < pathManager.currentPath.waypoints.Length - 1; i++)
            {
                Vector3 currentWaypoint = pathManager.currentPath.transform.position + pathManager.currentPath.waypoints[i];
                Vector3 nextWaypoint = pathManager.currentPath.transform.position + pathManager.currentPath.waypoints[i + 1];
                float lerpTime = Mathf.Lerp(.03f, .3f, (Vector2.Distance(currentWaypoint, nextWaypoint) + .5f) / (10 + .5f));
                for (float j = 0; j < lerpTime; j += Time.deltaTime)
                {
                    pathManager.currentPathParticles.transform.position = Vector2.Lerp(currentWaypoint, nextWaypoint, j / lerpTime);
                    yield return null;
                }
            }
        }
    }
    #endregion

    #region Structures
    public void StructurePlaced()
    {
        RemoveMoney(currentStructure.cost);
        AddPollution(currentStructure.pollution);
    }

    internal void SelectStructure(Cell structureCell)
    {
        selectedStructure.uiObject.SetActive(true);
        StructurePropertyScriptableObject hoverStructure = structureCell.structureProperty;
        //selectedStructure.selectedButton.interactable = hoverStructure.canSell;

        string requirementText = "";
        if (hoverStructure.buildRestrictions == StructurePropertyScriptableObject.LocationRestrictions.Water)
            requirementText = "<color=white>Requires: <color=#00E0FF>Water</color>";
        else if (hoverStructure.buildRestrictions == StructurePropertyScriptableObject.LocationRestrictions.Land)
            requirementText = "<color=white>Requires: <color=#8DCA42>Land</color>";

        string bonusText = "";
        if (hoverStructure.bonusProperties.emitFromTower)
            bonusText = "\n<color=#FFBA88>[Emits From Tower]</color>";
        if (hoverStructure.bonusProperties.AoE && hoverStructure.bonusProperties.slow)
            bonusText += "<color=white>Bonus: AoE(<color=#FF866C>" + hoverStructure.bonusProperties.radius + ")</color> / Slow(<color=#8EC6F8>" + hoverStructure.bonusProperties.slowDurationInSeconds.ToString("F1") + ")</color>";
        else if (hoverStructure.bonusProperties.AoE)
            bonusText += "<color=white>Bonus: AoE(<color=#FF866C>" + hoverStructure.bonusProperties.radius + ")</color>";
        else if (hoverStructure.bonusProperties.slow)
            bonusText += "<color=white>Bonus: Slow(<color=#8EC6F8>" + (hoverStructure.bonusProperties.slowPercent * 100).ToString("F1") + "%)</color>";

        string attackText = "\n<color=white>Total Damage: " + structureCell.totalDamage +
            "\n<color=white>Happiness Per Shot: " +
            (pollution.pollutionPercent > .4f ? "<color=#E89AF8>" + (hoverStructure.attackProperties.satisfaction * (1 - pollution.pollutionPercent)).ToString("F1") + "</color>" :
            "<color=#FF70BF>" + hoverStructure.attackProperties.satisfaction.ToString("F0")) + "</color>" +
            "\nShot CD: " + hoverStructure.attackProperties.attackSpeed.ToString("F1");

        string sellText;
        if (hoverStructure.canSell)
            sellText = "<color=white>Sell Price: <color=green>+ $" + hoverStructure.sellPrice + "</color>";
        else
            sellText = "<color=red>Cannot Sell</color>";

        string tooltip = "<b><color=black>[" + hoverStructure.structureName + "]</b></color>" +
           "\n<color=white>Size: " + hoverStructure.size +
           "\nRange: " + "Range: " + hoverStructure.range.ToString("F0") +
           "\n<color=#E89AF8>Pollution: " + hoverStructure.pollution.ToString("F0") +
           (hoverStructure.powerStructure ? "\n\n<b><color=yellow>Powers Other Towers</b>\n<color=white>(Does Not Shoot)" : "\n" +
           (structureCell.isPowered ? "" : "\n<color=#FF7C75>Requires Power To Shoot") + attackText) +
           (!string.IsNullOrEmpty(bonusText) ? "\n" + bonusText : "") +
           "\n\n" + sellText + "\n" +
            (!string.IsNullOrEmpty(requirementText) ? "\n" + requirementText : "");

        selectedStructure.tooltipText.text = tooltip;
        DisplayFactualTooltip(structureCell.structureProperty.name, structureCell.structureProperty.tooltip[UnityEngine.Random.Range(0, structureCell.structureProperty.tooltip.Length)]);
        currentStructureCell = structureCell;
    }

    void ShowStructureInfo(StructurePropertyScriptableObject structure)
    {
        selectedStructure.uiObject.SetActive(true);
        StructurePropertyScriptableObject hoverStructure = structure;
        string requirementText = "";
        if (hoverStructure.buildRestrictions == StructurePropertyScriptableObject.LocationRestrictions.Water)
            requirementText = "<color=white>Requires: <color=#00E0FF>Water</color>";
        else if (hoverStructure.buildRestrictions == StructurePropertyScriptableObject.LocationRestrictions.Land)
            requirementText = "<color=white>Requires: <color=#8DCA42>Land</color>";

        string bonusText = "";
        if (hoverStructure.bonusProperties.emitFromTower)
            bonusText = "\n<color=#FFBA88>[Emits From Tower]</color>";
        if (hoverStructure.bonusProperties.AoE && hoverStructure.bonusProperties.slow)
            bonusText += "<color=white>Bonus: AoE(<color=#FF866C>" + hoverStructure.bonusProperties.radius + ")</color> / Slow(<color=#8EC6F8>" + hoverStructure.bonusProperties.slowDurationInSeconds.ToString("F1") + ")</color>";
        else if (hoverStructure.bonusProperties.AoE)
            bonusText += "<color=white>Bonus: AoE(<color=#FF866C>" + hoverStructure.bonusProperties.radius + ")</color>";
        else if (hoverStructure.bonusProperties.slow)
            bonusText += "<color=white>Bonus: Slow(<color=#8EC6F8>" + (hoverStructure.bonusProperties.slowPercent * 100).ToString("F1") + "%)</color>";

        string attackText = "\n<color=white>Happiness Per Shot: " +
            (pollution.pollutionPercent > .4f ? "<color=#E89AF8>" + (hoverStructure.attackProperties.satisfaction * (1 - pollution.pollutionPercent)).ToString("F1") + "</color>" :
            "<color=#FF70BF>" + hoverStructure.attackProperties.satisfaction.ToString("F0")) + "</color>" +
            "\nShot CD: " + hoverStructure.attackProperties.attackSpeed.ToString("F1");

        string sellText;
        if (hoverStructure.canSell)
            sellText = "<color=white>Sell Price: <color=green>+ $" + hoverStructure.sellPrice + "</color>";
        else
            sellText = "<color=red>Cannot Sell</color>";

        string tooltip = "<b><color=black>[" + hoverStructure.structureName + "]</b></color>" +
           "\n<color=white>Size: " + hoverStructure.size +
           "\nRange: " + "Range: " + hoverStructure.range.ToString("F0") +
           "\n<color=#E89AF8>Pollution: " + hoverStructure.pollution.ToString("F0") +
           (hoverStructure.powerStructure ? "\n\n<b><color=yellow>Powers Other Towers</b>\n<color=white>(Does Not Shoot)" : "\n" +
            attackText) +
           (!string.IsNullOrEmpty(bonusText) ? "\n" + bonusText : "") +
           "\n\n" + sellText + "\n" +
            (!string.IsNullOrEmpty(requirementText) ? "\n" + requirementText : "");

        selectedStructure.tooltipText.text = tooltip;
        DisplayFactualTooltip(hoverStructure.name, hoverStructure.tooltip[UnityEngine.Random.Range(0, hoverStructure.tooltip.Length)]);
    }

    internal void DeselectStructure()
    {
        selectedStructure.uiObject.SetActive(false);
        HideFactualTooltip();
        currentStructureCell = null;
    }
    #endregion

    #region Tooltips
    internal void DisplayFactualTooltip(string name, string newTooltip)
    {
        factualTooltip.uiObject.SetActive(true);
        factualTooltip.header.text = name;
        factualTooltip.info.text = newTooltip;
    }

    internal void HideFactualTooltip()
    {
        factualTooltip.uiObject.SetActive(false);
    }

    internal void DisplayMouseTooltip()
    {
        mouseTooltip.uiObject.SetActive(true);
        mouseTooltip.tooltipTransform.localPosition = Input.mousePosition - mouseTooltip.myCanvas.transform.localPosition;
    }

    //Called From UI Events
    public void DisplayMouseTooltipForStructure(int structureID)
    {
        mouseTooltip.uiObject.SetActive(true);

        StructurePropertyScriptableObject hoverStructure = structureProperties[structureID];

        string requirementText = "";
        if (hoverStructure.buildRestrictions == StructurePropertyScriptableObject.LocationRestrictions.Water)
            requirementText = "<color=white>Requires: <color=#00E0FF>Water</color>";
        else if (hoverStructure.buildRestrictions == StructurePropertyScriptableObject.LocationRestrictions.Land)
            requirementText = "<color=white>Requires: <color=#8DCA42>Land</color>";

        string bonusText = "";
        if (hoverStructure.bonusProperties.emitFromTower)
            bonusText = "\n<color=#FFBA88>[Emits From Tower]</color>\n";
        if (hoverStructure.bonusProperties.AoE && hoverStructure.bonusProperties.slow)
            bonusText += "<color=white>Bonus: <color=#FF866C>AoE(" + hoverStructure.bonusProperties.radius + ")</color> / <color=#8EC6F8>Slow(" + (hoverStructure.bonusProperties.slowPercent * 100).ToString("F1") + "%)</color>";
        else if (hoverStructure.bonusProperties.AoE)
            bonusText += "<color=white>Bonus: <color=#FF866C>AoE(" + hoverStructure.bonusProperties.radius + ")</color>";
        else if (hoverStructure.bonusProperties.slow)
            bonusText += "<color=white>Bonus: <color=#8EC6F8>Slow(" + (hoverStructure.bonusProperties.slowPercent * 100).ToString("F1") + "%)</color>";

        //if (hoverStructure.bonusProperties.emitFromTower)
        //    bonusText += "\n<color=#763200>[Emits From Tower]</color>";


        string attackText = "\n<color=#FF7C75>Requires Power To Shoot\n<color=white>Happiness Per Shot: " +
            (pollution.pollutionPercent > .4f ? "<color=#E89AF8>" + (hoverStructure.attackProperties.satisfaction * (1 - pollution.pollutionPercent)).ToString("F1") + "</color>" :
            "<color=#FF70BF>" + hoverStructure.attackProperties.satisfaction.ToString("F0")) + "</color>" +
            "\nShot CD: " + hoverStructure.attackProperties.attackSpeed.ToString("F1");


        string sellText;
        if (hoverStructure.canSell)
            sellText = "<color=white>Sell Price: <color=green>+ $" + hoverStructure.sellPrice + "</color>";
        else
            sellText = "<color=red>Cannot Sell</color>";

        string tooltip = "<b><color=black>[" + hoverStructure.structureName + "]</b></color>" +
            (hoverStructure.powerStructure ? "\n<b><color=yellow>Powers Other Towers</b>\n<color=white>(Does Not Shoot)" : attackText) +
            "\n\n<color=white>Size: " + hoverStructure.size +
            "\nRange: " + hoverStructure.range.ToString("F0") +
            "\n<color=#E89AF8>Pollution: " + hoverStructure.pollution.ToString("F0") +
            (!string.IsNullOrEmpty(bonusText) ? "\n" + bonusText : "") +
            "\n\n" + (HasEnoughMoney(hoverStructure.cost) ? "<color=green>" : "<color=#FF7C75>") + "Cost: " + hoverStructure.cost +
            " <color=white>-- <color=yellow>(Current: " + economy.currentMoney + ")" +
            "\n" + sellText + "\n" +
            (!string.IsNullOrEmpty(requirementText) ? "\n" + requirementText : "");

        mouseTooltip.tooltipText.text = tooltip;
        //mouseTooltip.tooltipTransform.localPosition = Input.mousePosition - mouseTooltip.myCanvas.transform.localPosition;
    }

    //Called From UI Events
    public void HideMouseTooltip()
    {
        mouseTooltip.uiObject.SetActive(false);
        //mouseTooltip.tooltipTransform.localPosition = Input.mousePosition - mouseTooltip.myCanvas.transform.localPosition;
    }
    #endregion

    #region Economy
    public static bool HasEnoughMoney(int amount)
    {
        return amount <= singleton.economy.currentMoney;
    }

    internal void RemoveMoney(int moneyToRemove)
    {
        economy.currentMoney -= moneyToRemove;
        economy.currentMoneyText.text = "$ " + economy.currentMoney;
        UpdateButtons();
        if (moneyAddedRoutine != null) StopCoroutine(moneyAddedRoutine);
        moneyAddedRoutine = StartCoroutine(UpdateMoneyOverTime(economy.currentMoney + moneyToRemove));
    }

    public void AddMoney(int moneyToAdd)
    {
        economy.currentMoney += moneyToAdd;
        economy.currentMoneyText.text = "$ " + economy.currentMoney;
        if (moneyAddedRoutine != null) StopCoroutine(moneyAddedRoutine);
        moneyAddedRoutine = StartCoroutine(UpdateMoneyOverTime(economy.currentMoney - moneyToAdd));

        UpdateButtons();
    }

    void UpdateButtons()
    {
        for (int i = 0; i < UICostImages.Length; i++)
        {
            UICostImages[i].enabled = structureProperties[i].cost > economy.currentMoney;
        }
    }

    IEnumerator UpdateMoneyOverTime(int startMoney)
    {
        float lerpTime = .25f;

        for (float i = 0; i < lerpTime; i += Time.deltaTime)
        {
            float perc = i / lerpTime;

            economy.currentMoneyText.text = "$ " + (int)Mathf.Lerp(startMoney, economy.currentMoney, perc);
            yield return null;
        }

        economy.currentMoneyText.text = "$ " + economy.currentMoney;
    }

    internal void UnitFullySatisfied()
    {
        pathManager.UpdateFullySatisfiedCount(pathManager.currentPath.fullySatisfiedCount, GetCurrentGenSpawnAmount());
        AddMoney((int)(GetCurrentGenMoney() * .5f));
    }

    internal void UnitSatisfied()
    {
        pathManager.UpdateSatisfiedCount(pathManager.currentPath.satisfiedCount, GetCurrentGenSpawnAmount());
        AddMoney(GetCurrentGenMoney());
    }
    #endregion

    #region Pollution
    public static bool HasEnoughPollution(int amount)
    {
        return (amount + singleton.pollution.currentPollution) <= singleton.pollution.totalAllowedPollution;
    }

    public void AddPollution(int pollutionAmount)
    {
        pollution.currentPollution += pollutionAmount;
        pollution.currentPollution = Mathf.Clamp(pollution.currentPollution, 0, pollution.totalAllowedPollution);
        pollution.pollutionPercent = (float)pollution.currentPollution / pollution.totalAllowedPollution;
        pollution.pollutionText.text = "Pollution\n" + (pollution.pollutionPercent * 100).ToString("F0") + "%";
        if (pollutionFillRoutine != null) StopCoroutine(pollutionFillRoutine);
        pollutionFillRoutine = StartCoroutine(UpdateFillImageOverTime());
        CheckPollutionAmount(pollutionAmount > 0);
    }

    void CheckPollutionAmount(bool adding = false)
    {
        bool changed = false;
        if (adding)
        {
            while (pollutionLevel < pollution.pollutionChangeThresholds.Length && pollution.pollutionPercent >= pollution.pollutionChangeThresholds[pollutionLevel])
            {
                if (pollutionLevel >= pollution.pollutionChangeThresholds.Length)
                    break;

                //Debug.Log("Increasing Pollution");
                pollutionLevel++;
                changed = true;
            }
        }
        else
        {
            //Decrease Pollution Level
            while (pollutionLevel >= 1 && pollution.pollutionPercent < pollution.pollutionChangeThresholds[pollutionLevel - 1])
            {
                if (pollutionLevel <= 0)
                    break;

                changed = true;
                pollutionLevel--;
            }
        }

        if (changed)
            UpdatePollution();
    }

    void UpdatePollution()
    {
        string pollutionString = "Pollution Threat <color=green>Neutralized</color>";
        //Debug.Log("Updated Pollution: " + pollutionLevel);
        switch (pollutionLevel)
        {
            case 1:
                pollutionString = "Pollution Threat Minor -- Happiness Reduced <color=purple>40%!</color>";
                break;
            case 2:
                pollutionString = "Pollution Threat Medium -- Happiness Reduced <color=purple>75%!</color>";
                break;
            case 3:
                pollutionString = "Pollution Threat Major -- <color=purple>Happiness Reduced 90%!</color>";
                break;
            case 4:
                pollutionString = "<color=purple>Pollution Level Maximum -- Happiness Reduced 100%!</color>";
                break;

        }

        PlayNotification(pollutionString);
        onPollutionChange?.Invoke();
    }

    IEnumerator UpdateFillImageOverTime()
    {
        float lerpTime = .25f;
        float startPerc = pollution.pollutionFillImage.fillAmount;
        for (float i = 0; i < lerpTime; i += Time.deltaTime)
        {
            float perc = i / lerpTime;
            pollution.pollutionFillImage.fillAmount = Mathf.Lerp(startPerc, pollution.pollutionPercent, perc);
            yield return null;
        }
        pollution.pollutionFillImage.fillAmount = pollution.pollutionPercent;
    }
    #endregion
}

[System.Serializable]
public struct MainMenuUI
{
    public GameObject uiObject;
    public RectTransform gameLogo;
    public GameObject restartButton;
    public GameObject[] uiPanels;
}

[System.Serializable]
public struct StructureSelectedUI
{
    public GameObject uiObject;
    public TMP_Text tooltipText;
    public Button selectedButton;
}

[System.Serializable]
public struct FactualTooltip
{
    public GameObject uiObject;
    public TMP_Text header;
    public TMP_Text info;
}

[System.Serializable]
public struct MouseTooltip 
{
    public GameObject uiObject;
    public Canvas myCanvas; 
    public RectTransform tooltipTransform;
    public TMP_Text tooltipText;
}

[System.Serializable]
public struct GameNotificationUI
{
    public GameObject uiObject;
    public TMP_Text notificationText;
}

[System.Serializable]
public struct WaveRecapUI
{
    public GameObject uiObject;
    public TMP_Text playersHappyRecap;
    public TMP_Text playersReallyHappyRecap;
    public TMP_Text nextWaveText;
    public TMP_Text currentText;
}

[System.Serializable]
public struct Pollution
{
    public int totalAllowedPollution;
    public int currentPollution;
    public float pollutionPercent;
    public float[] pollutionChangeThresholds;
    public TMP_Text pollutionText;
    public Image pollutionFillImage;
}

[Serializable]
public struct PauseButton
{
    public Sprite[] pauseButtonIcons;
    public Image pauseImage;
}

[System.Serializable]
public struct FastForward
{
    internal enum FF_State { Normal, Medium, Maximum};

    public Image speedChangeImage;
    public Sprite[] ffIcons;
    public Button FFButton;
    internal FF_State ffState;
}

[System.Serializable]
public class Economy
{
    public int currentMoney = 250;
    public TMP_Text currentMoneyText;
}

[System.Serializable]
public class PathManager
{
    public GamePath[] gamePaths;
    public GameObject currentPathParticles;
    public GameObject startAreaText;
    public GameObject endAreaText;
    public GameObject currentWaveUI;
    public GameObject nextWaveUI;
    public TMP_Text currentWaveText;
    public TMP_Text satisfiedText;
    public TMP_Text fullySatisfiedText;
    public TMP_Text livesLostText;
    public TMP_Text nextWaveText;
    public TMP_Text nextWaveTimer;

    internal GamePath currentPath;

    internal int totalAllowedLeaks = 10;
    internal int totalLeaks = 0;
    internal bool warning = false;

    internal void Leak()
    {
        totalLeaks++;
        livesLostText.text = ": " + totalLeaks + " / " + totalAllowedLeaks;

        if(totalLeaks > totalAllowedLeaks)
        {
            GameManager.singleton.Fail();
        }
    }

    internal void EndWaveEarly()
    {
        currentPath.EndWaveEarly();
    }

    internal void SetupNextPath(int newPathID)
    {
        currentWaveText.text = "Wave " + (GameManager.singleton.totalWaves + 1);

        currentPath = gamePaths[newPathID];
        currentPathParticles.SetActive(true);
        startAreaText.SetActive(true);
        endAreaText.SetActive(true);
        currentWaveUI.SetActive(false);
        nextWaveUI.SetActive(true);
        UpdateNextWaveText();

        startAreaText.transform.position = currentPath.transform.position + currentPath.waypoints[0];
        endAreaText.transform.position = currentPath.transform.position + currentPath.waypoints[currentPath.waypoints.Length - 1];

        currentWaveUI.SetActive(false);

        //Show Info on next Wave Here Somewhere
    }

    internal void UpdateNextWaveText()
    {
        nextWaveText.text = "<size=25>Next Wave</size>" +
        
            "\nPeople: " + GameManager.GetCurrentGenSpawnAmount() +
            "\nHeath: " + GameManager.CurrentGenHappiness() +
            "\nSpeed: " + (GameManager.currentGeneration.unitSpeed * GameManager.GetCurrentGenSpeed()).ToString("F2");
    }

    internal void UpdateSatisfiedCount(int satisfied, int total)
    {
        satisfiedText.text = satisfied + " / "+ total;
    }

    internal void UpdateFullySatisfiedCount(int satisfied, int total)
    {
        fullySatisfiedText.text = satisfied + " / " + total;
    }

    internal void StartPath()
    {
        currentPathParticles.SetActive(false);
        startAreaText.SetActive(false);
        endAreaText.SetActive(false);
        nextWaveUI.SetActive(false);
        currentWaveUI.SetActive(true);
        satisfiedText.text = 0 + " / " + GameManager.currentGeneration.unitsPerWave;
        fullySatisfiedText.text = 0 + " / " + GameManager.currentGeneration.unitsPerWave;
        currentPath.SendWave();
        warning = false;
    }
}

[System.Serializable]
public class Placeholder
{
    public GameObject uiObject;
    public SpriteRenderer placementSprite;
    public Transform rangePlacement_Power;
    public Transform rangePlacement_Attack;
    internal bool show = false;

    internal void SetupPlaceholder(StructurePropertyScriptableObject structureProperty)
    {
        uiObject.SetActive(true);
        show = true;
        placementSprite.sprite = structureProperty.currentSprite;
        placementSprite.transform.localScale = Vector3.one * .75f * structureProperty.size;

        if (structureProperty.powerStructure)
        {
            rangePlacement_Power.localScale = Vector3.one * ((structureProperty.range * 2) + 1 + ((structureProperty.size - 1) * .7f) - .5f);
        }
        else
        {
            rangePlacement_Attack.localScale = Vector3.one * structureProperty.range * 3f;
        }
    }

    internal void UpdatePlaceholderPosition(Vector3 newPosition, bool power)
    {
        placementSprite.transform.position = newPosition;
        if (power)
        {
            rangePlacement_Power.position = newPosition;
        }
        else
        {
            rangePlacement_Attack.position = newPosition;
        }
    }

    internal void HidePlaceholder()
    {
        Vector3 newPosition = Vector3.down * 10000;
        placementSprite.transform.position = newPosition;
        rangePlacement_Power.position = newPosition;
        rangePlacement_Attack.position = newPosition;
    }

    internal void RemovePlaceholder()
    {
        show = false;
        uiObject.SetActive(false);
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(GameManager))]
public class GameManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        GameManager gm = (GameManager)target;
        GUI.enabled = gm.WaveActive();
        if (GUILayout.Button("End Wave Early"))
            gm.EndWaveEarly();
        GUI.enabled = true;
    }
}
#endif