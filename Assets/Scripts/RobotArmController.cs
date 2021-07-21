using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

/*
This class configures and controls the robot arm object attached to it.
It also contains functions that return information related to the robot arm object.

IMPORTANT NOTES:
* Must be attached to a robot arm object.
* All joints must have JointMotor script attached.
*/

public class RobotArmController : MonoBehaviour
{
    [System.Serializable]
    public struct Joint
    {
        public GameObject jointBody;
        public string rotationAxis;
    }
    public Joint[] joints;
    public int solverIterations = 10;
    private float jointStiffness = 100000.0f;
    private float jointDamping = 1000.0f;
    
    // Joint and velocity conversion factor between brachIOplexus positions and unity positions
    public double jointPositionConversionFactor = 0.087890625;
    public float velocityConversionFactor = 1.2f;

    void Start()
    {
        if (UDPConnection.brachIOplexusConnected)
        {
            // Get the joint limits from brachIOplexus inputs
            ConfigureJoints(brachIOplexusInputHandler.initJointMinLimits, brachIOplexusInputHandler.initJointMaxLimits);
        }
        else
        {
            // Else configure the joints with default joint limits
            ConfigureJoints();
        }
    }

    public void ConfigureJoints(List<int> posMinList = null, List<int> posMaxList = null)
    {
        for (int i = 0; i < joints.Length; i++)
        {
            // Configure settings for each joint
            // Get the articulation body for current joint
            Joint joint = joints[i];
            ArticulationBody articulation = joint.jointBody.GetComponent<ArticulationBody>();

            // Set type of joint
            articulation.jointType = ArticulationJointType.RevoluteJoint;
            articulation.solverIterations = solverIterations;

            // Set stiffness and damping for current joint drive
            var drive = articulation.xDrive;
            drive.stiffness = jointStiffness;
            drive.damping = jointDamping;
            // Check if we need to set joint limits from brachIOplexus
            if (posMaxList != null && posMinList != null)
            {
                drive.upperLimit = convertJointPositionToDegree(posMaxList[i]);
                drive.lowerLimit = convertJointPositionToDegree(posMinList[i]);
            }

            articulation.xDrive = drive;

            // Set rotation axis of current joint
            switch (joint.rotationAxis)
            {
                case "x":
                    articulation.anchorRotation = Quaternion.Euler(90, 0, 0);
                    break;
                case "y":
                    articulation.anchorRotation = Quaternion.Euler(0, 90, 0);
                    break;
                case "z":
                    articulation.anchorRotation = Quaternion.Euler(0, 0, 90);
                    break;
            }
        }
    }

    // Stop the movement of the entire arm
    public void StopRobotArmMovement()
    {
        for (int i = 0; i < joints.Length; i++)
        {
            StopJointMovement(joints[i].jointBody);
        }
    }

    // Stop the movement of a single joint
    public void StopJointMovement(GameObject jointBody)
    {
        UpdateRotationState(jointBody, RotationDirection.None, 0);
    }

    // Start the rotation of a joint, default velocity is 50
    public void StartJointRotation(GameObject jointBody, RotationDirection direction, float velocity = (float)50)
    {
        // Stop all existing joint movements to make sure we are moving only one joint at a time
        // Comment out this line if we want to enable multiple joint movements
        // StopRobotArmMovement();
        UpdateRotationState(jointBody, direction, velocity);
    }

    static void UpdateRotationState(GameObject jointBody, RotationDirection direction, float velocity = (float)50)
    {
        JointMotor jointMotor = jointBody.GetComponent<JointMotor>(); 
        jointMotor.rotationVelocity = velocity;
        jointMotor.rotationState = direction;
    }

    /* HELPERS */
    // Below are convenience functions for getting the current position and velocity for every joint of the arm
    // Returned values are brachIOplexus positions/velocities
    public List<int> GetJointPosition()
    {
        List<int> jointPosition = new List<int>();
        for (int i = 0; i < joints.Length; i++)
        {
            Joint joint = joints[i];
            ArticulationBody articulation = joint.jointBody.GetComponent<ArticulationBody>();
            int jointPos = convertJointDegreeToPosition(Mathf.Rad2Deg * articulation.jointPosition[0]);
            //The hand is a special case
            if (i == 4)
            {
                jointPos = 4096 - jointPos;
            }
            jointPosition.Add(jointPos);
        }
        //Debug.Log(jointPosition[1]);
        return jointPosition;
    }

    public List<float> GetJointVelocity()
    {
        List<float> jointVelocity = new List<float>();
        for (int i = 0; i < joints.Length; i++)
        {
            JointMotor motor = joints[i].jointBody.GetComponent<JointMotor>();
            jointVelocity.Add((float)motor.rotationVelocity/velocityConversionFactor);
        }
        return jointVelocity;
    }

    // Converts between brachIOplexus positions (int) and unity positions(float, in degrees)
    public int convertJointDegreeToPosition(float jointDegree)
    {
        return (int)((jointDegree + 180) / jointPositionConversionFactor);
    }

    public float convertJointPositionToDegree(int jointPosition)
    {
        return (float)(jointPosition * jointPositionConversionFactor - 180);
    }
}
