//-----------------------------------------------------------------------
// Fall 2018
// CS117 Final Project
// Vroom ARoom
// By Junya Honda, Han Lee, Moo Jin Kim, Jonathan Myong 
//-----------------------------------------------------------------------

using UnityEngine;
using UnityEngine.Networking;
using GoogleARCore.Examples.CloudAnchors;

/// <summary>
/// The AR Pong GameBall
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class GameBallCloud : NetworkBehaviour
{
    /// <summary>
    /// The starting speed of our ball.
    /// </summary>
    [Range(0, 1)]
    public float Speed = 0.12f;

    /// <summary>
    /// The rate at which our ball speeds up.
    /// </summary>
    [Range(0, 0.005f)]
    public float SpeedUp = 0.0005f;

    /// <summary>
    /// The Actual speed of our ball.
    /// </summary>
    private float ActualSpeed = 0;

    /// <summary>
    /// The velocity of our ball, with magnitude and direction.
    /// </summary>
    private Vector3 velocity;

    /// <summary>
    /// The supposed position of the ball.
    /// </summary>
    [SyncVar]
    private Vector3 BallPosition;

    /// <summary>
    /// The Player 1 score
    /// </summary>
    private int m_P1Score = 0;

    /// <summary>
    /// The Player 2 score
    /// </summary>
    private int m_P2Score = 0;

    /// <summary>
    /// The synced, new player 1 score.
    /// </summary>
    [SyncVar]
    private int m_newP1Score = 0;

    /// <summary>
    /// The synced, new player 2 score.
    /// </summary>
    [SyncVar]
    private int m_newP2Score = 0;

    /// <summary>
    /// The Max score for the AR Pong Game.
    /// </summary>
    private int m_MaxScore = 0;

    /// <summary>
    /// The lift required by the GameField.
    /// </summary>
    private const float ScaleLift = 0.5f;

    /// <summary>
    /// The Game Manager for our AR Pong Game.
    /// </summary>
    private GameManager GManager;

    /// <summary>
    /// The instantiated GameField object.
    /// </summary>
    private GameObject GameField;

    /// <summary>
    /// Start this instance.
    /// </summary>
    void Start()
    {
        GManager = GameObject.FindWithTag("GameManager").GetComponent(typeof(GameManager)) as GameManager;
        GameField = GameObject.FindWithTag("GameField");

        m_P1Score = 0;
        m_P2Score = 0;
        m_newP1Score = 0;
        m_newP2Score = 0;
        m_MaxScore = GManager.GetMaxScore();

        ResetBall();
        GManager.NewGame();
    }

    /// <summary>
    /// Update this instance.
    /// </summary>
    void Update()
    {
        if (m_newP2Score != m_P2Score)
        {
            m_P2Score = m_newP2Score;
            GManager.IncrementScore("Bound_P1");
        }
        if (m_newP1Score != m_P1Score)
        {
            m_P1Score = m_newP1Score;
            GManager.IncrementScore("Bound_P2");
        }
        if (m_P1Score >= m_MaxScore || m_P2Score >= m_MaxScore)
            GManager.GameOverMode();
    }

    /// <summary>
    /// Fixed updates.
    /// </summary>
    void FixedUpdate()
    {
        if (isServer)
        {
            velocity = velocity.normalized * ActualSpeed;
            BallPosition += velocity;
        }
        transform.localPosition = BallPosition;
        ActualSpeed += SpeedUp;
    }

    /// <summary>
    /// Resets the ball position.
    /// </summary>
    public void ResetBall()
    {
        Renderer BallRender = GetComponent<Renderer>();
        BallRender.enabled = false;

        transform.SetParent(GameField.gameObject.transform, false);
        ActualSpeed = Speed;

        if (!isServer)
        {
            transform.localPosition = new Vector3(0, ScaleLift, 0);
            BallRender.enabled = true;
            return;
        }

        BallPosition = new Vector3(0, ScaleLift, 0);
        transform.localPosition = BallPosition;

        float initial_x = Random.Range(0, 2) * 2f - 1f * Random.Range(0.2f, 1f);
        float initial_z = Random.Range(0, 2) * 2f - 1f;
        velocity = new Vector3(initial_x, 0, initial_z);

        BallRender.enabled = true;
    }

    /// <summary>
    /// Destroys the ball.
    /// </summary>
    public void DestroyBall()
    {
        Destroy(gameObject);
    }

    /// <summary>
    /// Stops the ball.
    /// </summary>
    public void StopBall()
    {
        velocity = new Vector3(0, 0, 0);
    }

    /// <summary>
    /// Handles the behavior on collision to an object.
    /// </summary>
    /// <param name="collision">The Collision object the ball collided into.</param>
    private void OnCollisionEnter(Collision collision)
    {
        if (!isServer)
            return;

        switch (collision.transform.name)
        {
            // bouncing off wall
            case "Bound_Right":
            case "Bound_Left":
                velocity.x *= -1f;
                return;
            // scoring&resetting
            case "Bound_P1":
                m_newP2Score++;
                ResetBall();
                return;
            case "Bound_P2":
                m_newP1Score++;
                ResetBall();
                return;
            // bouncing off paddle
            case "PlayerPaddle(Clone)":
                velocity.z *= -1f;
                return;
        }
    }
}
