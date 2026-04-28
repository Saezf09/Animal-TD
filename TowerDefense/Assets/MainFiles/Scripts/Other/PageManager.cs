using UnityEngine;
using UnityEngine.SceneManagement;

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
    public void BackToMenu()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}