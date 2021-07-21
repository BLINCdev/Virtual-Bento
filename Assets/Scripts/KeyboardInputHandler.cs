using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/*
This class handles all keyboard inputs.
*/

public enum KeyboardInputMode { RobotArm, CameraView };

public class KeyboardInputHandler : MonoBehaviour
{
    public GameObject robotArmRoot;
    public GameObject cameraView;
    public GameObject loadTaskMenu;
    public GameObject loadTaskText;    
    public GameObject helpMenu;
    public GameObject robotStandaloneText;
    public GameObject robotUDPText;
    public GameObject robotUDPNote;
    public KeyboardInputMode keyboardInputMode;

    private bool armShellsVisible = true;
    private bool armStandVisible = true;
    Scene scene;     // Used for selecting the scene/level using the built-in keyboard controls when brachI/Oplexus is not connected
    Text loadTasktxt;   // Text variable used to store text from the load task menu
    private int sceneIndex;     // Stores the desired scene to load or reload
    private GameObject[] armShellComponents;
    private GameObject armStand;

    // Joint Force Testing Code
    private List<int> positionFeedback;
    private List<float> velocityFeedback;

    // Start is called before the first frame update
    void Start()
    {
        armShellComponents = GameObject.FindGameObjectsWithTag("Armshell");
        armStand = GameObject.FindWithTag("Armstand");
        loadTasktxt = loadTaskText.GetComponent<Text>(); 

        // Set default keyboard input mode to camera view
        keyboardInputMode = KeyboardInputMode.CameraView;

    }

    // Update is called once per frame
    void Update()
    {
        // If brachI/Oplexus is not connected, then allow built-in keyboard controller for testing purposes
        // Else inputs for the robot arm stream from brachIOplexus
        if (!UDPConnection.brachIOplexusConnected)
        {
            SetRobotArmMovement(robotArmRoot);
            SetCameraView(cameraView);

            // If brachI/Oplexus is not connected, then display mapping for standalone keyboard controller in help menu
            robotStandaloneText.SetActive(true);
            robotUDPText.SetActive(false);
            robotUDPNote.SetActive(false);
        }
        else
        {
            SetCameraView(cameraView);

            // refer users to check brachIOplexus for the control mapping
            robotStandaloneText.SetActive(false);
            robotUDPText.SetActive(true);
            robotUDPNote.SetActive(true);
        }

    }

    private void SetRobotArmMovement(GameObject robotArmRoot)
    {
        RobotArmController robotArmController = robotArmRoot.GetComponent<RobotArmController>();

        GameObject shoulderJoint = robotArmController.joints[0].jointBody;
        GameObject elbowJoint = robotArmController.joints[1].jointBody;
        GameObject wristRotateJoint = robotArmController.joints[2].jointBody;
        GameObject wristFlexJoint = robotArmController.joints[3].jointBody;
        GameObject handJoint = robotArmController.joints[4].jointBody;

        positionFeedback = robotArmController.GetJointPosition();
        velocityFeedback = robotArmController.GetJointVelocity();
        //Debug.Log(positionFeedback[1]);

        // Rotate shoulder left
        if (Input.GetKey(KeyCode.A))
        {
            robotArmController.StartJointRotation(shoulderJoint, RotationDirection.Negative);
        }
        else if (Input.GetKeyUp(KeyCode.A))
        {
            robotArmController.StopJointMovement(shoulderJoint);
        }

        // Rotate shoulder right
        if (Input.GetKey(KeyCode.D))
        {
            robotArmController.StartJointRotation(shoulderJoint, RotationDirection.Positive);
        }
        else if (Input.GetKeyUp(KeyCode.D))
        {
            robotArmController.StopJointMovement(shoulderJoint);
        }

        // Rotate elbow up
        if (Input.GetKey(KeyCode.W))
        {
            robotArmController.StartJointRotation(elbowJoint, RotationDirection.Positive);
        }
        else if (Input.GetKeyUp(KeyCode.W))
        {
            robotArmController.StopJointMovement(elbowJoint);
        }

        // Rotate elbow down
        if (Input.GetKey(KeyCode.S))
        {
            robotArmController.StartJointRotation(elbowJoint, RotationDirection.Negative);
        }
        else if (Input.GetKeyUp(KeyCode.S))
        {
            robotArmController.StopJointMovement(elbowJoint);
        }

        // Rotate wrist left
        if (Input.GetKey(KeyCode.K))
        {
            robotArmController.StartJointRotation(wristRotateJoint, RotationDirection.Negative);
        }
        else if (Input.GetKeyUp(KeyCode.K))
        {
            robotArmController.StopJointMovement(wristRotateJoint);
        }
 
        // Rotate wrist right
        if (Input.GetKey(KeyCode.Semicolon))
        {
            robotArmController.StartJointRotation(wristRotateJoint, RotationDirection.Positive);
        }
        else if (Input.GetKeyUp(KeyCode.Semicolon))
        {
            robotArmController.StopJointMovement(wristRotateJoint);
        }

        // Rotate wrist up
        if (Input.GetKey(KeyCode.O))
        {
            robotArmController.StartJointRotation(wristFlexJoint, RotationDirection.Positive);
        }
        else if (Input.GetKeyUp(KeyCode.O))
        {
            robotArmController.StopJointMovement(wristFlexJoint);
        }

        // Rotate wrist down
        if (Input.GetKey(KeyCode.L))
        {
            robotArmController.StartJointRotation(wristFlexJoint, RotationDirection.Negative);
        }
        else if (Input.GetKeyUp(KeyCode.L))
        {
            robotArmController.StopJointMovement(wristFlexJoint);
        }

        // Close hand
        if (Input.GetKey(KeyCode.RightShift))
        {
            robotArmController.StartJointRotation(handJoint, RotationDirection.Positive);
        }
        else if (Input.GetKeyUp(KeyCode.RightShift))
        {
            robotArmController.StopJointMovement(handJoint);
        }

        // Open hand
        if (Input.GetKey(KeyCode.LeftShift))

        {
            robotArmController.StartJointRotation(handJoint, RotationDirection.Negative);
        }
        else if (Input.GetKeyUp(KeyCode.LeftShift))
        {
            robotArmController.StopJointMovement(handJoint);
        }

        // Else stop arm from moving
        if (Input.anyKey == false)
        {
            robotArmController.StopRobotArmMovement();
        }
    }

