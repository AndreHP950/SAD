using System.Collections;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    public Animator UIAnimator;
    private static UIManager instance;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(this);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        UIAnimator = GetComponent<Animator>();
    }

    void Start()
    {
        
    }
}
