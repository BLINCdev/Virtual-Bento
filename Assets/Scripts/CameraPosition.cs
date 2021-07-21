using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

/*
This class contols the position and rotation of the camera object attached to it.
It also contains functions that return information related to the camera object.

IMPORTANT NOTES:
* Must have a camera object attached.
*/

public class CameraPosition : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        // If brachIOplexus is connected, we initialize the camera position according to its inputs
        // Only update if the default task view is not selected in brachIOplexus
        if (UDPConnection.brachIOplexusConnected && brachIOplexusInputHandler.initCameraPosition[1] != 0)
        {
            transform.localPosition = brachIOplexusInputHandler.initCameraPosition;
            transform.eulerAngles = brachIOplexusInputHandler.initCameraRotation;
        }
    }

    public List<int> getCameraPositions()
    {
        // Returns the current camera position of the camera
        int posX = (int)transform.localPosition.x;
        int posY = (int)transform.localPosition.y;
        int posZ = (int)transform.localPosition.z;
        int rotX = (int)transform.eulerAngles.x;
        int rotY = (int)transform.eulerAngles.y;
        int rotZ = (int)transform.eulerAngles.z;
        List<int> positions = new List<int>() { posX, posY, posZ, rotX, rotY, rotZ };

        return positions;
    }
}
