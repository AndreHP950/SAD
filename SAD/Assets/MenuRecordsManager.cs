using Microsoft.Unity.VisualStudio.Editor;
using System.Collections.Generic;
using Unity.VisualScripting.FullSerializer;
using UnityEngine;

[System.Serializable]
public class CharacterScore
{
    public GameManager.AvailableCharacters character;
    public int score;

    public CharacterData Data => GameManager.instance.characterList[(int)character];
}

public class MenuRecordsManager : MonoBehaviour
{
    public List<CharacterScore> ranking = new List<CharacterScore>(6);

    public List<GameObject> recordsPlates;

    private void Start()
    {
        LoadRanking();
        UpdateUI();
    }

    private void LoadRanking()
    {
        ranking.Clear();

        if (!PlayerPrefs.HasKey("Rankings")) return;

        string data = PlayerPrefs.GetString("Rankings");
        string[] entries = data.Split('|');

        foreach (string entry in entries)
        {
            if (string.IsNullOrEmpty(entry)) continue;

            string[] parts = entry.Split(',');

            int id = int.Parse(parts[0]);
            int score = int.Parse(parts[1]);

            ranking.Add(new CharacterScore { character = (GameManager.AvailableCharacters)id, score = score });
        }

        ranking.Sort((a, b) => b.score.CompareTo(a.score));

    }

    private void SaveRanking()
    {
        string data = "";

        foreach (var c in ranking)
        {
            data += $"{(int)c.character},{c.score}|";
        }

        PlayerPrefs.SetString("Rankings", data);
        PlayerPrefs.Save();
    }

    private void UpdateUI()
    {
        for (int i = 0; i < recordsPlates.Count; i++)
        {
            if (i < ranking.Count)
            {
                recordsPlates[i].SetActive(true);

                UnityEngine.UI.Image img = recordsPlates[i].transform.Find("RecordCanva/CharacterImage").GetComponent<UnityEngine.UI.Image>();
                TMPro.TextMeshProUGUI txt = recordsPlates[i].transform.Find("RecordCanva/RecordText").GetComponent<TMPro.TextMeshProUGUI>();

                CharacterData data = ranking[i].Data;

                img.sprite = data.characterIcon;
                txt.text = ranking[i].score.ToString();
            }
        }
    }

    public void UpdateRanking(GameManager.AvailableCharacters character, int score)
    {
        ranking.Add(new CharacterScore { character = character, score = score });

        ranking.Sort((a, b) => b.score.CompareTo(a.score));

        if (ranking.Count > 6) ranking.RemoveAt(ranking.Count - 1);

        SaveRanking();
        UpdateUI();
    }
}
