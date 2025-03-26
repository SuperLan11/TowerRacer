using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using NETWORK_ENGINE;

public class NPM : NetworkComponent
{
    public string PName;
    [System.NonSerialized] public bool IsReady;
    [System.NonSerialized] public int ColorSelected;
    [System.NonSerialized] public int CharSelected;

    public override void HandleMessage(string flag, string value)
    {
        if (flag == "READY")
        {
            IsReady = bool.Parse(value);

            if (IsServer)
            {
                if (IsReady)
                {
                    GameManager.AdjustReady(1);
                }
                else
                {
                    GameManager.AdjustReady(-1);
                }
                SendUpdate("READY", value);
            }
        }
        else if (flag == "NAME")
        {
            PName = value;
            if (IsServer)
            {
                SendUpdate("NAME", value);
            }
        }
        else if (flag == "COLOR")
        {
            ColorSelected = int.Parse(value);
            if (IsServer)
            {
                SendUpdate("COLOR", value);
            }
        }
        else if (flag == "CHAR")
        {
            CharSelected = int.Parse(value);
            if (IsServer)
            {
                SendUpdate("CHAR", value);
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

    }

    public override void NetworkedStart()
    {
        if (!IsLocalPlayer)
        {
            this.transform.GetChild(0).gameObject.SetActive(false);
        }
    }

    public void UI_Ready(bool r)
    {
        if (IsLocalPlayer)
        {
            SendCommand("READY", r.ToString());
        }
    }

    public void UI_NameInput(string s)
    {
        if (IsLocalPlayer)
        {
            SendCommand("NAME", s);
        }

    }
    public void UI_ColorInput(int c)
    {
        if (IsLocalPlayer)
        {
            SendCommand("COLOR", c.ToString());
        }
    }

    public void UI_CharInput(int c)
    {
        if (IsLocalPlayer)
        {
            SendCommand("CHAR", c.ToString());
        }
    }


    public override IEnumerator SlowUpdate()
    {
        while (IsConnected)
        {
            if (IsServer)
            {

                if (IsDirty)
                {
                    SendUpdate("NAME", PName);
                    SendUpdate("COLOR", ColorSelected.ToString());
                    SendUpdate("CHAR", CharSelected.ToString());

                    IsDirty = false;
                }
            }
            yield return new WaitForSeconds(.1f);
        }
    }

    // Update is called once per frame
    void Update()
    {

    }
}
