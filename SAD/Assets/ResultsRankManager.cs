using UnityEngine;

public class ResultsRankManager : MonoBehaviour
{
    public GameObject[] ranks;

    public void NewRanking(int rank)
    {
        for (int i = 0; i < ranks.Length; i++)
        {
            if (i == rank) ranks[i].gameObject.SetActive(true);

            else ranks[i].gameObject.SetActive(false);
        }
    }
}
