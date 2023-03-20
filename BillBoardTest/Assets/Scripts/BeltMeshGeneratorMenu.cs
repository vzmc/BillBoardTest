using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class BeltMeshGenerator : MonoBehaviour
{
    public int radialSegments = 8;
    public int heightSegments = 1;
    public float radius = 1f;

    private float height = 0.1f;
    private const string assetPath = "Assets/BeltMesh.asset";

    private float AngleStep => 2 * Mathf.PI / radialSegments;
    private float QuadRadius => radius * Mathf.Cos(AngleStep / 2);

    [ContextMenu("Set Material Data")]
    private void SetMaterialData()
    {
        var renderer = GetComponent<MeshRenderer>();
        var material = renderer.sharedMaterial;
        material.SetFloat("_QuadRadius", QuadRadius);
        material.SetFloat("_AngleStep", AngleStep);
    }

    private void FixHeight()
    {
        float angle = Mathf.PI / radialSegments;
        height = radius * Mathf.Sin(angle) * 2;
    }

    private Mesh GenerateBeltMesh()
    {
        Mesh mesh = new Mesh();

        FixHeight();

        int vertexCount = (radialSegments + 1) * (heightSegments + 1);
        Vector3[] vertices = new Vector3[vertexCount];
        Vector2[] uv = new Vector2[vertexCount];
        int[] triangles = new int[radialSegments * heightSegments * 6];

        float angleStep = AngleStep;
        float heightStep = height / heightSegments;

        for (int j = 0, index = 0, triIndex = 0; j <= heightSegments; j++)
        {
            for (int i = 0; i <= radialSegments; i++, index++)
            {
                float angle = i * angleStep;
                vertices[index] = new Vector3(
                    radius * Mathf.Cos(angle),
                    (j - 0.5f) * heightStep,
                    radius * Mathf.Sin(angle)
                );
                uv[index] = new Vector2((float)i / radialSegments, (float)j / heightSegments);

                if (i < radialSegments && j < heightSegments)
                {
                    triangles[triIndex++] = index;
                    triangles[triIndex++] = index + radialSegments + 1;
                    triangles[triIndex++] = index + radialSegments + 2;

                    triangles[triIndex++] = index;
                    triangles[triIndex++] = index + radialSegments + 2;
                    triangles[triIndex++] = index + 1;
                }
            }
        }

        //ŽOŠpŒ`‚²‚Æ‚É3‚Â‚Ì’¸“_‚ð¶¬‚·‚é
        var newVertices = new List<Vector3>();
        var newTriangles = new List<int>();
        var newUV = new List<Vector2>();
        for (int i = 0; i < triangles.Length; i++)
        {
            var index = triangles[i];
            newVertices.Add(vertices[index]);
            newUV.Add(uv[index]);
            newTriangles.Add(i);
        }

        mesh.Clear();
        mesh.SetVertices(newVertices);
        mesh.SetUVs(0, newUV);
        mesh.SetTriangles(newTriangles, 0);
        mesh.RecalculateNormals();

        return mesh;
    }

    [ContextMenu("Generate & Save Belt Mesh")]
    private void GenerateAndSaveBeltMesh()
    {
        Mesh beltMesh = GenerateBeltMesh();
        SaveMeshAsAsset(beltMesh, assetPath);

        MeshFilter meshFilter = GetComponent<MeshFilter>();
        if (meshFilter != null)
        {
            meshFilter.sharedMesh = beltMesh;
        }
    }

    private void SaveMeshAsAsset(Mesh mesh, string assetPath)
    {
        //assetPath = AssetDatabase.GenerateUniqueAssetPath(assetPath);
        AssetDatabase.CreateAsset(mesh, assetPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }
}
