using UnityEngine;
using UnityEngine.UI;

public class TutorialGameUIManager : GameUIManager {

    // Elements of the ready UI.
    [Header("Ready UI")] [SerializeField] private Text countdown;

    public void Start() {
        SetReady(true);
    }

    // Sets the countdown.
    public void SetCountdown(int i) {
        if (i > 0)
            countdown.text = i.ToString();
        else
            countdown.text = "Fight!";
    }

    public override void SetColorAll(Color c) {
    }

}