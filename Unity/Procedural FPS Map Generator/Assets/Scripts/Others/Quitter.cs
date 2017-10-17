using UnityEngine;
using UnityEngine.SceneManagement;

public class Quitter : MonoBehaviour {

    void Update() {
        if (Input.GetKeyDown(KeyCode.Return)) {
            Cursor.lockState = CursorLockMode.None;
            SceneManager.LoadScene("Menu");
        }
    }

}
