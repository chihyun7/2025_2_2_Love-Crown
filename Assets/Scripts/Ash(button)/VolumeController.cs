using UnityEngine;
using UnityEngine.Audio; // Audio Mixer 사용을 위해 필수
using UnityEngine.UI;    // Slider 사용을 위해 필수

public class VolumeController : MonoBehaviour
{
    // 💡 Inspector 창에서 MainMixer와 Slider를 연결합니다.
    public AudioMixer mainMixer;
    public Slider bgmSlider;
    public Slider sfxSlider;

    // AudioMixer에 노출시킨 파라미터 이름 (대소문자 일치 필수!)
    private const string BGM_PARAM = "BGMVolume"; 
    private const string SFX_PARAM = "SFXVolume";

    void Start()
    {
        // 씬 시작 시 Slider의 OnValueChanged 이벤트에 함수 연결
        bgmSlider.onValueChanged.AddListener(SetBGMVolume);
        sfxSlider.onValueChanged.AddListener(SetSFXVolume);
        
        // (선택) 저장된 볼륨 값 로드 후 슬라이더 초기화
        LoadVolumeSettings();
    }

    // BGM 볼륨 조절 함수
    public void SetBGMVolume(float sliderValue)
    {
        // 슬라이더 값(0~1)을 로그 볼륨(데시벨)로 변환하여 Mixer에 설정
        // Mathf.Log10(sliderValue) * 20f는 -80dB ~ 0dB 사이의 값을 반환합니다.
        mainMixer.SetFloat(BGM_PARAM, Mathf.Log10(sliderValue) * 20f);
        
        // (선택) PlayerPrefs.SetFloat(BGM_PARAM, sliderValue); // 값 저장
    }

    // SFX 볼륨 조절 함수
    public void SetSFXVolume(float sliderValue)
    {
        mainMixer.SetFloat(SFX_PARAM, Mathf.Log10(sliderValue) * 20f);

        // (선택) PlayerPrefs.SetFloat(SFX_PARAM, sliderValue); // 값 저장
    }
    
    // (선택) 저장된 볼륨 설정 로드 함수
    void LoadVolumeSettings()
    {
        // PlayerPrefs.GetFloat(BGM_PARAM, 0.75f) 등으로 저장된 값 로드
        // 로드된 값을 bgmSlider.value = loadedValue; 형태로 초기화
        // SetBGMVolume(loadedValue); 함수를 호출하여 초기 볼륨 설정
    }
}