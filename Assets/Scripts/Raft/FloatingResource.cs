using UnityEngine;

public class FloatingResource : MonoBehaviour
{
    public ResourceType Type { get; private set; }

    float bobOffset;
    float driftAngle;
    Vector3 driftDir;
    const float DriftSpeed = 0.5f;
    const float BobAmplitude = 0.15f;
    const float BobSpeed = 1.5f;

    public void Init(ResourceType type)
    {
        Type = type;
        bobOffset = Random.Range(0f, Mathf.PI * 2f);
        driftAngle = Random.Range(0f, 360f);
        driftDir = new Vector3(Mathf.Cos(driftAngle * Mathf.Deg2Rad), 0, Mathf.Sin(driftAngle * Mathf.Deg2Rad));

        // Visual
        var mf = gameObject.AddComponent<MeshFilter>();
        var mr = gameObject.AddComponent<MeshRenderer>();

        switch (type)
        {
            case ResourceType.Wood:
                mf.mesh = RaftGame.Instance.CubeMesh;
                mr.material = RaftGame.Instance.WoodMat;
                transform.localScale = new Vector3(0.8f, 0.3f, 0.4f);
                break;
            case ResourceType.Plastic:
                mf.mesh = RaftGame.Instance.CubeMesh;
                mr.material = RaftGame.Instance.PlasticMat;
                transform.localScale = new Vector3(0.5f, 0.3f, 0.5f);
                break;
            case ResourceType.Coconut:
                mf.mesh = RaftGame.Instance.CubeMesh;
                mr.material = RaftGame.Instance.CoconutMat;
                transform.localScale = new Vector3(0.4f, 0.4f, 0.4f);
                break;
        }

        // Collider for hook detection
        var col = gameObject.AddComponent<SphereCollider>();
        col.radius = 1f;
        col.isTrigger = true;

    }

    void Update()
    {
        // Float on water surface with bobbing
        float waterY = Ocean.GetWaveHeight(transform.position.x, transform.position.z);
        float bob = Mathf.Sin(Time.time * BobSpeed + bobOffset) * BobAmplitude;
        transform.position = new Vector3(
            transform.position.x + driftDir.x * DriftSpeed * Time.deltaTime,
            waterY + bob + 0.1f,
            transform.position.z + driftDir.z * DriftSpeed * Time.deltaTime
        );

        // Slow rotation
        transform.Rotate(Vector3.up, 15f * Time.deltaTime);
    }

    public void Collect()
    {
        var game = RaftGame.Instance;
        if (Type == ResourceType.Coconut)
        {
            game.Survival.RestoreHunger(30f);
            game.Survival.RestoreThirst(30f);
        }
        else
        {
            game.Inv.Add(Type);
        }
        Destroy(gameObject);
    }
}
