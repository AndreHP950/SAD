using System.Collections;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    public Animator UIAnimator;

    private void Awake()
    {
        GameObject[] objs = GameObject.FindGameObjectsWithTag("UIManager");
        if (objs.Length > 1)
        {
            Destroy(gameObject);
        }
        else
        {
            DontDestroyOnLoad(gameObject);
        }

        UIAnimator = GetComponent<Animator>();
    }

    void Start()
    {
        
    }
}
