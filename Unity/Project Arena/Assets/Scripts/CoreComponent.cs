using UnityEngine;

public abstract class CoreComponent : MonoBehaviour {

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

}