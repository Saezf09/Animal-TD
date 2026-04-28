using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Sources")]
    [SerializeField] private AudioSource bgmSource;
    [SerializeField] private AudioSource sfxSource;

    [Header("Audio Clips")]
    public AudioClip backgroundMusic;
    public AudioClip enemyDeathSound;
    public AudioClip purchaseSound;

    private bool isMuted = false;
    private float masterVolume = 1f;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }

        InitializeBGM();
    }

    private void InitializeBGM()
    {
        if (bgmSource != null && backgroundMusic != null)
        {
            bgmSource.clip = backgroundMusic;
            bgmSource.loop = true;
            // Keep BGM slightly quieter than SFX by default
            bgmSource.volume = 0.3f * masterVolume;
            bgmSource.Play();
        }
    }
    public void PlayPurchaseSound()
    {
        if (sfxSource != null && purchaseSound != null)
        {
            sfxSource.PlayOneShot(purchaseSound);
        }
    }

    //  UPDATED: Now requires the specific clip from the tower 
    public void PlayAttackSound(AudioClip clip)
    {
        if (sfxSource != null && clip != null)
        {
            sfxSource.PlayOneShot(clip);
        }
    }

    public void PlayDeathSound()
    {
        if (sfxSource != null && enemyDeathSound != null)
        {
            sfxSource.PlayOneShot(enemyDeathSound);
        }
    }

    //  NEW: Audio Control Methods for the UI 
    public void ToggleMute()
    {
        isMuted = !isMuted;
        if (bgmSource != null) bgmSource.mute = isMuted;
        if (sfxSource != null) sfxSource.mute = isMuted;
    }

    public void SetMasterVolume(float volume)
    {
        masterVolume = Mathf.Clamp01(volume); // Ensure volume stays between 0 and 1

        if (bgmSource != null) bgmSource.volume = 0.3f * masterVolume;
        if (sfxSource != null) sfxSource.volume = 1.0f * masterVolume;
    }
}