using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using NETWORK_ENGINE;

using TMPro;
using UnityEngine.Video;

public class NPM : NetworkComponent
{
    [System.NonSerialized] public string PName = "";
    [System.NonSerialized] public bool IsReady = false;    
    [System.NonSerialized] public int CharSelected = 0;

    [System.NonSerialized] public GameObject npmPanel;
    [System.NonSerialized] public InputField nameField;
    [System.NonSerialized] public Toggle readyToggle;
    [System.NonSerialized] public TMP_Dropdown charDropdown;
    [System.NonSerialized] public Image charImage;
    [System.NonSerialized] public Image abilityImage;
    //[System.NonSerialized] public VideoPlayer abilityImage;

    public Sprite[] heroSprites;
    public Sprite[] abilitySprites;
    //public VideoClip[] abilitySprites;

    [SerializeField] private AudioSource readySfx;
    [SerializeField] private AudioSource allReadySfx;

    //client-side variable for overriding late localplayer assignments
    private bool panelEnabled = true;
    private bool ranStart = false;
    private GameManager gm;

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
            //handle message is sometimes dumb and runs before Start()
            //so need to run Start() manually to assign UI vars
            Start();

            IsReady = bool.Parse(value);            
            readyToggle.isOn = IsReady;                        

            if (IsServer)
            {
                if (IsReady)
                {
                    gm.AdjustReady(1);             
                }
                else
                {
                    gm.AdjustReady(-1);
                }
                SendUpdate("READY", value);

                //clients will hear the ready sfx of other players
                if (gm.playersReady == FindObjectsOfType<NPM>().Length)
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
            Start();

            PName = value;
            
            //to prevent assigning text to the same text box, which causes intereference when typing
            if(!IsLocalPlayer)
                nameField.text = value;

            if (IsServer)
            {
                SendUpdate("NAME", value);
            }
        }        
        else if (flag == "CHAR")
        {            
            Start();            

            CharSelected = int.Parse(value);            
            charDropdown.value = CharSelected;
            charImage.sprite = heroSprites[CharSelected];
            abilityImage.sprite = abilitySprites[CharSelected];   
            //WARNING. this was causing the game to never advance to the next round
            //because the video wasn't setup correctly I think
            //abilityImage.clip = abilitySprites[CharSelected];

            if (IsServer)
            {
                SendUpdate("CHAR", value);
            }
        }
        else if(flag == "SHOW_NPM")
        {
            if(IsServer)
            {
                SendUpdate("SHOW_NPM", "");
            }
            else if(IsClient)
            {
                Start();

                //this includes showing the current npm panel
                NPM[] npms = FindObjectsOfType<NPM>();
                for (int i = 0; i < npms.Length; i++)                
                    ShowNPM(npms[i].npmPanel);                
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
        gm = FindObjectOfType<GameManager>();
        //owner is set as soon as npm spawns, so you can access it before networked start
        string npmStr = "NPM" + Owner;
        npmPanel = GameObject.Find(npmStr);        
        
        nameField = npmPanel.GetComponentInChildren<InputField>();
        readyToggle = npmPanel.GetComponentInChildren<Toggle>();
        charDropdown = npmPanel.GetComponentInChildren<TMP_Dropdown>();
        //don't move the children!!
        charImage = npmPanel.transform.GetChild(3).GetComponent<Image>();
        abilityImage = npmPanel.transform.GetChild(4).GetComponent<Image>();
        //abilityImage = npmPanel.transform.GetChild(4).GetComponent<VideoPlayer>();

        //dynamically assign ui events so you can have anchored npm objects in the scene
        nameField.onValueChanged.AddListener(UI_NameChanged);
        readyToggle.onValueChanged.AddListener(UI_Ready);
        charDropdown.onValueChanged.AddListener(UI_CharInput);  

        //menu themes will start for each player as soon as they join, so it's asynchronous
        //FindObjectOfType<GameManager>().SendUpdate("PLAY_MENU_THEME", "");
    }    

    public override void NetworkedStart()
    {
        if (IsServer)
            SendUpdate("SHOW_NPM", "");

        //menu themes will start for each player as soon as they join, so it's asynchronous
        //FindObjectOfType<GameManager>().SendUpdate("PLAY_MENU_THEME", "");
        if (IsLocalPlayer){
            FindObjectOfType<GameManager>().menuTheme.Play();
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

    //this just makes the npm stuff interactable
    private void EnableNpmUI(GameObject panel)
    {
        Toggle toggle = panel.GetComponentInChildren<Toggle>();
        TMP_Dropdown dropdown = panel.GetComponentInChildren<TMP_Dropdown>();
        InputField name = panel.GetComponentInChildren<InputField>();

        toggle.interactable = true;
        dropdown.interactable = true;
        name.interactable = true;
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
        if (IsServer)
            return;

        //since IsLocalPlayer is kinda stupid and isn't always set by NetworkStart you have to manually override
        //the interactability
        bool disabled = this.npmPanel != null && readyToggle != null && !readyToggle.interactable;
        bool enabled = this.npmPanel != null && readyToggle != null && readyToggle.interactable;

        //wrongfully disabled ui, so correct it
        if (IsLocalPlayer && disabled)
        {            
            EnableNpmUI(this.npmPanel);            
        }
        //wrongfully enabled ui, so disable it
        else if (!IsLocalPlayer && enabled)
        {                    
            DisableNpmUI(this.npmPanel);            
        }
    }
}
