using UnityEngine;
using UnityEngine.SceneManagement;

public abstract class CoreComponent : MonoBehaviour {

    protected enum Error { SOFT_ERROR, HARD_ERROR, RETRY_ERROR };

    // Has the script completed the execution of the start method?
    private bool ready = false;

    // Has the script completed the execution of the start method?
    protected void SetReady(bool r) {
        ready = r;
    }

    // Tells if the scipt is done loading.
    public bool IsReady() {
        return ready;
    }

    // Menages errors going back to the main menu.
    protected void ManageError(Error error, int errorCode) {
        switch (error) {
            case Error.SOFT_ERROR:
                Debug.LogError("Unexpected soft error with code " + errorCode + ".");
                break;
            case Error.HARD_ERROR:
                if (GameObject.Find("Parameter Manager"))
                    GameObject.Find("Parameter Manager").GetComponent<ParameterManager>().ErrorBackToMenu(errorCode);
                else
                    Debug.LogError("Unexpected hard error with code " + errorCode + ". Can't find the Parameter Manager.");
                break;
            case Error.RETRY_ERROR:
                Debug.LogError("Unexpected error with code " + errorCode + ". The scene will be reloaded.");
                SceneManager.LoadScene(SceneManager.GetActiveScene().name);
                break;
        }
    }

    // Menages errors going back to the main menu.
    protected void ManageError(Error error, string errorMessage) {
        switch (error) {
            case Error.SOFT_ERROR:
                Debug.LogError("Unexpected soft error. " + errorMessage);
                break;
            case Error.HARD_ERROR:
                if (GameObject.Find("Parameter Manager"))
                    GameObject.Find("Parameter Manager").GetComponent<ParameterManager>().ErrorBackToMenu(errorMessage);
                else
                    Debug.LogError("Unexpected hard error. " + errorMessage + " Can't find the Parameter Manager.");
                break;
            case Error.RETRY_ERROR:
                Debug.LogError("Unexpected  error. " + errorMessage + " The scene will be reloaded.");
                SceneManager.LoadScene(SceneManager.GetActiveScene().name);
                break;
        }
    }

}