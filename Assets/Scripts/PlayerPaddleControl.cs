//-----------------------------------------------------------------------
// Fall 2018
// CS117 Final Project
// Vroom ARoom
// By Junya Honda, Han Lee, Moo Jin Kim, Jonathan Myong 
//-----------------------------------------------------------------------

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using GoogleARCore.Examples.CloudAnchors;

/// <summary>
/// Player paddle control.
/// </summary>
public class PlayerPaddleControl : NetworkBehaviour
{
    /// <summary>
    /// The speed of the paddle.
    /// </summary>
    [Range(0, 10)]
    public float Speed = 4.0f;

    /// <summary>
    /// The main camera.
    /// </summary>
    private Camera MainCamera;

    /// <summary>
    /// The Overall Cloud Controller
    /// </summary>
    private PongCloudController CloudControl;

    /// <summary>
    /// The location object
    /// </summary>
    private GameObject LocObject;

    /// <summary>
    /// The game field object.
    /// </summary>
    private GameObject GameField;

    /// <summary>
    /// The synced paddle position, relative to GameField.
    /// </summary>
    [SyncVar]
    private Vector3 PaddlePosition;

    /// <summary>
    /// The field rotation.
    /// </summary>
    private Quaternion FieldRotation;

    /// <summary>
    /// Checks if the local/non-local paddle has been correctly placed
    /// </summary>
    private bool locObjectCheck = false;

    /// <summary>
    /// Check for if the Left Button is being pressed.
    /// </summary>
    private bool buttonLeftPressed = false;

    /// <summary>
    /// Check for if the Right Button is being pressed.
    /// </summary>
    private bool buttonRightPressed = false;

    /// <summary>
    /// Check for if the Player is Player1
    /// </summary>
    private bool isAnchorHost = false;

    /// <summary>
    /// Check for if the Player is Player2
    /// </summary>
    private bool isAnchorClient = false;



    /// <summary>
    /// Initializes variables when the script is run for the first time
    /// </summary>
    void Start()
    {
        Debug.Log("Start Player!");
        InitializeVariables();
    }

    /// <summary>
    /// Update this instance.
    /// </summary>
    void Update()
    {
        // Updates for the paddle not on the corresponding device
        if (!isLocalPlayer)
        {
            if (!locObjectCheck)
            {
                if (isAnchorHost)
                {
                    // Player 2
                    LocObject = GameObject.FindWithTag("PLocation2");
                    GetComponent<MeshRenderer>().material.color = Color.blue;
                }
                else if (isAnchorClient)
                {
                    // Player 1
                    LocObject = GameObject.FindWithTag("PLocation1");
                    GetComponent<MeshRenderer>().material.color = Color.red;
                }
                ChangeLocationToLoc();
                locObjectCheck = true;
                return;
            }

            transform.localPosition = PaddlePosition;
            transform.rotation = LocObject.transform.rotation;
            return;
        }

        if (!locObjectCheck)
            return;

        transform.localPosition = PaddlePosition;
        transform.rotation = LocObject.transform.rotation;

        // ** BUTTON CODE: **
        if (buttonLeftPressed || buttonRightPressed)
        {
            if (buttonLeftPressed)
                CmdMovePlayerPositive();
            if (buttonRightPressed)
                CmdMovePlayerNegative();
            return;
        }

        // ** ARROW KEY CODE: **
        var x = Input.GetAxis("Horizontal") * Time.deltaTime * 10.0f;
        if (isAnchorHost)
            PaddlePosition += new Vector3(x, 0, 0);
        else if (isAnchorClient)
            PaddlePosition += new Vector3(-x, 0, 0);
    }

    /// <summary>
    /// Configures the local Player Paddle to the correct position and color
    /// </summary>
    public override void OnStartLocalPlayer()
    {
        InitializeVariables();

        if (isAnchorHost)
        {
            // Player 1
            LocObject = GameObject.FindWithTag("PLocation1");
            GetComponent<MeshRenderer>().material.color = Color.red;
        }
        else if (isAnchorClient)
        {
            // Player 2
            LocObject = GameObject.FindWithTag("PLocation2");
            GetComponent<MeshRenderer>().material.color = Color.blue;
        }
        else
        {
            Debug.Log("LocObject not found!!");
            return;
        }

        locObjectCheck = true;
        ChangeLocationToLoc();
    }

    /// <summary>
    /// Initializes the Controller variables.
    /// </summary>
    public void InitializeVariables()
    {
        MainCamera = GameObject.FindWithTag("MainCamera").GetComponent(typeof(Camera)) as Camera;
        CloudControl = GameObject.FindWithTag("CloudControl").GetComponent(typeof(PongCloudController)) as PongCloudController;
        GameField = GameObject.FindWithTag("GameField");

        isAnchorHost = CloudControl.GetIfCloudAnchorHost();
        isAnchorClient = CloudControl.GetIfCloudAnchorResolving();
        FieldRotation = GameField.transform.rotation;
    }

    /// <summary>
    /// Changes the location according to the LocObject. It should always be preceeded by a LocObject declaration.
    /// </summary>
    public void ChangeLocationToLoc()
    {
        transform.SetParent(LocObject.transform, false);
        transform.localPosition = new Vector3(0, 0, 0);
        transform.rotation = GameField.transform.rotation;
    }

    /// <summary>
    /// Action for when the Left Button is pressed down.
    /// </summary>
    public void ButtonLeft(bool heldDown)
    {
        buttonLeftPressed = heldDown;
    }

    /// <summary>
    /// Action for when the Right Button is pressed down.
    /// </summary>
    public void ButtonRight(bool heldDown)
    {
        buttonRightPressed = heldDown;
    }



    /// <summary>
    /// Moves the Player Paddle in the negative x direction.
    /// </summary>
    [Command]
    private void CmdMovePlayerNegative()
    {
        var x = Time.deltaTime * Speed;
        PaddlePosition += new Vector3(-x, 0, 0);
    }

    /// <summary>
    /// Moves the Player Paddle in the positive x direction.
    /// </summary>
    [Command]
    private void CmdMovePlayerPositive()
    {
        var x = Time.deltaTime * Speed;
        PaddlePosition += new Vector3(x, 0, 0);
    }
}
