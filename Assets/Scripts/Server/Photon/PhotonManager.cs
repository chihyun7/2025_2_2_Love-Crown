using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;
using System.Text;

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
    private static PhotonManager Instance;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
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
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Start()
    {
        ConnectToPhoton();
    }


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
        Debug.Log($"서버 연결이 끈김{cause}");
        ConnectToPhoton();
    }


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
        statusText.text = $"{newPlayer.NickName}님이 입장했습니다.";

        PlayerText();

        if (PhotonNetwork.IsMasterClient && PhotonNetwork.CurrentRoom.PlayerCount == PhotonNetwork.CurrentRoom.MaxPlayers)
        {
            if (gamestartButton != null) gamestartButton.gameObject.SetActive(true);
        }
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        Debug.Log($"서버 연결이 {otherPlayer.NickName}님이 퇴장했습니다.");
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
        if (PhotonNetwork.IsMasterClient)
        {
            PhotonNetwork.LoadLevel("GameScene");
        }
    }


    public override void OnLeftRoom()
    {
        if (Instance == this) Instance = null;
        Destroy(gameObject);
        Debug.Log("[ServerMasterClient] OnLeftRoom: 싱글톤 정리 완료. 로비 씬으로 이동합니다.");
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != "GameScene") return;

        if (!gameObject.activeInHierarchy)
        {
            Debug.LogWarning("PhotonManager 오브젝트가 비활성화 상태여서 코루틴을 시작할 수 없습니다.");
            return;
        }

        StartCoroutine(SpawnPlayerAfterSceneLoaded());
    }

    private IEnumerator SpawnPlayerAfterSceneLoaded()
    {
        yield return null;

        float startTime = Time.time;

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

}
