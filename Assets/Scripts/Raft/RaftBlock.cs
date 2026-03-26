using UnityEngine;

public class RaftBlock : MonoBehaviour
{
    public Vector2Int GridPos { get; set; }
    public float Health { get; set; } = 100f;

    MeshRenderer mr;

    public void Init()
    {
        var mf = gameObject.AddComponent<MeshFilter>();
        mf.mesh = RaftGame.Instance.CubeMesh;
        mr = gameObject.AddComponent<MeshRenderer>();
        mr.material = RaftGame.Instance.WoodMat;

        var col = gameObject.AddComponent<BoxCollider>();
        col.size = Vector3.one;

        transform.localScale = new Vector3(2f, 0.3f, 2f);
    }

    public void TakeDamage(float damage)
    {
        Health -= damage;
        UpdateVisual();
        if (Health <= 0)
        {
            RaftGame.Instance.RaftMgr.RemoveBlock(GridPos);
        }
    }

    void UpdateVisual()
    {
        float t = Health / 100f;
        mr.material.color = Color.Lerp(new Color(0.3f, 0.15f, 0.05f), new Color(0.55f, 0.35f, 0.15f), t);
    }
}
