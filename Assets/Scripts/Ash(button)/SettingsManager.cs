using UnityEngine;
using UnityEngine.EventSystems; // UI 이벤트 처리를 위해 여전히 필요합니다.

public class SettingsManager : MonoBehaviour
{
    // 💡 Inspector 창에서 SettingsPanel GameObject를 연결해 주세요.
    public GameObject settingsPanel;
    
    // 💡 슬라이더 조작 중인지 상태를 기록할 변수 (이전 방식)
    private bool isDraggingSlider = false; 

    // =======================================================
    // 🚨 마우스 입력 감지 및 패널 닫기 로직 (간소화)
    // =======================================================
    
    void Update()
    {
        // 1. 설정 패널이 활성화되어 있고,
        // 2. 현재 슬라이더를 드래그하고 있지 않을 때만
        // 3. 마우스 왼쪽 버튼을 뗀 이벤트를 감지합니다.
        if (settingsPanel.activeSelf && !isDraggingSlider)
        {
            if (Input.GetMouseButtonUp(0))
            {
                // 이 코드는 'UI 밖 클릭'을 확인하는 코드를 생략하고,
                // 슬라이더 조작 중이 아니라면 닫도록 가정합니다.
                
                // 만약 이 위치에서 닫히는 것이 문제라면, 아래 코드를 활성화하세요.
                // if (!IsPointerOverUIObject()) { CloseSettings(); } 
                // 하지만 일단은 가장 간단한 방법으로 시도합니다.

                // 임시로 CloseSettings() 호출을 막고 테스트합니다.
                // CloseSettings(); 
            }
        }

        if (Input.GetKeyDown(KeyCode.Escape) && settingsPanel.activeSelf)
        {
            CloseSettings();
        }
    }
    
    // 💡 슬라이더에 연결할 함수들: 드래그 시작/종료 시 호출됩니다.
    // **슬라이더 오브젝트에 이 함수들을 연결해야 합니다.**

    // 드래그를 시작할 때 (마우스가 슬라이더를 눌렀을 때)
    public void StartSliderDrag()
    {
        isDraggingSlider = true;
    }

    // 드래그를 끝낼 때 (마우스가 슬라이더에서 버튼을 뗄 때)
    public void EndSliderDrag()
    {
        isDraggingSlider = false;
    }
    
    // =======================================================
    // 패널 열기/닫기 함수
    // =======================================================

    // 설정창을 띄우는 함수
    public void OpenSettings()
    {
        settingsPanel.SetActive(true);
    }

    // 설정창을 닫는 함수
    public void CloseSettings()
    {
        settingsPanel.SetActive(false);
    }
}