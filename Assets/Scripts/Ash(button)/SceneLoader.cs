using UnityEngine;
using UnityEngine.SceneManagement; // 씬 관리를 위해 필요합니다!

public class SceneLoader : MonoBehaviour
{
    // 💡 참고: 씬 이름은 Unity의 Build Settings에 등록된 이름과 정확히 일치해야 합니다.
    
    [Header("전환할 씬 이름 설정")]
    [SerializeField] private string lobbySceneName = "LobbyScene";

    // 1. 게임 시작 버튼에 연결: 로비 씬으로 전환
    public void LoadLobbyScene()
    {
        Debug.Log("게임 시작! 로비 씬으로 이동합니다: " + lobbySceneName);
        SceneManager.LoadScene(lobbySceneName);
    }

 
}