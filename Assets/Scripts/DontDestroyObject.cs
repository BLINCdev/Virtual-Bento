using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
This function ensures that the UDPconnection object is not destroyed when we reload a scene.
Note: can modify the "tag" to include other objects.
*/

public class DontDestroyObject : MonoBehaviour
{
    void Awake()
    {
        // Find all objects with the tag "communication" and make sure these objects aren't destroyed on scene reload
        GameObject[] objs = GameObject.FindGameObjectsWithTag("Communication");

        if (objs.Length > 1)
        {
            Destroy(this.gameObject);
        }

        DontDestroyOnLoad(this.gameObject);
    }
}