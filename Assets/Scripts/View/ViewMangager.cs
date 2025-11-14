using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class ViewMangager : MonoBehaviour
{
    public static ViewMangager Instance;
    public int width = 1920;
    public int heght = 1080;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    private void Update()
    {
        if(Screen.width >= width || Screen.height >= heght)
        {
            Screen.SetResolution(width, heght, FullScreenMode.FullScreenWindow);
            Camera.main.backgroundColor = Color.black;
        }
    }
}
