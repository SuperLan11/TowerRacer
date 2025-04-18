using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NETWORK_ENGINE;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class EndDoor : NetworkComponent
{     
    private GameManager gm;
    private float resultsTimer = 5f;

    private AudioSource playerEnterSfx;
    
    public float alphaUpdateFreq = 0.01f;    

    public Dictionary<string, string> OTHER_FLAGS = new Dictionary<string, string>();

    public override void HandleMessage(string flag, string value)
    {
        if(flag == "ENTER_SFX")
        {
            if(IsClient)
            {
                playerEnterSfx.Play();
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
            
        gm = FindObjectOfType<GameManager>();
        playerEnterSfx = GetComponent<AudioSource>();
    }

    public override void NetworkedStart()
    {
        
    }

    private IEnumerator Wait(float seconds)
    {
        yield return new WaitForSeconds(seconds);        
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

            /*if(playerHit != null && !GameManager.tutorialFinished && !GameManager.playersFinished.Contains(playerHit))
            {
                GameManager.playersFinished.Add(playerHit);
                if (GameManager.playersFinished.Count == FindObjectsOfType<Player>().Length)
                    GameManager.tutorialFinished = true;

                SendUpdate("ENTER_SFX", "");
                playerHit.SendUpdate("CAM_FREEZE", "");
                playerHit.SendUpdate("HIT_DOOR", GameManager.playersFinished.Count.ToString());
            }
            //make sure player hit is not frozen in case of weird double collisions when teleporting
            else */if (playerHit != null && !GameManager.playersFinished.Contains(playerHit) && !playerHit.playerFrozen)
            {                
                GameManager.playersFinished.Add(playerHit);

                SendUpdate("ENTER_SFX", "");
                
                //need to finalize place since place labels aren't always accurate                
                playerHit.SendUpdate("PLACE", GameManager.playersFinished.Count.ToString());                
                                
                playerHit.playerFrozen = true;  
                playerHit.rigidbody.velocity = Vector2.zero;

                if (GameManager.playersFinished.Count == 1)
                {
                    playerHit.wins++;
                    playerHit.isRoundWinner = true;                    
                }                

                playerHit.SendUpdate("CAM_FREEZE", "");
                playerHit.transform.position = playerHit.startPos;
                //playerHit.SendUpdate("HIT_DOOR", GameManager.playersFinished.Count.ToString());
                playerHit.SendUpdate("HIT_DOOR", GameManager.playersFinished.Count.ToString() + ";" + playerHit.Owner);                

                Player[] players = FindObjectsOfType<Player>();
                if (GameManager.playersFinished.Count == players.Length)
                {                    
                    //this affects the start timer coroutine in GameManager                    
                    GameManager.everyoneFinished = true;
                    StartCoroutine(gm.ResetRound());
                }
                else if (!gm.timerStarted)
                {
                    gm.timerStarted = true;                    
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
