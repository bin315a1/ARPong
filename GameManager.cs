//-----------------------------------------------------------------------
// Fall 2018
// CS117 Final Project
// Vroom ARoom
// By Junya Honda, Han Lee, Moo Jin Kim, Jonathan Myong 
//-----------------------------------------------------------------------

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The Game Manager for our AR Pong game.
/// </summary>
public class GameManager : MonoBehaviour {

    /// <summary>
    /// The player1 score text.
    /// </summary>
    public Text txtPlayer1Score;

    /// <summary>
    /// The player2 score text.
    /// </summary>
    public Text txtPlayer2Score;

    /// <summary>
    /// The game over message text.
    /// </summary>
    public Text txtGameOverMessage;

    public GameObject Player1Score;
    public GameObject Player2Score;
    public GameObject GameMessage;


    /// <summary>
    /// The Score required to win the game.
    /// </summary>
    [Range(0, 10)]
    public int MaxScore = 5;

    /// <summary>
    /// The flag signifying whether the game was playing and is now finished.
    /// </summary>
    private bool gameOver = false;

    /// <summary>
    /// The flag signifying whether the game is currently playing
    /// </summary>
    private bool gameCurPlaying = false;

    /// <summary>
    /// The Player Scores.
    /// </summary>
    private int player1Score = 0, player2Score = 0;

    /// <summary>
    /// The game ball.
    /// </summary>
    private GameBallCloud GameBall;

    /// <summary>
    /// The Game Field.
    /// </summary>
    private GameFieldController GameField;

    /// <summary>
    /// Start this instance.
    /// </summary>
    public void Start()
    {
        Player1Score.SetActive(false);
        txtPlayer1Score.text = "";
        Player2Score.SetActive(false);
        txtPlayer2Score.text = "";
        GameMessage.SetActive(true);
        txtGameOverMessage.text = "Game has not started";
    }

    /// <summary>
    /// Configures the UI for a new game.
    /// </summary>
    public void NewGame()
    {
        gameOver = false;
        gameCurPlaying = true;
        player1Score = 0;
        player2Score = 0;

        Player1Score.SetActive(true);
        txtPlayer1Score.text = "Player 1: " + player1Score;
        Player2Score.SetActive(true);
        txtPlayer2Score.text = "Player 2: " + player2Score;
        GameMessage.SetActive(false);
        txtGameOverMessage.text = "";
    }

    /// <summary>
    /// Starts a new AR Pong game.
    /// </summary>
    public void StartNewGame()
    {
        NewGame();

        GameField = GameObject.FindWithTag("GameField").GetComponent(typeof(GameFieldController)) as GameFieldController;
        if (GameField != null)
            GameField.OnBallStart();
        GameBall = GameObject.FindWithTag("GameBall").GetComponent(typeof(GameBallCloud)) as GameBallCloud;
    }

    /// <summary>
    /// Stops the AR Pong Game.
    /// </summary>
    public void StopGame()
    {
        Player1Score.SetActive(false);
        txtPlayer1Score.text = "";
        Player2Score.SetActive(false);
        txtPlayer2Score.text = "";
        GameMessage.SetActive(true);
        txtGameOverMessage.text = "Game has not started";

        GameField = GameObject.FindWithTag("GameField").GetComponent(typeof(GameFieldController)) as GameFieldController;
        if (GameField != null)
            GameField.OnBallEnd();

        gameCurPlaying = false;
    }

    /// <summary>
    /// Increments the score.
    /// </summary>
    /// <param name="colliderName">Name used in calculating which side wins the point.</param>
    public void IncrementScore(string colliderName)
    {
        switch (colliderName) {
            case "Bound_P1":
                player2Score++;
                txtPlayer2Score.text = "Player 2: " + player2Score;
                break;
            case "Bound_P2":
                player1Score++;
                txtPlayer1Score.text = "Player 1: " + player1Score;
                break;
        }
        if (!gameOver) {
            txtGameOverMessage.text = "";
        }
    }

    /// <summary>
    /// Configures settings for when game is over.
    /// </summary>
    public void GameOverMode()
    {
        gameOver = true;
        gameCurPlaying = false;
        if (player1Score == MaxScore)
        {
            GameMessage.SetActive(true);
            txtGameOverMessage.text = "GAME OVER: Player 1 Wins!";
        }
        else if (player2Score == MaxScore)
        {
            GameMessage.SetActive(true);
            txtGameOverMessage.text = "GAME OVER: Player 2 Wins!";
        }
        if (GameBall != null)
            GameBall.StopBall();
    }

    /// <summary>
    /// Gets the max score for the AR Pong Game.
    /// </summary>
    /// <returns>The max score.</returns>
    public int GetMaxScore()
    {
        return MaxScore;
    }

    /// <summary>
    /// Check for if the game is over.
    /// </summary>
    /// <returns><c>true</c>, if game was over after playing, <c>false</c> otherwise.</returns>
    public bool IsGameOver()
    {
        return gameOver;
    }

    /// <summary>
    /// Check for if the game is playing.
    /// </summary>
    /// <returns><c>true</c>, if the game is currently playing, <c>false</c> otherwise.</returns>
    public bool IsGamePlaying()
    {
        return gameCurPlaying;
    }
}
