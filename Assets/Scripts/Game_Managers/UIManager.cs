using NUnit.Framework;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;
    [Header("Sound UI")]
    public GameObject button;
    public Sprite turnOnSound;
    public Sprite turnOffSound;

    [Header("Coin")]
    public TextMeshProUGUI textCoins;

    [Header("Setting Panels")]
    public GameObject LevelGO;
    public GameObject restartGO;

    private bool isSoundOn = true; 

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
        if (button != null)
        {
            Button btn = button.GetComponent<Button>();
            if (btn != null)
                btn.onClick.AddListener(ToggleSound);
        }

        UpdateSoundIcon();
        SafeUpdateCoinsText();
        if (restartGO != null) restartGO.SetActive(false);
    }

    private void Update()
    {
        textCoinsSetting();
    }



    
    public void textCoinsSetting()
    {
        SafeUpdateCoinsText();
    }

    private void SafeUpdateCoinsText()
    {
        if (textCoins == null) return;
        var gm = GameManager.instance;
        textCoins.text = gm != null ? gm.Coins.ToString() : "0";
    }


    public void ToggleSound()
    {
        isSoundOn = !isSoundOn; 
        UpdateSoundIcon();
        if (isSoundOn)
        {
            AudioManager.instance.turnOnAudio();
        }
        else
        {
            AudioManager.instance.turnOffAudio();
        }

        AudioListener.volume = isSoundOn ? 1f : 0f;
    }

    private void UpdateSoundIcon()
    {
        if (button == null) return;

        var image = button.GetComponent<Image>();
        if (image == null) return;

        image.sprite = isSoundOn ? turnOnSound : turnOffSound;
    }

    public void openRestart()
    {
        if (restartGO != null) restartGO.SetActive(true);
        LevelGO?.SetActive(true);

        var gm = GameManager.instance;
        if (gm != null)
        {
            gm.restartLevel();
        }
        else
        {
            Debug.LogWarning("GameManager.instance is null; cannot restart level.");
        }
    }

   public void nextLevl()
    {
        LevelManager.Instance.NextLevel(); 
    }
    
}
