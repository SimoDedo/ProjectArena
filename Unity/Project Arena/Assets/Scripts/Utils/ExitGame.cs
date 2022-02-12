using UnityEngine;

namespace Utils
{
    /// <summary>
    /// Quits the game.
    /// </summary>
    public class ExitGame : MonoBehaviour
    {
        private void Start()
        {
            Application.Quit();
        }
    }
}