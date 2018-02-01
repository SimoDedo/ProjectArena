using UnityEngine;

[RequireComponent(typeof(Light))]
public class LigthSetter : MonoBehaviour {

    [Header("Standalone settings")] [SerializeField] LightShadows standaloneShadowType;
    [SerializeField] float standaloneShadowIntensity;

    [Header("Web settings")] [SerializeField] LightShadows webShadowType;
    [SerializeField] float webShadowIntensity;

    private Light sceneLigth;

    void Start() {
        sceneLigth = gameObject.GetComponent<Light>();

        if (Application.isWebPlayer) {
            sceneLigth.shadows = webShadowType;
            sceneLigth.intensity = webShadowIntensity;
        } else {
            sceneLigth.shadows = standaloneShadowType;
            sceneLigth.intensity = standaloneShadowIntensity;
        }
    }

}
