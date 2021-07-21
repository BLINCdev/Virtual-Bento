using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
This class controls a single joint of a robotic arm.
*/

public enum RotationDirection { None = 0, Positive = 1, Negative = -1 };

public class JointMotor : MonoBehaviour
{
    public RotationDirection rotationState = RotationDirection.None;
    public float rotationVelocity;

    private ArticulationBody articulation;
    // Start is called before the first frame update
    void Start()
    {
        articulation = GetComponent<ArticulationBody>();
        rotationVelocity = 0.0f;
    }

    // Update is called once per frame
    void Update()
    {
        if (rotationState != RotationDirection.None)
        {
            float rotationTargetPosition = GetCurrentPosition() + GetRotationIncrement();
            RotateJointTo(rotationTargetPosition);
        }
        else
        {
            // If we are not rotating, set velocity back to 0
            rotationVelocity = 0.0f;
        }
    }

    // HELPER FUNCTIONS
    private float GetCurrentPosition()
    {
        return Mathf.Rad2Deg * articulation.jointPosition[0];
    }

    private float GetRotationIncrement()
    {
        return (float)rotationState * rotationVelocity * Time.fixedDeltaTime;
    }

    void RotateJointTo(float rotationTargetPosition)
    {
        var jointDrive = articulation.xDrive;
        jointDrive.target = rotationTargetPosition;
        articulation.xDrive = jointDrive;
    }
}
