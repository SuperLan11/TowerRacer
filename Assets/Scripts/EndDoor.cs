using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NETWORK_ENGINE;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class EndDoor : NetworkComponent
{
    private GameObject scorePanel;
    private Text roundScoreText;
    private GameManager gm;

    private int numPlayers;
    public float alphaUpdateFreq = 0.01f;
    //private int playersFinished = 0;
    bool waitDone = false;
    private List<PlayerController> playersFinished = new List<PlayerController>();

    public override void HandleMessage(string flag, string value)
    {
        if(flag == "FADE_IN")
        {
            if (IsClient)
            {
                float seconds = float.Parse(value);
                StartCoroutine(FadeScorePanelIn(seconds));
            }
        }    
    }

    private void Start()
    {
        scorePanel = GameObject.FindGameObjectWithTag("SCORE");
        roundScoreText = scorePanel.GetComponentInChildren<Text>();        
        gm = FindObjectOfType<GameManager>();
    }

    public override void NetworkedStart()
    {

    }

    private IEnumerator FadeScorePanelIn(float seconds)
    {
        //say seconds is 2, alphaFreq is 0.2
        //want alpha to increase by 1 every 2 seconds, so += 0.5 alpha per second        

        if (IsServer)
        {
            SendUpdate("FADE_IN", seconds.ToString());
        }
        
        //set background color based on place
        Image scoreBackground = scorePanel.GetComponent<Image>();

        Image[] images = scorePanel.GetComponentsInChildren<Image>();

        while(images[0].color.a < 1)
        {
            //using yield return with another coroutine pauses this coroutine until the other one finishes
            yield return Wait(alphaUpdateFreq);

            Color panelColor = scoreBackground.color;
            panelColor.a += alphaUpdateFreq/(2*seconds);
            scoreBackground.color = panelColor;

            foreach (Image image in images)
            {
                Color newColor = image.color;
                newColor.a += alphaUpdateFreq / seconds;
                image.color = newColor;                                
            }
        }
        Button nextBtn = scorePanel.GetComponentInChildren<Button>();
        nextBtn.enabled = true;
        nextBtn.GetComponentInChildren<Text>().enabled = true;
    }

    private IEnumerator Wait(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        waitDone = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (IsServer)
        {
            if (other.gameObject.GetComponent<PlayerController>() != null)
            {
                playersFinished.Add(other.gameObject.GetComponent<PlayerController>());
                //re-enable these when round restarts
                other.gameObject.GetComponent<SpriteRenderer>().enabled = false;
                other.gameObject.GetComponent<Collider2D>().enabled = false;
                
                PlayerController[] players = FindObjectsOfType<PlayerController>();
                if(playersFinished.Count == players.Length)
                {                    
                    StartCoroutine(FadeScorePanelIn(1f));
                    //prepare for next round
                    playersFinished.Clear();
                    //teleport players and level after a few seconds
                }                
                
                if (!gm.timerStarted)                                    
                    StartCoroutine(gm.StartTimer());                

                //to send back to main menu
                //GameManager.gameOver = true;                
            }
        }
    }

    public override IEnumerator SlowUpdate()
    {
        while (IsConnected)
        {
            if (IsServer)
            {
                IsDirty = false;
            }
            yield return new WaitForSeconds(0.05f);
        }
    }

    private void Update()
    {

    }
}
