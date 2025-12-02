using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;
using System.Linq;

public class CharmUI : MonoBehaviour
{
    [Header("Left (Local Player)")]
    public Text leftNameText;    // 왼쪽 위
    public Text leftCountText;   // 왼쪽 아래

    [Header("Right (Other Player)")]
    public Text rightNameText;   // 오른쪽 위
    public Text rightCountText;  // 오른쪽 아래

    private Player localPlayer;
    private Player otherPlayer;

    private void Start()
    {
        // 내 플레이어
        localPlayer = PhotonNetwork.LocalPlayer;

        // 상대 플레이어 (나 아닌 첫 번째)
        otherPlayer = PhotonNetwork.PlayerList
            .FirstOrDefault(p => p.ActorNumber != localPlayer.ActorNumber);

        // 이름 세팅
        if (leftNameText != null)
            leftNameText.text = $"이름: {localPlayer.NickName}";

        if (otherPlayer != null && rightNameText != null)
            rightNameText.text = $"이름: {otherPlayer.NickName}";
        else if (rightNameText != null)
            rightNameText.text = ""; // 아직 상대 없음
    }

    private void Update()
    {
        if (GameManager.Instance == null) return;

        // 내 귀속 NPC 수
        int myActor = localPlayer.ActorNumber;
        int myCount = GameManager.Instance.GetCharmedCount(myActor);

        if (leftCountText != null)
            leftCountText.text = $"귀속된 NPC 수 : {myCount} ";

        // 상대 귀속 NPC 수
        if (otherPlayer != null)
        {
            int enemyActor = otherPlayer.ActorNumber;
            int enemyCount = GameManager.Instance.GetCharmedCount(enemyActor);

            if (rightCountText != null)
                rightCountText.text = $"귀속된 NPC 수 : {enemyCount} ";
        }
        else
        {
            if (rightCountText != null)
                rightCountText.text = ""; // 상대 없으면 빈칸
        }
    }
}
