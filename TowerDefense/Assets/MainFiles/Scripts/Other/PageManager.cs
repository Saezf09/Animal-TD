using UnityEngine;

public class PageManager : MonoBehaviour
{
    [SerializeField] private GameObject startMenuPanel;
    [SerializeField] private GameObject controlsPanel;

    public void OpenControls()
    {
        startMenuPanel.SetActive(false);
        controlsPanel.SetActive(true);
    }

    public void CloseControls()
    {
        controlsPanel.SetActive(false);
        startMenuPanel.SetActive(true);
    }
}