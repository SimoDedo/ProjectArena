using UnityEngine;
using UnityEngine.UI;

public class GameGUIManager : CoreComponent {

    [SerializeField] private GameObject readyGUI;
    [SerializeField] private GameObject figthGUI;
    [SerializeField] private GameObject scoreGUI;

    // Elements of the ready GUI.
    [SerializeField] private GameObject player1;
    [SerializeField] private GameObject player2;
    [SerializeField] private GameObject countdown;

    // Elements of the figth GUI.
    [SerializeField] private GameObject health;
    [SerializeField] private GameObject ammo;
    [SerializeField] private GameObject time;
    [SerializeField] private GameObject killsPlayer1;
    [SerializeField] private GameObject killsPlayer2;

    // Elements of the score GUI.
    [SerializeField] private GameObject player1Wins;
    [SerializeField] private GameObject player2Wins;
    [SerializeField] private GameObject tie;

    private string namePlayer1;
    private string namePlayer2;

    // Text fields of the ready GUI.
    private Text countdownText;

    // Text fields of the figth GUI.
    private Text healthText;
    private Text ammoText;
    private Text timeText;
    private Text killsPlayer1Text;
    private Text killsPlayer2Text;

    void Start() {
        healthText = health.GetComponent<Text>();
        ammoText = ammo.GetComponent<Text>();
        timeText = time.GetComponent<Text>();
        killsPlayer1Text = killsPlayer1.GetComponent<Text>();
        killsPlayer2Text = killsPlayer2.GetComponent<Text>();

        countdownText = countdown.GetComponent<Text>();

        SetReady(true);
    }

    // Activates the ready GUI.
    public void ActivateReadyGUI() {
        readyGUI.SetActive(true);
        figthGUI.SetActive(false);
        scoreGUI.SetActive(false);
    }

    // Activates the figth GUI.
    public void ActivateFigthGUI() {
        readyGUI.SetActive(false);
        figthGUI.SetActive(true);
        scoreGUI.SetActive(false);
    }

    // Activates the score GUI.
    public void ActivateScoreGUI() {
        readyGUI.SetActive(false);
        figthGUI.SetActive(false);
        scoreGUI.SetActive(true);
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

    // Sets the ready GUI.
    public void SetReadyGUI() {
        player1.GetComponent<Text>().text = namePlayer1;
        player2.GetComponent<Text>().text = namePlayer1;
    }

    // Sets the score GUI.
    public void SetScoreGUI(int s1, int s2) {
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

    // Sets the health.
    public void SetHealth(int health, int tot) {
        healthText.text = health.ToString() + "/" + tot.ToString();
    }

    // Sets the ammo.
    public void SetAmmo(int charger, int tot) {
        ammoText.text = charger.ToString() + "/" + tot.ToString();
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