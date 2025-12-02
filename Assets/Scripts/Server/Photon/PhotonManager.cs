using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class PhotonManager : MonoBehaviourPunCallbacks
{
    public string gameVersion = "1.0";

    public InputField playerInput;
    public InputField createNameInput;
    public GameObject LobbyPanel;
    public GameObject GameInUI;
    public TextMeshProUGUI roomNameText;
    public GameObject gamestartButton;

    [Header("플레이어 카드01")]
    public GameObject playerCard01;
    public TextMeshProUGUI playerName01;
    public TextMeshProUGUI playerRoomName01;

    [Header("플레이어 카드02")]
    public GameObject playerCard02;
    public TextMeshProUGUI playerName02;
    public TextMeshProUGUI playerRoomName02;

    private static PhotonManager Instance;
    public bool isMaster;

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
        StartUI();
    }

    void StartUI()
    {
        if (LobbyPanel != null) LobbyPanel.SetActive(false);
        if (playerCard01 != null) playerCard01.SetActive(false);
        if (playerCard02 != null) playerCard02.SetActive(false);
        if (gamestartButton != null) gamestartButton.SetActive(false);
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
        ConnectToPhoton();
    }

    public void SetPlayerName()
    {
        if (playerInput != null && !string.IsNullOrEmpty(playerInput.text))
        {
            PhotonNetwork.NickName = playerInput.text;
        }
        else if (string.IsNullOrEmpty(PhotonNetwork.NickName))
        {
            PhotonNetwork.NickName = $"Player_{Random.Range(1000, 9999)}";
        }
    }

    public void CreateRoom()
    {
        SetPlayerName();

        if (string.IsNullOrEmpty(createNameInput.text))
        {
            return;
        }

        RoomOptions options = new RoomOptions
        {
            MaxPlayers = 2,
            IsOpen = true,
            IsVisible = true
        };

        PhotonNetwork.CreateRoom(createNameInput.text, options);
    }

    public void JoinRoom()
    {
        SetPlayerName();

        if (string.IsNullOrEmpty(createNameInput.text))
        {
            return;
        }

        PhotonNetwork.JoinRoom(createNameInput.text);
    }

    public override void OnJoinedRoom()
    {
        if (LobbyPanel != null) LobbyPanel.gameObject.SetActive(true);
        if (GameInUI != null) GameInUI.gameObject.SetActive(false);

        UpdatePlayerListUI();
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        UpdatePlayerListUI();
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        UpdatePlayerListUI();
    }

    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        UpdatePlayerListUI();
    }

    private void UpdatePlayerListUI()
    {
        Player[] players = PhotonNetwork.PlayerList;


        if (PhotonNetwork.InRoom && PhotonNetwork.CurrentRoom != null)
        {
            roomNameText.text = $"현재 방 : {PhotonNetwork.CurrentRoom.Name}";
        }
        else
        {
            roomNameText.text = "방 정보 없음";
        }

        if (players.Length >= 1)
        {
            playerCard01.SetActive(true);
            playerName01.text = players[0].NickName + (players[0].IsMasterClient ? " (방장)" : "");

            if (playerRoomName01 != null && PhotonNetwork.CurrentRoom != null)
            {
                playerRoomName01.text = PhotonNetwork.CurrentRoom.Name;
            }
        }
        else
        {
            playerCard01.SetActive(false);
        }

        if (players.Length >= 2)
        {
            playerCard02.SetActive(true);
            playerName02.text = players[1].NickName + (players[1].IsMasterClient ? " (방장)" : "");

            if (playerRoomName02 != null && PhotonNetwork.CurrentRoom != null)
            {
                playerRoomName02.text = PhotonNetwork.CurrentRoom.Name;
            }
        }
        else
        {
            playerCard02.SetActive(false);
        }

        // 게임 시작 버튼 업데이트
        if (gamestartButton != null)
        {
            bool maxPlayersReached = PhotonNetwork.CurrentRoom != null && PhotonNetwork.CurrentRoom.PlayerCount == PhotonNetwork.CurrentRoom.MaxPlayers;

            if (PhotonNetwork.IsMasterClient && maxPlayersReached)
            {
                gamestartButton.SetActive(true);
            }
            else
            {
                gamestartButton.SetActive(false);
            }
        }
    }

    public void OpenCreateRoomUIButton()
    {
        if (GameInUI != null)
        {
            GameInUI.gameObject.SetActive(!GameInUI.gameObject.activeSelf);
        }
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
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != "GameScene") return;

        if (!gameObject.activeInHierarchy)
        {
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
            yield break;
        }

        if (PlayerSpawnManager.instance.spawnPoints == null || PlayerSpawnManager.instance.spawnPoints.Length < 2)
        {
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
            yield break;
        }

        int playerIndex = actorNumber - 1;
        Vector3 spawnPos = PlayerSpawnManager.instance.GetSpawnPosition(playerIndex);

        GameObject player = PhotonNetwork.Instantiate(prefabName, spawnPos, Quaternion.identity);
    }
}