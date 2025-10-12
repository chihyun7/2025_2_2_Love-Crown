using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

public class PhotonManager : MonoBehaviourPunCallbacks
{
    [Header("Photon 설정")]
    public string gameVersion = "1.0";

    [Header("UI 연결")]
    public InputField createNameInput;
    public Text statusText;

    public GameObject gamestartButton;

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
            statusText.text = "서버에 연결 중...";
        }
        else
        {
            PhotonNetwork.JoinLobby();
            statusText.text = "로비로 진입...";
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
    }

    public void JoinRoom()
    {
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
        statusText.text = "방 입장 성공! GameScene 로딩...";
        statusText.text = $"현제 플레이어 : {PhotonNetwork.CurrentRoom.PlayerCount}명";
       

     
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        statusText.text = $"현제 플레이어 : {PhotonNetwork.CurrentRoom.PlayerCount}명";
        if (statusText != null) statusText = null;
        if (createNameInput != null) createNameInput = null;
        if(gamestartButton != null) gamestartButton.gameObject.SetActive(true);
    }

    public void GameStartButton()
    {
        PhotonNetwork.LoadLevel("GameScene");
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
        // 1. 씬 오브젝트의 Awake/Start가 완료될 때까지 기본적으로 한 프레임 대기
        yield return null;

        float startTime = Time.time;

        // 2. PlayerSpawnManager가 준비될 때까지 최대 5초간 대기하며 찾기
        while (PlayerSpawnManager.instance == null && Time.time < startTime + 5f)
        {
            Debug.LogWarning("PlayerSpawnManager 대기 중...");

            // 씬에서 직접 찾아서 인스턴스에 강제 할당 (가장 확실한 방법)
            PlayerSpawnManager foundManager = FindObjectOfType<PlayerSpawnManager>();

            if (foundManager != null)
            {
                // 찾은 오브젝트를 PlayerSpawnManager의 static instance에 할당하여 참조 확보
                PlayerSpawnManager.instance = foundManager;
                break; 
            }

            // 루프 내부에서는 오직 대기만 수행해야 합니다. (이전 오류 발생 로직 제거)
            yield return null; 
        }

        // 3. 최종 검증 (루프 탈출 후)
        if (PlayerSpawnManager.instance == null)
        {
            Debug.LogError("PlayerSpawnManager를 5초 내에 찾을 수 없어 스폰 실패! GameScene에 오브젝트가 있는지 확인하세요.");
            yield break;
        }

        // 스폰 포인트 준비 확인
        if (PlayerSpawnManager.instance.spawnPoints == null || PlayerSpawnManager.instance.spawnPoints.Length == 0)
        {
            Debug.LogError("PlayerSpawnManager에 스폰 포인트가 설정되지 않았습니다. 인스펙터 설정을 확인하세요.");
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