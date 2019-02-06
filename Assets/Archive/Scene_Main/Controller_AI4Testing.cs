using UnityEngine;

public class Controller_AI4Testing : MonoBehaviour {
    public Transform GameBall;
    
	// Update is called once per frame
	void FixedUpdate () {
        //Debug.Log(GameBall.position.x);
        Vector3 newPos = transform.position;
        newPos.x = Mathf.Lerp(transform.position.x, GameBall.position.x, 1);
        transform.position = newPos;
	}
}
