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
using UnityEngine.SceneManagement;

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
    private GameObject npmPanel;

    private Player winningPlayer = null;        

    //the timer starts when the first player reaches the end door
    //the timer ends the round so players don't have to wait on the last player forever    
    private int roundEndTime = 30;
    private float curTimer;
    [System.NonSerialized] public bool timerStarted = false;
    [System.NonSerialized] public bool timerFinished = false;
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
        else if(flag == "HIDE_CHAR_IMAGES")
        {
            if(IsClient)
            {
                int maxOwner = int.Parse(value);                
                
                for(int i = maxOwner+1; i < 4; i++)                                    
                    scorePanel.transform.GetChild(i).gameObject.SetActive(false);                
            }
        }
        else if (flag == "FADE_IN")
        {
            if (IsClient)
            {
                float seconds = float.Parse(value);                
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
            }
        }
        else if (flag == "HIDE_COUNTDOWN")
        {
            if (IsClient)
            {
                countdownLbl.enabled = false;
            }
        }
        else if (flag == "FLASH_WIN")
        {
            if (IsClient)
            {
                int roundWinOwner = int.Parse(value.Split(";")[0]);
                int wins = int.Parse(value.Split(";")[1]);
                StartCoroutine(FlashWinPoint(roundWinOwner, wins, 5, 0.2f));
            }
        }     
        else if(flag == "HIDE_NPMS")
        {
            if(IsClient)
            {
                Debug.Log("disabling npm ui");
                npmPanel.SetActive(false);
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
        if (playersReady >= numPlayers && numPlayers >= 1)
        {
            gameStarted = true;
        }        
    }

    //called by server
    private void RandomizeLevel(int numPieces)
    {        
        for (int i = 0; i < numPieces; i++)
        {
            if(i == 0)
            {
                GameObject startPiece = MyCore.NetCreateObject(Idx.START_LEVEL_PIECE, this.Owner,
                    new Vector3(CENTER_PIECE_X, LOWEST_PIECE_Y, 0), Quaternion.identity);

                RandomlyPlaceLadders(startPiece);
                RandomlyPlaceEnemies(startPiece);
                RandomlyPlaceItemBoxes(startPiece);
                RandomlyPlaceLadders(startPiece);
                continue;
            }

            int randIdx = Random.Range(0, Idx.NUM_LEVEL_PIECES);
            if (i == numPieces - 1)
            {
                GameObject endPiece = MyCore.NetCreateObject(Idx.END_LEVEL_PIECE, this.Owner,
                    new Vector3(CENTER_PIECE_X, LOWEST_PIECE_Y + i * 15, 0), Quaternion.identity);
                /*GameObject endPiece = MyCore.NetCreateObject(Idx.END_LEVEL_PIECE, this.Owner,
                    new Vector3(CENTER_PIECE_X, LOWEST_PIECE_Y - 10, 0), Quaternion.identity);*/
                PlaceDoor(endPiece);

                camEndY = endPiece.transform.position.y;                
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
        GameObject ladder = MyCore.NetCreateObject(Idx.LADDER, Owner, ladderPlaces[randPos], Quaternion.identity);
        Vector2 dismountPos = ladder.GetComponent<Collider2D>().bounds.max;
        dismountPos.x = ladder.transform.position.x;        

        GameObject dismount = MyCore.NetCreateObject(Idx.DISMOUNT, Owner, dismountPos, Quaternion.identity);
        dismount.transform.position += new Vector3(0, dismount.GetComponent<Collider2D>().bounds.size.y, 0);

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
        
        while (curTimer > 0)
        {            
            SendUpdate("TIMER", curTimer.ToString());
            yield return new WaitForSeconds(1);
            curTimer -= 1;
            timerLbl.text = curTimer + "s";

            if (EndDoor.everyoneFinished)            
                break;
        }

        if (!EndDoor.everyoneFinished)
        {
            timerFinished = true;
            timerLbl.enabled = false;
            
            foreach(Player player in FindObjectsOfType<Player>())
            {
                player.transform.position = player.startPos;
                player.playerFrozen = true;
                player.camFrozen = true;                
                
                int place = GetPlayerPlace(player);
                player.SendUpdate("FROZEN", "");
                player.SendUpdate("CAM_FREEZE", "");
                player.SendUpdate("HIT_DOOR", place.ToString());
            }            
        }
    }

    private int GetPlayerPlace(Player player)
    {
        Player[] sortedPlayers = FindObjectsOfType<Player>();        
        for(int i = 0; i < sortedPlayers.Length; i++)
        {
            for(int j = i+1; j < sortedPlayers.Length; j++)
            {
                if(sortedPlayers[j].transform.position.y > sortedPlayers[i].transform.position.y)
                { 
                    Player temp = sortedPlayers[i];
                    sortedPlayers[i] = sortedPlayers[j];
                    sortedPlayers[j] = temp;
                }
            }
        }

        for (int i = 0; i < sortedPlayers.Length; i++)
        {
            if (sortedPlayers[i] == player)
                return i + 1;
        }

        if (player == null)
            Debug.LogWarning("Player was null!!");
        else
            Debug.LogWarning("Player place for " + player.name + " was not found");   
        
        return -1;
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

        //this sets the alpha of all the score panel elements to 0 once one is zero
        //this prevents any element from being slightly visible once the fade "finishes"
        Color finalPanelColor = scoreBackground.color;
        finalPanelColor.a = 0;
        scoreBackground.color = finalPanelColor;

        foreach (Image image in images)
        {
            Color finalColor = image.color;
            finalColor.a = 0;
            image.color = finalColor;
        }

        foreach (Text text in labels)
        {
            Color finalColor = text.color;
            finalColor.a = 0;
            text.color = finalColor;
        }
    }

    private IEnumerator FlashWinPoint(int roundWinOwner, int wins, int numFlashes, float flashTime)
    {        
        Image dotToFlash = scorePanel.transform.GetChild(roundWinOwner).GetChild(1).GetChild(wins-1).GetComponent<Image>();        

        Color32 normalColor = dotToFlash.color;
        Color32 flashColor = new Color32(255, 220, 0, 255);

        for(int i = 0; i < numFlashes; i++)
        {            
            if(i % 2 == 0)
            {
                dotToFlash.color = flashColor;
            }
            //make sure dot does not end on normal color
            else if(i % 2 == 1 && (i != numFlashes-1))
            {
                dotToFlash.color = normalColor;
            }
            yield return Wait(flashTime);
        }        
    }

    public IEnumerator ResetRound()
    {
        if(IsServer)
        {
            timerLbl.enabled = false;
            SendUpdate("HIDE_TIMER", "");
            SendUpdate("HIDE_PLACE", "");

            //yield return prevents the following lines from running until the coroutine is done
            yield return FadeScorePanelIn(1f);

            TilemapCollider2D[] pieces = FindObjectsOfType<TilemapCollider2D>();
            foreach (TilemapCollider2D piece in pieces)
            {
                MyCore.NetDestroyObject(piece.GetComponentInParent<NetworkID>().NetId);
            }
            RandomizeLevel(5);

            Player[] players = FindObjectsOfType<Player>();
            foreach (Player player in players)
            {
                if (player.isRoundWinner)
                {
                    int owner = player.Owner;
                    int wins = player.wins;
                    SendUpdate("FLASH_WIN", owner + ";" + wins);
                    player.isRoundWinner = false;
                }
            }

            //time to look at results panel before fading out the panel
            yield return Wait(3.5f);
            
            //finding one player is enough to unfreeze all instances of the camera
            FindObjectOfType<Player>().SendUpdate("CAM_UNFREEZE", "");
            foreach (Player player in players)
            {                                
                if(player.wins >= 3)
                {
                    winningPlayer = player;                                        
                    StartCoroutine(FadeScorePanelOut(1f));

                    GameManager.gameOver = true;                    
                    //cool way to exit early from a ienumerator I just learned
                    yield break;
                }
            }

            StartCoroutine(FadeScorePanelOut(1f));                                
            yield return Wait(1f);

            yield return Countdown();

            foreach (Player player in players)
            {
                player.playerFrozen = false;
            }

            placeLbl.enabled = true;
            SendUpdate("SHOW_PLACE", "");
        }
    }

    private IEnumerator Countdown()
    {
        SendUpdate("COUNTDOWN", "3");
        yield return Wait(1f);

        SendUpdate("COUNTDOWN", "2");
        yield return Wait(1f);

        SendUpdate("COUNTDOWN", "1");
        yield return Wait(1f);

        SendUpdate("HIDE_COUNTDOWN", "");
    }

    private void InitUI()
    {        
        gameUI = GameObject.FindGameObjectWithTag("GAME_UI");
        scorePanel = GameObject.FindGameObjectWithTag("SCORE");
        placeLbl = GameObject.FindGameObjectWithTag("PLACE").GetComponent<Text>();
        timerLbl = GameObject.FindGameObjectWithTag("TIMER").GetComponent<Text>();
        countdownLbl = GameObject.FindGameObjectWithTag("COUNTDOWN").GetComponent<Text>();
        npmPanel = GameObject.FindGameObjectWithTag("NPM_PANEL");

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
            //!SENDUPDATE WILL NOT WORK HERE UNTIL THE NPM'S HAVE BEEN CREATED
            //DO NOT USE SENDUPDATE UNTIL AFTER THE NPM FOREACH LOOP BELOW
            //UNLESS YOU SENDUPDATE ON THE PLAYER AS YOU CREATE IT            

            while (!gameStarted)
            {
                yield return new WaitForSeconds(0.5f);
            }
            
            RandomizeLevel(5);

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

                player.SendUpdate("CAM_END", camEndY.ToString());
                player.SendUpdate("SET_CHAR_IMAGE", "");     
            }
            //don't move this line. put additional updates after this so clients have their ui
            SendUpdate("INIT_UI", "");

            SendUpdate("HIDE_NPMS", "");            
            int maxOwner = players[players.Length - 1].Owner;
            SendUpdate("HIDE_CHAR_IMAGES", maxOwner.ToString());
            SendUpdate("INIT_UI", "");

            MyCore.NetCreateObject(Idx.ITEM_BOX, Owner, new Vector3(0f, 0f, 0f), Quaternion.identity);
            MyCore.NetCreateObject(Idx.ITEM_BOX, Owner, new Vector3(-3f, 0f, 0f), Quaternion.identity);
            MyCore.NetCreateObject(Idx.ITEM_BOX, Owner, new Vector3(3, 0f, 0f), Quaternion.identity);
            MyCore.NetCreateObject(Idx.ITEM_BOX, Owner, new Vector3(5f, 0f, 0f), Quaternion.identity);
            MyCore.NetCreateObject(Idx.ITEM_BOX, Owner, new Vector3(-5f, 0f, 0f), Quaternion.identity);

            /*GameObject ladder = MyCore.NetCreateObject(Idx.LADDER, Owner, new Vector3(-8, -3, 0), Quaternion.identity);
            GameObject itemBox1 = MyCore.NetCreateObject(Idx.ITEM_BOX, Owner, new Vector3(8, -7, 0), Quaternion.identity);
            GameObject itemBox2 = MyCore.NetCreateObject(Idx.ITEM_BOX, Owner, new Vector3(5, -7, 0), Quaternion.identity);*/

            SendUpdate("GAMESTART", "1");
            //stops server from listening, so nobody new can join.
            MyCore.NotifyGameStart();

            //this is basically our regular Update()
            while (!gameOver)
            {                       
                //yield return is blocking, so the lines after it won't run until GameUpdate finishes
                yield return GameUpdate();
            }
            
            //zooms in on player that won          
            FindObjectOfType<Player>().SendUpdate("WINNER_CAM", winningPlayer.Owner.ToString());
              
            yield return new WaitForSeconds(5f);
                        
            gameStarted = false;

            //make sure to reset all stats on game over!!!

            MyId.NotifyDirty();
            MyCore.UI_Quit();
        }
        yield return new WaitForSeconds(0.1f);
    }
}