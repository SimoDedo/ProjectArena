using UnityEngine;
using UnityEngine.UI;

public class GameUIManager : CoreComponent {

    // Elements of the ready UI.
    [Header("Ready UI")] [SerializeField] private GameObject readyUI;
    [SerializeField] private GameObject player1;
    [SerializeField] private GameObject player2;
    [SerializeField] private GameObject countdown;

    // Elements of the figth UI.
    [Header("Figth UI")] [SerializeField] private GameObject figthUI;
    [SerializeField] private GameObject time;
    [SerializeField] private GameObject killsPlayer1;
    [SerializeField] private GameObject killsPlayer2;

    // Elements of the score UI.
    [Header("Score UI")] [SerializeField] private GameObject scoreUI;
    [SerializeField] private GameObject player1Wins;
    [SerializeField] private GameObject player2Wins;
    [SerializeField] private GameObject tie;

    private string namePlayer1;
    private string namePlayer2;

    // Text fields of the ready UI.
    private Text countdownText;

    // Text fields of the figth UI.
    private Text timeText;
    private Text killsPlayer1Text;
    private Text killsPlayer2Text;

    void Start() {
        timeText = time.GetComponent<Text>();
        killsPlayer1Text = killsPlayer1.GetComponent<Text>();
        killsPlayer2Text = killsPlayer2.GetComponent<Text>();

        countdownText = countdown.GetComponent<Text>();

        SetReady(true);
    }

    // Activates the ready UI.
    public void ActivateReadyUI() {
        readyUI.SetActive(true);
        figthUI.SetActive(false);
        scoreUI.SetActive(false);
    }

    // Activates the figth UI.
    public void ActivateFigthUI() {
        readyUI.SetActive(false);
        figthUI.SetActive(true);
        scoreUI.SetActive(false);
    }

    // Activates the score UI.
    public void ActivateScoreUI() {
        readyUI.SetActive(false);
        figthUI.SetActive(false);
        scoreUI.SetActive(true);
    }

    // Sets the player names.
    public void SetPlayersName(string p1, string p2) {
        namePlayer1 = p1;
        namePlayer2 = p2;
    }

    // Sets the countdown.
    public void SetCountdown(int i) {
        if (i > 0)
            countdownText.text = i.ToString();
        else
            countdownText.text = "Figth!";
    }

    // Sets the ready UI.
    public void SetReadyUI() {
        player1.GetComponent<Text>().text = namePlayer1;
        player2.GetComponent<Text>().text = namePlayer2;
    }

    // Sets the score UI.
    public void SetScoreUI(int s1, int s2) {
        if (s1 > s2) {
            player1Wins.SetActive(true);
            player2Wins.SetActive(false);
            tie.SetActive(false);

            player1Wins.GetComponent<Text>().text = namePlayer1 + " wins!";
        } else if (s1 < s2) {
            player1Wins.SetActive(false);
            player2Wins.SetActive(true);
            tie.SetActive(false);

            player2Wins.GetComponent<Text>().text = namePlayer2 + " wins!";
        } else {
            player1Wins.SetActive(false);
            player2Wins.SetActive(false);
            tie.SetActive(true);
        }
    }

    // Sets the remaining time.
    public void SetTime(int time) {
        string minutes = TimeToString(time / 60);
        string seconds = TimeToString(time % 60);

        timeText.text = minutes + ":" + seconds;
    }

    // Sets the kills of the first player.
    public void SetPlayer1Kills(int i) {
        killsPlayer1Text.text = i.ToString();
    }

    // Sets the kills of the second player.
    public void SetPlayer2Kills(int i) {
        killsPlayer2Text.text = i.ToString();
    }

    // Converts seconds and minutes to text and adds extra 0 if needed.
    private string TimeToString(int t) {
        string s = t.ToString();

        if (t < 0)
            return "00";
        else if (s.Length > 1)
            return s;
        else
            return "0" + s;
    }

}