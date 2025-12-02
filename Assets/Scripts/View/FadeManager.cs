using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor.Rendering.LookDev;
using UnityEngine;
using UnityEngine.UI;

public class FadeManager : MonoBehaviour
{
    public Image fadePanel;
    public float fadeDuration = 1.0f;
    public PhotonManager photonManager;

    private void Update()
    {
        if (!photonManager)
        {
            Debug.Log("미존재 확인");
            photonManager = FindAnyObjectByType<PhotonManager>();
        }

        if (photonManager != null && photonManager.isMaster)
        {
            StartFadeIn();
            return;
            
        }
        else
            Debug.Log("isMasterServer가 flase 입니다.");      
    }

    public void StartFadeIn()
    {
        StartCoroutine(Fade(0, fadeDuration));
    }

    IEnumerator Fade(float targetAlpha, float duration)
    {
        float startAlpha = fadePanel.color.a;
        float time = 0;

        while (time < duration)
        {
            time += Time.deltaTime;
            float newAlpha = Mathf.Lerp(startAlpha, targetAlpha, time / duration);

            fadePanel.color = new Color(fadePanel.color.r, fadePanel.color.g, fadePanel.color.b, newAlpha);

            yield return null;
        }
        fadePanel.color = new Color(fadePanel.color.r, fadePanel.color.g, fadePanel.color.b, targetAlpha);
    }
}
