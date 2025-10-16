using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;
using System.Text;
using System.Linq; // PlayerText 로직을 더 깔끔하게 하기 위해 필요할 수 있습니다.

public class PhotonManager : MonoBehaviourPunCallbacks
{
    [Header("Photon 설정")]
    public string gameVersion = "1.0";

    [Header("UI 연결")]
    public InputField playerInput;
    public InputField createNameInput;
    public Text statusText;

    public GameObject gamestartButton;
    private StringBuilder sb = new StringBuilder();
    private static PhotonManager instance;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            // 씬 전환 시에도 PhotonManager를 유지합니다.
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }

        PhotonNetwork.AutomaticallySyncScene = true;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        // 오브젝트가 파괴될 때 이벤트 등록을 해제하여 메모리 누수를 방지
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Start()
    {
        ConnectToPhoton();
    }

    #region Photon 연결

    public void ConnectToPhoton()
    {
        if (!PhotonNetwork.IsConnected)
        {
            PhotonNetwork.GameVersion = gameVersion;
            PhotonNetwork.ConnectUsingSettings();
            statusText.text = "\n서버에 연결 중...";
        }
        else
        {
            PhotonNetwork.JoinLobby();
            statusText.text = "\n로비로 진입...";
        }
    }

    public override void OnConnectedToMaster()
    {
        statusText.text = "마스터 서버 연결 성공!";
        PhotonNetwork.JoinLobby();
    }


    public override void OnDisconnected(DisconnectCause cause)
    {
        statusText.text = $"서버 연결 끊김: {cause}";
        // 연결이 끊기면 재접속
        ConnectToPhoton();
    }

    #endregion


    #region 방 생성 / 입장

    public void CreateRoom()
    {
        playerName();

        if (string.IsNullOrEmpty(createNameInput.text))
        {
            statusText.text = "방 이름을 입력하세요.";
            return;
        }

        RoomOptions options = new RoomOptions
        {
            MaxPlayers = 2,
            IsOpen = true,
            IsVisible = true
        };

        PhotonNetwork.CreateRoom(createNameInput.text, options);
        statusText.text = $"방 생성 시도: {createNameInput.text}";
        Debug.Log(PhotonNetwork.NickName);
    }

    public void playerName()
    {
        if (playerInput != null && !string.IsNullOrEmpty(playerInput.text))
        {
            PhotonNetwork.NickName = playerInput.text;
            statusText.text = $"닉네임 설정: {PhotonNetwork.NickName}";

        }
        else
        {
            PhotonNetwork.NickName = $"Player_{Random.Range(1000, 9999)}";
            statusText.text = $"닉네임 미입력, 자동 설정: {PhotonNetwork.NickName}";
        }
    }

    public void JoinRoom()
    {
        playerName();

        if (string.IsNullOrEmpty(createNameInput.text))
        {
            statusText.text = "방 이름을 입력하세요.";
            return;
        }

        PhotonNetwork.JoinRoom(createNameInput.text);
        statusText.text = $"방 입장 시도: {createNameInput.text}";
    }

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        statusText.text = "방 입장 실패: " + message;
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        statusText.text = "방 생성 실패: " + message;
    }

    public override void OnJoinedRoom()
    {

        statusText.text = "방 입장 성공! 플레이어 목록 업데이트 중...";


        PlayerText();

        if (PhotonNetwork.IsMasterClient)
        {
            if (gamestartButton != null) gamestartButton.gameObject.SetActive(true);
        }
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        // 1. 상태 메시지 업데이트
        statusText.text = $"{newPlayer.NickName}님이 입장했습니다.";

        // 2. 플레이어 목록 업데이트 (모두에게 동기화)
        PlayerText();

        // 마스터 클라이언트라면 인원수 확인 후 게임 시작 버튼 활성화
        if (PhotonNetwork.IsMasterClient && PhotonNetwork.CurrentRoom.PlayerCount == PhotonNetwork.CurrentRoom.MaxPlayers)
        {
            if (gamestartButton != null) gamestartButton.gameObject.SetActive(true);
        }
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        // 플레이어가 나가도 목록을 업데이트합니다.
        statusText.text = $"{otherPlayer.NickName}님이 퇴장했습니다.";
        PlayerText();

        if (!PhotonNetwork.IsMasterClient && gamestartButton != null)
        {
            if (PhotonNetwork.LocalPlayer.IsMasterClient)
            {
                gamestartButton.gameObject.SetActive(true);
            }
        }
    }


    void PlayerText()
    {
        if (statusText == null || !PhotonNetwork.InRoom) return;

        sb.Clear();

        sb.AppendLine($"--- 현제 플레이어: {PhotonNetwork.CurrentRoom.PlayerCount}/{PhotonNetwork.CurrentRoom.MaxPlayers} ---");

        int playerIndex = 1;
        foreach (var playerEntry in PhotonNetwork.CurrentRoom.Players)
        {
            string playerName = playerEntry.Value.NickName;

            string localIndicator = playerEntry.Value.IsLocal ? " (나)" : "";
            string masterIndicator = playerEntry.Value.IsMasterClient ? " (방장)" : "";

            sb.AppendLine($"플레이어 {playerIndex++}: {playerName}{localIndicator}{masterIndicator}");
        }

        statusText.text = sb.ToString();
    }

    public void GameStartButton()
    {
        // if (statusText != null) statusText = null;
        // if (createNameInput != null) createNameInput = null;

        if (PhotonNetwork.IsMasterClient)
        {
            PhotonNetwork.LoadLevel("GameScene");
        }
    }

    #endregion

    #region 플레이어 스폰

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != "GameScene") return;
        StartCoroutine(SpawnPlayerAfterSceneLoaded());

    }

    private IEnumerator SpawnPlayerAfterSceneLoaded()
    {
        // ... (PlayerSpawnManager 관련 로직은 유지) ...
        yield return null;

        float startTime = Time.time;

        // 2. PlayerSpawnManager가 준비될 때까지 최대 5초간 대기하며 찾기
        while (PlayerSpawnManager.instance == null && Time.time < startTime + 5f)
        {
            Debug.LogWarning("PlayerSpawnManager 대기 중...");
            PlayerSpawnManager foundManager = FindObjectOfType<PlayerSpawnManager>();

            if (foundManager != null)
            {
                PlayerSpawnManager.instance = foundManager;
                break;
            }
            yield return null;
        }

        // 3. 최종 검증 (루프 탈출 후)
        if (PlayerSpawnManager.instance == null)
        {
            Debug.LogError("PlayerSpawnManager를 5초 내에 찾을 수 없어 스폰 실패!");
            yield break;
        }

        if (PlayerSpawnManager.instance.spawnPoints == null || PlayerSpawnManager.instance.spawnPoints.Length == 0)
        {
            Debug.LogError("PlayerSpawnManager에 스폰 포인트가 설정되지 않았습니다.");
            yield break;
        }

        // 4. 플레이어 스폰
        int playerIndex = PhotonNetwork.LocalPlayer.ActorNumber - 1;
        Vector3 spawnPos = PlayerSpawnManager.instance.GetSpawnPosition(playerIndex);

        GameObject player = PhotonNetwork.Instantiate("Player", spawnPos, Quaternion.identity);

        if (player == null)
        {
            Debug.LogError("PlayerPrefab 스폰 실패! Resources 폴더와 이름을 확인하세요.");
        }
        else
        {
            Debug.Log($"네트워크 플레이어 오브젝트 생성 완료! 스폰 위치: {spawnPos}");
        }
    }


    #endregion
}