    private void SetCameraView(GameObject cameraView)
    {
        // Movement sensitivity constants
        float translation = 0.05f;
        float zoom = 0.2f;
        float rotation = 5.0f;

        // Zoom the camera in and out using right click button if left control key is also held down
        if (Input.GetMouseButton(1) && Input.GetKey(KeyCode.LeftControl))
        {
            cameraView.transform.Translate(new Vector3(0, 0, zoom/3 * Input.GetAxis("Mouse X") + zoom/3 * Input.GetAxis("Mouse Y")));
        }
        // Rotate camera using the right click button on the mouse
        else if (Input.GetMouseButton(1))
        {
            cameraView.transform.RotateAround(cameraView.transform.position, Vector3.up, rotation * Input.GetAxis("Mouse X"));
            cameraView.transform.Rotate(new Vector3(-rotation * Input.GetAxis("Mouse Y"), 0, 0));
            //cameraView.transform.RotateAround(cameraView.transform.position, Vector3.forward, -rotation * Input.GetAxis("Mouse Y"));
        }

        // Pan/Translate the camera using the middle click button the mouse
        if (Input.GetMouseButton(2))
        {
            cameraView.transform.Translate(new Vector3(-translation * Input.GetAxis("Mouse X"), 0, 0));
            cameraView.transform.Translate(new Vector3(0, -translation * Input.GetAxis("Mouse Y"), 0));
        }

        // Zoom the camera in and out using the scroll wheel on the mouse
        cameraView.transform.Translate(new Vector3(0, 0, zoom * Input.GetAxis("Mouse ScrollWheel")));

        // Display or hide arm shells
        //if (Input.GetKeyUp(KeyCode.Tab))
        if (Input.GetKeyUp(KeyCode.Backspace))
        {
            if (armShellsVisible)
            {
                foreach (GameObject armShellComponent in armShellComponents)
                {
                    armShellComponent.SetActive(false);
                }
                armShellsVisible = false;
            }
            else
            {
                foreach (GameObject armShellComponent in armShellComponents)
                {
                    armShellComponent.SetActive(true);
                }
                armShellsVisible = true;
            }
        }

        // Display or hide arm stand
        if (Input.GetKeyUp(KeyCode.Backslash))
        {
            if (armStandVisible)
            {
                armStand.SetActive(false);
                armStandVisible = false;
            }
            else
            {
                armStand.SetActive(true);
                armStandVisible = true;
            }
        }

        // Advance to the first task if user presses any keyboard key on the first start up scene
        if (Input.anyKeyDown == true && Input.GetKeyDown(KeyCode.F1) == false && !(Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1) || Input.GetMouseButtonDown(2)))
        {
            scene = SceneManager.GetActiveScene();
            int sceneIndex2 = scene.buildIndex;
            if (sceneIndex2 == 0)
            {
                sceneIndex = 1;
                LoadTaskOK();
            }
        }

