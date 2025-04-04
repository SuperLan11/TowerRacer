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
    [SerializeField] private float CENTER_PIECE_X = 0f;
    //these aren't serialized as they could break the game if accidentally changed in the inspector        

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

                foreach (NPM npm in FindObjectsOfType<NPM>())
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
            PlayerController[] players = FindObjectsOfType<PlayerController>();
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
            RandomizeLevel();
        }        
    }
    private IEnumerator WaitToDisplayScores(float seconds)
    {
        yield return new WaitForSeconds(seconds);

        Text roundScoreText = GameObject.FindGameObjectWithTag("SCORE").GetComponentInChildren<Text>();
        roundScoreText.enabled = true;
        PlayerController[] players = FindObjectsOfType<PlayerController>();
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
        for (int i = 0; i < Idx.NUM_LEVEL_PIECES; i++)
        {
            int randIdx = Random.Range(0, Idx.NUM_LEVEL_PIECES);
            GameObject piece = MyCore.NetCreateObject(Idx.FIRST_LEVEL_PIECE_IDX + randIdx, this.Owner,
                new Vector3(CENTER_PIECE_X, LOWEST_PIECE_Y + i * 15, 0), Quaternion.identity);
        }        
        //do jacob's translate thing here
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
            GameObject ladder = MyCore.NetCreateObject(8, Owner, new Vector3(5f, -4f, 0), Quaternion.identity);

            Vector2 dismountPos = ladder.GetComponent<Collider2D>().bounds.max;
            dismountPos.x = ladder.GetComponent<Collider2D>().bounds.center.x;
            dismountPos.y += 1f;
            GameObject dismount = MyCore.NetCreateObject(12, Owner, dismountPos, Quaternion.identity);
            dismount.GetComponent<DismountTrigger>().ladder = ladder.GetComponent<LadderObj>();

            GameObject vampire = MyCore.NetCreateObject(11, Owner, new Vector3(9f, 1f, 0), Quaternion.identity);

            GameObject itemBox = MyCore.NetCreateObject(13, Owner, new Vector3(-4f, -9f, 0), Quaternion.identity);
            GameObject itemBox2 = MyCore.NetCreateObject(13, Owner, new Vector3(8f, -9f, 0), Quaternion.identity);

            GameObject rope = MyCore.NetCreateObject(7, Owner, new Vector3(-7f, -4f, 0), Quaternion.identity);

            GameObject endDoor = MyCore.NetCreateObject(15, Owner, new Vector3(5, 12, 0), Quaternion.identity);
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