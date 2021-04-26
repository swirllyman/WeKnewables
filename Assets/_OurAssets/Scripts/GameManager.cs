using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager singleton;
    public static StructurePropertyScriptableObject currentStructure;

    public StructurePropertyScriptableObject[] structureProperties;
    public MainMenuUI mainMenuUI;
    public Tooltip myTooltip;
    public StructureSelectedUI selectedStructure;
    public Placeholder placeholder;
    public PathManager pathManager;

    internal int currentStructureID;

    int nextPathID = 0;

    Coroutine pathRoutine;

    private void Awake()
    {
        singleton = this;
        myTooltip.uiObject.SetActive(false);
        selectedStructure.uiObject.SetActive(false);

        ShowNextPath();
    }

    private void Update()
    {
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

    #region UI Buttons
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

    #region Path
    internal void ShowNextPath()
    {
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
        //GridInteraction.singleton.ActivateSelectMode();
    }

    internal void SelectStructure(StructurePropertyScriptableObject structureProperty)
    {
        selectedStructure.uiObject.SetActive(true);
        selectedStructure.towerName.text = structureProperty.name + " " + (structureProperty.currentLevel + 1);
        selectedStructure.attackSpeed.text = "Attack Speed: " + structureProperty.GetAttackSpeed().ToString("F0");
        selectedStructure.range.text = "Range: " + structureProperty.GetRange().ToString("F0");
        selectedStructure.satisfaction.text = "Satisfaction: " + structureProperty.GetSatisfaction().ToString("F0");
        selectedStructure.pollution.text = "Pollution: " + structureProperty.GetPollution().ToString("F0");

        DisplayTooltip(structureProperty.name +" "+ (structureProperty.currentLevel + 1), structureProperty.tooltip[Random.Range(0, structureProperty.tooltip.Length)]);
    }

    internal void DeselectStructure()
    {
        selectedStructure.uiObject.SetActive(false);
        HideTooltip();
    }
    #endregion

    #region Tooltips
    internal void DisplayTooltip(string name, string newTooltip)
    {
        myTooltip.uiObject.SetActive(true);
        myTooltip.header.text = name;
        myTooltip.info.text = newTooltip;
    }

    internal void HideTooltip()
    {
        myTooltip.uiObject.SetActive(false);
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
    public TMP_Text towerName;
    public TMP_Text attackSpeed;
    public TMP_Text range;
    public TMP_Text satisfaction;
    public TMP_Text pollution;
}

[System.Serializable]
public struct Tooltip
{
    public GameObject uiObject;
    public TMP_Text header;
    public TMP_Text info;
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

    internal GamePath currentPath;

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
    }

    internal void UpdateCount(int remainingCount)
    {
        waveCountText.text = "Current Not Happy\n#: " + remainingCount;
    }

    internal void StartPath()
    {
        currentPathParticles.SetActive(false);
        startAreaText.SetActive(false);
        endAreaText.SetActive(false);
        sendWaveButton.SetActive(false);
        currentWaveUI.SetActive(true);
        waveCountText.text = "Current Not Happy\n#:  " + currentPath.totalSpawnCount;
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
        placementSprite.sprite = structureProperty.currentSprites[structureProperty.currentLevel];

        if (structureProperty.powerStructure)
        {
            rangePlacement_Power.localScale = Vector3.one * ((structureProperty.GetRange() * 2) + 1 + ((structureProperty.size - 1) * .7f));
        }
        else
        {
            rangePlacement_Attack.localScale = Vector3.one * (structureProperty.GetRange() / 10);
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