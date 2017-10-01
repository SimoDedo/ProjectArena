using UnityEngine;

public abstract class MapAssebler : MonoBehaviour {

    // Has the script completed the execution of the start method?
    protected bool ready = false;

    public abstract void AssembleMap(char[,] map, float squareSize, float h);

    // Tells if the scipt is done loading.
    public bool IsReady() {
        return ready;
    }

}