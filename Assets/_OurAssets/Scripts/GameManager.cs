using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static GameManager singleton;
    public static StructurePropertyScriptableObject currentStructure;
    public static GenerationScriptableObject currentGeneration;
    public static int waveNum = 0;
    public static bool failed = false;

    public CameraController cameraController;
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

    [Header("Classes")]
    public Placeholder placeholder;
    public PathManager pathManager;
    public Economy economy;

    internal int currentStructureID;
    internal int nextPathID = 0;

    int currentGenerationID = 0;
    Cell currentStructureCell;

    Coroutine pathRoutine;
    Coroutine currentNotificationRoutine;
    Coroutine pollutionFillRoutine;
    Coroutine moneyAddedRoutine;

    private void Awake()
    {
        singleton = this;
        factualTooltip.uiObject.SetActive(false);
        selectedStructure.uiObject.SetActive(false);
        HideMouseTooltip();
        economy.currentMoneyText.text = "$ " +economy.currentMoney;

        currentGeneration = generationProperties[0];
        ShowNextPath();
        AddPollution(0);
        UpdateButtons();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            DisplayMouseTooltip();
        }

        if (placeholder.show)
        {
            if(GridInteraction.singleton.currentCell == null)
            {
                placeholder.HidePlaceholder();
            }
            else
            {
                placeholder.UpdatePlaceholderPosition(GridInteraction.singleton.GetCurrentPlacementPosition(), currentStructure.powerStructure);
            }
        }
    }

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
        yield return new WaitForSeconds(holdTime);
        gameNotification.notificationText.transform.localScale = Vector3.one;
        LeanTween.scale(gameNotification.notificationText.rectTransform, Vector3.zero, .25f).setEaseInExpo();
    }

    #region UI Buttons
    internal void StartNewGame()
    {
        pathManager.totalLeaks = 0;
        failed = false;
    }

    public void SellCurrentTower()
    {
        print("Getting Here?FDgsDS?");
        if (currentStructureCell != null)
        {
            print("Getting Here?FDgsDS?");
            currentStructureCell.SellStructure();
        }
    }

    //Called Fom UI Button
    public void SendWave()
    {
        StopCoroutine(pathRoutine);
        pathManager.StartPath();
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
    }
    #endregion

    #region Waves
    public void Fail()
    {
        failed = true;
        StartCoroutine(FailedGameRoutine());
    }

    IEnumerator FailedGameRoutine()
    {
        PlayNotification("Game Failed. Too Many Unfulfilled People.", 3.0f);
        yield return new WaitForSeconds(3.0f);
        PlayNotification("Wave: "+waveNum+", Gen: "+currentGenerationID, 3.0f);
        yield return new WaitForSeconds(3.0f);
        PlayNotification("Game Restarting in 1 second.");
        yield return new WaitForSeconds(1.0f);
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    internal void WaveFinished()
    {
        StartCoroutine(PostRoundDisplay());
    }

    IEnumerator PostRoundDisplay()
    {
        int path = nextPathID;
        PlayNotification("Wave "+ nextPathID+" Complete!", 1.5f);
        yield return new WaitForSeconds(.5f);
        cameraController.MoveToOverview();

        yield return new WaitForSeconds(1.0f);
        PlayNotification("Wave Bonus: "+currentGeneration.waveEndBonus, 1.5f);
        AddMoney(currentGeneration.waveEndBonus);
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

            waveRecap.playersHappyRecap.text = "<u>Happy Players</u>\n" + (int)satisfied + " / " + totalSpawnCount + "\t<color=yellow>+" + (int)(satisfied * currentGeneration.unitMoney);
            yield return null;
        }

        waveRecap.playersHappyRecap.text = "<u>Happy Players</u>\n" + pathManager.currentPath.satisfiedCount + " / " + pathManager.currentPath.totalSpawnCount +
           "\t<color=yellow>+" + pathManager.currentPath.satisfiedCount * currentGeneration.unitMoney;
        AddMoney(currentGeneration.unitMoney * pathManager.currentPath.satisfiedCount);

        lerpTime = Mathf.Lerp(.25f, 1.0f, pathManager.currentPath.fullySatisfiedCount / totalSpawnCount);
        for (float i = 0.0f; i <= lerpTime; i += Time.deltaTime)
        {
            float perc = Mathf.Clamp01(i / lerpTime);
            fullySatisfied = Mathf.Lerp(0, pathManager.currentPath.fullySatisfiedCount, perc);
            waveRecap.playersReallyHappyRecap.text = "<u>Very Happy Players</u>\n" + (int)fullySatisfied + " / " + totalSpawnCount + "\t<color=yellow>+" + (int)(fullySatisfied * currentGeneration.unitMoney);
            yield return null;
        }

        waveRecap.playersReallyHappyRecap.text = "<u>Very Happy Players</u>\n" + pathManager.currentPath.fullySatisfiedCount + " / " + pathManager.currentPath.totalSpawnCount +
            "\t<color=yellow>+" + pathManager.currentPath.fullySatisfiedCount * currentGeneration.unitMoney;

        AddMoney(currentGeneration.unitMoney * pathManager.currentPath.fullySatisfiedCount);
        for (int i = 3; i > 0; i--)
        {
            waveRecap.nextWaveText.text = "Next Wave Starting In: " + i;
            yield return new WaitForSeconds(1.0f);
        }

        waveRecap.nextWaveText.text = "";
        waveRecap.uiObject.SetActive(false);

        yield return new WaitForSeconds(.25f);
        
        if(nextPathID == 0)
        {
            PlayNotification("Generation "+ (++currentGenerationID) +" Finished!");
            yield return new WaitForSeconds(1.5f);
        }

        ShowNextPath();

        PlayNotification("Wave " + nextPathID + " Starting");
        waveNum = (waveNum + 1) % pathManager.gamePaths.Length;
        yield return new WaitForSeconds(.5f);
        cameraController.MoveToDestination(path);
        yield return new WaitForSeconds(.5f);
        PlayNotification("Build Phase!");
    }

    internal void ShowNextPath()
    {
        currentGeneration = generationProperties[currentGenerationID % generationProperties.Length];
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
                Vector3 currentWaypoint = pathManager.currentPath.transform.position +  pathManager.currentPath.waypoints[i];
                Vector3 nextWaypoint = pathManager.currentPath.transform.position +  pathManager.currentPath.waypoints[i + 1];
                float lerpTime = Mathf.Lerp(.03f, .3f, (Vector2.Distance(currentWaypoint, nextWaypoint) + .5f) / (10 + .5f));
                for(float j = 0; j < lerpTime; j += Time.deltaTime)
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
        selectedStructure.selectedButton.interactable = hoverStructure.canSell;

        string requirementText = "";
        if (hoverStructure.buildRestrictions == StructurePropertyScriptableObject.LocationRestrictions.Water)
            requirementText = "<color=white>Requires: <color=#00E0FF>Water</color>";
        else if (hoverStructure.buildRestrictions == StructurePropertyScriptableObject.LocationRestrictions.Land)
            requirementText = "<color=white>Requires: <color=#8DCA42>Land</color>";

        string bonusText = "";
        if (hoverStructure.bonusProperties.AoE && hoverStructure.bonusProperties.slow)
            bonusText = "<color=white>Bonus: AoE(<color=green>" + hoverStructure.bonusProperties.radius + "</color>) / Slow(<color=blue>" + hoverStructure.bonusProperties.slowDurationInSeconds.ToString("F1") + "</color>)";
        else if (hoverStructure.bonusProperties.AoE)
            bonusText = "<color=white>Bonus: AoE(<color=green>" + hoverStructure.bonusProperties.radius + "</color>)";
        else if (hoverStructure.bonusProperties.slow)
            bonusText = "<color=white>Bonus: Slow(<color=blue>" + (hoverStructure.bonusProperties.slowPercent * 100).ToString("F1") + "%</color>)";
        if (hoverStructure.bonusProperties.emitFromTower)
            bonusText += "\n[Emits From Tower]";

        string sellText;
        if (hoverStructure.canSell)
            sellText = "<color=white>Sell Price: <color=green>+ $" + hoverStructure.sellPrice+ "</color>";
        else
            sellText = "<color=red>Cannot Sell</color>";

        string tooltip = "<u><b><color=black>[" + hoverStructure.structureName + "]</u></b></color>" +
           "\nSize: " + hoverStructure.size +
           "\nRange: " + "Range: " + hoverStructure.range.ToString("F0") +
           "\n<color=purple>Pollution: " + hoverStructure.pollution.ToString("F0") +
           (hoverStructure.powerStructure ? "\n\n<b><color=yellow>Powers Other Towers</b>\n<color=white>(Does Not Shoot)" : "\n" +
           (structureCell.isPowered ? "" : "\n<color=red>Requires Power To Shoot") + "\n<color=white>Happiness Per Shot: " + hoverStructure.attackProperties.satisfaction.ToString("F0") +
           "\nShots Per Second: " + hoverStructure.attackProperties.attackSpeed.ToString("F1")) +
           "\n\n" + sellText + "\n" +
            (!string.IsNullOrEmpty(requirementText) ? "\n" + requirementText : "") +
            (!string.IsNullOrEmpty(bonusText) ? "\n" + bonusText : "");

        selectedStructure.tooltipText.text = tooltip;
        DisplayFactualTooltip(structureCell.structureProperty.name, structureCell.structureProperty.tooltip[Random.Range(0, structureCell.structureProperty.tooltip.Length)]);
        currentStructureCell = structureCell;
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
        if(hoverStructure.buildRestrictions == StructurePropertyScriptableObject.LocationRestrictions.Water)
            requirementText = "<color=white>Requires: <color=#00E0FF>Water</color>";
        else if(hoverStructure.buildRestrictions == StructurePropertyScriptableObject.LocationRestrictions.Land)
            requirementText = "<color=white>Requires: <color=#8DCA42>Land</color>";

        string bonusText = "";
        if(hoverStructure.bonusProperties.AoE && hoverStructure.bonusProperties.slow)
            bonusText = "<color=white>Bonus: <color=#FFC100>AoE(" + hoverStructure.bonusProperties.radius + "</color>) / <color=blue>Slow(" + hoverStructure.bonusProperties.slowDurationInSeconds.ToString("F1") + "</color>)";
        else if (hoverStructure.bonusProperties.AoE)
            bonusText = "<color=white>Bonus: <color=#FFC100>AoE(" + hoverStructure.bonusProperties.radius + "</color>)";
        else if (hoverStructure.bonusProperties.slow)
            bonusText = "<color=white>Bonus: <color=blue>Slow(" + (hoverStructure.bonusProperties.slowPercent * 100).ToString("F1") + "%</color>)";

        if (hoverStructure.bonusProperties.emitFromTower)
            bonusText += "\n<color=#763200>[Emits From Tower]</color>";

        string sellText;
        if (hoverStructure.canSell)
            sellText = "<color=white>Sell Price: <color=green>+ $" + hoverStructure.sellPrice + "</color>";
        else
            sellText = "<color=red>Cannot Sell</color>";

        string tooltip = "<u><b><color=black>[" + hoverStructure.structureName + "]</u></b></color>" +
            "\nSize: " + hoverStructure.size +
            "\nRange: " + "Range: " + hoverStructure.range.ToString("F0") +
            "\n<color=purple>Pollution: " + hoverStructure.pollution.ToString("F0") +
            (hoverStructure.powerStructure ? "\n\n<b><color=yellow>Powers Other Towers</b>\n<color=white>(Does Not Shoot)" : "\n\n<color=red>Requires Power To Shoot\n<color=white>Happiness Per Shot: " + hoverStructure.attackProperties.satisfaction.ToString("F0") +
            "\nShots Per Second: " + hoverStructure.attackProperties.attackSpeed.ToString("F1")) +
            "\n\n" + (HasEnoughMoney(hoverStructure.cost) ? "<color=green>" : "<color=red>") + "Cost: " + hoverStructure.cost +
            " <color=white>-- <color=yellow>(Current: " + economy.currentMoney + ")" +
            "\n" + sellText + "\n" +
            (!string.IsNullOrEmpty(requirementText) ? "\n" + requirementText : "") +
            (!string.IsNullOrEmpty(bonusText) ? "\n" + bonusText : "");

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
    }

    public void AddMoney(int moneyToAdd)
    {
        economy.currentMoney += moneyToAdd;
        economy.currentMoneyText.text = "$ " + economy.currentMoney;
        if (moneyAddedRoutine != null) StopCoroutine(moneyAddedRoutine);
        moneyAddedRoutine = StartCoroutine(AddMoneyOverTime(economy.currentMoney - moneyToAdd));

        UpdateButtons();
    }

    void UpdateButtons()
    {
        for (int i = 0; i < UICostImages.Length; i++)
        {
            UICostImages[i].enabled = structureProperties[i].cost > economy.currentMoney;
        }
    }

    IEnumerator AddMoneyOverTime(int startMoney)
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
        pathManager.UpdateFullySatisfiedCount(pathManager.currentPath.fullySatisfiedCount, currentGeneration.unitsPerWave);
    }

    internal void UnitSatisfied()
    {
        pathManager.UpdateSatisfiedCount(pathManager.currentPath.satisfiedCount, currentGeneration.unitsPerWave);
        AddMoney(currentGeneration.unitMoney);
    }
    #endregion

    #region Pollution
    public static bool HasEnoughPollution(int amount)
    {
        return (amount  + singleton.pollution.currentPollution) <= singleton.pollution.totalAllowedPollution;
    }

    public void AddPollution(int pollutionAmount)
    {
        pollution.currentPollution += pollutionAmount;
        pollution.currentPollution = Mathf.Clamp(pollution.currentPollution, 0, pollution.totalAllowedPollution);
        pollution.pollutionPercent = (float)pollution.currentPollution / pollution.totalAllowedPollution;
        pollution.pollutionText.text = "<u>Pollution</u>\n" + (pollution.pollutionPercent * 100).ToString("F0") + "%";
        if (pollutionFillRoutine != null) StopCoroutine(pollutionFillRoutine);
        pollutionFillRoutine = StartCoroutine(UpdateFillImageOverTime());
    }

    IEnumerator UpdateFillImageOverTime()
    {
        float lerpTime = .25f;
        float startPerc = pollution.pollutionFillImage.fillAmount;
        for (float i = 0; i < lerpTime; i+= Time.deltaTime)
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
    public GameObject[] uiPanels;
}

