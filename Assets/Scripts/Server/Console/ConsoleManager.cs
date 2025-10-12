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

    #region console 명령어 목록
    #region 조회 명령어
    private string ls = "ls";
    private string ls_room = "ls room";
    #endregion

    #region 수행 명령어
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
            #region 명령어 조회
            if (ConsoleInputFind.text == ls)
            {
                consoleText.color = Color.yellow;
                consoleText.text = "Console 명령어는 Linux 명령어 기반으로 개발 했습니다. \n 방 찾기: ls room,   GameScene 강제 전환: cat GameScene start";
            }
                #endregion

            #region 강제 씬 전환
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

            #region 방 찾기
            if (ConsoleInputFind.text == ls_room)
            {
                if(PhotonNetwork.CurrentRoom != null && PhotonNetwork.CurrentRoom.PlayerCount == 1)
                {
                    consoleText.color = Color.green;
                    consoleText.text = $"{PhotonNetwork.CurrentRoom.Name}방이 존재 합니다.";
                }
                else
                {
                    consoleText.color = Color.white;
                    consoleText.text = "방이 존재 하지 않습니다.";
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

