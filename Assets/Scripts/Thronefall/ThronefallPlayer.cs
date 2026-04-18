using UnityEngine;

public class ThronefallBuildNodeMarker : MonoBehaviour
{
    public int nodeIndex;
}

public class ThronefallPlayer : MonoBehaviour
{
    const float MoveSpeed = 8f;

    CharacterController cc;
    GameObject visual;
    ThronefallBuildNodeMarker currentNearMarker;

    void Start()
    {
        cc = gameObject.AddComponent<CharacterController>();
        cc.height = 2f;
        cc.radius = 0.4f;
        cc.center = new Vector3(0, 1f, 0);

        var game = ThronefallGame.Instance;

        // Capsule visual using scaled cube
        visual = ProceduralMeshUtil.CreatePrimitive("PlayerVisual", game.CubeMesh, game.PlayerMat, transform);
        visual.transform.localPosition = new Vector3(0, 1f, 0);
        visual.transform.localScale = new Vector3(0.8f, 2f, 0.8f);

        // Horse indicator (smaller cube behind player)
        var horse = ProceduralMeshUtil.CreatePrimitive("Horse", game.CubeMesh, game.PlayerMat, transform);
        horse.transform.localPosition = new Vector3(0, 0.5f, -0.5f);
        horse.transform.localScale = new Vector3(0.6f, 1f, 1.2f);

        if (game.Cam != null)
            game.Cam.Init(transform);
    }

    void Update()
    {
        var game = ThronefallGame.Instance;
        if (game == null || game.CurrentPhase == ThronefallGame.GamePhase.GameOver)
            return;

        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        Vector3 dir = new Vector3(h, 0, v).normalized;

        Vector3 move = dir * MoveSpeed * Time.deltaTime;
        move.y -= 9.8f * Time.deltaTime;
        cc.Move(move);

        if (dir.sqrMagnitude > 0.01f)
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), 10f * Time.deltaTime);

        if (game.CurrentPhase == ThronefallGame.GamePhase.Day &&
            Input.GetKeyDown(KeyCode.Space) &&
            currentNearMarker != null)
        {
            game.BuildSys.TryBuild(currentNearMarker.nodeIndex);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        var marker = other.GetComponent<ThronefallBuildNodeMarker>();
        if (marker == null) return;

        currentNearMarker = marker;

        var game = ThronefallGame.Instance;
        if (game != null && game.CurrentPhase == ThronefallGame.GamePhase.Day && game.UI != null)
        {
            var config = game.BuildSys.GetNodeConfig(marker.nodeIndex);
            if (config != null)
                game.UI.ShowBuildPanel(config);
        }
    }

    void OnTriggerExit(Collider other)
    {
        var marker = other.GetComponent<ThronefallBuildNodeMarker>();
        if (marker == null) return;

        if (currentNearMarker == marker)
        {
            currentNearMarker = null;
            var game = ThronefallGame.Instance;
            if (game != null && game.UI != null)
                game.UI.HideBuildPanel();
        }
    }
}
