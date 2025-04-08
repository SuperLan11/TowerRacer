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
    private GameManager gm;
    private float resultsTimer = 5f;
    
    public float alphaUpdateFreq = 0.01f;
    //private int playersFinished = 0;
    public static bool roundDone = false;
    private List<Player> playersFinished = new List<Player>();

    public Dictionary<string, string> OTHER_FLAGS = new Dictionary<string, string>();

    public override void HandleMessage(string flag, string value)
    {
        if (flag == "FADE_IN")
        {
            if (IsClient)
            {
                float seconds = float.Parse(value);
                Debug.Log("fading in on client");
                StartCoroutine(FadeScorePanelIn(seconds));
            }
        }
        else if (flag == "FADE_OUT")
        {
            if (IsClient)
            {
                float seconds = float.Parse(value);
                StartCoroutine(FadeScorePanelOut(seconds));
            }
        }        
        else if (flag == "DEBUG")
        {
            Debug.Log(value);
            if (IsClient)
            {
                SendCommand(flag, value);
            }
        }
        else if (!OTHER_FLAGS.ContainsKey(flag))
        {
            Debug.LogWarning(flag + " is not a valid flag in " + this.GetType().Name + ".cs");
            if (IsClient)
            {
                SendCommand("DEBUG", flag + " is not a valid flag in " + this.GetType().Name + ".cs");
            }
        }
    }

    private void Start()
    {
        if (GetComponent<NetworkRB2D>() != null)
            OTHER_FLAGS = GetComponent<NetworkRB2D>().FLAGS;
        else if (GetComponentInChildren<NetworkRB2D>() != null)
            OTHER_FLAGS = GetComponentInChildren<NetworkRB2D>().FLAGS;
        else if (GetComponent<NetworkTransform>() != null)
            OTHER_FLAGS = GetComponent<NetworkTransform>().FLAGS;
        else if (GetComponentInChildren<NetworkTransform>() != null)
            OTHER_FLAGS = GetComponentInChildren<NetworkTransform>().FLAGS;

        scorePanel = GameObject.FindGameObjectWithTag("SCORE");        
        gm = FindObjectOfType<GameManager>();
    }

    public override void NetworkedStart()
    {
        
    }

    private int GetPlayerPlace(Player player)
    { 
        for(int i = 0; i < playersFinished.Count; i++)
        {
            if(playersFinished[i] == player)           
                return i + 1;
        }
        return -1;
    }

    private IEnumerator FadeScorePanelIn(float seconds)
    {        
        if (IsServer)
        {
            SendUpdate("FADE_IN", seconds.ToString());
        }

        Debug.Log("start of fadein");
        //set background color based on place later
        Image scoreBackground = scorePanel.GetComponent<Image>();

        Image[] images = scorePanel.GetComponentsInChildren<Image>();
        Text[] labels = scorePanel.GetComponentsInChildren<Text>();
        Debug.Log("got to middle");
        while(images[0].color.a < 1)
        {
            //using yield return with another coroutine pauses this coroutine until the other one finishes
            yield return Wait(alphaUpdateFreq);

            Color newPanelColor = scoreBackground.color;
            newPanelColor.a += alphaUpdateFreq/(2*seconds);
            scoreBackground.color = newPanelColor;

            foreach (Image image in images)
            {
                Color newColor = image.color;
                newColor.a += alphaUpdateFreq / seconds;
                image.color = newColor;                                
            }

            foreach(Text text in labels)
            {
                Color newColor = text.color;
                newColor.a += alphaUpdateFreq / seconds;
                text.color = newColor;
            }
        }

        yield return Wait(resultsTimer);
        StartCoroutine(FadeScorePanelOut(1f));
    }

    private IEnumerator FadeScorePanelOut(float seconds)
    {
        if (IsServer)
        {
            SendUpdate("FADE_OUT", seconds.ToString());
        }
        
        Image scoreBackground = scorePanel.GetComponent<Image>();

        Image[] images = scorePanel.GetComponentsInChildren<Image>();
        Text[] labels = scorePanel.GetComponentsInChildren<Text>();

        while (images[0].color.a > 0)
        {
            //using yield return with another coroutine pauses this coroutine until the other one finishes
            yield return Wait(alphaUpdateFreq);

            Color newPanelColor = scoreBackground.color;
            newPanelColor.a -= alphaUpdateFreq / (2 * seconds);
            scoreBackground.color = newPanelColor;

            foreach (Image image in images)
            {
                Color newColor = image.color;
                newColor.a -= alphaUpdateFreq / seconds;
                image.color = newColor;
            }

            foreach (Text text in labels)
            {
                Color newColor = text.color;
                newColor.a -= alphaUpdateFreq / seconds;
                text.color = newColor;
            }
        }
    }

    private IEnumerator Wait(float seconds)
    {
        yield return new WaitForSeconds(seconds);        
    }    

    private void ShowOverallResults()
    {

    }

    private void StartNextRound()
    {

    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (IsServer)
        {
            Player playerHit = other.gameObject.GetComponentInParent<Player>();
            if (playerHit != null && !playersFinished.Contains(playerHit))
            {
                Debug.Log("end door hit player");
                playersFinished.Add(playerHit);                
                
                //re-enable these when round restarts
                playerHit.GetComponent<SpriteRenderer>().enabled = false;
                //playerHit.camFrozen = true;
                //other.transform.position = other.GetComponent<Player>().startPos;
                //SendUpdate to hide
                
                Player[] players = FindObjectsOfType<Player>();
                if(playersFinished.Count == players.Length)
                {                    
                    StartCoroutine(FadeScorePanelIn(1f));
                    //prepare for next round
                    playersFinished.Clear();
                    roundDone = true;
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
