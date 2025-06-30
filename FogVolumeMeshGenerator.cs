using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class FogVolumeMeshGenerator : MonoBehaviour
{
    [Header("Fog Volume Size")]
    public Vector3 size = new Vector3(10, 5, 10);

    [Header("Subdivisions")]
    public int verticalSegments = 8;  // Y axis
    public int horizontalSegments = 8; // Along X/Z

    [Header("Include Top/Bottom")]
    public bool includeTop = true;
    public bool includeBottom = true;

    private void Start()
    {
        GenerateMesh();
    }

    public Vector2 worldMinXZ = Vector2.zero;  // Add this as a serialized field

    public void GenerateMesh()
    {
        Mesh mesh = new Mesh();
        mesh.name = "FogVolume";

        var vertices = new List<Vector3>();
        var triangles = new List<int>();

        // Offsets
        float halfX = size.x * 0.5f;
        float halfY = size.y * 0.5f;
        float halfZ = size.z * 0.5f;

        // This offset moves the mesh so that its XZ min corner matches the worldMin
        Vector3 originOffset = new Vector3(worldMinXZ.x + 0, 0, worldMinXZ.y + 0);

        // Generate vertical walls (front, back, left, right)
        AddWall(vertices, triangles, originOffset + new Vector3(0, -halfY, 0),                             // base origin
            new Vector3(0, 0, 1), Vector3.up, size.z, size.y); // Left

        AddWall(vertices, triangles, originOffset + new Vector3(size.x, -halfY, size.z), 
            new Vector3(0, 0, -1), Vector3.up, size.z, size.y); // Right

        AddWall(vertices, triangles, originOffset + new Vector3(size.x, -halfY, 0),
            new Vector3(-1, 0, 0), Vector3.up, size.x, size.y); // Front

        AddWall(vertices, triangles, originOffset + new Vector3(0, -halfY, size.z),
            new Vector3(1, 0, 0), Vector3.up, size.x, size.y); // Back

        if (includeTop)
        {
            AddWall(vertices, triangles, originOffset + new Vector3(0, halfY, 0),
                Vector3.right, Vector3.forward, size.x, size.z);
        }

        if (includeBottom)
        {
            AddWall(vertices, triangles, originOffset + new Vector3(0, -halfY, size.z),
                Vector3.right, Vector3.back, size.x, size.z);
        }

        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        GetComponent<MeshFilter>().sharedMesh = mesh;
    }

    private void AddWall(List<Vector3> verts, List<int> tris, Vector3 origin, Vector3 right, Vector3 up, float width, float height)
    {
        int vCount = verts.Count;

        for (int y = 0; y <= verticalSegments; y++)
        {
            for (int x = 0; x <= horizontalSegments; x++)
            {
                Vector3 pos = origin +
                              right * (width * (x / (float)horizontalSegments)) +
                              up * (height * (y / (float)verticalSegments));
                verts.Add(pos);
            }
        }

        for (int y = 0; y < verticalSegments; y++)
        {
            for (int x = 0; x < horizontalSegments; x++)
            {
                int rowStart = y * (horizontalSegments + 1);
                int nextRowStart = (y + 1) * (horizontalSegments + 1);

                int a = vCount + rowStart + x;
                int b = vCount + rowStart + x + 1;
                int c = vCount + nextRowStart + x;
                int d = vCount + nextRowStart + x + 1;

                tris.Add(a);
                tris.Add(c);
                tris.Add(b);

                tris.Add(b);
                tris.Add(c);
                tris.Add(d);
            }
        }
    }
}
