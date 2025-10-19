using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager instance;
    [Header("Audio Source")]
    public AudioSource source;
    [Header("BackGround")]
    public AudioClip backGroundSource;
    [Header("SFX")]
    public AudioClip clickBlock;
    public AudioClip destroyBlock;

    private void Awake()
    {
        if(instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        playBackGround();
    }

    public void playBackGround()
    {
        source.clip = backGroundSource;
        source.loop = true;
        source.volume = 0.8f;
        source.Play();
    }

    private void playSFX(AudioClip clip)
    {
        if (clip == null) return;
        AudioSource.PlayClipAtPoint(clip, Camera.main.transform.transform.position, 1f);
    }
    public void playClickBlock() => playSFX(clickBlock);
    public void playDestroyBlock() => playSFX(destroyBlock);
}
