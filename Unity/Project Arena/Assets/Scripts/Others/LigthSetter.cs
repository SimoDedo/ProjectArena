using UnityEngine;

namespace Others
{
    /// <summary>
    ///     LigthSetter sets the main ligth of the current scene depending on the build.
    /// </summary>
    [RequireComponent(typeof(Light))]
    public class LigthSetter : MonoBehaviour
    {
        [Header("Standalone settings")] [SerializeField]
        private LightShadows standaloneShadowType;

        [SerializeField] private float standaloneLightIntensity;

        [Header("Web settings")] [SerializeField]
        private LightShadows webShadowType;

        [SerializeField] private float webLightIntensity;

        private Light sceneLigth;

        private void Start()
        {
            sceneLigth = gameObject.GetComponent<Light>();

            if (Application.platform == RuntimePlatform.WebGLPlayer)
            {
                sceneLigth.shadows = webShadowType;
                sceneLigth.intensity = webLightIntensity;
            }
            else
            {
                sceneLigth.shadows = standaloneShadowType;
                sceneLigth.intensity = standaloneLightIntensity;
            }
        }
    }
}