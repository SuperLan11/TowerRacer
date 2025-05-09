﻿/*
@Authors - Landon, Patrick
@Description - General game management, and also potentially level generation
*/

//!Ask Towle if we should make this a singleton that has DontDestroyOnLoad(), or if that'd be pointless considering we're not scene switching

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using NETWORK_ENGINE;
using UnityEngine.Tilemaps;
using UnityEngine.InputSystem.LowLevel;

public class PatrickTestManager : NetworkComponent
{    
    //sync vars
    public static bool gameOver;
    private static bool gameStarted = false;
    public static int playersReady = 0;
    public static gameState currentGameState;

    //non-sync vars
    private Vector3[] starts = new Vector3[4];
    private int roundNum = 0;
    private PlayerController[] overallPlayerLeaderboard;
    private PlayerController[] currentPlayerLeaderboard;
    [SerializeField] private float LOWEST_PIECE_Y = -15f;
    //these aren't serialized as they could break the game if accidentally changed in the inspector
    private const int FIRST_LEVEL_PIECE_IDX = 3;
    private const int NUM_LEVEL_PIECES = 3;

    public static double levelTime;
    public static bool debugMode = true;    

    public enum gameState {
        LOBBY,
        GAME,
        BETWEEN_ROUNDS,
        GAME_OVER,
    }

    public override void HandleMessage(string flag, string value)
    {
        if (flag == "GAMESTART")
        {
            if (IsClient)
            {
                gameStarted = true;

                foreach (PatrickTestNPM npm in FindObjectsOfType<PatrickTestNPM>())
                {
                    if (npm.GetComponentInChildren<Canvas>() != null)
                        npm.gameObject.GetComponentInChildren<Canvas>().enabled = false;
                }
            }
        }
        else if(flag == "SCORE")
        {
            Text roundScoreText = GameObject.FindGameObjectWithTag("SCORE").GetComponentInChildren<Text>();
            roundScoreText.enabled = true;
            Player[] players = FindObjectsOfType<Player>();
            for (int i = 0; i < players.Length; i++)
            {
                roundScoreText.text += "Player " + (i + 1) + " score: " + Random.Range(0, 5) + '\n';
            }
        }
        //for objects in scene before clients connect, can't use SendUpdate because
        //SendUpdate only works if IsLocalPlayer and it's impossible to determine IsLocalPlayer
        //for an object already in the scene
        else if (flag == "DEBUG")
        {
            if (IsClient)
            {
                Debug.Log(value);
            }
        }        
    }
    

    // Start is called before the first frame update
    void Start()
    {
        // GameObject start1 = GameObject.Find("P1Start");
        // GameObject start2 = GameObject.Find("P2Start");
        // GameObject start3 = GameObject.Find("P3Start");
        // GameObject start4 = GameObject.Find("P4Start");

        // starts[0] = start1.transform.position;
        // starts[1] = start2.transform.position;
        // starts[2] = start3.transform.position;
        // starts[3] = start4.transform.position;
    }

    public override void NetworkedStart()
    {
        if(IsServer)
        {
            levelTime = 0;

            if (debugMode){
                Enemy[] enemies = GetAllEnemies();
                DestroyAllEnemies(enemies);
            }
            //RandomizeLevel();
        }        
    }
    private IEnumerator WaitToDisplayScores(float seconds)
    {
        yield return new WaitForSeconds(seconds);

        Text roundScoreText = GameObject.FindGameObjectWithTag("SCORE").GetComponentInChildren<Text>();
        roundScoreText.enabled = true;
        Player[] players = FindObjectsOfType<Player>();
        for (int i = 0; i < players.Length; i++)
        {
            roundScoreText.text += "Player " + (i + 1) + " score: " + Random.Range(0, 5) + '\n';
        }

        if (IsServer)
        {
            gameOver = true;
            SendUpdate("SCORE", "");            
        }
    }

    // I'd like to only change static variables through wrapper functions
    // so we can debug in the function to see its value no matter when or
    // where we change the variable
    public static void AdjustReady(int change)
    {
        // Debug.Log(change);
        
        playersReady += change;
        int numPlayers = FindObjectsOfType<PatrickTestNPM>().Length;
        // change to numPlayers > 1 later
        if (playersReady >= numPlayers && numPlayers > 0)
        {
            gameStarted = true;
        }        
    }

