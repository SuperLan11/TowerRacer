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
    private Player[] overallPlayerLeaderboard;
    private Player[] currentPlayerLeaderboard;
    [SerializeField] private float LOWEST_PIECE_Y = -2f;
    public static float CENTER_PIECE_X = 0f;

    private Text placeLbl;
    private GameObject gameUI;
    private Text timerLbl;
    private GameObject scorePanel;
    private Text countdownLbl;

    //the timer starts when the first player reaches the end door
    //the timer ends the round so players don't have to wait on the last player forever    
    private int roundEndTime = 120;
    private float curTimer;
    public bool timerStarted = false;
    public bool timerFinished = false;
    private float camEndY;

    private float resultsTimer = 5f;
    private float alphaUpdateFreq = 0.01f;

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
                //this isn't used but may help later
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
        else if (flag == "INIT_UI")
        {
            if (IsClient)
            {
                Debug.Log("client is getting ui...");
                InitUI();
            }
        }
        else if (flag == "FADE_IN")
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
        else if (flag == "SCORE")
        {
            Text roundScoreText = GameObject.FindGameObjectWithTag("SCORE").GetComponent<Text>();
            roundScoreText.enabled = true;
            Player[] players = FindObjectsOfType<Player>();
            for (int i = 0; i < players.Length; i++)
            {
                roundScoreText.text += "Player " + (i + 1) + " score: " + Random.Range(0, 5) + '\n';
            }
        }
        else if (flag == "SHOW_TIMER")
        {
            if (IsClient)
            {
                timerLbl.enabled = true;
                timerStarted = true;
            }
        }
        else if (flag == "HIDE_TIMER")
        {
            if (IsClient)
            {
                timerLbl.enabled = false;
            }
        }
        else if (flag == "TIMER")
        {
            if (IsClient)
            {
                timerLbl.text = value + "s";
            }
        }
        else if (flag == "SHOW_PLACE")
        {
            if (IsClient)
            {
                placeLbl.enabled = true;
            }
        }
        else if (flag == "HIDE_PLACE")
        {
            if (IsClient)
            {
                placeLbl.enabled = false;
            }
        }
        else if (flag == "COUNTDOWN")
        {
            if (IsClient)
            {                
                countdownLbl.enabled = true;
                countdownLbl.text = value;
                Debug.Log("countdown text: " + countdownLbl.text);
                Debug.Log("countdown enabled: " + countdownLbl.enabled);
            }
        }
        else if(flag == "HIDE_COUNTDOWN")
        {
            if(IsClient)
            {
                countdownLbl.enabled = false;
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
        else
        {
            Debug.LogWarning(flag + " is not a valid flag in GameManager.cs!");
        }
    }
    

    // Start is called before the first frame update
    void Start()
    {
        GameObject start1 = GameObject.Find("P1Start");
        GameObject start2 = GameObject.Find("P2Start");
        GameObject start3 = GameObject.Find("P3Start");
        GameObject start4 = GameObject.Find("P4Start");
        
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
                /*GameObject endPiece = MyCore.NetCreateObject(Idx.END_LEVEL_PIECE, this.Owner,
                    new Vector3(CENTER_PIECE_X, LOWEST_PIECE_Y + i * 15, 0), Quaternion.identity);*/
                GameObject endPiece = MyCore.NetCreateObject(Idx.END_LEVEL_PIECE, this.Owner,
                    new Vector3(CENTER_PIECE_X, LOWEST_PIECE_Y - 10, 0), Quaternion.identity);
                PlaceDoor(endPiece);

                camEndY = endPiece.transform.position.y;                
                break;
            }
                      
            GameObject piece = MyCore.NetCreateObject(Idx.FIRST_LEVEL_PIECE_IDX + randIdx, this.Owner,
                new Vector3(CENTER_PIECE_X, LOWEST_PIECE_Y + i * 15, 0), Quaternion.identity);

            RandomlyPlaceRope(piece);
            //RandomlyPlaceEnemies(piece);
            RandomlyPlaceItemBoxes(piece);
            RandomlyPlaceLadders(piece);            
        }
        //do jacob's translate thing here
    }

    private void DisableRooms()
    {

    }

    private void PlaceDoor(GameObject endPiece)
    {
        for(int i = 0; i < endPiece.transform.childCount; i++)
        {
            if(endPiece.transform.GetChild(i).tag == "END_DOOR_POS")
            {
                MyCore.NetCreateObject(Idx.END_DOOR, Owner, endPiece.transform.GetChild(i).transform.position, Quaternion.identity);
            }
        }
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
        Player[] players = FindObjectsOfType<Player>();
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
        InitUI();
        SendUpdate("INIT_UI", "");

        timerLbl.enabled = true;
        SendUpdate("SHOW_TIMER", "");        
        timerStarted = true;
        //I tried doing this at the start of SlowUpdate but the clients did not run for some reason
        
        while (curTimer > 0)
        {            
            SendUpdate("TIMER", curTimer.ToString());
            yield return new WaitForSeconds(1);
            curTimer -= 1;
            timerLbl.text = curTimer + "s";

            if (EndDoor.timerStopped)
            {
                break;
            }
        }

        if (!EndDoor.timerStopped)
        {
            timerFinished = true;
            timerLbl.enabled = false;
            Debug.Log("do something when timer ends!!");
        }
    }

    private IEnumerator Wait(float seconds)
    {
        yield return new WaitForSeconds(seconds);
    }

    private IEnumerator FadeScorePanelIn(float seconds)
    {
        if (IsServer)
        {
            SendUpdate("FADE_IN", seconds.ToString());
        }
        
        //set background color based on place later
        Image scoreBackground = scorePanel.GetComponent<Image>();

        Image[] images = scorePanel.GetComponentsInChildren<Image>();
        Text[] labels = scorePanel.GetComponentsInChildren<Text>();
        
        while (images[0].color.a < 1)
        {
            //using yield return with another coroutine pauses this coroutine until the other one finishes
            yield return Wait(alphaUpdateFreq);

            Color newPanelColor = scoreBackground.color;
            newPanelColor.a += alphaUpdateFreq / (2 * seconds);
            scoreBackground.color = newPanelColor;

            foreach (Image image in images)
            {
                Color newColor = image.color;
                newColor.a += alphaUpdateFreq / seconds;
                image.color = newColor;
            }

            foreach (Text text in labels)
            {
                Color newColor = text.color;
                newColor.a += alphaUpdateFreq / seconds;
                text.color = newColor;
            }
        }
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

    public IEnumerator ResetRound()
    {
        if(IsServer)
        {
            timerLbl.enabled = false;
            SendUpdate("HIDE_TIMER", "");            

            //yield return prevents the following lines from running until the coroutine is done
            yield return FadeScorePanelIn(1f);

            TilemapCollider2D[] pieces = FindObjectsOfType<TilemapCollider2D>();
            foreach (TilemapCollider2D piece in pieces)
            {
                MyCore.NetDestroyObject(piece.GetComponentInParent<NetworkID>().NetId);
            }
            RandomizeLevel();

            //5 seconds to look at results panel before fading out the panel
            yield return Wait(5f);
            
            Player[] players = FindObjectsOfType<Player>();
            foreach (Player player in players)
            {                
                player.SendUpdate("CAM_UNFREEZE", "");
            }

            placeLbl.enabled = false;
            SendUpdate("HIDE_PLACE", "");

            StartCoroutine(FadeScorePanelOut(1f));                                
            yield return Wait(3f);
            
            SendUpdate("COUNTDOWN", "3");
            yield return Wait(1f);
                        
            SendUpdate("COUNTDOWN", "2");
            yield return Wait(1f);
            
            SendUpdate("COUNTDOWN", "1");            
            yield return Wait(1f);

            SendUpdate("HIDE_COUNTDOWN", "");

            foreach (Player player in players)
            {
                player.playerFrozen = false;
            }

            placeLbl.enabled = true;
            SendUpdate("SHOW_PLACE", "");
        }
    }

    private void InitUI()
    {        
        gameUI = GameObject.FindGameObjectWithTag("GAME_UI");
        scorePanel = GameObject.FindGameObjectWithTag("SCORE");
        placeLbl = GameObject.FindGameObjectWithTag("PLACE").GetComponent<Text>();
        timerLbl = GameObject.FindGameObjectWithTag("TIMER").GetComponent<Text>();
        countdownLbl = GameObject.FindGameObjectWithTag("COUNTDOWN").GetComponent<Text>();

        /*Debug.Log("gameUI == null: " + (gameUI == null));
        Debug.Log("scorePanel == null: " + (scorePanel == null));
        Debug.Log("placeLbl == null: " + (placeLbl == null));
        Debug.Log("timerLbl == null: " + (timerLbl == null));
        Debug.Log("countdownLbl == null: " + (countdownLbl == null));*/
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
            InitUI();            

            while (!gameStarted)
            {
                yield return new WaitForSeconds(0.5f);
            }

            RandomizeLevel();                              

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

                GameObject temp = MyCore.NetCreateObject(Idx.ARCHER + n.CharSelected, n.Owner, spawnPos, Quaternion.identity);                
                Player player = temp.GetComponent<Player>();
                if (player == null)
                    Debug.LogWarning("player is null!!");

                player.SendUpdate("CAM_END", camEndY.ToString());                
            }
            //this doesn't work at the start of slow update for some reason
            SendUpdate("INIT_UI", "");

            GameObject ladder = MyCore.NetCreateObject(Idx.LADDER, Owner, new Vector3(-7, -3, 0), Quaternion.identity);                        

            GameObject itemBox1 = MyCore.NetCreateObject(Idx.ITEM_BOX, Owner, new Vector3(8, -7, 0), Quaternion.identity);
            GameObject itemBox2 = MyCore.NetCreateObject(Idx.ITEM_BOX, Owner, new Vector3(5, -7, 0), Quaternion.identity);

            SendUpdate("GAMESTART", "1");
            //stops server from listening, so nobody new can join.
            MyCore.NotifyGameStart();

            //this is basically our regular Update()
            while (!gameOver)
            {                       
                //yield return is blocking, so the lines after it won't run until GameUpdate finishes
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