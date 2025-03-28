using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CollideTest : MonoBehaviour
{    
    // Start is called before the first frame update
    void Start()
    {
        
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        /*Debug.Log("trigger entered!");
        if(collision.gameObject.name == "Rope")
        {
            grabbedRope = collision.gameObject.GetComponent<NetlessRope>();
        }*/
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
