using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ZYB.Scripts
{
    public class CalcQuadCenter : MonoBehaviour
    {
        [SerializeField, Range(2, 7)]
        private int writeUVChannel = 5;
    
        private const int VertexCountPerQuad = 6;   // 一枚のQuadは2つの三角形(6つの頂点)で出来ている

        [ContextMenu("Set quad center to uv")]
        private void CalcQuadCenterAndSetToUV()
        {
            Mesh mesh = null;
            Mesh tempMesh = new Mesh();
            Transform root = null;
            var rendererComp = GetComponent<Renderer>();
            switch (rendererComp)
            {
                case MeshRenderer mr: 
                    mesh = mr.GetComponent<MeshFilter>().sharedMesh;
                    tempMesh = mesh;
                    break;
                case SkinnedMeshRenderer smr: 
                    mesh = smr.sharedMesh;
                    smr.BakeMesh(tempMesh);　// todo: うまくいってない
                    root = smr.rootBone;
                    break;
            }

            if (mesh == null)
            {
                Debug.LogError("No mesh!!!");
                return;
            }
        
            var quadCenters = GetQuadCenterPositions(tempMesh, root);
            mesh.SetUVs(writeUVChannel, quadCenters);
        
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        // Quadごとに中心座標を計算して返す
        private List<Vector3> GetQuadCenterPositions(Mesh mesh, Transform root = null)
        {
            var centerPositions = new List<Vector3>();
            var vertices = mesh.vertices;
            var triangles = mesh.triangles;

            var vertexIndexList = new List<int>();
            var tempVector = Vector3.zero;
            for (int i = 0; i < triangles.Length; i++)
            {
                var vertexOS = vertices[triangles[i]];
            
                // rootが存在する時、頂点のオブジェクト空間座標をroot座標系に変換する
                // todo: うまくいってない
                if (root != null)
                {
                    var vertexWS = transform.TransformPoint(vertexOS);
                    vertexOS = root.InverseTransformPoint(vertexWS);
                }
            
                tempVector += vertexOS;
                if ((i + 1) % VertexCountPerQuad == 0)
                {
                    // Quadの頂点座標を全部足して、頂点数で割って、中心座標を求める
                    var centerPos = tempVector / VertexCountPerQuad;
                    for (int j = 0; j < VertexCountPerQuad; j++)
                    {
                        int index = i - (VertexCountPerQuad - 1 - j);
                        int vertexIndex = triangles[index];
                        // Quadの2つの三角形が頂点を共用することもあるため、共用する頂点の重複追加はしない
                        if (vertexIndexList.Contains(vertexIndex))
                        {
                            continue;
                        }
                        centerPositions.Add(centerPos);
                        vertexIndexList.Add(vertexIndex);
                    }
                    tempVector.Set(0, 0, 0);
                }
            }
            return centerPositions;
        }
    }
}
