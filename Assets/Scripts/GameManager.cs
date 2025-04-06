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
    private static float LOWEST_PIECE_Y = -2f;
    public static float CENTER_PIECE_X = 0f;

    private Text placeLbl;
    private GameObject gameUI;

    //the timer starts when the first player reaches the end door
    //the timer ends the round so players don't have to wait on the last player forever
    private Text timerLbl;
    private int roundEndTime = 120;
    private float curTimer;
    public bool timerStarted = false;
    public bool timerFinished = false;

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
                //Debug.Log("gameUi == null: " + (gameUI == null));
                gameUI.GetComponent<Canvas>().enabled = true;
                gameStarted = true;

                foreach (NPM npm in FindObjectsOfType<NPM>())
                {
                    if (npm.GetComponentInChildren<Canvas>() != null)
                    {
                        npm.gameObject.GetComponentInChildren<Canvas>().enabled = false;
                    }
                }
            }
        }
        else if(flag == "INIT_UI")
        {
            if(IsClient)
            {
                gameUI = GameObject.FindGameObjectWithTag("GAME_UI");
                placeLbl = GameObject.FindGameObjectWithTag("PLACE").GetComponent<Text>();
                gameUI = GameObject.FindGameObjectWithTag("GAME_UI");
                timerLbl = GameObject.FindGameObjectWithTag("TIMER").GetComponent<Text>();
                Debug.Log("client got ui");
            }
        }
        else if (flag == "SCORE")
        {
            Text roundScoreText = GameObject.FindGameObjectWithTag("SCORE").GetComponent<Text>();
            roundScoreText.enabled = true;
            PlayerController[] players = FindObjectsOfType<PlayerController>();
            for (int i = 0; i < players.Length; i++)
            {
                roundScoreText.text += "Player " + (i + 1) + " score: " + Random.Range(0, 5) + '\n';
            }
        }
        else if (flag == "TIMER_SHOW")
        {
            if (IsClient)
            {
                timerLbl.enabled = true;
                timerStarted = true;
            }
        }
        else if (flag == "TIMER")
        {
            if (IsClient)
            {
                timerLbl.text = value + "s";
            }
        }
        //for objects in scene before clients connect, can't use SendCommand because
        //SendCommand only works if IsLocalPlayer and it's impossible to determine IsLocalPlayer
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

        placeLbl = GameObject.FindGameObjectWithTag("PLACE").GetComponent<Text>();
        gameUI = GameObject.FindGameObjectWithTag("GAME_UI");
        timerLbl = GameObject.FindGameObjectWithTag("TIMER").GetComponent<Text>();
        curTimer = roundEndTime;        

        curTimer = roundEndTime;

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

            if (debugMode){
                Enemy[] enemies = GetAllEnemies();
                DestroyAllEnemies(enemies);
            }

            SendUpdate("INIT_UI", "");
        }        
    }

    // nice to have this as a wrapper function
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
            if (i == Idx.NUM_LEVEL_PIECES - 1)
            {
                GameObject endPiece = MyCore.NetCreateObject(Idx.END_LEVEL_PIECE, this.Owner,
                    new Vector3(CENTER_PIECE_X, LOWEST_PIECE_Y + i * 15, 0), Quaternion.identity);
                break;
            }
                      
            GameObject piece = MyCore.NetCreateObject(Idx.FIRST_LEVEL_PIECE_IDX + randIdx, this.Owner,
                new Vector3(CENTER_PIECE_X, LOWEST_PIECE_Y + i * 15, 0), Quaternion.identity);

            RandomlyPlaceRope(piece);
            RandomlyPlaceEnemies(piece);
            RandomlyPlaceItemBoxes(piece);
            RandomlyPlaceLadders(piece);            
        }
        //do jacob's translate thing here
    }

    private void DisableRooms()
    {

    }

    private void RandomlyPlaceRope(GameObject levelPiece)
    {
        List<Vector3> ropePlaces = new List<Vector3>();
        for(int i = 0; i < levelPiece.transform.childCount; i++)
        {
            if (levelPiece.transform.GetChild(i).tag == "ROPE_POS")
                ropePlaces.Add(levelPiece.transform.GetChild(i).position);
        }

        if (ropePlaces.Count == 0)
        {            
            return;
        }
        
        int randPos = Random.Range(0, ropePlaces.Count);        
        MyCore.NetCreateObject(Idx.ROPE, Owner, ropePlaces[randPos], Quaternion.identity);       
    }

    private void RandomlyPlaceEnemies(GameObject levelPiece)
    {
        List<Vector3> enemyPlaces = new List<Vector3>();
        for (int i = 0; i < levelPiece.transform.childCount; i++)
        {
            if (levelPiece.transform.GetChild(i).tag == "ENEMY_POS")
                enemyPlaces.Add(levelPiece.transform.GetChild(i).position);
        }

        if (enemyPlaces.Count == 0)
        {                        
            return;
        }

        int randPos = Random.Range(0, enemyPlaces.Count);
        int lastEnemyIdx = Idx.FIRST_ENEMY_IDX + Idx.NUM_ENEMIES - 1;
        int randEnemy = Random.Range(Idx.FIRST_ENEMY_IDX, lastEnemyIdx);        
        MyCore.NetCreateObject(randEnemy, Owner, enemyPlaces[randPos], Quaternion.identity);
    }

    private void RandomlyPlaceItemBoxes(GameObject levelPiece)
    {
        List<Vector3> itemPlaces = new List<Vector3>();
        for (int i = 0; i < levelPiece.transform.childCount; i++)
        {
            if (levelPiece.transform.GetChild(i).tag == "ITEM_POS")
                itemPlaces.Add(levelPiece.transform.GetChild(i).position);
        }

        if (itemPlaces.Count == 0)
        {            
            return;
        }

        int randPos = Random.Range(0, itemPlaces.Count);
        MyCore.NetCreateObject(Idx.ITEM_BOX, Owner, itemPlaces[randPos], Quaternion.identity);
    }

    private void RandomlyPlaceLadders(GameObject levelPiece)
    {
        List<Vector3> ladderPlaces = new List<Vector3>();
        for (int i = 0; i < levelPiece.transform.childCount; i++)
        {
            if (levelPiece.transform.GetChild(i).tag == "LADDER_POS")
                ladderPlaces.Add(levelPiece.transform.GetChild(i).position);
        }

        if (ladderPlaces.Count == 0)
        {            
            return;
        }

        int randPos = Random.Range(0, ladderPlaces.Count);
        MyCore.NetCreateObject(Idx.LADDER, Owner, ladderPlaces[randPos], Quaternion.identity);
    }

    public Enemy[] GetAllEnemies(){
        return GameObject.FindObjectsOfType<Enemy>();
    }

    public void DestroyAllEnemies(Enemy[] enemies){
        foreach (Enemy enemy in enemies){
            MyCore.NetDestroyObject(enemy.NetId);
        }
    }

    private void UpdatePlaces()
    {
        PlayerController[] players = FindObjectsOfType<PlayerController>();
        int[] playerIdxsByHeight = new int[players.Length];

        for(int i = 0; i < players.Length; i++)
        {
            playerIdxsByHeight[i] = i;
        }        

        //selection sort to rank players by height descending
        for(int i = 0; i < playerIdxsByHeight.Length; i++)
        {
            float playerY1 = players[i].transform.position.y;
            for (int j = i + 1; j < players.Length; j++)
            {
                float playerY2 = players[j].transform.position.y;
                if(playerY2 > playerY1)
                {
                    int tempIdx = playerIdxsByHeight[i];
                    playerIdxsByHeight[i] = playerIdxsByHeight[j];
                    playerIdxsByHeight[j] = tempIdx;
                }
            }
        }

        for(int i = 0; i < playerIdxsByHeight.Length; i++)
        {
            int playerIdx = playerIdxsByHeight[i];            
            //now that players are ranked, send each an update with their placement (i+1)
            players[playerIdx].SendUpdate("PLACE", (i+1).ToString());
        }        
    }

    //called by EndDoor script
    public IEnumerator StartTimer()
    {
        timerLbl.enabled = true;
        timerStarted = true;
        SendUpdate("TIMER_SHOW", "");

        while (curTimer > 0)
        {            
            SendUpdate("TIMER", curTimer.ToString());
            yield return new WaitForSeconds(1);
            curTimer -= 1;
            timerLbl.text = curTimer + "s";
        }
        timerFinished = true;
        timerLbl.enabled = false;
        Debug.Log("do something when timer ends!!");
    }

    public IEnumerator GameUpdate(){                
        UpdatePlaces();

        //don't make this timer too fast as UpdatePlaces is somewhat high on performance
        yield return new WaitForSeconds(0.5f);
    }

    public override IEnumerator SlowUpdate()
    {
        if (IsServer)
        {
            RandomizeLevel();
            
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

                GameObject temp = MyCore.NetCreateObject(0, n.Owner, spawnPos, Quaternion.identity);
                PlayerController player = temp.GetComponent<PlayerController>();
                if (player == null)
                    Debug.Log("player is null!!");

                player.ColorSelected = n.ColorSelected;
                player.PName = n.PName;

                Sprite spriteSelected = player.heroSprites[n.CharSelected];
                temp.GetComponent<SpriteRenderer>().sprite = spriteSelected;

                temp.GetComponentInChildren<Text>().text = n.PName;
                player.SendUpdate("START", n.PName + ";" + n.ColorSelected + ";" + n.CharSelected);
            }            

            /*GameObject ladder = MyCore.NetCreateObject(Idx.LADDER, Owner, new Vector3(-7, -3, 0), Quaternion.identity);
            GameObject rope = MyCore.NetCreateObject(Idx.ROPE, Owner, new Vector3(0, 0, 0), Quaternion.identity);*/

            GameObject itemBox1 = MyCore.NetCreateObject(Idx.ITEM_BOX, Owner, new Vector3(8, -7, 0), Quaternion.identity);
            GameObject itemBox2 = MyCore.NetCreateObject(Idx.ITEM_BOX, Owner, new Vector3(5, -7, 0), Quaternion.identity);

            SendUpdate("GAMESTART", "1");            
            //stops server from listening, so nobody new can join.
            MyCore.NotifyGameStart();            

            //this is basically our regular Update()
            while (!gameOver)
            {                        
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