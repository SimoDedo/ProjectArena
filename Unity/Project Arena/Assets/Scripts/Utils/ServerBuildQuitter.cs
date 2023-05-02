using UnityEngine;

public class ServerBuildQuitter : MonoBehaviour
{
    void Awake()
    {
        #if UNITY_SERVER && !UNITY_EDITOR
            Application.Quit(-1);
        #endif
    }
}
