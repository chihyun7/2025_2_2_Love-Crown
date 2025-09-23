using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.UI;

//  MonoBehaviourPunCallbacks를 상속받아 포톤 서버에서 발생하는 이벤트를 감지
public class PhotonManager : MonoBehaviourPunCallbacks
{
    public string gameVersion = "1.0";                      //  버전 관리를 위한 변수(게임 버전이 같은 유저끼리 매칭된다.)
    public InputField createNameInput;
    public Text statusText;                                 

    private void Awake()
    {
        // 씬을 자동으로 동기화하여 모든 클라이언트가 같은 씬을 로드하도록 설정
        // 만약 마스터 플레이어가 게임 씬 이동시 방 안에 있는 모든 플레이어들이 자동으로 씬이 이동된다.
        PhotonNetwork.AutomaticallySyncScene = true;                                  
        Debug.Log("씬 자동 동기화");
    }

    private void Start()
    {
        ConnectToPhoton();
    }

    public void ConnectToPhoton()
    {
        if (!PhotonNetwork.IsConnected)                 // 만약 서버에 연결되지 않았다면
        {
            // PhotonServerSettings 파일에 설정된 정보를 사용하여 서버에 연결
            PhotonNetwork.ConnectUsingSettings();
            PhotonNetwork.GameVersion = gameVersion;

            Debug.Log("서버에 연결하는중...");
            statusText.text = "서버에 연결하는중...";
        }
        else                                          // 이미 서버에 연결되 있다면
        {
            PhotonNetwork.JoinLobby();
            Debug.Log("이미 연결되어 있어 로비 진입 함수 호출");
        }
    }


    public override void OnConnectedToMaster()     // 마스터 서버에 연결되었을 때 호출되는 콜백 함수
    {
        Debug.Log("마스터 서버 연결 성공!");
        statusText.text = "마스터 서버 연결 성공!";
        PhotonNetwork.JoinLobby();
    }

    public void CreateRoom()
    {
        if (string.IsNullOrEmpty(createNameInput.text))
        {
            Debug.LogWarning("방 이름이 비어있습니다.");
            statusText.text = "방 이름이 비어있습니다.";

            return;
        }

        RoomOptions roomOptions = new RoomOptions();
        roomOptions.MaxPlayers = 2;                                 
        roomOptions.IsVisible = true;                                  // 로비에서 다른 플레이어에게 이 방이 보이도록 설정
        roomOptions.IsOpen = true;                                    // 방에 다른 플레이어가 입장할 수 있도록 설정

        //  입력받은 방 이름으로 새로운 방을 생성
        PhotonNetwork.CreateRoom(createNameInput.text, roomOptions);
        Debug.Log($"방 생성 시도: {createNameInput.text}");
        statusText.text = $"방 생성 시도: {createNameInput.text}";

    }

    public void JoinRoom()
    {

        // 입력받은 방이름으로 입장
        PhotonNetwork.JoinRoom(createNameInput.text);
        Debug.Log($"방 입장 시도: {createNameInput.text}");
        statusText.text = $"방 입장 시도: {createNameInput.text}";
    }

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        statusText.text = "방 입장 실패: " + message;
        Debug.Log("방 입장 실패: " + message);
    }

    public override void OnCreateRoomFailed(short returnCode, string message)           // 방생성 실패시 호출 함수
    {
        Debug.LogError("방 생성 실패 : " + message);
        statusText.text = "방 생성 실패 : " + message;
    }

    public override void OnJoinedRoom()
    {
        Debug.Log("방 입장 성공 현재 방 플레이서 수 " + PhotonNetwork.CurrentRoom.PlayerCount);
        statusText.text = "방 입장 성공 현재 방 플레이서 수" + PhotonNetwork.CurrentRoom.PlayerCount;
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        Debug.Log("새로운 플레이어 입장! 현재 방 플레이어 수: " + PhotonNetwork.CurrentRoom.PlayerCount);
        statusText.text = "새로운 플레이어 입장! 현재 플레이어 수: " + PhotonNetwork.CurrentRoom.PlayerCount;

        if (PhotonNetwork.CurrentRoom.PlayerCount == 2)
        {
            Debug.Log("방에 2명의 플레이어가 모였습니다. 게임을 시작합니다.");
            statusText.text = "2명 플레이어 입장! 게임을 시작합니다.";
            // PhotonNetwork.LoadLevel("GameScene"); // 예를 들어 게임 씬으로 이동.
        }
    }
}
