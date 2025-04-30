using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MainCamera : MonoBehaviour
{
    void Start()
    {
        //hopefully it will get reset every time player gets booted back to main menu
        Cursor.visible = true; 
    }
}