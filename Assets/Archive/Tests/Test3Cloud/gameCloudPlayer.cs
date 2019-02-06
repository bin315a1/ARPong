using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class gameCloudPlayer : NetworkBehaviour
{

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
        //// Since both paddles use the same script, we return if the script
        //// is currently referring to the other paddle.
        if (!isLocalPlayer)
            return;

        //// ** ARROW KEY CODE: **
        //// Toggle arrow key movement
        //var x = Input.GetAxis("Horizontal") * Time.deltaTime * 10.0f;
        //if (gameObject.name == "Player_Paddle1(Clone)")
        //    transform.Translate(-x, 0, 0); // why -x? Well I really don't know lol.


        //// ** TOUCH SCREEN CODE: **
        //if (Input.touchCount != 1)
        //{
        //    //if no touchinput
        //    return;
        //}
        ////moving the paddle along x-axis
        //var ray = cam1.ScreenPointToRay(Input.touches[0].position); // Would put 'null' here, but it won't let me.
        //if (gameObject.name == "Player_Paddle1(Clone)")
        //    ray = cam1.ScreenPointToRay(Input.touches[0].position);
        //var hitInfo = new RaycastHit();
        //if (Physics.Raycast(ray, out hitInfo))
        //{
        //    if ((gameObject.name == "Player_Paddle1(Clone)" && hitInfo.transform.name == "Bound_P1"))
        //    {
        //        var newPos = transform.position;
        //        newPos.x = hitInfo.point.x;
        //        transform.position = newPos;
        //    }
        //}
    }
}