        // Load the next task
        if (Input.GetKeyUp(KeyCode.PageUp))
        {
            //// Scene Selection code for bypassing the Load Task confirmation menu
            //scene = SceneManager.GetActiveScene();
            //int sceneIndex = scene.buildIndex + 1;
            //if (sceneIndex > SceneManager.sceneCountInBuildSettings - 1)
            //{
            //    sceneIndex = SceneManager.sceneCountInBuildSettings - 1;
            //}
            //SceneManager.LoadScene(scene.buildIndex + 1, LoadSceneMode.Single);

            // Scene selection code for using the Load Task confirmation menu
            if (loadTaskMenu.activeSelf == false || (loadTaskMenu.activeSelf == true && loadTasktxt.text != "Load the next task?"))
            {
                helpMenu.SetActive(false);
                loadTasktxt.text = "Load the next task?";
                loadTaskMenu.SetActive(true);
                scene = SceneManager.GetActiveScene();
                sceneIndex = scene.buildIndex + 1;
                if (sceneIndex > SceneManager.sceneCountInBuildSettings - 1)
                {
                    sceneIndex = SceneManager.sceneCountInBuildSettings - 1;
                }
            }
            else if (loadTasktxt.text == "Load the next task?")
            {
                LoadTaskOK();
            }

        }

        // Load the previous task
        if (Input.GetKeyUp(KeyCode.PageDown))
        {
            //// Scene Selection code for bypassing the Load Task confirmation menu
            //scene = SceneManager.GetActiveScene();
            //int sceneIndex = scene.buildIndex - 1;
            //if (sceneIndex < 0)
            //{
            //    sceneIndex = 0;
            //}
            //SceneManager.LoadScene(sceneIndex, LoadSceneMode.Single);

            // Scene selection code for using the Load Task confirmation menu
            if (loadTaskMenu.activeSelf == false || (loadTaskMenu.activeSelf == true && loadTasktxt.text != "Load the previous task?"))
            {
                helpMenu.SetActive(false);
                loadTasktxt.text = "Load the previous task?";
                loadTaskMenu.SetActive(true);
                scene = SceneManager.GetActiveScene();
                sceneIndex = scene.buildIndex - 1;
                if (sceneIndex < 0)
                {
                    sceneIndex = 0;
                }
            }
            else if (loadTasktxt.text == "Load the previous task?")
            {
                LoadTaskOK();
            }
        }

        // Restart the current start
        if (Input.GetKeyUp(KeyCode.Home))
        {
            //// Scene Selection code for bypassing the Load Task confirmation menu
            //scene = SceneManager.GetActiveScene();
            //SceneManager.LoadScene(scene.buildIndex, LoadSceneMode.Single);

            // Scene selection code for using the Load Task confirmation menu
            if (loadTaskMenu.activeSelf == false || (loadTaskMenu.activeSelf == true && loadTasktxt.text != "Reload the current task?"))
            {
                helpMenu.SetActive(false);
                loadTasktxt.text = "Reload the current task?";
                loadTaskMenu.SetActive(true);
                scene = SceneManager.GetActiveScene();
                sceneIndex = scene.buildIndex;
            }
            else if (loadTasktxt.text == "Reload the current task?")
            {
                LoadTaskOK();
            }
        }

        // Open Help Menu if user hits the F1 key
        if (Input.GetKeyUp(KeyCode.F1))
        {
            if (helpMenu.activeSelf == false)
            {
                loadTaskMenu.SetActive(false);
                helpMenu.SetActive(true);
            }
            else
            {
                helpMenu.SetActive(false);
            }
        }

        // Close all menus if user hits the escape key
        if (Input.GetKeyUp(KeyCode.Escape))
        {
            loadTaskMenu.SetActive(false);
            helpMenu.SetActive(false);
        }
    }

    //  Load or reload the corresponding task when the user clicks the "OK" button on the Load Task dialog box
    public void LoadTaskOK()
    {
        //brachIOplexusInputHandler.initCameraPosition = new Vector3(0, 0, 0);
        //brachIOplexusInputHandler.initCameraRotation = new Vector3(0, 0, 0);
        SceneManager.LoadScene(sceneIndex, LoadSceneMode.Single);
    }

    // Close the Load Task dialog box when the user clicks the "Cancel" button on the Load Task dialog box
    public void LoadTaskCancel()
    {
        loadTaskMenu.SetActive(false);
    }

    // Close the Help Menu dialog box when the user clicks the "X" button in the top right corner
    public void helpMenuClose()
    {
        helpMenu.SetActive(false);
    }
}
