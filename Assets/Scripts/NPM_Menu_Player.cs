using System.Collections;
using System.Collections.Generic;
using NETWORK_ENGINE;
using UnityEngine;
using UnityEngine.InputSystem;

public class NPM_Menu_Player : NetworkComponent
{
    public bool menuInput {get; private set;}
    public static NPM_Menu_Player instance;
    public PlayerInput input;
    public InputAction inputAction;
    
    public override void HandleMessage(string flag, string value)
    {
        
    }

    public override void NetworkedStart()
    {
        if (instance == null){
            instance = this;   
        }
    }

    void Update()
    {
        if (IsLocalPlayer){
            menuInput = inputAction.WasPressedThisFrame();
        }
    }

    public override IEnumerator SlowUpdate()
    {
        while (IsConnected){
            yield return new WaitForSeconds(MyCore.MasterTimer);
        }
    }
}
