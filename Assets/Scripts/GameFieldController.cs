//-----------------------------------------------------------------------
// Fall 2018
// CS117 Final Project
// Vroom ARoom
// By Junya Honda, Han Lee, Moo Jin Kim, Jonathan Myong 
//-----------------------------------------------------------------------

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The Game Field Controller, used as a shell for the GameField
/// </summary>
public class GameFieldController : MonoBehaviour {

    /// <summary>
    /// The ball spawner object.
    /// </summary>
    public PongBallSpawner BallSpawnerObject;

    /// <summary>
    /// The scale ratio between default size and the optimal size for AR.
    /// </summary>
    [Range(0, 0.1f)]
    public float ScaleRatio;

    private void Start()
    {
        transform.localScale = new Vector3(ScaleRatio, ScaleRatio, ScaleRatio);
    }

    /// <summary>
    /// Gets the scale ratio.
    /// </summary>
    /// <returns>The scale ratio.</returns>
    public float GetScaleRatio()
    {
        return ScaleRatio;
    }

    /// <summary>
    /// Starts the GameBall using the Ball Spawner.
    /// </summary>
    public void OnBallStart()
    {
        BallSpawnerObject.StartGameBall();
    }

    /// <summary>
    /// Ends the Gameball using the Ball Spawner.
    /// </summary>
    public void OnBallEnd()
    {
        BallSpawnerObject.EndGameBall();
    }
}
