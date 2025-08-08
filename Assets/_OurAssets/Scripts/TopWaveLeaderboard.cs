using System;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TMPro;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Leaderboards;
using UnityEngine;
using UnityEngine.UI;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections;



#if UNITY_EDITOR
using UnityEditor;
#endif

[Serializable]
public class ScoreMetadata
{
    public string playerName;
}

[Serializable]
public class ScoreEntryReferences
{
    public TMP_Text nameText;
    public TMP_Text waveScoreText;
}


public class TopWaveLeaderboard : MonoBehaviour
{
    // Create a leaderboard with this ID in the Unity Dashboard
    const string LeaderboardId = "top-scores";
    [SerializeField]
    ScoreEntryReferences m_LocalPlayerNewScore;

    [SerializeField]
    ScoreEntryReferences[] m_scoreEntryReferences = new ScoreEntryReferences[10];

    [SerializeField] GameObject m_HighScoreObject;
    [SerializeField] Button m_SubmitScoreButton;
    [SerializeField] TMP_Text m_ScoreSubmittedText;

    Color m_StartColor;

    private async void Awake()
    {
        m_HighScoreObject.SetActive(false);
        m_SubmitScoreButton.gameObject.SetActive(true);
        m_ScoreSubmittedText.gameObject.SetActive(false);

        await UnityServices.InitializeAsync();
        if(!AuthenticationService.Instance.IsSignedIn)
            await AuthenticationService.Instance.SignInAnonymouslyAsync();

        m_SubmitScoreButton.onClick.AddListener(() => SubmitCurrentScore());
        GetScores();
        m_StartColor = m_ScoreSubmittedText.color;
    }

    void OnDestroy()
    {
        m_SubmitScoreButton.onClick.RemoveListener(() => SubmitCurrentScore());
    }

    [ContextMenu("Submit Score")]
    public void SubmitScore()
    {
        int randomWave = UnityEngine.Random.Range(1, 7);
        CheckScoreAndSubmit(randomWave);
    }

    [ContextMenu("Get Scores")]
    public void GetScores()
    {
        GetScoresWithMetadata();
    }

    [ContextMenu("FindScoreEntryReferences")]
    public void FindScoreEntryReferences()
    {
        m_scoreEntryReferences = new ScoreEntryReferences[10];
        // Find the root object somewhere in this gameobjects with the name "slots"
        // Iterate through each child object and populate the m_scoreEntryReferences array
        // Find references by name "NameText", "GenScoreText", and "WaveScoreText"
        var root = FindChildOrGrandchildByName("Slots", transform);
        if (root == null) return;
        for (int i = 0; i < root.childCount; i++)
        {
            m_scoreEntryReferences[i] = new ScoreEntryReferences
            {
                nameText = root.GetChild(i).Find("NameText").GetComponent<TMP_Text>(),
                waveScoreText = root.GetChild(i).Find("WaveScoreText").GetComponent<TMP_Text>()
            };
        }
    }

    Transform FindChildOrGrandchildByName(string name, Transform parent)
    {
        if (parent == null) 
            return null;

        var queue = new Queue<Transform>();
        queue.Enqueue(parent);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            if (current.name == name)
                return current;

            foreach (Transform child in current)
            {
                queue.Enqueue(child);
            }
        }

        return null;
    }

    public async void CheckScoreAndSubmit(int newWave)
    {

        try
        {
            var allScoresResponse = await LeaderboardsService.Instance
                .GetScoresAsync(
                    LeaderboardId,
                    new GetScoresOptions { Limit = 10 }
                );

            // Check to make sure the new score should be added
            if (allScoresResponse.Results.Count > 9 && allScoresResponse.Results.Last().Score >= newWave)
            {
                Debug.Log($"Submitted score {newWave} is not better than last place in top 10 {allScoresResponse.Results.Last().Score}.");
                return;
            }
        }
        catch (Exception)
        {
            Debug.Log("Failed to get leaderboard scores. Possibly a bug?");
        }

        try
        {
            var localBestResponse = await LeaderboardsService.Instance
            .GetPlayerScoreAsync(LeaderboardId);

            // Check new score is less than our current best
            if (localBestResponse != null && newWave <= localBestResponse.Score)
            {
                Debug.Log($"Submitted score {newWave} is not better than existing local player best {localBestResponse.Score}.");
                return;
            }
        }
        catch (Exception)
        {
            Debug.Log("Failed to get local score. Possibly non-existent?");
        }

        m_HighScoreObject.SetActive(true);
        m_SubmitScoreButton.gameObject.SetActive(true);
        m_ScoreSubmittedText.gameObject.SetActive(false);
        m_LocalPlayerNewScore.waveScoreText.text = newWave.ToString();
    }

    void SubmitCurrentScore()
    {
        m_SubmitScoreButton.gameObject.SetActive(false);
        m_ScoreSubmittedText.gameObject.SetActive(true);
        m_ScoreSubmittedText.color = m_StartColor;

        AddScore(int.Parse(m_LocalPlayerNewScore.waveScoreText.text), m_LocalPlayerNewScore.nameText.text);
    }

    async void AddScore(int newWave, string newName)
    {
        var scoreMetadata = new ScoreMetadata { playerName = newName };
        var playerEntry = await LeaderboardsService.Instance.AddPlayerScoreAsync(LeaderboardId, newWave, new AddPlayerScoreOptions { Metadata = scoreMetadata });
        Debug.Log(JsonConvert.SerializeObject(playerEntry));

        StartCoroutine(ResetHighScorePanel());
    }

    IEnumerator ResetHighScorePanel()
    {
        yield return new WaitForSeconds(.15f);
        LeanTween.value(m_ScoreSubmittedText.gameObject, f => m_ScoreSubmittedText.color = Color.Lerp(m_StartColor, Color.clear, f), 0, 1, 1.0f);
        yield return new WaitForSeconds(1.1f);

        m_ScoreSubmittedText.gameObject.SetActive(false);
        m_HighScoreObject.SetActive(false);

        GetScores();
    }

    async void GetScoresWithMetadata()
    {
        var scoresResponse = await LeaderboardsService.Instance
            .GetScoresAsync(
                LeaderboardId,
                new GetScoresOptions { Limit = 10, IncludeMetadata = true }
            );

        string metadataString = "";
        int count = 0;
        string playerName = "";
        foreach (var scoreEntry in scoresResponse.Results)
        {
            if (!string.IsNullOrEmpty(scoreEntry.Metadata))
            {
                JObject metadataJson = JObject.Parse(scoreEntry.Metadata);
                playerName = metadataJson["playerName"]?.ToString();
                m_scoreEntryReferences[count].nameText.text = playerName;
                m_scoreEntryReferences[count].waveScoreText.text = scoreEntry.Score.ToString();
            }

            Debug.Log($"{scoreEntry.Rank}. {playerName} - {scoreEntry.Score} - {metadataString}");

            count++;
        }
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(TopWaveLeaderboard))]
public class TopWaveLeaderboardEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        TopWaveLeaderboard myScript = (TopWaveLeaderboard)target;
        if (GUILayout.Button("Submit Random Score"))
        {
            myScript.SubmitScore();
        }
    }
}
#endif
