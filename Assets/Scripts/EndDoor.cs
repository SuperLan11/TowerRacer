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

    public override void HandleMessage(string flag, string value)
    {
        if(flag == "SCORE")
        {            
            roundScoreText.text = value;
            roundScoreText.enabled = true;
        }        
    }

    private void Start()
    {        
        scorePanel = GameObject.FindGameObjectWithTag("SCORE");
        roundScoreText = scorePanel.GetComponentInChildren<Text>();
    }

    public override void NetworkedStart()
    {

    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (IsServer)
        {
            if (other.gameObject.GetComponent<PlayerController>() != null)
            {                
                roundScoreText.enabled = true;
                PlayerController[] players = FindObjectsOfType<PlayerController>();
                for (int i = 0; i < players.Length; i++)
                {
                    roundScoreText.text += "Player " + (i + 1) + " score: " + Random.Range(0, 5) + '\n';
                }
                SendUpdate("SCORE", roundScoreText.text);

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
