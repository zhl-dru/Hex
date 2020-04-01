using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 网格
/// </summary>
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class HexMesh : MonoBehaviour
{
    Mesh hexMesh;
    MeshCollider meshCollider;
    //static List<Vector3> vertices = new List<Vector3>();
    //static List<Color> colors = new List<Color>();
    //static List<int> triangles = new List<int>();
    //[NonSerialized] List<Vector3> vertices, terrainTypes;
    //[NonSerialized] List<Color> colors;
    [NonSerialized] List<Vector3> vertices, cellIndices;
    [NonSerialized] List<Color> cellWeights;
    [NonSerialized] List<Vector2> uvs, uv2s;
    [NonSerialized] List<int> triangles;
    //public bool useCollider, useColors, useUVCoordinates, useUV2Coordinates;
    //public bool useTerrainTypes;
    public bool useCollider, useCellData, useUVCoordinates, useUV2Coordinates;

    private void Awake()
    {
        GetComponent<MeshFilter>().mesh = hexMesh = new Mesh();
        if (useCollider)
        {
            meshCollider = gameObject.AddComponent<MeshCollider>();
        }
        hexMesh.name = "Hex Mesh";
        //vertices = new List<Vector3>();
        //colors = new List<Color>();
        //triangles = new List<int>();
    }



    /// <summary>
    /// 添加三角形
    /// </summary>
    /// <param name="v1">角1的坐标</param>
    /// <param name="v2">角2的坐标</param>
    /// <param name="v3">角3的坐标</param>
    public void AddTriangle(Vector3 v1, Vector3 v2, Vector3 v3)
    {
        //地图中三角形顶点数量
        int vertexIndex = vertices.Count;
        vertices.Add(HexMetrics.Perturb(v1));
        vertices.Add(HexMetrics.Perturb(v2));
        vertices.Add(HexMetrics.Perturb(v3));
        triangles.Add(vertexIndex);
        triangles.Add(vertexIndex + 1);
        triangles.Add(vertexIndex + 2);
    }
    ///// <summary>
    ///// 三角形颜色,单一颜色时
    ///// </summary>
    ///// <param name="color">颜色</param>
    //public void AddTriangleColor(Color color)
    //{
    //    colors.Add(color);
    //    colors.Add(color);
    //    colors.Add(color);
    //}
    ///// <summary>
    ///// 三角形颜色,混合颜色时
    ///// </summary>
    ///// <param name="c1"></param>
    ///// <param name="c2"></param>
    ///// <param name="c3"></param>
    //public void AddTriangleColor(Color c1, Color c2, Color c3)
    //{
    //    colors.Add(c1);
    //    colors.Add(c2);
    //    colors.Add(c3);
    //}
    /// <summary>
    /// 添加四边形
    /// </summary>
    /// <param name="v1"></param>
    /// <param name="v2"></param>
    /// <param name="v3"></param>
    /// <param name="v4"></param>
    public void AddQuad(Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4)
    {
        int vertexIndex = vertices.Count;
        vertices.Add(HexMetrics.Perturb(v1));
        vertices.Add(HexMetrics.Perturb(v2));
        vertices.Add(HexMetrics.Perturb(v3));
        vertices.Add(HexMetrics.Perturb(v4));
        triangles.Add(vertexIndex);
        triangles.Add(vertexIndex + 2);
        triangles.Add(vertexIndex + 1);
        triangles.Add(vertexIndex + 1);
        triangles.Add(vertexIndex + 2);
        triangles.Add(vertexIndex + 3);
    }
    ///// <summary>
    ///// 四边形颜色,四种颜色混合
    ///// </summary>
    ///// <param name="c1"></param>
    ///// <param name="c2"></param>
    ///// <param name="c3"></param>
    ///// <param name="c4"></param>
    //public void AddQuadColor(Color c1, Color c2, Color c3, Color c4)
    //{
    //    colors.Add(c1);
    //    colors.Add(c2);
    //    colors.Add(c3);
    //    colors.Add(c4);
    //}
    ///// <summary>
    ///// 四边形颜色,两种颜色混合
    ///// </summary>
    ///// <param name="c1"></param>
    ///// <param name="c2"></param>
    //public void AddQuadColor(Color c1, Color c2)
    //{
    //    colors.Add(c1);
    //    colors.Add(c1);
    //    colors.Add(c2);
    //    colors.Add(c2);
    //}
    ///// <summary>
    ///// 四边形颜色,一种颜色
    ///// </summary>
    ///// <param name="color"></param>
    //public void AddQuadColor(Color color)
    //{
    //    colors.Add(color);
    //    colors.Add(color);
    //    colors.Add(color);
    //    colors.Add(color);
    //}
    /// <summary>
    /// 添加一个三角形,不微扰顶点
    /// </summary>
    /// <param name="v1"></param>
    /// <param name="v2"></param>
    /// <param name="v3"></param>
    public void AddTriangulateUnperturbed(Vector3 v1, Vector3 v2, Vector3 v3)
    {
        int vertexIndex = vertices.Count;
        vertices.Add(v1);
        vertices.Add(v2);
        vertices.Add(v3);
        triangles.Add(vertexIndex);
        triangles.Add(vertexIndex + 1);
        triangles.Add(vertexIndex + 2);
    }
    /// <summary>
    /// 添加三角形的UV坐标
    /// </summary>
    /// <param name="uv1"></param>
    /// <param name="uv2"></param>
    /// <param name="uv3"></param>
    public void AddTriangulateUV(Vector2 uv1, Vector2 uv2, Vector3 uv3)
    {
        uvs.Add(uv1);
        uvs.Add(uv2);
        uvs.Add(uv3);
    }
    /// <summary>
    /// 添加四边形的UV坐标
    /// </summary>
    /// <param name="uv1"></param>
    /// <param name="uv2"></param>
    /// <param name="uv3"></param>
    /// <param name="uv4"></param>
    public void AddQuadUV(Vector2 uv1, Vector2 uv2, Vector3 uv3, Vector3 uv4)
    {
        uvs.Add(uv1);
        uvs.Add(uv2);
        uvs.Add(uv3);
        uvs.Add(uv4);
    }
    /// <summary>
    /// 添加长方形UV区域
    /// </summary>
    /// <param name="uMin"></param>
    /// <param name="uMax"></param>
    /// <param name="vMin"></param>
    /// <param name="vMax"></param>
    public void AddQuadUV(float uMin, float uMax, float vMin, float vMax)
    {
        uvs.Add(new Vector2(uMin, vMin));
        uvs.Add(new Vector2(uMax, vMin));
        uvs.Add(new Vector2(uMin, vMax));
        uvs.Add(new Vector2(uMax, vMax));
    }
    /// <summary>
    /// 添加三角形的UV2坐标
    /// </summary>
    /// <param name="uv1"></param>
    /// <param name="uv2"></param>
    /// <param name="uv3"></param>
    public void AddTriangleUV2(Vector2 uv1, Vector2 uv2, Vector3 uv3)
    {
        uv2s.Add(uv1);
        uv2s.Add(uv2);
        uv2s.Add(uv3);
    }
    /// <summary>
    /// 添加四边形的UV2坐标
    /// </summary>
    /// <param name="uv1"></param>
    /// <param name="uv2"></param>
    /// <param name="uv3"></param>
    /// <param name="uv4"></param>
    public void AddQuadUV2(Vector2 uv1, Vector2 uv2, Vector3 uv3, Vector3 uv4)
    {
        uv2s.Add(uv1);
        uv2s.Add(uv2);
        uv2s.Add(uv3);
        uv2s.Add(uv4);
    }
    /// <summary>
    /// 添加长方形的UV2区域
    /// </summary>
    /// <param name="uMin"></param>
    /// <param name="uMax"></param>
    /// <param name="vMin"></param>
    /// <param name="vMax"></param>
    public void AddQuadUV2(float uMin, float uMax, float vMin, float vMax)
    {
        uv2s.Add(new Vector2(uMin, vMin));
        uv2s.Add(new Vector2(uMax, vMin));
        uv2s.Add(new Vector2(uMin, vMax));
        uv2s.Add(new Vector2(uMax, vMax));
    }
    /// <summary>
    /// 添加四边形,不微扰顶点
    /// </summary>
    /// <param name="v1"></param>
    /// <param name="v2"></param>
    /// <param name="v3"></param>
    /// <param name="v4"></param>
    public void AddQuadUnperturbed(Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4)
    {
        int vertexIndex = vertices.Count;
        vertices.Add(v1);
        vertices.Add(v2);
        vertices.Add(v3);
        vertices.Add(v4);
        triangles.Add(vertexIndex);
        triangles.Add(vertexIndex + 2);
        triangles.Add(vertexIndex + 1);
        triangles.Add(vertexIndex + 1);
        triangles.Add(vertexIndex + 2);
        triangles.Add(vertexIndex + 3);
    }
    ///// <summary>
    ///// 添加三角形地形类型
    ///// </summary>
    ///// <param name="types"></param>
    //public void AddTriangulateTerrainType(Vector3 types)
    //{
    //    terrainTypes.Add(types);
    //    terrainTypes.Add(types);
    //    terrainTypes.Add(types);
    //}
    ///// <summary>
    ///// 添加四边形地形类型
    ///// </summary>
    ///// <param name="types"></param>
    //public void AddQuadTerrainType(Vector3 types)
    //{
    //    terrainTypes.Add(types);
    //    terrainTypes.Add(types);
    //    terrainTypes.Add(types);
    //    terrainTypes.Add(types);
    //}
    /// <summary>
    /// 清理
    /// </summary>
    public void Clear()
    {
        hexMesh.Clear();
        vertices = ListPool<Vector3>.Get();
        //if (useColors)
        //{
        //    colors = ListPool<Color>.Get();
        //}
        if (useCellData)
        {
            cellWeights = ListPool<Color>.Get();
            cellIndices = ListPool<Vector3>.Get();
        }
        if (useUVCoordinates)
        {
            uvs = ListPool<Vector2>.Get();
        }
        if (useUV2Coordinates)
        {
            uv2s = ListPool<Vector2>.Get();
        }
        //if (useTerrainTypes)
        //{
        //    terrainTypes = ListPool<Vector3>.Get();
        //}
        triangles = ListPool<int>.Get();
    }
    /// <summary>
    /// 应用网格数据
    /// </summary>
    public void Apply()
    {
        hexMesh.SetVertices(vertices);
        ListPool<Vector3>.Add(vertices);
        //if (useColors)
        //{
        //    hexMesh.SetColors(colors);
        //    ListPool<Color>.Add(colors);
        //}
        if (useCellData)
        {
            hexMesh.SetColors(cellWeights);
            ListPool<Color>.Add(cellWeights);
            hexMesh.SetUVs(2, cellIndices);
            ListPool<Vector3>.Add(cellIndices);
        }
        if (useUVCoordinates)
        {
            hexMesh.SetUVs(0, uvs);
            ListPool<Vector2>.Add(uvs);
        }
        if (useUV2Coordinates)
        {
            hexMesh.SetUVs(1, uv2s);
            ListPool<Vector2>.Add(uv2s);
        }
        //if (useTerrainTypes)
        //{
        //    hexMesh.SetUVs(2, terrainTypes);
        //    ListPool<Vector3>.Add(terrainTypes);
        //}
        hexMesh.SetTriangles(triangles, 0);
        ListPool<int>.Add(triangles);
        hexMesh.RecalculateNormals();
        if (useCollider)
        {
            meshCollider.sharedMesh = hexMesh;
        }
    }


    public void AddTriangleCellData(Vector3 indices, Color weights1, Color weights2, Color weights3)
    {
        cellIndices.Add(indices);
        cellIndices.Add(indices);
        cellIndices.Add(indices);
        cellWeights.Add(weights1);
        cellWeights.Add(weights2);
        cellWeights.Add(weights3);
    }

    public void AddTriangleCellData(Vector3 indices, Color weights)
    {
        AddTriangleCellData(indices, weights, weights, weights);
    }

    public void AddQuadCellData(Vector3 indices, Color weights1, Color weights2, Color weights3, Color weights4)
    {
        cellIndices.Add(indices);
        cellIndices.Add(indices);
        cellIndices.Add(indices);
        cellIndices.Add(indices);
        cellWeights.Add(weights1);
        cellWeights.Add(weights2);
        cellWeights.Add(weights3);
        cellWeights.Add(weights4);
    }

    public void AddQuadCellData(Vector3 indices, Color weights1, Color weights2)
    {
        AddQuadCellData(indices, weights1, weights1, weights2, weights2);
    }

    public void AddQuadCellData(Vector3 indices, Color weights)
    {
        AddQuadCellData(indices, weights, weights, weights, weights);
    }
}
