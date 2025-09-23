using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.UI;

//  MonoBehaviourPunCallbacks�� ��ӹ޾� ���� �������� �߻��ϴ� �̺�Ʈ�� ����
public class PhotonManager : MonoBehaviourPunCallbacks
{
    public string gameVersion = "1.0";                      //  ���� ������ ���� ����(���� ������ ���� �������� ��Ī�ȴ�.)
    public InputField createNameInput;
    public Text statusText;                                 

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
            statusText.text = "������ �����ϴ���...";
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
        statusText.text = "������ ���� ���� ����!";
        PhotonNetwork.JoinLobby();
    }

    public void CreateRoom()
    {
        if (string.IsNullOrEmpty(createNameInput.text))
        {
            Debug.LogWarning("�� �̸��� ����ֽ��ϴ�.");
            statusText.text = "�� �̸��� ����ֽ��ϴ�.";

            return;
        }

        RoomOptions roomOptions = new RoomOptions();
        roomOptions.MaxPlayers = 2;                                 
        roomOptions.IsVisible = true;                                  // �κ񿡼� �ٸ� �÷��̾�� �� ���� ���̵��� ����
        roomOptions.IsOpen = true;                                    // �濡 �ٸ� �÷��̾ ������ �� �ֵ��� ����

        //  �Է¹��� �� �̸����� ���ο� ���� ����
        PhotonNetwork.CreateRoom(createNameInput.text, roomOptions);
        Debug.Log($"�� ���� �õ�: {createNameInput.text}");
        statusText.text = $"�� ���� �õ�: {createNameInput.text}";

    }

    public void JoinRoom()
    {

        // �Է¹��� ���̸����� ����
        PhotonNetwork.JoinRoom(createNameInput.text);
        Debug.Log($"�� ���� �õ�: {createNameInput.text}");
        statusText.text = $"�� ���� �õ�: {createNameInput.text}";
    }

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        statusText.text = "�� ���� ����: " + message;
        Debug.Log("�� ���� ����: " + message);
    }

    public override void OnCreateRoomFailed(short returnCode, string message)           // ����� ���н� ȣ�� �Լ�
    {
        Debug.LogError("�� ���� ���� : " + message);
        statusText.text = "�� ���� ���� : " + message;
    }

    public override void OnJoinedRoom()
    {
        Debug.Log("�� ���� ���� ���� �� �÷��̼� �� " + PhotonNetwork.CurrentRoom.PlayerCount);
        statusText.text = "�� ���� ���� ���� �� �÷��̼� ��" + PhotonNetwork.CurrentRoom.PlayerCount;
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        Debug.Log("���ο� �÷��̾� ����! ���� �� �÷��̾� ��: " + PhotonNetwork.CurrentRoom.PlayerCount);
        statusText.text = "���ο� �÷��̾� ����! ���� �÷��̾� ��: " + PhotonNetwork.CurrentRoom.PlayerCount;

        if (PhotonNetwork.CurrentRoom.PlayerCount == 2)
        {
            Debug.Log("�濡 2���� �÷��̾ �𿴽��ϴ�. ������ �����մϴ�.");
            statusText.text = "2�� �÷��̾� ����! ������ �����մϴ�.";
            // PhotonNetwork.LoadLevel("GameScene"); // ���� ��� ���� ������ �̵�.
        }
    }
}
