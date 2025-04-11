using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using NETWORK_ENGINE;

using TMPro;

public class NPM : NetworkComponent
{
    public string PName;
    [System.NonSerialized] public bool IsReady;
    [System.NonSerialized] public int ColorSelected;
    [System.NonSerialized] public int CharSelected;

    public GameObject npmPanel;
    private InputField nameField;
    private Toggle readyToggle;
    private TMP_Dropdown charDropdown;

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
        else if(flag == "SHOW_NPM")
        {
            if(IsClient)
            {
                Debug.Log("the owner receiving the npm update is: " + this.Owner);
                //this includes showing the current npm panel

                NPM[] npms = FindObjectsOfType<NPM>();
                for (int i = 0; i < npms.Length; i++)
                {
                    ShowNPM(npms[i].npmPanel);
                }

                if (!IsLocalPlayer)
                {
                    Debug.Log("disabling ui for owner " + this.Owner);
                    DisableNpmUI(this.npmPanel);
                }
                else
                {
                    Debug.Log("owner " + this.Owner + " is not local player");
                }

                //one player can edit the last two of three players' settings
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
        //owner is set as soon as npm spawns, so you can access it before networked start
        string npmStr = "NPM" + Owner;
        npmPanel = GameObject.Find(npmStr);        
        npmPanel.GetComponentInChildren<Toggle>().onValueChanged.AddListener(UI_Ready);
        nameField = npmPanel.GetComponentInChildren<InputField>();
        readyToggle = npmPanel.GetComponentInChildren<Toggle>();        
    }

    public override void NetworkedStart()
    {
        if (IsServer)
        {
            foreach (NPM npm in FindObjectsOfType<NPM>())
                npm.SendUpdate("SHOW_NPM", "");
        }
    }

    private void DisableNpmUI(GameObject panel)
    {
        TMP_Dropdown dropdown = panel.GetComponentInChildren<TMP_Dropdown>();
        InputField nameField = panel.GetComponentInChildren<InputField>();
        Toggle ready = panel.GetComponentInChildren<Toggle>();

        dropdown.interactable = false;
        nameField.interactable = false;
        ready.interactable = false;
    }

    private void ShowNPM(GameObject panel)
    {
        Image[] images = panel.GetComponentsInChildren<Image>();
        TextMeshProUGUI[] labels = panel.GetComponentsInChildren<TextMeshProUGUI>();
        Text[] texts = panel.GetComponentsInChildren<Text>();

        foreach (Image image in images)
        {
            Color newColor = image.color;
            newColor.a = 1;
            image.color = newColor;
        }

        foreach (TextMeshProUGUI lbl in labels)
        {
            lbl.enabled = true;
            //these lines are for the placeholder labels as they are always enabled,
            //so you need to change alpha instead of just enabling it
            Color newColor = lbl.color;
            newColor.a = 1;
            lbl.color = newColor;
        }

        foreach (Text text in texts)
        {
            if (text.name.Contains("Placeholder"))
            {
                Color halfColor = text.color;
                halfColor.a = 0.5f;
                text.color = halfColor;
            }
            else
            {
                text.enabled = true;
                Color fullColor = text.color;
                fullColor.a = 1f;
                text.color = fullColor;
            }            
        }
    }    

    public void UI_Ready(bool r)
    {
        if (IsLocalPlayer)
        {
            Debug.Log("local player changed ready!");
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
