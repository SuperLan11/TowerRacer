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
}
