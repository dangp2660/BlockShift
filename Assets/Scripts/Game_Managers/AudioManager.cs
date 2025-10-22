using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager instance;
    [Header("Audio Source")]
    public AudioSource source;
    [Header("SFX")]
    public AudioClip destroyBlock;
    public AudioClip coinClip;

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

    public void turnOnAudio()
    {
        source.enabled = true;
    }
    public void turnOffAudio()
    {
        source.enabled = false;
    }

    private void playSFX(AudioClip clip)
    {
        if (clip == null) return;
        AudioSource.PlayClipAtPoint(clip, Camera.main.transform.transform.position, 1f);
    }//playSFX
    public void playCoinIncrease() => playSFX(coinClip);
    public void playDestroyBlock() => playSFX(destroyBlock);
}
