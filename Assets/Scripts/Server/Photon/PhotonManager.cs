using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;
using System.Text;
using TMPro;

public class PhotonManager : MonoBehaviourPunCallbacks
{
    public string gameVersion = "1.0";

    public InputField playerInput;
    public InputField createNameInput;
    public Text statusText;
    public GameObject LobbyPanel;
    public GameObject GameInUI;
    public TextMeshProUGUI roomNameText;
    public bool isMaster;
    public GameObject gamestartButton;
    private StringBuilder sb = new StringBuilder();
    private static PhotonManager Instance;
    private bool isCreateRoomUI;
    private bool isLobbyUIPanel;

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
        }
        else
        {
            PhotonNetwork.JoinLobby();
        }
    }

    public override void OnConnectedToMaster()
    {
        PhotonNetwork.JoinLobby();
        isMaster = true;
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
            Debug.Log("방 이름을 입력하세요.");
            return;
        }

        RoomOptions options = new RoomOptions
        {
            MaxPlayers = 2,
            IsOpen = true,
            IsVisible = true
        };

        PhotonNetwork.CreateRoom(createNameInput.text, options);
        Debug.Log($"방 생성 시도: {createNameInput.text}");
        Debug.Log(PhotonNetwork.NickName);
    }

    public void playerName()
    {
        if (playerInput != null && !string.IsNullOrEmpty(playerInput.text))
        {
            PhotonNetwork.NickName = playerInput.text;
            Debug.Log($"닉네임 설정: {PhotonNetwork.NickName}");

        }
        else
        {
            PhotonNetwork.NickName = $"Player_{Random.Range(1000, 9999)}";
            Debug.Log($"닉네임 미입력, 자동 설정: {PhotonNetwork.NickName}");
        }
    }

    public void JoinRoom()
    {
        playerName();

        if (string.IsNullOrEmpty(createNameInput.text))
        {
            Debug.Log("방 이름을 입력하세요.");
            return;
        }

        PhotonNetwork.JoinRoom(createNameInput.text);
        Debug.Log($"방 입장 시도: {createNameInput.text}");
    }

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        Debug.Log("방 입장 실패: " + message);
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        Debug.Log("방 생성 실패: " + message);
    }

    public override void OnJoinedRoom()
    {
        Debug.Log("방 입장 성공! 플레이어 목록 업데이트 중...");
        LobbyPanel.gameObject.SetActive(true);

        PlayerText();
        if (!isLobbyUIPanel)
        {
            LobbyPanel.gameObject.SetActive(true);
            roomNameText.text = PhotonNetwork.CurrentRoom.Name;
            isLobbyUIPanel = true;
            isCreateRoomUI = false;
        }

        if (PhotonNetwork.IsMasterClient && PhotonNetwork.CurrentRoom.PlayerCount == 2)
        {
            if (gamestartButton != null) gamestartButton.gameObject.SetActive(true);
        }
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        Debug.Log($"{newPlayer.NickName}님이 입장했습니다.");
        PlayerText();

        if (PhotonNetwork.IsMasterClient && PhotonNetwork.CurrentRoom.PlayerCount == 2)
        {
            if (gamestartButton != null) gamestartButton.gameObject.SetActive(true);
        }
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        Debug.Log($"서버 연결이 {otherPlayer.NickName}님이 퇴장했습니다.");
        PlayerText();
    }

    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        if (newMasterClient.IsLocal && PhotonNetwork.CurrentRoom.PlayerCount == 2)
        {
            if (gamestartButton != null)
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

    public void OpenCreateRoomUIButton()
    {
        if (!isCreateRoomUI)
        {
            GameInUI.gameObject.SetActive(true);
            isCreateRoomUI = true;
            isLobbyUIPanel = false;
        }
        else
        {
            GameInUI.gameObject.SetActive(false);

        }
    }

    public void GameStartButton()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            PhotonNetwork.LoadLevel("GameScene");
            isLobbyUIPanel = false;
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

       
        int actorNumber = PhotonNetwork.LocalPlayer.ActorNumber;
        string prefabName = "";

        if (actorNumber == 1)
        {
            prefabName = "Player1Prefab";
        }
        else if (actorNumber == 2)
        {
            prefabName = "Player2Prefab"; 
        }
        else
        {
            Debug.LogError($"ActorNumber {actorNumber}에 해당하는 프리팹이 정의되지 않았습니다. 현재는 1번과 2번만 지원합니다.");
            yield break;
        }

        int playerIndex = actorNumber - 1;
        Vector3 spawnPos = PlayerSpawnManager.instance.GetSpawnPosition(playerIndex);

        GameObject player = PhotonNetwork.Instantiate(prefabName, spawnPos, Quaternion.identity);

        if (player == null)
        {
            Debug.LogError($"{prefabName} 스폰 실패! Resources 폴더와 이름을 확인하세요.");
        }
        else
        {
            Debug.Log($"네트워크 플레이어 오브젝트 생성 완료! 프리팹: {prefabName}, 스폰 위치: {spawnPos}");
        }
    }
}