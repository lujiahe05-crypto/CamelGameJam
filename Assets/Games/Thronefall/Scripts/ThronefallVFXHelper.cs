using UnityEngine;

public class ThronefallTimedDestroy : MonoBehaviour
{
    public float lifetime;

    void Update()
    {
        lifetime -= Time.deltaTime;
        if (lifetime <= 0f)
            Destroy(gameObject);
    }
}

public static class ThronefallVFXHelper
{
    public static void SpawnSpearTrail(Vector3 origin, Vector3 direction, float length)
    {
        var game = ThronefallGame.Instance;
        if (game == null) return;

        var mat = new Material(game.ThrustTrailMat);
        var go = ProceduralMeshUtil.CreatePrimitive("SpearTrail", game.CubeMesh, mat, game.RootContainer);
        go.transform.position = origin + direction * (length * 0.5f) + Vector3.up;
        go.transform.rotation = Quaternion.LookRotation(direction);
        go.transform.localScale = new Vector3(0.3f, 0.3f, length);

        var destroyer = go.AddComponent<ThronefallTimedDestroy>();
        destroyer.lifetime = 0.15f;
    }
}
