using Photon.Pun;
using Photon.Pun.Demo.Cockpit;
using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

//  MonoBehaviourPunCallbacks�� ��ӹ޾� ���� �������� �߻��ϴ� �̺�Ʈ�� ����
public class PhotonManager : MonoBehaviourPunCallbacks
{
    public string gameVersion = "1.0";                      //  ���� ������ ���� ����(���� ������ ���� �������� ��Ī�ȴ�.)
    public InputField createNameInput;
  //  public Text statusText;                                 

    private void Awake()
    {
        // ���� �ڵ����� ����ȭ�Ͽ� ��� Ŭ���̾�Ʈ�� ���� ���� �ε��ϵ��� ����
        // ���� ������ �÷��̾ ���� �� �̵��� �� �ȿ� �ִ� ��� �÷��̾���� �ڵ����� ���� �̵��ȴ�.
        PhotonNetwork.AutomaticallySyncScene = true;                                  
        Debug.Log("�� �ڵ� ����ȭ");
    }

    private void Start()
    {
        ConnectToPhoton();
    }

    public void ConnectToPhoton()
    {
        if (!PhotonNetwork.IsConnected)                 // ���� ������ ������� �ʾҴٸ�
        {
            // PhotonServerSettings ���Ͽ� ������ ������ ����Ͽ� ������ ����
            PhotonNetwork.ConnectUsingSettings();
            PhotonNetwork.GameVersion = gameVersion;

            Debug.Log("������ �����ϴ���...");
        }
        else                                          // �̹� ������ ����� �ִٸ�
        {
            PhotonNetwork.JoinLobby();
            Debug.Log("�̹� ����Ǿ� �־� �κ� ���� �Լ� ȣ��");
        }
    }


    public override void OnConnectedToMaster()     // ������ ������ ����Ǿ��� �� ȣ��Ǵ� �ݹ� �Լ�
    {
        Debug.Log("������ ���� ���� ����!");
        PhotonNetwork.JoinLobby();
    }

    public void CreateRoom()
    {
        if (string.IsNullOrEmpty(createNameInput.text))
        {
            Debug.LogWarning("�� �̸��� ����ֽ��ϴ�.");
            return;
        }

        RoomOptions roomOptions = new RoomOptions();
        roomOptions.MaxPlayers = 2;                                 
        roomOptions.IsVisible = true;                                  // �κ񿡼� �ٸ� �÷��̾�� �� ���� ���̵��� ����
        roomOptions.IsOpen = true;                                    // �濡 �ٸ� �÷��̾ ������ �� �ֵ��� ����

        //  �Է¹��� �� �̸����� ���ο� ���� ����
        PhotonNetwork.CreateRoom(createNameInput.text, roomOptions);

        Debug.Log($"�� ���� �õ�: {createNameInput.text}");
    }

    public override void OnCreateRoomFailed(short returnCode, string message)           // ����� ���н� ȣ�� �Լ�
    {
        Debug.LogError("����� ���� : " + message);
    }

    public override void OnJoinedRoom()
    {
        Debug.Log("�� ���� ���� ���� �� �÷��̼� �� " + PhotonNetwork.CurrentRoom.PlayerCount);

        // ���� �� �̵� 
        // PhotonNetwork.LoadLevel("�̵��� ���� ��");
    }

}
