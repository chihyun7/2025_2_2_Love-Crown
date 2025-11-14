using UnityEngine;
using TMPro;
using Photon.Pun;
using ExitGames.Client.Photon; // 여기서 Hashtable을 가져옵니다.
using UnityEngine.SceneManagement;
using System.Collections; // IEnumerator 사용을 위해 추가

/// <summary>
/// 15분 제한 타이머를 전 클라이언트에서 동기화하고 HUD에 표시합니다.
/// 마스터가 종료 시각을 Room CustomProperties에 저장하며, 클라이언트들은 이를 참조합니다.
/// </summary>
public class MiniGameTimer : MonoBehaviourPunCallbacks
{
    [Header("UI 연결")]
    public TextMeshProUGUI timerText;    // 남은 시간 표시용 TMP 텍스트
    public GameObject resultPanel;       // 결과 패널 (게임 종료 시 활성화)
    public TextMeshProUGUI resultText;   // 결과 텍스트

    [Header("설정")]
    public double matchDuration = 15 * 60.0; // 15분 (초 단위)

    private const string END_TIME_KEY = "MiniGameEndTime";
    private double endTime;     // PhotonNetwork.Time 기준 절대 시간
    private bool gameEnded = false;

    void Start()
    {
        // 1. 결과 패널 초기 상태 설정 (필요하다면)
        if (resultPanel != null)
        {
            resultPanel.SetActive(false);
        }

        // --- 👇 [수정] 포톤 방에 입장한 상태인지 먼저 확인합니다. ---
        if (!PhotonNetwork.InRoom)
        {
            Debug.LogWarning("[MiniGameTimer] 포톤 방에 입장해 있지 않아 타이머를 시작할 수 없습니다. (에디터 테스트일 수 있음)");

            // timerText가 연결 안 됐을 수도 있으니 null 체크
            if (timerText != null)
            {
                timerText.text = "xx:xx"; // 테스트 중임을 알림
            }
            return; // 방에 없으면 Start 함수를 여기서 종료
        }
        // ---------------------------------------------------------

        // 2. 마스터 클라이언트만 종료 시각 설정 (이제 InRoom이 보장됨)
        if (PhotonNetwork.IsMasterClient)
        {
            double now = PhotonNetwork.Time;
            endTime = now + matchDuration;

            ExitGames.Client.Photon.Hashtable roomProps = new ExitGames.Client.Photon.Hashtable();
            roomProps[END_TIME_KEY] = endTime;
            PhotonNetwork.CurrentRoom.SetCustomProperties(roomProps);
            Debug.Log($"[MiniGameTimer] 마스터 클라이언트: 종료 시각 설정 완료 ({endTime:F2})");
        }
        else
        {
            // 일반 클라이언트는 룸 속성에서 종료 시각 가져오기
            if (PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(END_TIME_KEY, out object value))
            {
                endTime = (double)value;
                Debug.Log($"[MiniGameTimer] 일반 클라이언트: 종료 시각 수신 완료 ({endTime:F2})");
            }
            else
            {
                Debug.LogWarning("[MiniGameTimer] 종료 시각 속성을 찾을 수 없습니다. 마스터 설정을 기다립니다.");
            }
        }
    }

    void Update()
    {
        if (gameEnded || endTime <= 0) return;

        // 모든 클라이언트가 동일한 PhotonNetwork.Time을 기준으로 남은 시간 계산
        double remaining = endTime - PhotonNetwork.Time;
        if (remaining < 0) remaining = 0;

        int totalSeconds = Mathf.CeilToInt((float)remaining);
        int minutes = totalSeconds / 60;
        int seconds = totalSeconds % 60;

        // UI 업데이트
        if (timerText != null)
        {
            timerText.text = $"{minutes:00}:{seconds:00}";
        }

        // 게임 종료 조건 확인
        if (remaining <= 0 && !gameEnded)
        {
            EndGame();
        }
    }

    /// <summary>
    /// 타이머가 0이 되었을 때 게임을 종료하고 승자를 판정합니다.
    /// </summary>
    void EndGame()
    {
        gameEnded = true;

        Debug.Log("[MiniGameTimer] 타이머 종료. 게임 종료 처리 시작.");

        if (PhotonNetwork.IsMasterClient)
        {
            // 마스터 클라이언트만 승자 판정 및 결과 전파를 호출
            GameManager.Instance?.AnnounceWinner();
        }
    }

    /// <summary>
    /// 마스터 클라이언트의 RPC 호출로 모든 클라이언트에게 결과를 표시합니다.
    /// </summary>
    /// <param name="message">결과 메시지</param>
    [PunRPC]
    public void RpcShowResult(string message)
    {
        Debug.Log($"[MiniGameTimer] RpcShowResult() 호출됨 - 결과 표시: {message}");

        // 결과 UI 표시
        if (resultPanel != null) resultPanel.SetActive(true);
        if (resultText != null) resultText.text = message;
    }

    /// <summary>
    /// 로비로 돌아가기 버튼 클릭 이벤트 핸들러.
    /// </summary>
    public void OnClickReturnToLobby()
    {
        Debug.Log("[MiniGameTimer] 로비로 돌아가기 버튼 클릭됨");
        StartCoroutine(ReturnToLobbyRoutine());
    }

    private IEnumerator ReturnToLobbyRoutine()
    {
        // UI 연출을 위한 짧은 대기 시간
        yield return new WaitForSeconds(0.5f);

        if (PhotonNetwork.InRoom)
        {
            // 현재 방에 있다면 나가기 요청
            PhotonNetwork.LeaveRoom();
            Debug.Log("[MiniGameTimer] Photon 방 나가기 요청");
        }
        else
        {
            // 이미 방을 나갔거나 방이 없는 경우 바로 씬 이동
            Debug.Log("[MiniGameTimer] 이미 방을 나갔음. 로비 씬으로 바로 이동.");
            SceneManager.LoadScene("LobbyScene");
        }
    }

    /// <summary>
    /// Photon 콜백: 방을 성공적으로 나갔을 때 자동 호출됩니다.
    /// </summary>
    public override void OnLeftRoom()
    {
        // 방 나가기가 완료되면 로비 씬으로 이동합니다.
        Debug.Log("[MiniGameTimer] OnLeftRoom 콜백: 방 나가기 완료 → 로비로 이동");
        SceneManager.LoadScene("LobbyScene");
    }

    /// <summary>
    /// 룸 속성이 변경될 때 종료 시각을 업데이트합니다.
    /// </summary>
    public override void OnRoomPropertiesUpdate(ExitGames.Client.Photon.Hashtable propertiesThatChanged)
    {
        // ExitGames.Client.Photon.Hashtable로 타입 명시
        if (propertiesThatChanged.ContainsKey(END_TIME_KEY))
        {
            endTime = (double)propertiesThatChanged[END_TIME_KEY];
            Debug.Log($"[MiniGameTimer] 룸 속성 업데이트: 새로운 종료 시각 수신 ({endTime:F2})");
        }
    }
}
