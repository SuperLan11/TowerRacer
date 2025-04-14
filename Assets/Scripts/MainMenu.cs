using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    // Canvasses
    public GameObject mainMenuCanvas;
    public GameObject optionsMenuCanvas;
    public AudioMixer mixer;

    private int oldWidth;
    private int oldHeight;

    // Start is called before the first frame update
    void Start()
    {
        // Automatically load WAN scene if the loaded game is master server
        string[] args = System.Environment.GetCommandLineArgs();
        foreach (string a in args) {
            if (a.StartsWith("PORT_") || a.Contains("MASTER")) {
                SceneManager.LoadScene(1);
            }
        }

        // Set the main menu active and options menu inactive
        mainMenuCanvas.SetActive(true);
        optionsMenuCanvas.SetActive(false);
    }

    // Update is called once per frame
    void Update() {}

    public void WANConnect() { SceneManager.LoadScene(1); }

    public void LANConnect() { SceneManager.LoadScene(2); }

    public void GoToOptions()
    {
        mainMenuCanvas.SetActive(false);
        optionsMenuCanvas.SetActive(true);
    }

    public void QuitMe() { Application.Quit(); }


    // OPTIONS MENU BUTTONS
    public void BackToMenu()
    {
        mainMenuCanvas.SetActive(true);
        optionsMenuCanvas.SetActive(false);
    }

    public void MusicVolume(float f)
    {
        mixer.SetFloat("Music", Mathf.Log10(f) * 20);
    }

    public void SFXVolume(float f)
    {
        mixer.SetFloat("SFX", Mathf.Log10(f) * 20);
    }

    public void FullscreenToggle(bool b)
    {
        if (b) {
            // Save the current resolution
            oldWidth = Screen.currentResolution.width;
            oldHeight = Screen.currentResolution.height;
            Screen.SetResolution(Display.main.systemWidth, Display.main.systemHeight, false);
            Screen.fullScreenMode = FullScreenMode.ExclusiveFullScreen;
        } else {
            // Set the resolution back to the old one
            Screen.SetResolution(oldWidth, oldHeight, false);
            Screen.fullScreenMode = FullScreenMode.Windowed;
        }
    }
}
