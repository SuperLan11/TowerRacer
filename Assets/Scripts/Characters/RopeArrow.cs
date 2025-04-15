/*
@Authors - Patrick
@Description - Rope arrow is in motion by default, and will stop being in motion when coming in contact with anything
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using NETWORK_ENGINE;

public class RopeArrow : NetworkComponent
{
    //?change this to line renderer index?
    //private const int ROPE_SPAWN_PREFAB_INDEX = Idx.ROPE;
    
    //may want to change this to default rope jump SFX
    private AudioSource spawnRopeSfx;
    [System.NonSerialized] public Player currentPlayer;
    public LineRenderer lineRenderer;

    //passed from server to client every frame cause I can't think of a better way to do it
    public Vector3 mostRecentPlayerPos;

    //wait a fraction of a second to prevent jank
    private const float JANK_LINE_WAIT_TIME = 0.01f;
    private bool inJankCooldown;

    
    public override void HandleMessage(string flag, string value)
    {
        if(flag == "HIDE")
        {
            if(IsClient)
            {
                spawnRopeSfx.Play();
                GetComponent<SpriteRenderer>().enabled = false;
                GetComponent<Collider2D>().enabled = false;
            }
        }else if (flag == "SET_PLAYER_POS"){
            if (IsClient){
                mostRecentPlayerPos = NetworkCore.Vector3FromString(value);
            }
        }
    }

    private void Start()
    {
        spawnRopeSfx = GetComponent<AudioSource>();
    }

    public override void NetworkedStart()
    {
        GetComponent<Rigidbody2D>().gravityScale = 0f;
        //lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.positionCount = 2;


        if (IsClient){
            inJankCooldown = false;
            lineRenderer.enabled = false;
            StartCoroutine(Waiting());
        }else if (IsServer){
            lineRenderer.enabled = true;
        }
    }

    private IEnumerator Waiting(){
        yield return new WaitForSecondsRealtime(JANK_LINE_WAIT_TIME);

        inJankCooldown = false;
        lineRenderer.enabled = true;
    }

    private void DrawRope(Vector3 pos1, Vector3 pos2){
        if (lineRenderer.positionCount < 2){
            return;
        }

        lineRenderer.SetPosition(0, pos1);
        lineRenderer.SetPosition(1, pos2);
    }

    public override IEnumerator SlowUpdate(){
        while (IsConnected){
            yield return new WaitForSeconds(MyCore.MasterTimer);
        }
    }

    /*
    void OnCollisionEnter2D(Collision2D collision){
        if (IsServer){
            GetComponent<Rigidbody2D>().velocity = Vector2.zero;

            MyCore.NetCreateObject(ROPE_SPAWN_PREFAB_INDEX, Owner, this.transform.position, Quaternion.identity);
            MyCore.NetDestroyObject(this.NetId);
        }
    }
    */

    private IEnumerator DestroyAfterSfx()
    {
        while(spawnRopeSfx.isPlaying)
        {
            yield return new WaitForSeconds(0.1f);
        }

        MyCore.NetDestroyObject(this.NetId);
    }

    void OnTriggerEnter2D (Collider2D collider){
        if (IsServer){            
            currentPlayer.archerArrowHitPosition = this.transform;
            currentPlayer.archerArrowHitObj = true;
            
            GetComponent<Rigidbody2D>().velocity = Vector2.zero;
            GetComponent<SpriteRenderer>().enabled = false;
            GetComponent<Collider2D>().enabled = false;
            SendUpdate("HIDE", "");

            spawnRopeSfx.Play();
            StartCoroutine(DestroyAfterSfx());
        }
    }

    void Update(){
        if (IsServer){
            DrawRope(this.transform.position, currentPlayer.transform.position);
            SendUpdate("SET_PLAYER_POS", currentPlayer.transform.position.ToString());
        }else if (IsClient && !inJankCooldown){
            DrawRope(this.transform.position, mostRecentPlayerPos);
        }
    }
}