using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using NETWORK_ENGINE;

using TMPro;

public class NPM : NetworkComponent
{
    [System.NonSerialized] public string PName = "";
    [System.NonSerialized] public bool IsReady = false;
    [System.NonSerialized] public int ColorSelected = 0;
    [System.NonSerialized] public int CharSelected = 0;

    [System.NonSerialized] public GameObject npmPanel;
    [System.NonSerialized] public InputField nameField;
    [System.NonSerialized] public Toggle readyToggle;
    [System.NonSerialized] public TMP_Dropdown charDropdown;
    [System.NonSerialized] public Image charImage;

    public Sprite[] heroSprites;

    [SerializeField] private AudioSource readySfx;
    [SerializeField] private AudioSource allReadySfx;

    /*
     * NOTE TO JACOB OR ANYONE ELSE DOING UI
     * The npm is not the panel you see in the scene. 
     * It is an empty game object that is linked to its panel 
     * so that the ui panels can be anchored properly without spawning in prefabs
     * */

    public override void HandleMessage(string flag, string value)
    {        
        if (flag == "READY")
        {
            IsReady = bool.Parse(value);            
            readyToggle.isOn = IsReady;                        

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

                //clients will hear the ready sfx of other players
                if (GameManager.playersReady == FindObjectsOfType<NPM>().Length)
                    SendUpdate("ALL_READY_SFX", "");
                else
                    SendUpdate("READY_SFX", "");
            }
        }
        else if(flag == "READY_SFX")
        {
            if(IsClient)
            {
                readySfx.Play();
            }
        }
        else if(flag == "ALL_READY_SFX")
        {
            if(IsClient)
            {
                allReadySfx.Play();
            }
        }
        else if (flag == "NAME")
        {
            PName = value;            
            nameField.text = value;            

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
            charDropdown.value = CharSelected;
            charImage.sprite = heroSprites[CharSelected];

            if (IsServer)
            {
                SendUpdate("CHAR", value);
            }
        }
        else if(flag == "SHOW_NPM")
        {
            if(IsClient)
            {
                //this includes showing the current npm panel
                NPM[] npms = FindObjectsOfType<NPM>();
                for (int i = 0; i < npms.Length; i++)
                {
                    ShowNPM(npms[i].npmPanel);
                }                

                //sometimes doesn't work, so see coroutine lower in this same file
                if(!IsLocalPlayer)
                {
                    SendCommand("DEBUG", this.npmPanel + " would have been disabled for owner " + this.Owner);
                    //DisableNpmUI(this.npmPanel);
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
        //owner is set as soon as npm spawns, so you can access it before networked start
        string npmStr = "NPM" + Owner;
        npmPanel = GameObject.Find(npmStr);        
        
        nameField = npmPanel.GetComponentInChildren<InputField>();
        readyToggle = npmPanel.GetComponentInChildren<Toggle>();
        charDropdown = npmPanel.GetComponentInChildren<TMP_Dropdown>();
        charImage = npmPanel.transform.GetChild(3).GetComponent<Image>();

        //dynamically assign ui events so you can have anchored npm objects in the scene
        nameField.onValueChanged.AddListener(UI_NameChanged);
        readyToggle.onValueChanged.AddListener(UI_Ready);
        charDropdown.onValueChanged.AddListener(UI_CharInput);
    }    

    public override void NetworkedStart()
    {
        //StartCoroutine(WaitToUpdateUI(0.5f));        

        if(IsServer)
            SendUpdate("SHOW_NPM", "");
    }

    //sometimes IsLocalPlayer on the npm isn't set when NetworkedStart runs (which is weird)
    //so this makes sure the ui is updated after a delay in case it is set late
    private IEnumerator WaitToUpdateUI(float seconds)
    {
        yield return new WaitForSeconds(seconds);

        if (!this.npmPanel.activeInHierarchy)
        {
            StopAllCoroutines();
            yield break;
        }

        if (!IsLocalPlayer)
        {
            //Debug.Log("disable ui");
            DisableNpmUI(this.npmPanel);
        }
        else
        {
            //Debug.Log("enable local ui");
            ShowNPM(this.npmPanel);
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

        Toggle toggle = panel.GetComponentInChildren<Toggle>();
        TMP_Dropdown dropdown = panel.GetComponentInChildren<TMP_Dropdown>();
        InputField name = panel.GetComponentInChildren<InputField>();

        toggle.enabled = true;
        dropdown.enabled = true;
        name.enabled = true;

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

    public void UI_NameChanged(string s)
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
                    SendUpdate("READY", IsReady.ToString());                    

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
