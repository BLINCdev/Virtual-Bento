using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System;
using System.Text;
using System.Net;               // Added for TCPIP communication
using System.Net.Sockets;       // Added for TCPIP communication
using System.Threading;         // Added for TCPIP communication

/* 
This class handles receiving information from brachI/Oplexus and sending information to brachI/Oplexus.
*/
 
public class UDPConnection : MonoBehaviour
{
    // Indicates whether or not brachI/Oplexus is connected
    public static bool brachIOplexusConnected = false;

    // Input handler to process inputs
    private brachIOplexusInputHandler inputHandler;
    private RobotArmController robotArmController;
    private CameraPosition cameraPosition;

    public Int32 simulationPortRX = 30004;
    public Int32 simulationPortTX = 30005;
    public IPAddress simulationIPAddress = IPAddress.Parse("127.0.0.1");

    private UdpClient unityUdpClientRX;
    private IPEndPoint unityIPEndPointRX;
    private System.Threading.Timer sendingThread;

    private UdpClient unityUdpClientTX;
    private IPEndPoint unityIPEndPointTX;
    private Thread receivingThread = null;

    // For holding real-time feedback
    private List<float> velocityFeedback;
    private List<int> positionFeedback;

    // Start is called before the first frame update
    void Start()
    {
        // Get unity main thread
        // We need to send actions from the communication thread to the main thread in order to execute them
        // Thread-safe implementation
        // Link to resource: https://stackoverflow.com/a/41333540
        UnityThread.initUnityThread();

        // Get the first input handler
        inputHandler = GameObject.Find("_brachIOplexusInput").GetComponent<brachIOplexusInputHandler>();

        unityUdpClientRX = new UdpClient(simulationPortRX);
        unityIPEndPointRX = new IPEndPoint(simulationIPAddress, simulationPortRX);

        unityUdpClientTX = new UdpClient();
        unityIPEndPointTX = new IPEndPoint(simulationIPAddress, simulationPortTX);

        SceneManager.sceneLoaded += LoadScene;

        receivingThread = new Thread(receiveInfo);
        receivingThread.IsBackground = true;
        receivingThread.Start();
        sendingThread = new System.Threading.Timer(new TimerCallback(sendInfo), null, 0, 15);
    }

    void LoadScene(Scene scene, LoadSceneMode mode)
    {
        // Get input handler from loaded scene
        inputHandler = GameObject.Find("_brachIOplexusInput").GetComponent<brachIOplexusInputHandler>();
        if (scene.buildIndex != 0)
        {
            // Get the target game objects of input handler
            robotArmController = inputHandler.robotArmRoot.GetComponent<RobotArmController>();
            cameraPosition = inputHandler.cameraView.GetComponent<CameraPosition>();
        }
    }
    
    void Update()
    {
        if (brachIOplexusConnected)
        {
            // Get the current position and velocity of the arm
            positionFeedback = robotArmController.GetJointPosition();
            velocityFeedback = robotArmController.GetJointVelocity();
        }
    }

    void receiveInfo(object state)
    {
        while (true)
        {
            byte[] packet = unityUdpClientRX.Receive(ref unityIPEndPointRX);
            
            try
            {
                brachIOplexusConnected = true;
                if (validate(packet))
                {
                    // We received a request to send camera position
                    if (packet[2] == 3)
                    {
                        UnityThread.executeInUpdate(() =>
                        {
                            sendCameraView();
                        });
                    }
                    else
                    {
                        // We need to handle the information being send to us
                        inputHandler.parsePacket(packet);
                    }
                }
            }
            catch (Exception e)
            {
                //Debug.LogError(e.Message);
            }
        }
    }

    void sendInfo(object state)
    {
        try
        {
            if (brachIOplexusConnected)
            {
                byte[] packet = getFeedbackPacket();
                unityUdpClientTX.Send(packet, packet.Length, unityIPEndPointTX);
            }
        }
        catch (Exception e)
        {
            //Debug.LogError(e.Message);
        }
    }

    // Functions below are for sending feedback back to brachI/Oplexus from Unity
    private byte[] getFeedbackPacket()
    {
        byte[] packet;
        byte length;

        // How many pieces of data each motor contains
        byte numData = 6;
        // How many joint motors we care about
        int numJoints = 5;
        // Index of packet to start filling data
        int startIndex;

        length = (byte)(numJoints * numData);
        // Builds the packet 
        packet = new byte[5 + length];
        packet[0] = 255;            // Header
        packet[1] = 255;            // Header
        packet[2] = 4;              // Type: 4
        packet[3] = length;         // Length of Data 

        startIndex = 4;
        for (int i = 0; i < numJoints; i++)
        {
            packet[startIndex] = (byte)i;
            packet[startIndex + 1] = low_byte((UInt16)Math.Abs(positionFeedback[i]));
            packet[startIndex + 2] = high_byte((UInt16)Math.Abs(positionFeedback[i]));
            if (positionFeedback[i] >= 0)
            {
                packet[startIndex + 3] = 1; // Positive position
            }
            else
            {
                packet[startIndex + 3] = 0; // Negative position
            }

            packet[startIndex + 4] = low_byte((UInt16)velocityFeedback[i]);
            packet[startIndex + 5] = high_byte((UInt16)velocityFeedback[i]);

            startIndex += numData;
        }

        packet[34] = getCheckSum(packet);

        return packet;
    }

    private void sendCameraView()
    {
        List<int> positions = cameraPosition.getCameraPositions();
        byte[] packet = getCameraPacket(positions);
        unityUdpClientTX.Send(packet, packet.Length, unityIPEndPointTX);
    }

    private byte[] getCameraPacket(List<int> positions)
    {
        byte[] packet;
        byte length;

        // How many pieces of data each index contains
        byte numData = 3;
        int startIndex;

        length = (byte)(numData * 6);
        // Builds the packet 
        packet = new byte[5 + length];
        packet[0] = 255;            // Header
        packet[1] = 255;            // Header
        packet[2] = 5;              // Type: 5
        packet[3] = length;         // Length of Data 

        startIndex = 4;
        for (int i = 0; i < 6; i++)
        {
            packet[startIndex] = low_byte((UInt16)Math.Abs(positions[i]));
            packet[startIndex + 1] = high_byte((UInt16)Math.Abs(positions[i]));
            if (positions[i] >= 0)
            {
                packet[startIndex + 2] = 1; // Positive camera position
            }
            else
            {
                packet[startIndex + 2] = 0; // Negative camera position
            }
            startIndex += numData;
        }

        packet[22] = getCheckSum(packet);
        return packet;
    }

    // Helper functions for transforming ints to bytes
    private byte low_byte(ushort number)
    {
        return (byte)(number & 0xff);
    }

    private byte high_byte(ushort number)
    {
        return (byte)(number >> 8);
    }

    private byte getCheckSum(byte[] packet)
    {
        // Stores calculated check sum 
        byte checkSum;

        // Reset check sum
        checkSum = 0;

        // Iterates through the data portion of the packet 
        for (int i = 2; i < packet.Length - 1; i++)
        {
            checkSum += packet[i];
        }

        // Will only take lower byte if the check sum is less than -1 
        if ((byte)~checkSum >= 255)
        {
            checkSum = low_byte((UInt16)~checkSum);
        }
        else
        {
            checkSum = (byte)~checkSum;
        }

        return checkSum;
    }

    private bool validate(byte[] packet)
    {
        byte checksum = 0;
        checksum = getCheckSum(packet);
        if (checksum == packet[packet.Length - 1] && packet[0] == 255 && packet[1] == 255)
        {
            return true;
        }
        else
        {
            return false;
        }

    }
}
