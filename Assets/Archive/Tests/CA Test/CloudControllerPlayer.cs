using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using GoogleARCore.Examples.CloudAnchors;


public class CloudControllerPlayer : NetworkBehaviour
{

    public Camera MainCamera;
    public GameManager gameManager;

    private bool buttonLeftPressed = false;
    private bool buttonRightPressed = false;

    // Initializes variables when the script is run for the first time, per instantiated object.
    // (This will actually run twice since this script is attached to two
    // prefabs, but that's okay.)
    void Start() {
        MainCamera = GameObject.FindWithTag("MainCamera").GetComponent(typeof(Camera)) as Camera;
        gameManager = GameObject.FindWithTag("GameManager").GetComponent(typeof(GameManager)) as GameManager;
        gameManager.StartNewGame();
    }

    void Update() {

        // Since both paddles use the same script, we return if the script
        // is currently referring to the other paddle.
        if (!isLocalPlayer)
            return;

        if (buttonLeftPressed || buttonRightPressed) {
            if (buttonLeftPressed)
                MovePlayerLeft();
            else if (buttonRightPressed)
                MovePlayerRight();
            return;
        }

        // ** TOUCH SCREEN CODE: **
        if (Input.touchCount != 1){
            //if no touchinput
            // ** ARROW KEY CODE: **
            // Toggle arrow key movement
            var x = Input.GetAxis("Horizontal") * Time.deltaTime * 10.0f;
            if (isServer)
                transform.Translate(-x, 0, 0); // why -x? Well I really don't know lol.
            else if (isClient)
                transform.Translate(x, 0, 0);
            return;
        }

        //moving the paddle along x-axis
        var ray = MainCamera.ScreenPointToRay(Input.touches[0].position); // Would put 'null' here, but it won't let me.
        var hitInfo = new RaycastHit();
        if(Physics.Raycast(ray,out hitInfo)){
            if (isServer && hitInfo.transform.name == "Bound_P1")
            {
                var newPos = transform.position;
                newPos.x = hitInfo.point.x;
                transform.position = newPos;
            }
            else if (isClient && hitInfo.transform.name == "Bound_P2")
            {
                var newPos = transform.position;
                newPos.x = hitInfo.point.x;
                transform.position = newPos;
            }
        }
    }

    public override void OnStartLocalPlayer()
    {
        GetComponent<MeshRenderer>().material.color = Color.red;
    }

    public void ButtonLeftAction() { buttonLeftPressed = true; }
    public void ButtonLeftStop() { buttonLeftPressed = false; }
    private void MovePlayerLeft()
    {
        var x = Time.deltaTime * 0.7f;
        transform.Translate(x, 0, 0);
        Debug.Log("MOVE L");
    }

    public void ButtonRightAction() { buttonRightPressed = true; }
    public void ButtonRightStop() { buttonRightPressed = false; }
    private void MovePlayerRight()
    {
        var x = Time.deltaTime * 0.7f;
        transform.Translate(-x, 0, 0);
        Debug.Log("MOVE R");
    }
}
