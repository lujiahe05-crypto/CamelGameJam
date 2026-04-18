using UnityEngine;

public class ThronefallArrowProjectile : MonoBehaviour
{
    Vector3 startPos;
    Vector3 targetPos;
    float arcHeight;
    float speed;
    int damage;
    float totalDistance;
    float traveled;
    bool hit;

    public static void Spawn(Vector3 origin, Vector3 target, float speed, float arcHeight, int damage)
    {
        var game = ThronefallGame.Instance;
        if (game == null) return;

        var go = new GameObject("Arrow");
        go.transform.SetParent(game.RootContainer);
        go.transform.position = origin;

        var arrow = go.AddComponent<ThronefallArrowProjectile>();
        arrow.startPos = origin;
        arrow.targetPos = target;
        arrow.speed = speed;
        arrow.arcHeight = arcHeight;
        arrow.damage = damage;

        Vector3 flatStart = new Vector3(origin.x, 0, origin.z);
        Vector3 flatTarget = new Vector3(target.x, 0, target.z);
        arrow.totalDistance = Vector3.Distance(flatStart, flatTarget);
        if (arrow.totalDistance < 0.1f) arrow.totalDistance = 0.1f;

        var mat = new Material(game.ProjectileMat);
        var visual = ProceduralMeshUtil.CreatePrimitive("ArrowVisual", game.CubeMesh, mat, go.transform);
        visual.transform.localScale = new Vector3(0.15f, 0.15f, 0.6f);
    }

    void Update()
    {
        if (hit) return;

        traveled += speed * Time.deltaTime;
        float t = Mathf.Clamp01(traveled / totalDistance);

        Vector3 flatPos = Vector3.Lerp(
            new Vector3(startPos.x, 0, startPos.z),
            new Vector3(targetPos.x, 0, targetPos.z), t);

        float baseY = Mathf.Lerp(startPos.y, targetPos.y, t);
        float arc = arcHeight * 4f * t * (1f - t);
        Vector3 newPos = new Vector3(flatPos.x, baseY + arc, flatPos.z);

        Vector3 velocity = newPos - transform.position;
        if (velocity.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.LookRotation(velocity);

        transform.position = newPos;

        if (t >= 1f)
            OnHit();
    }

    void OnHit()
    {
        hit = true;

        var colliders = Physics.OverlapSphere(transform.position, 0.8f);
        foreach (var col in colliders)
        {
            var enemy = col.GetComponent<ThronefallEnemy>();
            if (enemy != null && enemy.IsAlive)
            {
                enemy.TakeDamage(damage);
                break;
            }
        }

        Destroy(gameObject);
    }
}
