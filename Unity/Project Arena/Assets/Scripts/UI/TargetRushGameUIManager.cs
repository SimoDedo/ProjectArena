using UnityEngine;
using UnityEngine.UI;

public class TargetRushGameUIManager : GameUIManager {

    // Elements of the ready UI.
    [Header("Ready UI")] [SerializeField] private GameObject countdown;

    // Text fields of the ready UI.
    private Text countdownText;
    
    public void Start() {
        countdownText = countdown.GetComponent<Text>();

        SetReady(true);
    }

    // Sets the countdown.
    public void SetCountdown(int i) {
        if (i > 0)
            countdownText.text = i.ToString();
        else
            countdownText.text = "Figth!";
    }

    public override void SetColorAll(Color c) { }

}