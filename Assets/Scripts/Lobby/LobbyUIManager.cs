using UnityEngine;

public class LobbyUIManager : MonoBehaviour
{
    [Header("UI 패널 연결")]
    public GameObject descriptionPanel;

    public void OpenDescription()
    {
        descriptionPanel.SetActive(true);
    }

    public void CloseDescription()
    {
        descriptionPanel.SetActive(false);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape) && descriptionPanel.activeSelf)
        {
            CloseDescription();
        }
    }
}