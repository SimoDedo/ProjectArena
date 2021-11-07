using UnityEngine;

public class ExperimentFrameController : MonoBehaviour
{
    private void Start()
    {
        if (Application.isBatchMode)
        {
            Time.captureFramerate = 30;
        }
    }
}