[System.Serializable]
public struct StructureSelectedUI
{
    public GameObject uiObject;
    public TMP_Text tooltipText;
    public Button selectedButton;
    //public TMP_Text towerName;
    //public TMP_Text attackSpeed;
    //public TMP_Text range;
    //public TMP_Text satisfaction;
    //public TMP_Text bonuses;
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
}

[System.Serializable]
public struct Pollution
{
    public int totalAllowedPollution;
    public int currentPollution;
    public float pollutionPercent;
    public TMP_Text pollutionText;
    public Image pollutionFillImage;
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
    public GameObject sendWaveButton;
    public GameObject currentWaveUI;
    public TMP_Text waveCountText;
    public TMP_Text fullySatisfiedText;
    public TMP_Text livesLostText;

    internal GamePath currentPath;

    internal int totalAllowedLeaks = 10;
    internal int totalLeaks = 0;
    internal void Leak()
    {
        totalLeaks++;
        livesLostText.text = "Lives Lost: " + totalLeaks + " / " + totalAllowedLeaks;

        if(totalLeaks > totalAllowedLeaks)
        {
            GameManager.singleton.Fail();
        }
    }

    internal void SetupNextPath(int newPathID)
    {
        currentPath = gamePaths[newPathID];
        currentPathParticles.SetActive(true);
        startAreaText.SetActive(true);
        endAreaText.SetActive(true);
        sendWaveButton.SetActive(true);
        currentWaveUI.SetActive(false);

        startAreaText.transform.position = currentPath.transform.position + currentPath.waypoints[0];
        endAreaText.transform.position = currentPath.transform.position + currentPath.waypoints[currentPath.waypoints.Length - 1];

        sendWaveButton.SetActive(true);
        currentWaveUI.SetActive(false);

        //Show Info on next Wave Here Somewhere
    }

    internal void UpdateSatisfiedCount(int satisfied, int total)
    {
        waveCountText.text = "Happy\n#: " + satisfied + " / "+ total;
    }

    internal void UpdateFullySatisfiedCount(int satisfied, int total)
    {
        fullySatisfiedText.text = "Super Happy\n#: " + satisfied + " / " + total;
    }

    internal void StartPath()
    {
        currentPathParticles.SetActive(false);
        startAreaText.SetActive(false);
        endAreaText.SetActive(false);
        sendWaveButton.SetActive(false);
        currentWaveUI.SetActive(true);
        waveCountText.text = "Happy\n#:  " + 0 / currentPath.totalSpawnCount;
        fullySatisfiedText.text = "Super Happy\n#: " + 0 / currentPath.totalSpawnCount;
        currentPath.SendWave();
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
            rangePlacement_Power.localScale = Vector3.one * ((structureProperty.range * 2) + 1 + ((structureProperty.size - 1) * .7f));
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