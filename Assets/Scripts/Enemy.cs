using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public abstract class Enemy : Character
{
    // Start is called before the first frame update
    private void Start()
    {
        
    }

    protected abstract void Move();    
}
