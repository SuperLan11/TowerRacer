using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NETWORK_ENGINE;
using UnityEngine.UI;

public class PlayerController : NetworkComponent
{
    private Color[] colors;
    [System.NonSerialized] public Text PlayerName;    
    [System.NonSerialized] public int ColorSelected = -1;
    [System.NonSerialized] public string PName = "<Default>";

    public static List<string> OTHER_FLAGS = new List<string> { "POS", "VEL", "ROT", "ANG" };

    public override void HandleMessage(string flag, string value)
    {
        if(flag == "START")
        {            
            if(IsClient)
            {
                PName = value.Split(";")[0];
                GetComponentInChildren<Text>().text = PName;                
                ColorSelected = int.Parse(value.Split(";")[1]);        

                //calling a function soon after creating object sometimes runs before Start
                Start();

                GetComponent<SpriteRenderer>().color = colors[ColorSelected];
            }
            else if(IsServer)
            {
                SendUpdate("START", value);
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
        else if (!OTHER_FLAGS.Contains(flag))
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
        colors = new Color[3];
        colors[0] = new Color(255, 0, 0, 255); //red
        colors[1] = new Color(0, 0, 255, 255); //blue
        colors[2] = new Color(0, 255, 0, 255); //green
    }

    public override void NetworkedStart()
    {

    }    


    public override IEnumerator SlowUpdate()
    {
        while(IsConnected)
        { 
            if(IsServer)
            {
                if(IsDirty)
                {
                    IsDirty = false;
                }
            }
            yield return new WaitForSeconds(.1f);
        }
    }   

    // Update is called once per frame
    void Update()
    {
        if(IsClient)
        {
            Debug.Log("testing SendUpdate from PlayerController...");
            SendUpdate("DEBUG", "");
        }
    }
}
