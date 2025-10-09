using UnityEngine;

public class GameExitManager : MonoBehaviour
{
    // 게임 종료 버튼에 연결할 함수
    public void QuitGame()
    {
        // 🚨 유의사항:
        // 1. 이 코드는 에디터에서는 작동하지 않고 'Play' 모드가 멈춥니다.
        // 2. 실제 빌드된 게임(exe 파일 등)에서만 완전히 종료됩니다.
        
        #if UNITY_EDITOR
            // 유니티 에디터에서 실행 중일 때
            Debug.Log("게임을 종료합니다. (에디터에서는 'Play' 모드만 멈춥니다)");
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            // 빌드된 게임에서 실행 중일 때
            Application.Quit();
        #endif
    }
}