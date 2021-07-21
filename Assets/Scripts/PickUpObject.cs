using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
This class makes it easier to pick up objects.
*/

public class PickUpObject : MonoBehaviour
{
    private bool leftChopstick = false;
    private bool rightChopstick = false;
    private Rigidbody objectBody;
    private bool pickedUp = false;

    // Start is called before the first frame update
    void Start()
    {
        objectBody = GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    void Update()
    {
        if (!pickedUp)
        {
            if (leftChopstick && rightChopstick)
            {
                pickedUp = true;
                //Debug.Log("Picked UP");
                // Disable gravity so that object doesn't slip out too easily
                objectBody.useGravity = false;
            }
        }

        else
        {
            if (!leftChopstick || !rightChopstick)
            {
                pickedUp = false;
                //Debug.Log("Dropped");
                objectBody.useGravity = true;
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.tag == "LeftTrigger")
        {
            leftChopstick = true;
        }
        if (other.gameObject.tag == "RightTrigger")
        {
            rightChopstick = true;
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.gameObject.tag == "LeftTrigger")
        {
            leftChopstick = false;
        }
        if (other.gameObject.tag == "RightTrigger")
        {
            rightChopstick = false;
        }
    }

}
