using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    /// <summary>
    ///     DuelGameUIManager is an implementation of GameUIManager. The figth UI explains the controls.
    /// </summary>
    public class TutorialGameUIManager : GameUIManager
    {
        // Elements of the ready UI.
        [Header("Ready UI")] [SerializeField] private Text countdown;

        // Elements of the figth UI.
        [Header("Figth UI")] [SerializeField] private GameObject[] commands;

        public void Start()
        {
            SetReady(true);
        }

        // Sets the countdown.
        public void SetCountdown(int i)
        {
            if (i > 0)
                countdown.text = i.ToString();
            else
                countdown.text = "Fight!";
        }

        public override void SetColorAll(Color c)
        {
            foreach (var g in commands)
            {
                var texts = g.GetComponentsInChildren<Text>();
                var images = g.GetComponentsInChildren<Image>();
                foreach (var t in texts) t.color = new Color(c.r, c.g, c.b, t.color.a);
                foreach (var i in images) i.color = new Color(c.r, c.g, c.b, i.color.a);
            }
        }
    }
}