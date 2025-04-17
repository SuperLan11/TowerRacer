/*
@Authors - Landon, Patrick
@Description - General game management, and also potentially level generation
*/

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
    private List<GameObject> createdTutorials = new List<GameObject>();
    public static bool inCountdown = false;
    private Color32[] playerPanelColors;

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
    private GameObject itemSquare;
    //making these serializable cause it's multiple components on the same obj
    [SerializeField] private AudioSource theme;
    [SerializeField] public AudioSource menuTheme;      //AKA professor morning song    
    [SerializeField] private AudioSource winGameSfx;
    [SerializeField] private AudioSource countdownSfx;

    public static Player winningPlayer = null;
    public static List<Player> playersFinished = new List<Player>();
    public static bool everyoneFinished = false;
    public static bool tutorialFinished = false;

    //the timer starts when the first player reaches the end door
    //the timer ends the round so players don't have to wait on the last player forever    
    private int roundEndTime = 30;
    private float curTimer;
    [System.NonSerialized] public bool timerStarted = false;    
    private float camEndY;

    private float resultsTimer = 5f;
    private float alphaUpdateFreq = 0.01f;

    public static double levelTime;
    public static bool debugMode = true;

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
                //Debug.Log("client is getting ui...");
                InitUI();
            }
        }
        else if (flag == "HIDE_CHAR_IMAGES")
        {
            if (IsClient)
            {
                int maxOwner = int.Parse(value);

                for (int i = maxOwner + 1; i < 4; i++)
                    scorePanel.transform.GetChild(i).gameObject.SetActive(false);
            }
        }
        else if (flag == "FADE_IN")
        {
            if (IsClient)
            {
                float seconds = float.Parse(value);
                StartCoroutine(FadeScorePanelIn(seconds, 0.5f));
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
        else if (flag == "COUNTDOWN_SFX")
        {
            if (IsClient)
            {
                countdownSfx.Play();
            }
        }
        else if (flag == "FLASH_WIN")
        {
            if (IsClient)
            {
                int roundWinOwner = int.Parse(value.Split(";")[0]);
                int wins = int.Parse(value.Split(";")[1]);
                StartCoroutine(FlashWinPoint(roundWinOwner, wins, 7, 0.2f));
            }
        }
        else if (flag == "WIN_GAME_SFX")
        {
            if (IsClient)
            {
                winGameSfx.Play();
            }
        }
        else if (flag == "WINNING_PLAYER")
        {
            if (IsClient)
            {
                int winningOwner = int.Parse(value);
                foreach (Player player in FindObjectsOfType<Player>())
                {
                    if (player.Owner == winningOwner)
                        winningPlayer = player;
                }
            }
        }
        else if (flag == "HIDE_NPMS")
        {
            if (IsClient)
            {
                npmPanel.SetActive(false);
            }
        }
        else if (flag == "HIDE_ITEM")
        {
            if (IsClient)
            {
                Color hiddenColor = itemSquare.GetComponent<Image>().color;
                hiddenColor.a = 0;
                itemSquare.GetComponent<Image>().color = hiddenColor;
                itemSquare.transform.GetChild(0).GetComponent<Image>().color = hiddenColor;
            }
        }
        else if (flag == "SHOW_ITEM")
        {
            if (IsClient)
            {
                Color visibleColor = itemSquare.GetComponent<Image>().color;
                Color fullItemColor = itemSquare.transform.GetChild(0).GetComponent<Image>().color;
                Color invisibleItemColor = itemSquare.transform.GetChild(0).GetComponent<Image>().color;

                visibleColor.a = 0.5f;
                fullItemColor.a = 1f;
                invisibleItemColor.a = 0f;

                itemSquare.GetComponent<Image>().color = visibleColor;
                itemSquare.transform.GetChild(0).GetComponent<Image>().color = fullItemColor;

                foreach (Player player in FindObjectsOfType<Player>())
                {
                    bool hasItem = player.hasBomb || player.hasChicken || player.hasSpeedBoost;
                    if (player.IsLocalPlayer && !hasItem)
                        itemSquare.transform.GetChild(0).GetComponent<Image>().color = invisibleItemColor;
                }
            }
        }
        else if (flag == "PLAY_THEME")
        {
            if (IsClient)
            {
                theme.Play();
            }
        }
        else if (flag == "STOP_THEME")
        {
            if (IsClient)
            {
                theme.Stop();
            }
        }
        /*else if (flag == "PLAY_MENU_THEME")
        {
            
            //needs to be local player cause it starts asynchronously
            if (IsLocalPlayer){
                menuTheme.Play();
            }
        }*/
        else if (flag == "STOP_MENU_THEME")
        {
            if (IsClient)
            {
                menuTheme.Stop();
            }
        }
        else if (flag == "CLEAR_ITEM")
        {
            if (IsClient)
            {
                if (itemSquare.transform.childCount > 0)
                {
                    Color invisibleColor = itemSquare.transform.GetChild(0).GetComponent<Image>().color;
                    invisibleColor.a = 0;
                    itemSquare.transform.GetChild(0).GetComponent<Image>().color = invisibleColor;
                }
            }
        }
        else if (flag == "CURSOR_VISIBLE")
        {
            if (IsClient)
            {
                Cursor.visible = bool.Parse(value);
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

        playerPanelColors = new Color32[4];
        playerPanelColors[0] = new Color32(255, 220, 0, 255); //gold for first
        playerPanelColors[1] = new Color32(148, 148, 148, 255); //silver for second
        playerPanelColors[2] = new Color32(196, 132, 0, 255); //bronze for third
        playerPanelColors[3] = new Color32(255, 255, 255, 255); //white for fourth
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
    
    //chance to place is from 0 to 1 and represents the chance a tagged object will have that object placed
    private void RandomizeLevel(int numPieces)
    {        
        for (int i = 0; i < numPieces; i++)
        {
            if(i == 0)
            {
                GameObject startPiece = MyCore.NetCreateObject(Idx.START_LEVEL_PIECE, this.Owner,
                    new Vector3(CENTER_PIECE_X, LOWEST_PIECE_Y, 0), Quaternion.identity);

                RandomlyPlaceRopes(startPiece, 100);
                RandomlyPlaceEnemies(startPiece, 100);
                RandomlyPlaceItemBoxes(startPiece, 100);
                RandomlyPlaceLadders(startPiece, 100);
                continue;
            }

            int randIdx = Random.Range(0, Idx.NUM_LEVEL_PIECES);
            if (i == numPieces - 1)
            {
                GameObject endPiece = MyCore.NetCreateObject(Idx.END_LEVEL_PIECE, this.Owner,
                    new Vector3(CENTER_PIECE_X, LOWEST_PIECE_Y + i * 15, 0), Quaternion.identity);                
                PlaceDoor(endPiece);

                camEndY = endPiece.transform.position.y;                
                break;
            }
                      
            GameObject piece = MyCore.NetCreateObject(Idx.FIRST_LEVEL_PIECE_IDX + randIdx, this.Owner,
                new Vector3(CENTER_PIECE_X, LOWEST_PIECE_Y + i * 15, 0), Quaternion.identity);

            RandomlyPlaceRopes(piece, 100);
            RandomlyPlaceEnemies(piece, 100);
            RandomlyPlaceItemBoxes(piece, 100);
            RandomlyPlaceLadders(piece, 100);
        }        
    }

    private GameObject GetFloorPiece(GameObject piece)
    {
        for(int i = 0; i < piece.transform.childCount; i++)
        {
            if (piece.transform.GetChild(i).tag == "FLOOR")
                return piece.transform.GetChild(i).gameObject;
        }
        return null;
    }

    private void CreateTutorials()
    {
        NPM[] npms = FindObjectsOfType<NPM>();
        List<NPM> npmList = new List<NPM>(npms);
        int minOwner = 0;
        int tutorialsPlaced = 0;

        for(int i = 0; i < npms.Length; i++)
        { 
            for(int j = 0; j < npms.Length; j++)
            {
                //create tutorial prefabs in order of connection count
                if (npmList[j].Owner == minOwner)
                {
                    Vector2 pos = new Vector2(-35, 0);

                    if (createdTutorials.Count > 0)
                    {
                        GameObject prevPiece = createdTutorials[createdTutorials.Count - 1];                                                
                        pos = new Vector2(prevPiece.transform.position.x + 23f, 0);
                    }                    

                    //create appropriate tutorial for character chosen
                    GameObject tutorial = MyCore.NetCreateObject(Idx.ARCHER_TUTORIAL + npms[j].CharSelected, Owner, pos, Quaternion.identity);
                    createdTutorials.Add(tutorial);
                    Debug.Log("created " + tutorial.name);
                    PlaceDoor(tutorial);

                    tutorialsPlaced++;
                    minOwner++;                    
                    //don't remove player from list or you get concurrency error
                }
            }                   
        }                
    }

    private void MovePlayersToTutorial()
    {
        Player[] players = FindObjectsOfType<Player>();
        int minOwner = 0;
        for (int i = 0; i < players.Length; i++)
        {
            for (int j = 0; j < players.Length; j++)
            {
                //create tutorial prefabs in order of connection count
                if (players[j].Owner == minOwner)
                {
                    //startPos needs to be first child of tutorial prefab for this to work
                    Vector2 startPos = createdTutorials[minOwner].transform.GetChild(0).transform.position;
                    players[j].transform.position = startPos;
                    Debug.Log("moved " + players[j].name + " to " + startPos);
                    minOwner++;
                }
            }
        }
    }

    private void PlaceDoor(GameObject endPiece)
    {        
        for(int i = 0; i < endPiece.transform.childCount; i++)
        {
            if(endPiece.transform.GetChild(i).tag == "END_DOOR_POS")            
                MyCore.NetCreateObject(Idx.END_DOOR, Owner, endPiece.transform.GetChild(i).transform.position, Quaternion.identity);
        }
    }

    private void RandomlyPlaceRopes(GameObject levelPiece, int chanceToPlace)
    {        
        for(int i = 0; i < levelPiece.transform.childCount; i++)
        {
            int randomizedChance = Random.Range(0, 101);
            bool gotChance = randomizedChance <= chanceToPlace;
            GameObject child = levelPiece.transform.GetChild(i).gameObject;

            if (child.tag == "ROPE_POS" && gotChance)
                MyCore.NetCreateObject(Idx.ROPE, Owner, child.transform.position, Quaternion.identity);
        }   
    }

    private void RandomlyPlaceEnemies(GameObject levelPiece, int chanceToPlace)
    {        
        for (int i = 0; i < levelPiece.transform.childCount; i++)
        {
            int randomizedChance = Random.Range(0, 101);
            bool gotChance = randomizedChance <= chanceToPlace;
            GameObject child = levelPiece.transform.GetChild(i).gameObject;

            if (child.tag == "ENEMY_POS" && gotChance)
            {                                
                int randEnemy = Random.Range(Idx.FIRST_ENEMY_IDX, Idx.FIRST_ENEMY_IDX + Idx.NUM_ENEMIES);
                MyCore.NetCreateObject(randEnemy, Owner, child.transform.position, Quaternion.identity);
            }
        }
    }

    private void RandomlyPlaceItemBoxes(GameObject levelPiece, int chanceToPlace)
    {       
        for (int i = 0; i < levelPiece.transform.childCount; i++)
        {
            int randomizedChance = Random.Range(0, 101);
            bool gotChance = randomizedChance <= chanceToPlace;
            GameObject child = levelPiece.transform.GetChild(i).gameObject;

            if (child.tag == "ITEM_POS" && gotChance)
                MyCore.NetCreateObject(Idx.ITEM_BOX, Owner, child.transform.position, Quaternion.identity);
        }
    }

    private void RandomlyPlaceLadders(GameObject levelPiece, float chanceToPlace)
    {
        for (int i = 0; i < levelPiece.transform.childCount; i++)
        {
            int randomizedChance = Random.Range(0, 101);
            bool gotChance = randomizedChance <= chanceToPlace;
            GameObject child = levelPiece.transform.GetChild(i).gameObject;

            if (child.tag == "LADDER_POS" && gotChance)
            {
                GameObject ladder = MyCore.NetCreateObject(Idx.LADDER, Owner, child.transform.position, Quaternion.identity);

                Vector2 dismountPos = ladder.GetComponent<Collider2D>().bounds.max;
                dismountPos.x = ladder.transform.position.x;
                GameObject dismount = MyCore.NetCreateObject(Idx.DISMOUNT, Owner, dismountPos, Quaternion.identity);
                dismount.transform.position += new Vector3(0, dismount.GetComponent<Collider2D>().bounds.size.y, 0);
            }
        }
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
        List<Player> unfinishedPlayers = new List<Player>();

        //only update places for unfinished players
        foreach(Player player in players)
        {
            if (!playersFinished.Contains(player))
                unfinishedPlayers.Add(player);
        }
        int[] playerIdxsByHeight = new int[unfinishedPlayers.Count];

        for(int i = 0; i < unfinishedPlayers.Count; i++)
        {
            playerIdxsByHeight[i] = i;
        }        

        //selection sort to rank players by height descending
        for(int i = 0; i < playerIdxsByHeight.Length; i++)
        {
            float playerY1 = unfinishedPlayers[i].transform.position.y;
            for (int j = i + 1; j < unfinishedPlayers.Count; j++)
            {
                float playerY2 = unfinishedPlayers[j].transform.position.y;
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
            //account for finished players when determining the place            
            int place = playersFinished.Count + (i + 1);
            //Debug.Log("unfinished players: " + unfinishedPlayers.Count + ", place for " + unfinishedPlayers[playerIdx].name + ", " + place);
            unfinishedPlayers[playerIdx].SendUpdate("PLACE", place.ToString());
        }
    }

    //called by EndDoor script
    public IEnumerator StartTimer()
    {
        timerLbl.enabled = true;
        SendUpdate("SHOW_TIMER", "");        
        timerStarted = true;        
        
        while (curTimer > 0 && !everyoneFinished)
        {            
            SendUpdate("TIMER", curTimer.ToString());
            //be careful of this 1 second timer. will not check if everyoneFinished for 1 second intervals
            yield return new WaitForSeconds(1f);
            curTimer -= 1;
            timerLbl.text = curTimer + "s";                                                        
        }

        //reset timer for next round        
        timerStarted = false;
        curTimer = roundEndTime;        

        if (everyoneFinished)
            yield break;

        //means timer ended and will claim some victims
        foreach(Player player in FindObjectsOfType<Player>())
        {
            if (playersFinished.Contains(player))
                continue;

            //only need to run this code for unfinished players
            player.transform.position = player.startPos;
            player.playerFrozen = true;
            player.camFrozen = true;                
                                
            player.SendUpdate("TIMED_OUT", "");
            player.SendUpdate("CAM_FREEZE", "");
        }
        StartCoroutine(ResetRound());        
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

    private IEnumerator FadeScorePanelIn(float seconds, float finalPanelOpacity)
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
                
                if (image.tag == "PLAYER_PANEL")                
                    newColor.a += (finalPanelOpacity)*alphaUpdateFreq / seconds;
                else
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

        //this sets the alpha of all the score panel elements to 1 once one is 1
        //this prevents certain elements from being slightly transparent once the fade "finishes"
        Color finalPanelColor = scoreBackground.color;
        finalPanelColor.a = 1;
        scoreBackground.color = finalPanelColor;

        foreach (Image image in images)
        {
            Color finalColor = image.color;
            if (image.tag == "PLAYER_PANEL")
                finalColor.a = finalPanelOpacity;
            else
                finalColor.a = 1;

            image.color = finalColor;
        }

        foreach (Text text in labels)
        {
            Color finalColor = text.color;
            finalColor.a = 1;
            text.color = finalColor;
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
            timerStarted = false;
            curTimer = roundEndTime;            
            SendUpdate("HIDE_TIMER", "");
            SendUpdate("HIDE_PLACE", "");
            SendUpdate("HIDE_ITEM", "");
            SendUpdate("STOP_THEME", "GoodMorning");

            DestroyAllEnemies();

            //yield return prevents the following lines from running until the coroutine is done
            yield return FadeScorePanelIn(1f, 0.5f);

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
                    player.SendUpdate("WIN_ROUND_SFX", "");                    
                    SendUpdate("FLASH_WIN", owner + ";" + wins);
                    player.isRoundWinner = false;
                }                
            }

            //time to look at results panel before fading out the panel
            yield return Wait(3.5f);
                        
            foreach(Player player in players)
            {
                player.SendUpdate("CAM_UNFREEZE", "");
            }
            
            foreach (Player player in players)
            {                                
                if(player.wins >= 3)
                {
                    SendUpdate("WIN_GAME_SFX", "");
                    winningPlayer = player;                    
                    StartCoroutine(FadeScorePanelOut(1f));

                    GameManager.gameOver = true;                    
                    //cool way to exit early from a ienumerator I just learned
                    //this breaks out of the entire coroutine, not just the for loop
                    yield break;
                }
            }

            StartCoroutine(FadeScorePanelOut(1f));
            yield return Wait(1f);

            yield return Countdown();

            foreach (Player player in players){            
                player.playerFrozen = false;
                player.SendUpdate("ENABLE_TRAIL", "GoodMorning");
            } 

            SendUpdate("PLAY_THEME", "GoodMorning");         

            placeLbl.enabled = true;
            SendUpdate("SHOW_PLACE", "");
            SendUpdate("SHOW_ITEM", "");

            //wait to do this so that players forced to finish by the timer have the correct background color
            //and so that the timer resets correctly            
            playersFinished.Clear();
            everyoneFinished = false;
        }
    }

    private IEnumerator Countdown()
    {
        inCountdown = true;
        
        SendUpdate("COUNTDOWN_SFX", "");

        SendUpdate("COUNTDOWN", "3");
        yield return Wait(1f);

        SendUpdate("COUNTDOWN", "2");
        yield return Wait(1f);

        SendUpdate("COUNTDOWN", "1");
        yield return Wait(1f);

        SendUpdate("HIDE_COUNTDOWN", "");

        inCountdown = false;
    }

    private void DestroyAllEnemies()
    {
        Enemy[] enemies = FindObjectsOfType<Enemy>();
        for(int i = enemies.Length-1; i >= 0; i--)
        {
            MyCore.NetDestroyObject(enemies[i].NetId);
        }
    }

    private void MovePlayersToRound()
    {
        foreach (Player player in FindObjectsOfType<Player>())
        {
            switch (player.Owner)
            {
                case 0:
                    player.transform.position = starts[0];
                    break;
                case 1:
                    player.transform.position = starts[1];
                    break;
                case 2:
                    player.transform.position = starts[2];
                    break;
                case 3:
                    player.transform.position = starts[3];
                    break;
            }
        }
    }

    private void InitUI()
    {        
        gameUI = GameObject.FindGameObjectWithTag("GAME_UI");
        scorePanel = GameObject.FindGameObjectWithTag("SCORE");
        placeLbl = GameObject.FindGameObjectWithTag("PLACE").GetComponent<Text>();
        timerLbl = GameObject.FindGameObjectWithTag("TIMER").GetComponent<Text>();
        countdownLbl = GameObject.FindGameObjectWithTag("COUNTDOWN").GetComponent<Text>();
        npmPanel = GameObject.FindGameObjectWithTag("NPM_PANEL");
        itemSquare = GameObject.FindGameObjectWithTag("ITEM_UI");

        /*Debug.Log("gameUI == null: " + (gameUI == null));
        Debug.Log("scorePanel == null: " + (scorePanel == null));
        Debug.Log("placeLbl == null: " + (placeLbl == null));
        Debug.Log("timerLbl == null: " + (timerLbl == null));
        Debug.Log("countdownLbl == null: " + (countdownLbl == null));*/
    }

    private int GetMaxOwner()
    {
        NPM[] npms = FindObjectsOfType<NPM>();
        int maxOwner = -1;
        for(int i = 0; i < npms.Length; i++)
        {
            if(npms[i].Owner > maxOwner)
            {
                maxOwner = npms[i].Owner;
            }    
        }
        return maxOwner;
    }

    private void ResetVariables()
    {
        gameStarted = false;
        gameOver = false;

        timerStarted = false;
        curTimer = roundEndTime;

        playersReady = 0;
        everyoneFinished = false;
        winningPlayer = null;
        playersFinished.Clear();
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
            //CreateTutorials();            

            NPM[] players = GameObject.FindObjectsOfType<NPM>();
            foreach (NPM n in players)
            {                           
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
                player.playerName = n.PName;

                player.SendUpdate("INIT_NAME", n.PName);

                //for testing
                if (camEndY == 0)
                    camEndY = Mathf.Infinity;

                player.SendUpdate("CAM_END", camEndY.ToString());
                player.SendUpdate("INIT_SCORE_PANEL", "");                
            }
            //MovePlayersToTutorial();
            //
            //don't move this line. put additional updates after this so clients have their ui
            SendUpdate("INIT_UI", "");
            
            int maxOwner = GetMaxOwner();
            SendUpdate("HIDE_CHAR_IMAGES", maxOwner.ToString());

            MyCore.NetCreateObject(Idx.ITEM_BOX, Owner, new Vector3(0f, 0f, 0f), Quaternion.identity);
            MyCore.NetCreateObject(Idx.ITEM_BOX, Owner, new Vector3(-3f, 0f, 0f), Quaternion.identity);
            MyCore.NetCreateObject(Idx.ITEM_BOX, Owner, new Vector3(3, 0f, 0f), Quaternion.identity);
            MyCore.NetCreateObject(Idx.ITEM_BOX, Owner, new Vector3(5f, 0f, 0f), Quaternion.identity);
            MyCore.NetCreateObject(Idx.ITEM_BOX, Owner, new Vector3(-5f, 0f, 0f), Quaternion.identity);

            SendUpdate("GAMESTART", "1");
            //stops server from listening, so nobody new can join.
            MyCore.NotifyGameStart();
            SendUpdate("STOP_MENU_THEME", "GoodMorning");            
            SendUpdate("HIDE_PLACE", "");

            /*while(!tutorialFinished)
            {
                yield return new WaitForSeconds(0.5f);
            }
            playersFinished.Clear();

            SendUpdate("STOP_THEME", "");
            SendUpdate("FADE_IN", "1");

            yield return Wait(1f);
            
            //destroy tutorials in reverse to avoid concurrency issues
            for(int i = createdTutorials.Count-1; i >= 0; i--)
            {
                int netID = createdTutorials[i].GetComponentInParent<NetworkID>().NetId;
                MyCore.NetDestroyObject(netID);
            }*/

            //RandomizeLevel(5);
            //SendUpdate("FADE_OUT", "1");
            //MovePlayersToRound();
            
            //so that the players don't see the default skybox the instant the npm panel disappears
            yield return Wait(0.5f);
            SendUpdate("HIDE_NPMS", "");
            yield return Wait(0.5f);

            foreach (Player player in FindObjectsOfType<Player>())
            {
                player.playerFrozen = true;
            }

            yield return Countdown();            

            SendUpdate("PLAY_THEME", "GoodMorning");
            SendUpdate("SHOW_PLACE", "");

            foreach (Player player in FindObjectsOfType<Player>())
            {
                player.playerFrozen = false;
            }            

            //this is basically our regular Update()
            while (!gameOver)
            {                
                //yield return is blocking, so the lines after it won't run until GameUpdate finishes
                yield return GameUpdate();
            }

            //zooms in on player that won
            foreach (Player player in FindObjectsOfType<Player>())
            {
                player.SendUpdate("WINNING_PLAYER", winningPlayer.Owner.ToString());
            }

            yield return new WaitForSeconds(4f);

            //make sure to reset all stats on game over!!!
            ResetVariables();
            SendUpdate("CLEAR_ITEM", "");
            SendUpdate("CURSOR_VISIBLE", true.ToString());

            MyId.NotifyDirty();
            MyCore.UI_Quit();
        }
        yield return new WaitForSeconds(0.1f);
    }
}