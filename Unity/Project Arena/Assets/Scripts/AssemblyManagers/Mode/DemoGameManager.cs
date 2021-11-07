using UnityEngine;

/// <summary>
/// DemoGameManager is an implementation of GameManager. It simply generates a new map every generationInterval seconds.
/// </summary>
public class DemoGameManager : GameManager {

    [Header("Demo variables")] [SerializeField] [Range(0, 10)] protected float generationInterval;

    private float lastGenerationTime;

    private void Start() {
        /* #if UNITY_EDITOR
            UnityEditor.SceneView.FocusWindowIfItsOpen(typeof(UnityEditor.SceneView));
        #endif */
    }

    private void Update() {
        if (!IsReady() && mapManagerScript.IsReady()) {
            mapManagerScript.ManageMap(true);
            lastGenerationTime = Time.time;
            SetReady(true);
        } else if (IsReady() && (Time.time - lastGenerationTime > generationInterval)) {
            mapManagerScript.ResetMap();
            mapManagerScript.ManageMap(true);
            lastGenerationTime = Time.time;
        }

        if (Input.GetKeyDown(KeyCode.Escape)) {
            Quit();
        }
    }

    public override void AddScore(int i, int j) { }

    public override void ManageEntityDeath(GameObject g, Entity e) { }

    public override void Pause() { }

    public override void SetUIColor(Color c) { }

    protected override void ManageGame() { }

    protected override void UpdateGamePhase() { }

}