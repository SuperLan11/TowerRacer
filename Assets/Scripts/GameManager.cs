using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using NETWORK_ENGINE;

public class GameManager : NetworkComponent
{
    //[System.NonSerialized] public bool gameStarted;
    [System.NonSerialized] public bool gameOver;
    public static int playersReady = 0;
    private static bool gameStarted = false;

    private Vector3[] starts = new Vector3[4];

    //if player prefabs don't start at 0 in spawn prefab array, you'll need to change this.
    public const uint offsetFromSpawnPrefabArray = 0;

    public override void HandleMessage(string flag, string value)
    {
        if (IsServer)
        {
            Debug.Log("server got flag " + flag + " in " + this.GetType().Name);
        }

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
        else if (flag == "DEBUG")
        {
            Debug.Log(value);
            if (IsClient)
            {
                SendCommand(flag, value);
            }
        }
        else
        {
            Debug.LogWarning(flag + " is not a valid flag in " + this.GetType().Name + ".cs");
            if (IsClient)
            {
                SendCommand(flag, value);
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

    }

    // I'd like to only change static variables through wrapper functions
    // so we can debug in the function to see its value no matter when or
    // where we change the variable
    public static void AdjustReady(int change)
    {
        playersReady += change;
        int numPlayers = FindObjectsOfType<NPM>().Length;
        if (playersReady >= numPlayers && numPlayers > 0)
        {
            gameStarted = true;
        }
        //Debug.Log("num players ready: " + playersReady);
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

            SendUpdate("GAMESTART", "1");
            //stops server from listening, so nobody new can join.
            MyCore.NotifyGameStart();

            while (!gameOver)
            {
                //game is playing
                //turn-based logic
                //maintain score
                //maintain metrics
                yield return new WaitForSeconds(0.1f);
            }
            //wait until game ends...
            SendUpdate("GAMEOVER", "");
            //disable controls or delete player
            //Show scores
            yield return new WaitForSeconds(30f);

            //MyId.NotifyDirty();

            //StartCoroutine(MyCore.DisconnectServer());
            MyCore.UI_Quit();
        }
        yield return new WaitForSeconds(0.1f);
    }

    // Update is called once per frame
    void Update()
    {

    }
}