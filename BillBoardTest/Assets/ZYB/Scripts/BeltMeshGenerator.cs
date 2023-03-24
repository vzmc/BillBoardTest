using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ZYB.Scripts
{
    public class BeltMeshGenerator : MonoBehaviour
    {
        public int radialSegments = 8;  // 横分割数
        public int heightSegments = 1;  // 縦分割数
        public float radius = 1f;       // 半径
    
        private const string assetPath = "Assets/ZYB/Model/BeltMesh.asset";

        private float AngleStep => 2 * Mathf.PI / radialSegments;
        private float Height => radius * Mathf.Sin(AngleStep / 2) * 2;  // Quadの高さを正方形にするように半径に応じて計算

        private readonly Vector3 offset = new(2.1f, 1.45f, 3.1f);
    

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

        // ベルト状のMesh生成
        private Mesh GenerateBeltMesh()
        {
            Mesh mesh = new Mesh();

            int vertexCount = (radialSegments + 1) * (heightSegments + 1);
            Vector3[] vertices = new Vector3[vertexCount];
            Vector2[] uv = new Vector2[vertexCount];
            int[] triangles = new int[radialSegments * heightSegments * 6];

            float angleStep = AngleStep;
            float heightStep = Height / heightSegments;
            for (int j = 0, index = 0, triIndex = 0; j <= heightSegments; j++)
            {
                for (int i = 0; i <= radialSegments; i++, index++)
                {
                    float angle = i * angleStep;
                    vertices[index] = new Vector3(
                        radius * Mathf.Cos(angle),
                        (j - 0.5f) * heightStep,
                        radius * Mathf.Sin(angle)
                    ) + offset;
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
        
            // 三角形ごとに3つの頂点を持たせる
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

            const int vertexCountPerQuad = 6;
            const int writeChannel = 5;
            var quadCenterPositions = GetQuadCenterPositions(ref mesh, vertexCountPerQuad);
            mesh.SetUVs(writeChannel, quadCenterPositions);

            return mesh;
        }
    
        // Quadごとに中心座標を計算し、各頂点との偏差ベクトルを返す
        // private List<Vector3> GetQuadCenterOffsets(ref Mesh mesh, int vertexCountPerQuad)
        // {
        //     var centerOffsets = new List<Vector3>();
        //     var vertices = mesh.vertices;
        //     var triangles = mesh.triangles;
        //
        //     var tempVector = Vector3.zero;
        //     for (int i = 0; i < triangles.Length; i++)
        //     {
        //         tempVector += vertices[triangles[i]];
        //         if ((i + 1) % vertexCountPerQuad == 0)
        //         {
        //             tempVector /= vertexCountPerQuad;
        //             for (int j = 0; j < vertexCountPerQuad; j++)
        //             {
        //                 var vertex = vertices[triangles[i - (vertexCountPerQuad - 1 - j)]];
        //                 centerOffsets.Add(tempVector - vertex);
        //             }
        //             tempVector.Set(0, 0, 0);
        //         }
        //     }
        //
        //     return centerOffsets;
        // }

        // Quadごとに中心座標を計算して返す
        private List<Vector3> GetQuadCenterPositions(ref Mesh mesh, int vertexCountPerQuad)
        {
            var centerPositions = new List<Vector3>();
            var vertices = mesh.vertices;
            var triangles = mesh.triangles;

            var tempVector = Vector3.zero;
            for (int i = 0; i < triangles.Length; i++)
            {
                tempVector += vertices[triangles[i]];
                if ((i + 1) % vertexCountPerQuad == 0)
                {
                    tempVector /= vertexCountPerQuad;
                    for (int j = 0; j < vertexCountPerQuad; j++)
                    {
                        centerPositions.Add(tempVector);
                    }
                    tempVector.Set(0, 0, 0);
                }
            }

            return centerPositions;
        }

        private void SaveMeshAsAsset(Mesh mesh, string assetPath)
        {
            //assetPath = AssetDatabase.GenerateUniqueAssetPath(assetPath);
            AssetDatabase.CreateAsset(mesh, assetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }
}
