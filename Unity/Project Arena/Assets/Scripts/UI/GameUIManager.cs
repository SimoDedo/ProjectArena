using UnityEngine;
using UnityEngine.UI;

public abstract class GameUIManager : CoreComponent {

    [SerializeField] protected GameObject readyUI;
    [SerializeField] protected GameObject fightUI;
    [SerializeField] protected GameObject scoreUI;
    [SerializeField] protected GameObject pauseUI;

    [SerializeField] protected FadeUI fadeUIScript;

    // Elements of the pause UI.
    [Header("Pause UI")] [SerializeField] protected Slider sensibilitySlider;

    // Activates the ready UI.
    public void ActivateReadyUI() {
        readyUI.SetActive(true);
        fightUI.SetActive(false);
        scoreUI.SetActive(false);
    }

    // Activates the fight UI.
    public void ActivateFightUI() {
        readyUI.SetActive(false);
        fightUI.SetActive(true);
        scoreUI.SetActive(false);
    }

    // Activates the score UI.
    public void ActivateScoreUI() {
        pauseUI.SetActive(false);
        readyUI.SetActive(false);
        fightUI.SetActive(false);
        scoreUI.SetActive(true);
    }

    // Activates or deactivates the pause UI.
    public void ActivatePauseUI(bool b) {
        UpdateSensibility();
        fightUI.SetActive(!b);
        pauseUI.SetActive(b);
    }

    // Converts seconds and minutes to text and adds extra 0 if needed.
    protected string TimeToString(int t) {
        string s = t.ToString();

        if (t < 0)
            return "00";
        else if (s.Length > 1)
            return s;
        else
            return "0" + s;
    }

    public void Fade(float min, float max, bool mustLigthen, float duration) {
        fadeUIScript.StartFade(min, max, mustLigthen, duration);
    }

    public abstract void SetColorAll(Color c);

    // Updates the sensibilitiy slider.
    private void UpdateSensibility() {
        if (PlayerPrefs.HasKey("MouseSensibility")) {
            sensibilitySlider.value = PlayerPrefs.GetFloat("MouseSensibility");
        } else
            PlayerPrefs.SetFloat("MouseSensibility", sensibilitySlider.value);
    }

    // Initializes the sensibilitiy slider.
    public void SetMouseSensibility() {
        PlayerPrefs.SetFloat("MouseSensibility", sensibilitySlider.value);
        Player playerScript = GameObject.Find("Player").GetComponent<Player>();
        if (playerScript != null)
            playerScript.SetSensibility(sensibilitySlider.value);
    }

}