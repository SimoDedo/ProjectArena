using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TargetRushGameUIManager : GameUIManager {

    // Elements of the ready UI.
    [Header("Ready UI")] [SerializeField] private Text countdown;

    // Elements of the figth UI.
    [Header("Figth UI")] [SerializeField] private Text time;
    [SerializeField] private Text score;
    [SerializeField] private Text wave;
    [SerializeField] private Text targets;

    // Elements of the score UI.
    [Header("Score UI")] [SerializeField] private GameObject gameover;
    [SerializeField] private GameObject victory;
    [SerializeField] private Text finalScore;
    [SerializeField] private Text finalWave;

    private Queue<string> additiveTimeQueue = new Queue<string>();
    private Queue<string> additiveScoreQueue = new Queue<string>();
    private float addedTimeTime = 0f;
    private float addedScoreTime = 0f;
    private bool addedTimeDisplayed = false;
    private bool addedScoreDisplayed = false;
    private string additiveTime = "";
    private string additiveScore = "";

    public void Start() {
        SetReady(true);
    }

    public void Update() {
        if (figthUI.activeSelf) {
            // Menage the time adder.
            if (addedTimeDisplayed) {
                if (Time.time > addedTimeTime + 1f) {
                    additiveTime = "";
                    time.text.Replace(additiveTime, "");
                    addedTimeDisplayed = false;
                }
            } else if (additiveTimeQueue.Count > 0) {
                additiveTime = additiveTimeQueue.Dequeue();
                time.text += additiveTime;
                addedTimeDisplayed = true;
                addedTimeTime = Time.time;
            }
            // Menage the score adder.
            if (addedScoreDisplayed) {
                if (Time.time > +1f) {
                    additiveScore = "";
                    score.text.Replace(additiveScore, "");
                    addedScoreDisplayed = false;
                }
            } else if (additiveScoreQueue.Count > 0) {
                additiveScore = additiveScoreQueue.Dequeue();
                score.text += additiveScore;
                addedScoreDisplayed = true;
                addedScoreTime = Time.time;
            }
        }
    }

    // Sets the countdown.
    public void SetCountdown(int i) {
        if (i > 0)
            countdown.text = i.ToString();
        else
            countdown.text = "Figth!";
    }

    // Sets the remaining time.
    public void SetTime(int t) {
        string minutes = TimeToString(t / 60);
        string seconds = TimeToString(t % 60);

        time.text = minutes + ":" + seconds + additiveTime;
    }

    public override void SetColorAll(Color c) {
        time.color = c;
        score.color = c;
        wave.color = c;
        targets.color = c;
    }

    public void SetScore(int s) {
        score.text = "Score: " + s + additiveScore;
    }

    public void SetTargets(int t) {
        targets.text = "Targets: " + t;
    }

    public void SetFinalScore(int s) {
        finalScore.text = "Score: " + s;
    }

    public void SetFinalWave(int w) {
        finalWave.text = "Wave: " + w;
    }

    public void SetWave(int w) {
        wave.text = "Wave: " + w;
    }

    public void SetVictory(bool b) {
        if (b)
            victory.SetActive(true);
        else
            gameover.SetActive(true);
    }

    public void AddTime(int t) {
        additiveTimeQueue.Enqueue(" + " + t);
    }

    public void AddScore(int s) {
        additiveScoreQueue.Enqueue(" + " + s);
    }

}