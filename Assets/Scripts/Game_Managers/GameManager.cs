using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using TMPro;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;
    public int Coins;
    public int currentScene = 0;


    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            currentScene = SceneManager.GetActiveScene().buildIndex;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        UpdateCoinUI();
    }

    public void restartLevel()
    {
        SceneManager.LoadScene(currentScene);
    }

    public void nextLevel()
    {
        StartCoroutine(IncreaseCoinsSmoothly(Coins + 15, 1f, () =>
        {
            currentScene++;
            SceneManager.LoadScene(currentScene);
        }));
    }

    public void exitGame()
    {
        Application.Quit();
    }

    private void UpdateCoinUI()
    {

        if (UIManager.Instance.textCoins != null)
            UIManager.Instance.textCoins.text = Coins.ToString();

    }
    private IEnumerator IncreaseCoinsSmoothly(int targetCoins, float duration, System.Action onComplete = null)
    {
        int startCoins = Coins;
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            Coins = Mathf.RoundToInt(Mathf.Lerp(startCoins, targetCoins, timer / duration));
            UpdateCoinUI();
            yield return null;
        }

        Coins = targetCoins;
        UpdateCoinUI();
        AudioManager.instance.playCoinIncrease();
        onComplete?.Invoke();
    }
}
