using UnityEngine;

public class Ocean : MonoBehaviour
{
    Mesh mesh;
    Vector3[] baseVerts;
    Vector3[] animVerts;

    void Start()
    {
        mesh = GetComponent<MeshFilter>().mesh;
        baseVerts = mesh.vertices;
        animVerts = new Vector3[baseVerts.Length];
    }

    void Update()
    {
        float t = Time.time;
        for (int i = 0; i < baseVerts.Length; i++)
        {
            var v = baseVerts[i];
            float wave = Mathf.Sin(v.x * 0.15f + t * 0.8f) * 0.3f
                       + Mathf.Sin(v.z * 0.1f + t * 0.6f) * 0.2f
                       + Mathf.Sin((v.x + v.z) * 0.08f + t * 1.1f) * 0.15f;
            animVerts[i] = new Vector3(v.x, wave, v.z);
        }
        mesh.vertices = animVerts;
        mesh.RecalculateNormals();
    }

    public static float GetWaveHeight(float x, float z)
    {
        float t = Time.time;
        return Mathf.Sin(x * 0.15f + t * 0.8f) * 0.3f
             + Mathf.Sin(z * 0.1f + t * 0.6f) * 0.2f
             + Mathf.Sin((x + z) * 0.08f + t * 1.1f) * 0.15f;
    }
}
