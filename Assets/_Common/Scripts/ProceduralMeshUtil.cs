using UnityEngine;

public static class ProceduralMeshUtil
{
    public static Mesh CreateCube(float sizeX = 1f, float sizeY = 1f, float sizeZ = 1f)
    {
        float x = sizeX / 2f, y = sizeY / 2f, z = sizeZ / 2f;
        var mesh = new Mesh { name = "ProcCube" };

        mesh.vertices = new Vector3[]
        {
            // Front
            new(-x,-y, z), new( x,-y, z), new( x, y, z), new(-x, y, z),
            // Back
            new( x,-y,-z), new(-x,-y,-z), new(-x, y,-z), new( x, y,-z),
            // Top
            new(-x, y, z), new( x, y, z), new( x, y,-z), new(-x, y,-z),
            // Bottom
            new(-x,-y,-z), new( x,-y,-z), new( x,-y, z), new(-x,-y, z),
            // Right
            new( x,-y, z), new( x,-y,-z), new( x, y,-z), new( x, y, z),
            // Left
            new(-x,-y,-z), new(-x,-y, z), new(-x, y, z), new(-x, y,-z),
        };

        mesh.normals = new Vector3[]
        {
            Vector3.forward, Vector3.forward, Vector3.forward, Vector3.forward,
            Vector3.back, Vector3.back, Vector3.back, Vector3.back,
            Vector3.up, Vector3.up, Vector3.up, Vector3.up,
            Vector3.down, Vector3.down, Vector3.down, Vector3.down,
            Vector3.right, Vector3.right, Vector3.right, Vector3.right,
            Vector3.left, Vector3.left, Vector3.left, Vector3.left,
        };

        mesh.triangles = new int[]
        {
            0,2,1, 0,3,2,
            4,6,5, 4,7,6,
            8,10,9, 8,11,10,
            12,14,13, 12,15,14,
            16,18,17, 16,19,18,
            20,22,21, 20,23,22,
        };

        mesh.RecalculateBounds();
        return mesh;
    }

    public static Mesh CreatePlane(float width, float depth, int segX = 1, int segZ = 1)
    {
        var mesh = new Mesh { name = "ProcPlane" };
        int vertCountX = segX + 1;
        int vertCountZ = segZ + 1;
        var verts = new Vector3[vertCountX * vertCountZ];
        var uvs = new Vector2[verts.Length];
        var normals = new Vector3[verts.Length];

        for (int iz = 0; iz <= segZ; iz++)
        {
            for (int ix = 0; ix <= segX; ix++)
            {
                int i = iz * vertCountX + ix;
                float px = (float)ix / segX * width - width / 2f;
                float pz = (float)iz / segZ * depth - depth / 2f;
                verts[i] = new Vector3(px, 0, pz);
                uvs[i] = new Vector2((float)ix / segX, (float)iz / segZ);
                normals[i] = Vector3.up;
            }
        }

        var tris = new int[segX * segZ * 6];
        int t = 0;
        for (int iz = 0; iz < segZ; iz++)
        {
            for (int ix = 0; ix < segX; ix++)
            {
                int bl = iz * vertCountX + ix;
                int br = bl + 1;
                int tl = bl + vertCountX;
                int tr = tl + 1;
                tris[t++] = bl; tris[t++] = tl; tris[t++] = br;
                tris[t++] = br; tris[t++] = tl; tris[t++] = tr;
            }
        }

        mesh.vertices = verts;
        mesh.uv = uvs;
        mesh.normals = normals;
        mesh.triangles = tris;
        mesh.RecalculateBounds();
        return mesh;
    }

    public static Mesh CreateSharkMesh()
    {
        // Simple shark: elongated body + tail fin
        var mesh = new Mesh { name = "Shark" };

        // Body is a tapered shape along Z axis
        // Nose at Z=1.5, tail at Z=-1.5, widest at Z=0
        var verts = new Vector3[]
        {
            // Nose
            new(0, 0, 1.5f),
            // Front ring (Z=0.5)
            new(0, 0.3f, 0.5f), new(0.25f, 0, 0.5f), new(0, -0.2f, 0.5f), new(-0.25f, 0, 0.5f),
            // Mid ring (Z=0, widest)
            new(0, 0.4f, 0), new(0.35f, 0, 0), new(0, -0.25f, 0), new(-0.35f, 0, 0),
            // Rear ring (Z=-0.8)
            new(0, 0.25f, -0.8f), new(0.2f, 0, -0.8f), new(0, -0.15f, -0.8f), new(-0.2f, 0, -0.8f),
            // Tail tip
            new(0, 0, -1.5f),
            // Dorsal fin tip
            new(0, 0.7f, -0.2f),
            // Tail fin top
            new(0, 0.5f, -1.7f),
            // Tail fin bottom
            new(0, -0.3f, -1.7f),
        };

        var tris = new System.Collections.Generic.List<int>();

        // Nose to front ring
        for (int i = 0; i < 4; i++)
        {
            int a = 1 + i;
            int b = 1 + (i + 1) % 4;
            tris.Add(0); tris.Add(a); tris.Add(b);
        }

        // Front ring to mid ring
        for (int i = 0; i < 4; i++)
        {
            int a = 1 + i, b = 1 + (i + 1) % 4;
            int c = 5 + i, d = 5 + (i + 1) % 4;
            tris.Add(a); tris.Add(c); tris.Add(b);
            tris.Add(b); tris.Add(c); tris.Add(d);
        }

        // Mid ring to rear ring
        for (int i = 0; i < 4; i++)
        {
            int a = 5 + i, b = 5 + (i + 1) % 4;
            int c = 9 + i, d = 9 + (i + 1) % 4;
            tris.Add(a); tris.Add(c); tris.Add(b);
            tris.Add(b); tris.Add(c); tris.Add(d);
        }

        // Rear ring to tail
        for (int i = 0; i < 4; i++)
        {
            int a = 9 + i;
            int b = 9 + (i + 1) % 4;
            tris.Add(a); tris.Add(13); tris.Add(b);
        }

        // Dorsal fin (triangle: mid-top, rear-top, fin tip)
        tris.Add(5); tris.Add(14); tris.Add(9);
        tris.Add(9); tris.Add(14); tris.Add(5);

        // Tail fin (two triangles)
        tris.Add(13); tris.Add(15); tris.Add(9);  // top fin
        tris.Add(13); tris.Add(11); tris.Add(16);  // bottom fin

        mesh.vertices = verts;
        mesh.triangles = tris.ToArray();
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    public static Material CreateMaterial(Color color, bool transparent = false)
    {
        var shader = transparent
            ? Shader.Find("Standard")
            : Shader.Find("Standard");
        var mat = new Material(shader);
        mat.color = color;

        if (transparent)
        {
            mat.SetFloat("_Mode", 3); // Transparent
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.EnableKeyword("_ALPHABLEND_ON");
            mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            mat.renderQueue = 3000;
        }

        return mat;
    }

    public static GameObject CreatePrimitive(string name, Mesh mesh, Material mat, Transform parent = null)
    {
        var go = new GameObject(name);
        if (parent != null) go.transform.SetParent(parent);
        var mf = go.AddComponent<MeshFilter>();
        mf.mesh = mesh;
        var mr = go.AddComponent<MeshRenderer>();
        mr.material = mat;
        return go;
    }
}
