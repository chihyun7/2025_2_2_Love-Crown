using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;


public class ConsoleManager : MonoBehaviour
{
    public GameObject ConsoleUI;
    public InputField ConsoleInputFind;
    public Text consoleText;

    public PhotonManager photonManager;

    #region console ��ɾ� ���
    #region ��ȸ ��ɾ�
    private string ls = "ls";
    private string ls_room = "ls room";
    #endregion

    #region ���� ��ɾ�
    private string cat_GameScene_start = "cat GameScene start";
    #endregion

    #endregion

    private void Start()
    {
        photonManager = GetComponent<PhotonManager>();
    }
    void Update()
    {
        if (ConsoleInputFind == null) return;

        if (Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Return))
        {
            #region ��ɾ� ��ȸ
            if (ConsoleInputFind.text == ls)
            {
                consoleText.color = Color.yellow;
                consoleText.text = "Console ��ɾ�� Linux ��ɾ� ������� ���� �߽��ϴ�. \n �� ã��: ls room,   GameScene ���� ��ȯ: cat GameScene start";
            }
                #endregion

            #region ���� �� ��ȯ
                if (ConsoleInputFind.text == cat_GameScene_start)
            {
                if (PhotonNetwork.CurrentRoom != null && PhotonNetwork.CurrentRoom.PlayerCount == 1)
                {
                    consoleText.text = "Start GameScene";
                    consoleText.color = Color.green;
                    StartCoroutine(SceneLoad());
                }
                else
                {
                     consoleText.color = Color.red;
                     consoleText.text = "Null Reference Room ";
                }
            }
            #endregion

            #region �� ã��
            if (ConsoleInputFind.text == ls_room)
            {
                if(PhotonNetwork.CurrentRoom != null && PhotonNetwork.CurrentRoom.PlayerCount == 1)
                {
                    consoleText.color = Color.green;
                    consoleText.text = $"{PhotonNetwork.CurrentRoom.Name}���� ���� �մϴ�.";
                }
                else
                {
                    consoleText.color = Color.white;
                    consoleText.text = "���� ���� ���� �ʽ��ϴ�.";
                }   
            }
            #endregion
        }
    }
     IEnumerator SceneLoad()
     {
        yield return new WaitForSeconds(3f);

        PhotonNetwork.LoadLevel("GameScene");
     }

    public void ConsoleOpenButton()
    {
        if (ConsoleUI != null) ConsoleUI.gameObject.SetActive(true);
    }

    public void ConsoleCloseButton()
    {
        if (ConsoleUI != null) ConsoleUI.gameObject.SetActive(false);
    }
 }

