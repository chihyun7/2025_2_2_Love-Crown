using UnityEngine;
using TMPro;
using Photon.Pun;
using ExitGames.Client.Photon;
using UnityEngine.SceneManagement;

/// <summary>
/// 15분 제한 타이머를 전 클라이언트에서 동기화하고 HUD에 표시.
/// 마스터가 종료 시각을 Room CustomProperties에 저장, 클라들은 이를 참조.
/// </summary>
public class MiniGameTimer : MonoBehaviourPunCallbacks
{
    [Header("UI 연결")]
    public TextMeshProUGUI timerText;   // 남은 시간 표시용 TMP 텍스트
    public GameObject resultPanel;      // 결과 패널
    public TextMeshProUGUI resultText;  // 결과 텍스트

    [Header("설정")]
    public double matchDuration = 15 * 60.0; // 15분 (초 단위)

    private const string END_TIME_KEY = "MiniGameEndTime";
    private double endTime;  // PhotonNetwork.Time 기준 절대 시간
    private bool gameEnded = false;

    void Start()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            // 마스터가 종료 시각을 처음 설정
            double now = PhotonNetwork.Time;
            endTime = now + matchDuration;

            Hashtable roomProps = new Hashtable();
            roomProps[END_TIME_KEY] = endTime;
            PhotonNetwork.CurrentRoom.SetCustomProperties(roomProps);
        }
        else
        {
            // 이미 설정된 종료 시각 가져오기
            if (PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(END_TIME_KEY, out object value))
            {
                endTime = (double)value;
            }
        }
    }

    void Update()
    {
        if (gameEnded || endTime <= 0) return;

        double remaining = endTime - PhotonNetwork.Time;
        if (remaining < 0) remaining = 0;

        int totalSeconds = Mathf.CeilToInt((float)remaining);
        int minutes = totalSeconds / 60;
        int seconds = totalSeconds % 60;

        if (timerText != null)
        {
            timerText.text = $"{minutes:00}:{seconds:00}";
        }

        if (remaining <= 0 && !gameEnded)
        {
            EndGame();
        }
    }

    void EndGame()
    {
        gameEnded = true;

        if (PhotonNetwork.IsMasterClient)
        {
            // 마스터만 승자 판정 호출
            ServerMasterClient.Instance?.AnnounceWinner();
        }
    }

    // 마스터가 Rpc로 호출하여 모든 클라에 결과를 표시
    [PunRPC]
    public void RpcShowResult(string message)
    {
        if (resultPanel != null) resultPanel.SetActive(true);
        if (resultText != null) resultText.text = message;
    }


   // 로비로 돌아가기 버튼 
    public void OnClickReturnToLobby()
    {
        Debug.Log("[MiniGameTimer] RpcShowResult() 호출됨 - 결과 패널 활성화 시도");
        Debug.Log("[MiniGameTimer] 로비로 돌아가기 버튼 클릭됨");
        StartCoroutine(ReturnToLobbyRoutine());
    }

    private System.Collections.IEnumerator ReturnToLobbyRoutine()
    {
        // 잠시 대기 (UI 연출용)
        yield return new WaitForSeconds(0.5f);

        if (PhotonNetwork.InRoom)
        {
            PhotonNetwork.LeaveRoom();
            Debug.Log("[MiniGameTimer] Photon 방 나가기 요청");
        }
        else
        {
            // 혹시 이미 나가졌다면 바로 로비 씬으로 이동
            SceneManager.LoadScene("LobbyScene");
        }
    }

    // Photon 콜백: 방을 성공적으로 나갔을 때 자동 호출
    public override void OnLeftRoom()
    {
        Debug.Log("[MiniGameTimer] 방 나가기 완료 → 로비로 이동");
        SceneManager.LoadScene("LobbyScene");
    }

}
