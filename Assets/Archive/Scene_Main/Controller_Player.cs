using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class Controller_Player : NetworkBehaviour
{

    public GameManager gameManager;
    public Camera cam1;
    public Camera cam2;
    public GoogleARCore.Examples.CloudAnchors.OurCloudController CloudController;

    // Initializes variables when the script is run for the first time, per instantiated object.
    // (This will actually run twice since this script is attached to two
    // prefabs, but that's okay.)
    void Start() {
        cam1 = GameObject.FindWithTag("Camera1").GetComponent(typeof(Camera)) as Camera;
        //cam1 = GameObject.FindWithTag("MainCamera").GetComponent(typeof(Camera)) as Camera;
        cam2 = GameObject.FindWithTag("Camera2").GetComponent(typeof(Camera)) as Camera;
        gameManager = GameObject.FindWithTag("GameManager").GetComponent(typeof(GameManager)) as GameManager;
        gameManager.StartNewGame();
    }

    void Update (){

        // Since both paddles use the same script, we return if the script
        // is currently referring to the other paddle.
        if (!isLocalPlayer)
            return;
            
        // Set cameras for both players.
        if (gameObject.name == "Player_Paddle1(Clone)") {  // The "(Clone)" part of the name
            cam1.enabled = true;                           // is automatically attached to an
            cam2.enabled = false;                          // instantiated prefab.
        } else if (gameObject.name == "Player_Paddle2(Clone)") {
            cam1.enabled = false;
            cam2.enabled = true;
        }

        // ** ARROW KEY CODE: **
        // Toggle arrow key movement
        var x = Input.GetAxis("Horizontal") * Time.deltaTime * 10.0f;
        if (gameObject.name == "Player_Paddle1(Clone)")
        //if (CloudController.GetCurrentMode() == GoogleARCore.Examples.CloudAnchors.OurCloudController.ApplicationMode.Hosting)
           transform.Translate(-x, 0, 0); // why -x? Well I really don't know lol.
        if (gameObject.name == "Player_Paddle2(Clone)")
        //if (CloudController.GetCurrentMode() == GoogleARCore.Examples.CloudAnchors.OurCloudController.ApplicationMode.Resolving)
            transform.Translate(x, 0, 0);

        // ** TOUCH SCREEN CODE: **
        if(Input.touchCount != 1){
            //if no touchinput
            return;
        }

        //moving the paddle along x-axis
        var ray = cam1.ScreenPointToRay(Input.touches[0].position); // Would put 'null' here, but it won't let me.
        if (gameObject.name == "Player_Paddle1(Clone)")
            ray = cam1.ScreenPointToRay(Input.touches[0].position);
        if (gameObject.name == "Player_Paddle2(Clone)")
            ray = cam2.ScreenPointToRay(Input.touches[0].position);
        var hitInfo = new RaycastHit();
        if(Physics.Raycast(ray,out hitInfo)){
            if ((gameObject.name == "Player_Paddle1(Clone)" && hitInfo.transform.name == "Bound_P1") ||
                (gameObject.name == "Player_Paddle2(Clone)" && hitInfo.transform.name == "Bound_P2"))
            //if (CloudController.GetCurrentMode() == GoogleARCore.Examples.CloudAnchors.OurCloudController.ApplicationMode.Hosting)
            {
                //if (hitInfo.transform.name == "Bound_P1")
                //{
                    var newPos = transform.position;
                    newPos.x = hitInfo.point.x;
                    transform.position = newPos;
                //}
            }
            //else if (CloudController.GetCurrentMode() == GoogleARCore.Examples.CloudAnchors.OurCloudController.ApplicationMode.Resolving)
            //{
                //if (hitInfo.transform.name == "Bound_P2")
                //{
                    //var newPos = transform.position;
                    //newPos.x = hitInfo.point.x;
                    //transform.position = newPos;
                //}
            //}
        }
    }
}
