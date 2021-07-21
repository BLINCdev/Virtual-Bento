using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.Threading.Tasks;

/*
This class parses the information from brachI/Oplexus and handles the information accordingly.
Variables are either initialized or updated.
*/

public class brachIOplexusInputHandler : MonoBehaviour
{
    // Gets the robot arm and the camera that this input handler controls
    public GameObject robotArmRoot;
    public GameObject cameraView;

    // Initialization variables that the robot arm and camera need to access in order to start properly
    // These variables are only used if brachIOplexus is connected
    public static Vector3 initCameraPosition;
    public static Vector3 initCameraRotation;
    public static List<int> initJointMinLimits;
    public static List<int> initJointMaxLimits;

    // Define the default camera positions for each task
    // Required to get proper camera changing behavior when using keyboard and brachIOplexus camera controls
    // If default camera positions are changed they need to be updated here and in cameraPositions.csv, so that the values are identical
    Scene scene;
    int[,] defaultCameraPositions = new int[7, 6] { { -185, 500, 0, 66, 270, 0 },       // StartUpScene
                                                    { -185, 500, 0, 66, 270, 0 },       // DefaultEmptyScene
                                                    { -185, 500, 0, 66, 270, 0 },       // BallCupTask_Basic
                                                    { -185, 500, 0, 66, 270, 0 },       // BallCupTask_Pour 
                                                    { -208, 403, -222, 55, 299, 0 },    // BallCupTask_Stack
                                                    { -320, 536, 0, 82, 270, 0 },       // BlockTask_Basic
                                                    { -208, 403, -222, 55, 299, 0 },};  // CupStackTask_Basic

    public void parsePacket(byte[] packet)
    {
        switch (packet[2])
        {
            case 0:
                parseScenePacket(packet);
                break;
            case 1:
                parseControlPacket(packet);
                break;
            case 2:
                parseUpdatePacket(packet);
                break;
            default:
                return;
        }
    }

    private void parseScenePacket(byte[] packet)
    {
        // Get scene
        int sceneIndex = (int)packet[4];
        int frameRate = (int)packet[5];
        UnityThread.executeInUpdate(() =>
        {
            SceneManager.LoadScene(sceneIndex);
            Application.targetFrameRate = frameRate;
        });

        // Get camera positions
        int startIndex = 6;
        int numData = 3;
        List<int> positions = new List<int>();
        for (int i = 0; i < 6; i++)
        {
            int pos = (int)transformByteToFloat(packet[startIndex], packet[startIndex + 1]);
            if (packet[startIndex + 2] == 0) // Camera position is a negative number
            {
                pos *= (-1);
            }
            positions.Add(pos);
            startIndex += numData;
        }
        // Send the initial camera positions to the camera
        initializeCameraPosition(positions);

        // Get joint limits
        List<int> posMinList = new List<int>();
        List<int> posMaxList = new List<int>();
        for (int i = 0; i < 5; i++)
        {
            int posMin = (int)transformByteToFloat(packet[startIndex], packet[startIndex + 1]);
            int posMax = (int)transformByteToFloat(packet[startIndex + 2], packet[startIndex + 3]);
            // The hand is a special case, we need to reverse the max and min values
            if (i == 4)
            {
                int temp = posMin;
                posMin = 4096 - posMax;
                posMax = 4096 - temp;
            }
            posMinList.Add(posMin);
            posMaxList.Add(posMax);
            startIndex += 4;
        }
        // Send the initial joint limits to the robot arm
        initializeJointLimits(posMinList, posMaxList);
    }

    private void parseControlPacket(byte[] packet)
    {
        int startIndex = 4;
        int numJoints = 5;

        for (int i = 0; i < numJoints; i++)
        {
            int jointIndex = (int)packet[startIndex];
            int jointDirection = (int)packet[startIndex + 3];
            float jointVelocity = transformByteToFloat(packet[startIndex + 1], packet[startIndex + 2]) * 1.2f;
            //Debug.LogError("Joint: " + Convert.ToString(jointIndex) + ",  Direction: " + Convert.ToString(jointDirection) + ", Velocity: " + Convert.ToString(jointVelocity));
            if (jointDirection != 3)
            {
                UnityThread.executeInUpdate(() =>
                {
                    rotateJoint(jointIndex, jointDirection, jointVelocity);
                });
            }          
            startIndex += 4;
        }
    }

