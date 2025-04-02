using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    // Canvasses
    public GameObject mainMenuCanvas;
    public GameObject optionsMenuCanvas;

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
}
