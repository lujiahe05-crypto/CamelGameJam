using UnityEngine;

public class RaftBlock : MonoBehaviour
{
    public Vector2Int GridPos { get; set; }
    public float Health { get; set; } = 100f;

    public GameObject PlacedBuilding { get; private set; }
    public int PlacedBuildingId { get; private set; }
    public bool HasBuilding => PlacedBuilding != null;

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

    public void SetBuilding(int buildingId, GameObject buildingObj)
    {
        PlacedBuildingId = buildingId;
        PlacedBuilding = buildingObj;
        buildingObj.transform.SetParent(transform);
        buildingObj.transform.localPosition = new Vector3(0, 1.2f, 0);
    }

    public void ClearBuilding()
    {
        if (PlacedBuilding != null)
        {
            Destroy(PlacedBuilding);
            PlacedBuilding = null;
            PlacedBuildingId = 0;
        }
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
