using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using NETWORK_ENGINE;

public class GameMaster : NetworkComponent
{
    public bool gameStarted;
    public bool gameOver;
    public int numPlayers = 0;

    private Vector3[] starts = new Vector3[4];

    //if player prefabs don't start at 0 in spawn prefab array, you'll need to change this.
    public const uint offsetFromSpawnPrefabArray = 0;

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
    }

    public override void NetworkedStart()
    {

    }

    public override IEnumerator SlowUpdate()
    {
        if (IsServer)
        {
            //innocent until proven guilty! We assume the game has started, but continue the loop if it is not.
            NPM[] players;
            bool tempGameStarted = true;

            do
            {
                players = GameObject.FindObjectsOfType<NPM>();
                tempGameStarted = true;
                yield return new WaitForSeconds(0.1f);

                foreach (NPM n in players)
                {
                    if (!n.IsReady)
                    {
                        tempGameStarted = false;
                    }
                }
            } while (!tempGameStarted || players.Length < 2);

            players = GameObject.FindObjectsOfType<NPM>();
            int playerIdx = 0;
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

                GameObject temp = MyCore.NetCreateObject(0, Owner, spawnPos, Quaternion.identity);
                temp.GetComponent<PlayerController>().ColorSelected = n.ColorSelected;
                temp.GetComponent<PlayerController>().PName = n.PName;
                temp.GetComponentInChildren<Text>().text = n.PName;
                //temp.GetComponent<PlayerController>().InitChar(n.PName + ";" + n.ColorSelected);
                temp.GetComponent<PlayerController>().SendUpdate("START", n.PName + ";" + n.ColorSelected);
                playerIdx++;
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
            StartCoroutine(MyCore.DisconnectServer());
        }
        yield return new WaitForSeconds(0.1f);
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

    // Update is called once per frame
    void Update()
    {

    }
}