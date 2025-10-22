using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance;
    [Header("UI Next Level")]
    public GameObject UINextLV;
    public GameObject LV;

    [Header("Request")]
    public List<RequestToNextLV> requests = new List<RequestToNextLV>();
    
    public bool endLV = false;
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            if (Instance != this)
            {
                Destroy(Instance.gameObject);
                Instance = this;
            }
        }
    }
    private void Start()
    {
        UpdateRequestUI();
    }


    public void AddProgress(int requestName, int amount)
    {
        RequestToNextLV req = requests.Find(r => r.ColorID == requestName);
        if (req != null)
        {
            req.currentAmount += amount;
            UpdateRequestUI();
        }
    }

    private void UpdateRequestUI()
    {
        bool allDone = true;

        foreach (var req in requests)
        {
            int remaining = Mathf.Max(req.requiredAmount - req.currentAmount, 0);
            if (req.textUI != null)
            {
                req.textUI.text = remaining.ToString();
            }

            if (!req.IsCompleted) allDone = false;
        }
        if (allDone)
        {
            if (LV != null) LV.SetActive(false);
            else Debug.LogWarning("LevelManager.LV is not assigned; cannot hide current level UI.");

            if (UINextLV != null) UINextLV.SetActive(true);
            else Debug.LogWarning("LevelManager.UINextLV is not assigned; cannot show next level UI.");
        }
    }

    public void NextLevel()
    {
        GameManager.instance.nextLevel();
    }
}
