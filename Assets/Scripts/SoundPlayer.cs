/*
@Authors - Patrick
@Description - Offline script that plays sounds. GameManager/player is responsible for making sure that the sounds are only
played on the the correct connects (localplayer, all clients, etc)
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundPlayer : MonoBehaviour
{
    [SerializeField] private AudioSource theme;   
    void Start()
    {
        
    }

    
    void Update()
    {
        
    }

    public void PlayTheme(){
        theme.Play();
    }

    public void StopTheme(){
        theme.Stop();
    }
}
