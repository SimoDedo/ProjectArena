using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuUIManager : MonoBehaviour {

    // List of scenes name
    [SerializeField] List<string> sceneNames;

    public void LoadScene(int scene) {
        SceneManager.LoadScene(sceneNames[scene]);
    }

    public void Exit() {
        Application.Quit();
    }

}