/*
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

public class GameManager : NetworkComponent
{    
    //sync vars
    [System.NonSerialized] public bool gameOver;
    [System.NonSerialized] private static bool gameStarted = false;
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
    public static bool debugMode = false;    

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

                foreach (NPM npm in FindObjectsOfType<NPM>())
                {
                    if (npm.GetComponentInChildren<Canvas>() != null)
                        npm.gameObject.GetComponentInChildren<Canvas>().enabled = false;
                }
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
        GameObject start1 = GameObject.Find("P1Start");
        GameObject start2 = GameObject.Find("P2Start");
        GameObject start3 = GameObject.Find("P3Start");
        GameObject start4 = GameObject.Find("P4Start");

        starts[0] = start1.transform.position;
        starts[1] = start2.transform.position;
        starts[2] = start3.transform.position;
        starts[3] = start4.transform.position;
    }

    public override void NetworkedStart()
    {
        if(IsServer)
        {
            levelTime = 0;
            //RandomizeLevel();
        }        
    }

    // I'd like to only change static variables through wrapper functions
    // so we can debug in the function to see its value no matter when or
    // where we change the variable
    public static void AdjustReady(int change)
    {
        playersReady += change;
        int numPlayers = FindObjectsOfType<NPM>().Length;
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
        if (debugMode)
            {
                Enemy[] enemies = GetAllEnemies();
                DestroyAllEnemies(enemies);
            }
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
                yield return new WaitForSeconds(0.5f);
            }

            NPM[] players = GameObject.FindObjectsOfType<NPM>();
            foreach (NPM n in players)
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

                GameObject temp = MyCore.NetCreateObject(0, n.Owner, Vector3.zero, Quaternion.identity);
                temp.GetComponent<PlayerController>().ColorSelected = n.ColorSelected;
                temp.GetComponent<PlayerController>().PName = n.PName;
                temp.GetComponentInChildren<Text>().text = n.PName;
                temp.GetComponent<PlayerController>().SendUpdate("START", n.PName + ";" + n.ColorSelected);
            }
            MyCore.NetCreateObject(7, Owner, new Vector3(4, 0, 0), Quaternion.identity);

            SendUpdate("GAMESTART", "1");
            //stops server from listening, so nobody new can join.
            MyCore.NotifyGameStart();

            //this is basically our regular Update()
            while (!gameOver)
            {
                GameUpdate();
            }
            //wait until game ends...
            SendUpdate("GAMEOVER", "");
            //disable controls or delete player
            //Show scores
            yield return new WaitForSeconds(30f);

            //MyId.NotifyDirty();

            //StartCoroutine(MyCore.DisconnectServer());
            Debug.Log("QUITTING GAME");
            MyCore.UI_Quit();
        }
        yield return new WaitForSeconds(0.1f);
    }
}