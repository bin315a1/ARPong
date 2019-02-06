using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class GameBall : MonoBehaviour {

    public GameManager gameManager;

    Vector3 velocity;
    [Range(0,1)]
    public float speed = 0.1f;

	void Start (){
        ResetBall();   //initializing pos. & vel. as function for reusability
    }

    public void ResetBall(){
        transform.position = Vector3.zero;

        //setting starting position & velocity of game ball
        float initial_z = Random.Range(0, 2) * 2f - 1f;
        float initial_x = Random.Range(0, 2) * 2f - 1f * Random.Range(0.2f, 1f);
        velocity = new Vector3(initial_x, 0, initial_z);
    }
	
	void FixedUpdate (){
        velocity = velocity.normalized * speed;
        transform.position += velocity;
	}

    public void StopBall() {
        velocity = new Vector3(0, 0, 0);
    }

    private void OnCollisionEnter(Collision collision){
        switch (collision.transform.name)
        {
            //bouncing off wall
            case "Bound_Right":
            case "Bound_Left":
                velocity.x *= -1f;
                return;
            //scoring&resetting
            case "Bound_P1":
            case "Bound_P2":
                ResetBall();
                gameManager.IncrementScore(collision.transform.name);
                return;
            //bouncing off paddle
            case "Player_Paddle1(Clone)":
            case "Player_Paddle2(Clone)":
                velocity.z *= -1f;
                return;
        }
    }
}
