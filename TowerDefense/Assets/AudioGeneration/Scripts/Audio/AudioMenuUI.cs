using UnityEngine;
using UnityEngine.UI;

public class AudioMenuUI : MonoBehaviour
{
    [Header("UI Controls")]
    [SerializeField] private Button muteButton;
    [SerializeField] private Slider volumeSlider;

    private void Start()
    {
        if (muteButton != null)
        {
            muteButton.onClick.AddListener(OnMuteClicked);
        }

        if (volumeSlider != null)
        {
            // Set slider to max by default
            volumeSlider.value = 1f;
            volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
        }
    }

    private void OnMuteClicked()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.ToggleMute();
        }
    }

    private void OnVolumeChanged(float newVolume)
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetMasterVolume(newVolume);
        }
    }
}