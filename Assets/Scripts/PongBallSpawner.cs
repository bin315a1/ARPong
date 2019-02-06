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
/// The GameBall Spawner, used to create and destroy the GameBall.
/// </summary>
public class PongBallSpawner : NetworkBehaviour
{
    /// <summary>
    /// The ball prefab.
    /// </summary>
    public GameObject BallPrefab;

    /// <summary>
    /// The Game Field
    /// </summary>
    public GameObject GameField;

    /// <summary>
    /// Start this instance.
    /// </summary>
    void Start()
    {
        ClientScene.RegisterPrefab(BallPrefab);
    }

    /// <summary>
    /// Starts the game ball.
    /// </summary>
    public void StartGameBall()
    {
        float ScaleLift = 0.5f * GameField.GetComponent<GameFieldController>().GetScaleRatio();
        float new_x = GameField.transform.position.x;
        float new_y = GameField.transform.position.y + ScaleLift;
        float new_z = GameField.transform.position.z;

        var spawnPosition = new Vector3(new_x, new_y, new_z);
        var spawnRotation = Quaternion.Euler(0, 0, 0);

        var ball = (GameObject)Instantiate(BallPrefab, spawnPosition, spawnRotation);
        NetworkServer.Spawn(ball);
    }

    /// <summary>
    /// Ends the game ball.
    /// </summary>
    public void EndGameBall()
    {
        GameBallCloud CurrentBall = GameObject.FindWithTag("GameBall").GetComponent(typeof(GameBallCloud)) as GameBallCloud;
        if (CurrentBall != null)
        {
            CurrentBall.DestroyBall();
        }
    }
}
