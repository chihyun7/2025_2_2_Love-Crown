using Photon.Pun;
using Photon.Pun.Demo.Cockpit;
using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

//  MonoBehaviourPunCallbacks를 상속받아 포톤 서버에서 발생하는 이벤트를 감지
public class PhotonManager : MonoBehaviourPunCallbacks
{
    public string gameVersion = "1.0";                      //  버전 관리를 위한 변수(게임 버전이 같은 유저끼리 매칭된다.)
    public InputField createNameInput;
  //  public Text statusText;                                 

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
        PhotonNetwork.JoinLobby();
    }

    public void CreateRoom()
    {
        if (string.IsNullOrEmpty(createNameInput.text))
        {
            Debug.LogWarning("방 이름이 비어있습니다.");
            return;
        }

        RoomOptions roomOptions = new RoomOptions();
        roomOptions.MaxPlayers = 2;                                 
        roomOptions.IsVisible = true;                                  // 로비에서 다른 플레이어에게 이 방이 보이도록 설정
        roomOptions.IsOpen = true;                                    // 방에 다른 플레이어가 입장할 수 있도록 설정

        //  입력받은 방 이름으로 새로운 방을 생성
        PhotonNetwork.CreateRoom(createNameInput.text, roomOptions);

        Debug.Log($"방 생성 시도: {createNameInput.text}");
    }

    public override void OnCreateRoomFailed(short returnCode, string message)           // 방생성 실패시 호출 함수
    {
        Debug.LogError("방생성 실패 : " + message);
    }

    public override void OnJoinedRoom()
    {
        Debug.Log("방 입장 성공 현재 방 플레이서 수 " + PhotonNetwork.CurrentRoom.PlayerCount);

        // 게임 씬 이동 
        // PhotonNetwork.LoadLevel("이동할 게임 씬");
    }

}