    //called by server
    private void RandomizeLevel()
    {        
        for (int i = 0; i < NUM_LEVEL_PIECES; i++)
        {
            int randIdx = Random.Range(0, NUM_LEVEL_PIECES);
            MyCore.NetCreateObject(FIRST_LEVEL_PIECE_IDX + randIdx, this.Owner,
                new Vector3(0, LOWEST_PIECE_Y + i * 15, 0), Quaternion.identity);
        }        
    }

    private void DisableRooms()
    {

    }

    public Enemy[] GetAllEnemies(){
        return GameObject.FindObjectsOfType<Enemy>();
    }

    public void DestroyAllEnemies(Enemy[] enemies){
        foreach (Enemy enemy in enemies){
            MyCore.NetDestroyObject(enemy.NetId);
        }
    }

    public IEnumerator GameUpdate(){
        //game is playing
        //turn-based logic
        //maintain score
        //maintain metrics        
        yield return new WaitForSeconds(0.5f);
    }

    public override IEnumerator SlowUpdate()
    {
        if (IsServer)
        {
            while (!gameStarted)
            {
                // Debug.Log(gameStarted);
                yield return new WaitForSeconds(0.5f);
            }

            PatrickTestNPM[] players = GameObject.FindObjectsOfType<PatrickTestNPM>();
            foreach (PatrickTestNPM n in players)
            {
                //create object and set proper networked variables                
                //Go to each NPM and look at their options
                //Create the appropriate character for their options                
                Vector3 spawnPos = Vector3.zero;
                switch (n.Owner)
                {
                    case 0:
                        spawnPos = starts[0];
                        break;
                    case 1:
                        spawnPos = starts[1];
                        break;
                    case 2:
                        spawnPos = starts[2];
                        break;
                    case 3:
                        spawnPos = starts[3];
                        break;
                }

                //spawn player
                //GameObject temp = MyCore.NetCreateObject(0, n.Owner, Vector3.zero, Quaternion.identity);
                int archer = 17, mage = 18, bandit = 19, knight = 20;
                
                GameObject temp = MyCore.NetCreateObject(archer, n.Owner, Vector3.zero, Quaternion.identity);

                
                // temp.GetComponent<PlayerController>().ColorSelected = n.ColorSelected;
                // temp.GetComponent<PlayerController>().PName = n.PName;
                // temp.GetComponentInChildren<Text>().text = n.PName;
                // temp.GetComponent<PlayerController>().SendUpdate("START", n.PName + ";" + n.ColorSelected);
            }
            //!spawn your BS here
            MyCore.NetCreateObject(11, Owner, new Vector3(14f, -9f, 0f), Quaternion.identity);
            /*
            MyCore.NetCreateObject(13, Owner, new Vector3(1f, 0f, 0f), Quaternion.identity);
            MyCore.NetCreateObject(13, Owner, new Vector3(2f, 0f, 0f), Quaternion.identity);
            MyCore.NetCreateObject(13, Owner, new Vector3(3f, 0f, 0f), Quaternion.identity);
            MyCore.NetCreateObject(13, Owner, new Vector3(4f, 0f, 0f), Quaternion.identity);
            MyCore.NetCreateObject(13, Owner, new Vector3(5f, 0f, 0f), Quaternion.identity);
*/
            
            //GameObject rope = MyCore.NetCreateObject(1, Owner, new Vector3(6.5f, -6.8f, 0), Quaternion.identity);

            //dismount.GetComponent<DismountTrigger>().ladder = ladder.GetComponent<LadderObj>();
            /*MyCore.NetCreateObject(9, Owner, new Vector3(1.7f, 0.5f, 0), Quaternion.identity);
            MyCore.NetCreateObject(10, Owner, new Vector3(5f, 0.5f, 0), Quaternion.identity);
            MyCore.NetCreateObject(11, Owner, new Vector3(-3.5f, 5f, 0), Quaternion.identity);*/
            //MyCore.NetCreateObject(1, Owner, new Vector3(4, 3f, 0), Quaternion.identity);

            SendUpdate("GAMESTART", "1");
            //stops server from listening, so nobody new can join.
            MyCore.NotifyGameStart();            

            //this is basically our regular Update()
            while (!gameOver)
            {
                //game is playing
                //turn-based logic
                //maintain score
                //maintain metrics                
                yield return GameUpdate();
            }
            Debug.Log("GAME OVER");            
            SendUpdate("GAMEOVER", "");
            //wait until game ends...
            //disable controls or delete player
            //Show scores
            yield return new WaitForSeconds(5f);
            
            Debug.Log("QUITTING GAME");
            gameStarted = false;
            MyId.NotifyDirty();
            MyCore.UI_Quit();
        }
        yield return new WaitForSeconds(0.1f);
    }
}