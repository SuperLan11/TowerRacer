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
    public static bool timerStopped = false;
    private List<Player> playersFinished = new List<Player>();

    public Dictionary<string, string> OTHER_FLAGS = new Dictionary<string, string>();

    public override void HandleMessage(string flag, string value)
    {
        if (flag == "DEBUG")
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

    /*
     * 1. inc wins for first player to touch door
     * 2. SendUpdate to set camFreeze
     * 3. teleport player to startPos and SendUpdate
     * 4. Fade in scores
     * 5. Randomize level
     * 6. Wait 5 seconds
     * 7. Fade out scores and unfreeze camera
     * 8. Wait 3 seconds
     * 9. SendUpdate to start countdown
     * */

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (IsServer)
        {
            Player playerHit = other.gameObject.GetComponentInParent<Player>();
            if (playerHit != null && !playersFinished.Contains(playerHit))
            {                
                playersFinished.Add(playerHit);

                if (playersFinished.Count == 1)
                    playerHit.wins++;

                //if(playerHit.wins >= 3)
                //show a winscreen of the player                
                //GameManager.gameOver = true;
                //make sure to reset all stats on game over!!!

                playerHit.SendUpdate("CAM_FREEZE", "");
                playerHit.transform.position = playerHit.startPos;
                playerHit.SendUpdate("TELEPORT", playerHit.startPos.ToString());

                Player[] players = FindObjectsOfType<Player>();
                if (playersFinished.Count == players.Length)
                {
                    playersFinished.Clear();
                    timerStopped = true;

                    StartCoroutine(gm.ResetRound());
                }
                else if (!gm.timerStarted)
                {
                    StartCoroutine(gm.StartTimer());
                }                
                
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
