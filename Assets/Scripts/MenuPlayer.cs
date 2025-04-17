using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

public class MenuPlayer : MonoBehaviour
{
    public bool menuInput {get; private set;}
    public static MenuPlayer instance;
    public PlayerInput input;
    public InputAction inputAction;


    void Awake()
    {
        if (instance == null){
            instance = this;   
        }
    }

    void Update()
    {
        menuInput = inputAction.WasPressedThisFrame();
    }
}
