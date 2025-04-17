using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class NPM_Menu_Manager : MonoBehaviour
{
    public GameObject WAN_Menu_First;

    void Start()
    {
        EventSystem.current.SetSelectedGameObject(WAN_Menu_First);
    }

    
    void Update()
    {
        
    }
}