    private void parseUpdatePacket(byte[] packet)
    {
        int startIndex = 4;
        List<int> positions = new List<int>();
        for (int i = 0; i < 6; i++)
        {
            int pos = (int)transformByteToFloat(packet[startIndex], packet[startIndex + 1]);
            if (packet[startIndex + 2] == 0) // Camera position is a negative number
            {
                pos *= (-1);
            }
            positions.Add(pos);
            startIndex += 3;
        }

        // Send the initial camera positions to the camera. Required to get proper camera changing behavior when using keyboard and brachIOplexus camera controls
        initializeCameraPosition(positions);
        
        UnityThread.executeInUpdate(() =>
        {
            updateCameraPosition(positions);
        });

        List<int> posMinList = new List<int>();
        List<int> posMaxList = new List<int>();
        for (int i = 0; i < 5; i++)
        {
            int posMin = (int)transformByteToFloat(packet[startIndex], packet[startIndex + 1]);
            int posMax = (int)transformByteToFloat(packet[startIndex + 2], packet[startIndex + 3]);
            // The hand is a special case, we need to reverse the max and min values
            if (i == 4)
            {
                int temp = posMin;
                posMin = 4096 - posMax;
                posMax = 4096 - temp;
            }
            posMinList.Add(posMin);
            posMaxList.Add(posMax);
            startIndex += 4;
        }
        UnityThread.executeInUpdate(() =>
        {
            updateJointLimits(posMinList, posMaxList);
        });
    }

    private void rotateJoint(int jointIndex, int jointDirection, float jointVelocity)
    {
        RotationDirection direction;
        RobotArmController robotArmController = robotArmRoot.GetComponent<RobotArmController>();
        GameObject joint = robotArmController.joints[jointIndex].jointBody;

        // Motor state conversions from brachIOplexus to unity: 1 --> 1, 2 --> -1, 0 --> 0
        // ***NOTE*** the hand joint is different: 1 --> -1, 2 --> 1, 0 --> 0
        if (jointDirection == 2)
        {
            if (jointIndex == 4)
            {
                direction = (RotationDirection)(1);
            }
            else
            {
                direction = (RotationDirection)(-1);
            }
            robotArmController.StartJointRotation(joint, direction, jointVelocity);
        }
        else if (jointDirection == 1)
        {
            if (jointIndex == 4)
            {
                direction = (RotationDirection)(-1);
            }
            else
            {
                direction = (RotationDirection)(1);
            }
            robotArmController.StartJointRotation(joint, direction, jointVelocity);
        }
        else
        {
            direction = (RotationDirection)(0);
            robotArmController.StopJointMovement(joint);
        }
    }

    private void initializeCameraPosition(List<int> positions)
    {
        initCameraPosition = new Vector3(positions[0], positions[1], positions[2]);
        initCameraRotation = new Vector3(positions[3], positions[4], positions[5]);
    }

    private void initializeJointLimits(List<int> posMinList, List<int> posMaxList)
    {
        initJointMinLimits = posMinList;
        initJointMaxLimits = posMaxList;
    }

    private void updateCameraPosition(List<int> positions)
    {
        // Only update if the default task view is not selected in brachIOplexus
        //if (initCameraPosition[1] != 0)
        if (positions[1] != 0)
        {
            cameraView.transform.localPosition = new Vector3(positions[0], positions[1], positions[2]);
            cameraView.transform.eulerAngles = new Vector3(positions[3], positions[4], positions[5]);
        }
        else if (positions[1] == 0)
        {
            // Send the initial camera positions to the camera. Required to get proper camera changing behavior when using keyboard and brachIOplexus camera controls
            scene = SceneManager.GetActiveScene();
            int sceneIndex = scene.buildIndex;

            // If the number of scenes has been increased, but the developer has not updated the defaultCameraPositions array use the default camera positions for scene 0 rather than throwing an exception
            if (sceneIndex > defaultCameraPositions.GetLength(0)-1)
            {
                sceneIndex = 0;
            }
            cameraView.transform.localPosition = new Vector3(defaultCameraPositions[sceneIndex, 0], defaultCameraPositions[sceneIndex, 1], defaultCameraPositions[sceneIndex, 2]);
            cameraView.transform.eulerAngles = new Vector3(defaultCameraPositions[sceneIndex, 3], defaultCameraPositions[sceneIndex, 4], defaultCameraPositions[sceneIndex, 5]);
        }
    }

    private void updateJointLimits(List<int> posMinList, List<int> posMaxList)
    {
        RobotArmController robotArmController = robotArmRoot.GetComponent<RobotArmController>();
        for (int i = 0; i < 5; i++)
        {
            ArticulationBody articulation = robotArmController.joints[i].jointBody.GetComponent<ArticulationBody>();
            // Get the max and min joint positions and convert them to degrees
            float posMin = robotArmController.convertJointPositionToDegree(posMinList[i]);
            float posMax = robotArmController.convertJointPositionToDegree(posMaxList[i]);
            // Get the current position of the arm
            float currentPos = Mathf.Rad2Deg * articulation.jointPosition[0];
            var drive = articulation.xDrive;           
            if (currentPos > posMax)
            {
                drive.target = posMax;
            }
            else if (currentPos < posMin)
            {
                drive.target = posMin;
            }
            drive.upperLimit = posMax;
            drive.lowerLimit = posMin;
            articulation.xDrive = drive;
        }
    }

    private float transformByteToFloat(byte lowByte, byte highByte)
    {
        UInt16 value = (UInt16)((lowByte) | (highByte << 8));
        return (float)value;
    }
}
