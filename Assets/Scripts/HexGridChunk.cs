using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HexGridChunk : MonoBehaviour
{
    HexCell[] cells;

    public HexMesh terrain, rivers, roads, water, waterShore, estuaries;
    public HexFeatureManager features;
    Canvas gridCanvas;


    static Color weights1 = new Color(1f, 0f, 0f);
    static Color weights2 = new Color(0f, 1f, 0f);
    static Color weights3 = new Color(0f, 0f, 1f);






    private void Awake()
    {
        gridCanvas = GetComponentInChildren<Canvas>();

        cells = new HexCell[HexMetrics.chunkSizeX * HexMetrics.chunkSizeZ];
        //默认隐藏单元格坐标
        //ShowUI(false);
    }

    private void LateUpdate()
    {
        Triangulate();
        enabled = false;
    }
    /// <summary>
    /// 添加单元格
    /// </summary>
    /// <param name="index"></param>
    /// <param name="cell"></param>
    public void AddCell(int index, HexCell cell)
    {
        cells[index] = cell;
        cell.chunk = this;
        cell.transform.SetParent(transform, false);
        cell.uiRect.SetParent(gridCanvas.transform, false);
    }
    /// <summary>
    /// 刷新
    /// </summary>
    public void Refresh()
    {
        enabled = true;
    }

    public void ShowUI(bool visible)
    {
        gridCanvas.gameObject.SetActive(visible);
    }
    /// <summary>
    /// 三角化地图
    /// </summary>
    /// <param name="cells">单元格列表</param>
    public void Triangulate()
    {
        terrain.Clear();
        rivers.Clear();
        roads.Clear();
        water.Clear();
        waterShore.Clear();
        estuaries.Clear();
        features.Clear();

        for (int i = 0; i < cells.Length; i++)
        {
            Triangulate(cells[i]);
        }

        terrain.Apply();
        rivers.Apply();
        roads.Apply();
        water.Apply();
        waterShore.Apply();
        estuaries.Apply();
        features.Apply();
    }
    /// <summary>
    /// 三角化单元格
    /// </summary>
    /// <param name="cell"></param>
    void Triangulate(HexCell cell)
    {
        for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++)
        {
            Triangulate(d, cell);
        }

        if (!cell.IsUnderwater)
        {
            if (!cell.HasRiver && !cell.HasRoads)
            {
                features.AddFeature(cell, cell.Position);
            }

            if (cell.IsSpecial)
            {
                features.AddSpecialFeature(cell, cell.Position);
            }
        }
    }

    /// <summary>
    /// 三角化三角形
    /// </summary>
    /// <param name="direction"></param>
    /// <param name="cell"></param>
    void Triangulate(HexDirection direction, HexCell cell)
    {
        Vector3 center = cell.Position;
        EdgeVertices e = new EdgeVertices(
            center + HexMetrics.GetFirstSolidCorner(direction),
            center + HexMetrics.GetSecondSolidCorner(direction)
            );

        //当河流流经时将边上的中间顶点降低为河床高度
        if (cell.HasRiver)
        {
            if (cell.HasRiverThroughEdge(direction))
            {
                e.v3.y = cell.StreamBedY;
                if (cell.HasRiverBeginOrEnd)
                {
                    TriangulateWithRiverBeginOrEnd(direction, cell, center, e);
                }
                else
                {
                    TriangulateWithRiver(direction, cell, center, e);
                }
            }
            else
            {
                TriangulateAdjacentToRiver(direction, cell, center, e);
            }
        }
        else
        {
            TriangulateWithoutRiver(direction, cell, center, e);

            if (!cell.IsUnderwater && !cell.HasRoadThroughEdge(direction))
            {
                features.AddFeature(cell, (center + e.v1 + e.v5) * (1f / 3f));
            }
        }

        if (direction <= HexDirection.SE)
        {
            TriangulateConnection(direction, cell, e);
        }

        if (cell.IsUnderwater)
        {
            TriangulateWater(direction, cell, center);
        }

    }
    /// <summary>
    /// 三角化单元格间的连接部分
    /// </summary>
    /// <param name="direction"></param>
    /// <param name="cell"></param>
    /// <param name="v1"></param>
    /// <param name="v2"></param>
    void TriangulateConnection(HexDirection direction, HexCell cell, EdgeVertices e1)
    {
        HexCell neighbor = cell.GetNeighbor(direction);

        if (neighbor == null)
        {
            return;
        }

        Vector3 bridge = HexMetrics.GetBridge(direction);
        bridge.y = neighbor.Position.y - cell.Position.y;
        EdgeVertices e2 = new EdgeVertices(
            e1.v1 + bridge,
            e1.v5 + bridge
            );

        bool hasRiver = cell.HasRiverThroughEdge(direction);
        bool hasRoad = cell.HasRoadThroughEdge(direction);

        //当河流流经时将边上的中间顶点降低为河床高度
        if (hasRiver)
        {
            e2.v3.y = neighbor.StreamBedY;
            Vector3 indices;
            indices.x = indices.z = cell.Index;
            indices.y = neighbor.Index;

            if (!cell.IsUnderwater)
            {
                if (!neighbor.IsUnderwater)
                {
                    TriangulateRiverQuad(
                                    e1.v2, e1.v4, e2.v2, e2.v4,
                                    cell.RiverSurfaceY, neighbor.RiverSurfaceY, 0.8f,
                                    cell.HasIncomingRiver && cell.IncomingRiver == direction,
                                    indices
                                    );
                }
                else if (cell.Elevation > neighbor.WaterLevel)
                {
                    TriangulateWaterfallInWater(
                        e1.v2, e1.v4, e2.v2, e2.v4,
                        cell.RiverSurfaceY, neighbor.RiverSurfaceY,
                        neighbor.WaterSurfaceY, indices
                        );
                }
            }
            else if (!neighbor.IsUnderwater && neighbor.Elevation > cell.WaterLevel)
            {
                TriangulateWaterfallInWater(
                    e2.v4, e2.v2, e1.v4, e1.v2,
                    neighbor.RiverSurfaceY, cell.RiverSurfaceY,
                    cell.WaterSurfaceY, indices
                    );
            }
        }

        //当边缘连接为斜坡时加入阶地
        if (cell.GetEdgeType(direction) == HexEdgeType.Slope)
        {
            TriangulateEdgeTerraces(e1, cell, e2, neighbor, hasRoad);
        }
        else
        {
            //TriangulateEdgeStrip(e1, color1, e2, color2, hasRoad);
            TriangulateEdgeStrip(e1, weights1, cell.Index, e2, weights2, neighbor.Index, hasRoad);
        }

        //围墙
        features.AddWall(e1, cell, e2, neighbor, hasRiver, hasRoad);

        HexCell nextNeighbor = cell.GetNeighbor(direction.Next());
        if (direction <= HexDirection.E && nextNeighbor != null)
        {
            Vector3 v5 = e1.v5 + HexMetrics.GetBridge(direction.Next());
            v5.y = nextNeighbor.Position.y;

            if (cell.Elevation <= neighbor.Elevation)
            {
                if (cell.Elevation <= nextNeighbor.Elevation)
                {
                    TriangulateCorner(e1.v5, cell, e2.v5, neighbor, v5, nextNeighbor);
                }
                else
                {
                    TriangulateCorner(v5, nextNeighbor, e1.v5, cell, e2.v5, neighbor);
                }
            }
            else if (neighbor.Elevation <= nextNeighbor.Elevation)
            {
                TriangulateCorner(e2.v5, neighbor, v5, nextNeighbor, e1.v5, cell);
            }
            else
            {
                TriangulateCorner(v5, nextNeighbor, e1.v5, cell, e2.v5, neighbor);
            }
        }
    }
    /// <summary>
    /// 三角化单元格边缘连接
    /// </summary>
    /// <param name="beginLeft"></param>
    /// <param name="beginRight"></param>
    /// <param name="beginCell"></param>
    /// <param name="endLeft"></param>
    /// <param name="endRight"></param>
    /// <param name="endCell"></param>
    void TriangulateEdgeTerraces(EdgeVertices begin, HexCell beginCell, EdgeVertices end, HexCell endCell, bool hasRoad)
    {
        EdgeVertices e2 = EdgeVertices.TerraceLerp(begin, end, 1);
        Color c2 = HexMetrics.TerraceLerp(weights1, weights2, 1);

        float t1 = beginCell.Index;
        float t2 = endCell.Index;

        //TriangulateEdgeStrip(begin, color1, e2, c2, hasRoad);
        TriangulateEdgeStrip(begin, weights1, t1, e2, c2, t2, hasRoad);

        for (int i = 2; i < HexMetrics.terraceSteps; i++)
        {
            EdgeVertices e1 = e2;
            Color c1 = c2;
            e2 = EdgeVertices.TerraceLerp(begin, end, i);
            c2 = HexMetrics.TerraceLerp(weights1, weights2, i);
            //TriangulateEdgeStrip(e1, c1, e2, c2, hasRoad);
            TriangulateEdgeStrip(e1, c1, t1, e2, c2, t2, hasRoad);
        }

        //TriangulateEdgeStrip(e2, c2, end, color2, hasRoad);
        TriangulateEdgeStrip(e2, c2, t1, end, weights2, t2, hasRoad);

    }
    /// <summary>
    /// 三角化单元格间的角部连接
    /// </summary>
    /// <param name="bottom"></param>
    /// <param name="bottomCell"></param>
    /// <param name="left"></param>
    /// <param name="leftCell"></param>
    /// <param name="right"></param>
    /// <param name="rightCell"></param>
    void TriangulateCorner(Vector3 bottom, HexCell bottomCell, Vector3 left, HexCell leftCell, Vector3 right, HexCell rightCell)
    {
        HexEdgeType leftEdgeType = bottomCell.GetEdgeType(leftCell);
        HexEdgeType rightEdgeType = bottomCell.GetEdgeType(rightCell);

        if (leftEdgeType == HexEdgeType.Slope)
        {
            if (rightEdgeType == HexEdgeType.Slope)
            {
                TriangulateCornerTerraces(bottom, bottomCell, left, leftCell, right, rightCell);
            }
            else if (rightEdgeType == HexEdgeType.Flat)
            {
                TriangulateCornerTerraces(left, leftCell, right, rightCell, bottom, bottomCell);
            }
            else
            {
                TriangulateCornerTerracesCliff(bottom, bottomCell, left, leftCell, right, rightCell);
            }

        }
        else if (rightEdgeType == HexEdgeType.Slope)
        {
            if (leftEdgeType == HexEdgeType.Flat)
            {
                TriangulateCornerTerraces(right, rightCell, bottom, bottomCell, left, leftCell);
            }
            else
            {
                TriangulateCornerCliffTerraces(bottom, bottomCell, left, leftCell, right, rightCell);
            }

        }
        else if (leftCell.GetEdgeType(rightCell) == HexEdgeType.Slope)
        {
            if (leftCell.Elevation < rightCell.Elevation)
            {
                TriangulateCornerCliffTerraces(right, rightCell, bottom, bottomCell, left, leftCell);
            }
            else
            {
                TriangulateCornerTerracesCliff(left, leftCell, right, rightCell, bottom, bottomCell);
            }

        }
        else
        {
            terrain.AddTriangle(bottom, left, right);
            //terrain.AddTriangleColor(color1, color2, color3);

            //Vector3 types;
            //types.x = bottomCell.TerrainTypeIndex;
            //types.y = leftCell.TerrainTypeIndex;
            //types.z = rightCell.TerrainTypeIndex;
            //terrain.AddTriangulateTerrainType(types);
            Vector3 indices;
            indices.x = bottomCell.Index;
            indices.y = leftCell.Index;
            indices.z = rightCell.Index;
            terrain.AddTriangleCellData(indices, weights1, weights2, weights3);
        }

        //角部围墙
        features.AddWall(bottom, bottomCell, left, leftCell, right, rightCell);
    }
    /// <summary>
    /// 三角化角部连接,当角部三角形的边是两个斜坡与一个平地时
    /// </summary>
    /// <param name="begin"></param>
    /// <param name="beginCell"></param>
    /// <param name="left"></param>
    /// <param name="leftCell"></param>
    /// <param name="right"></param>
    /// <param name="rightCell"></param>
    void TriangulateCornerTerraces(Vector3 begin, HexCell beginCell, Vector3 left, HexCell leftCell, Vector3 right, HexCell rightCell)
    {
        Vector3 v3 = HexMetrics.TerraceLerp(begin, left, 1);
        Vector3 v4 = HexMetrics.TerraceLerp(begin, right, 1);
        Color w3 = HexMetrics.TerraceLerp(weights1, weights2, 1);
        Color w4 = HexMetrics.TerraceLerp(weights1, weights3, 1);

        Vector3 indices;
        indices.x = beginCell.Index;
        indices.y = leftCell.Index;
        indices.z = rightCell.Index;

        terrain.AddTriangle(begin, v3, v4);
        terrain.AddTriangleCellData(indices, weights1, w3, w4);
        //terrain.AddTriangleColor(color1, c3, c4);
        //terrain.AddTriangulateTerrainType(types);

        for (int i = 2; i < HexMetrics.terraceSteps; i++)
        {
            Vector3 v1 = v3;
            Vector3 v2 = v4;
            Color w1 = w3;
            Color w2 = w4;
            v3 = HexMetrics.TerraceLerp(begin, left, i);
            v4 = HexMetrics.TerraceLerp(begin, right, i);
            w3 = HexMetrics.TerraceLerp(weights1, weights2, i);
            w4 = HexMetrics.TerraceLerp(weights1, weights3, i);
            terrain.AddQuad(v1, v2, v3, v4);
            terrain.AddQuadCellData(indices, w1, w2, w3, w4);
            //terrain.AddQuadColor(c1, c2, c3, c4);
            //terrain.AddQuadTerrainType(types);
        }

        terrain.AddQuad(v3, v4, left, right);
        terrain.AddQuadCellData(indices, w3, w4, weights2, weights3);
        //terrain.AddQuadColor(c3, c4, color2, color3);
        //terrain.AddQuadTerrainType(types);
    }
    /// <summary>
    /// 三角化角部连接,当角部三角形的边是SCS与SCC
    /// </summary>
    /// <param name="begin"></param>
    /// <param name="beginCell"></param>
    /// <param name="left"></param>
    /// <param name="leftCell"></param>
    /// <param name="right"></param>
    /// <param name="rightCell"></param>
    void TriangulateCornerTerracesCliff(Vector3 begin, HexCell beginCell, Vector3 left, HexCell leftCell, Vector3 right, HexCell rightCell)
    {
        float b = 1f / (rightCell.Elevation - beginCell.Elevation);
        if (b < 0)
        {
            b = -b;
        }
        Vector3 boundary = Vector3.Lerp(HexMetrics.Perturb(begin), HexMetrics.Perturb(right), b);
        Color boundaryWeights = Color.Lerp(weights1, weights3, b);

        Vector3 indices;
        indices.x = beginCell.Index;
        indices.y = leftCell.Index;
        indices.z = rightCell.Index;

        TriangulateBoundaryTriangle(begin, weights1, left, weights2, boundary, boundaryWeights, indices);

        if (leftCell.GetEdgeType(rightCell) == HexEdgeType.Slope)
        {
            TriangulateBoundaryTriangle(left, weights2, right, weights3, boundary, boundaryWeights, indices);
        }
        else
        {
            //terrain.AddTriangle(left, right, boundary);
            terrain.AddTriangulateUnperturbed(HexMetrics.Perturb(left), HexMetrics.Perturb(right), boundary);
            terrain.AddTriangleCellData(indices, weights2, weights3, boundaryWeights);
            //terrain.AddTriangleColor(color2, color3, boundaryColor);
            //terrain.AddTriangulateTerrainType(types);
        }
    }
    /// <summary>
    /// 三角化角部连接,当角部三角形的边是CCSR与CCSL
    /// </summary>
    /// <param name="begin"></param>
    /// <param name="beginCell"></param>
    /// <param name="left"></param>
    /// <param name="leftCell"></param>
    /// <param name="boundary"></param>
    /// <param name="boundaryColor"></param>
    void TriangulateBoundaryTriangle(Vector3 begin, Color beginWeights, Vector3 left, Color leftWeights, Vector3 boundary, Color boundaryWeights, Vector3 indices)
    {
        Vector3 v2 = HexMetrics.Perturb(HexMetrics.TerraceLerp(begin, left, 1));
        Color w2 = HexMetrics.TerraceLerp(beginWeights, leftWeights, 1);

        //terrain.AddTriangle(begin, v2, boundary);
        terrain.AddTriangulateUnperturbed(HexMetrics.Perturb(begin), v2, boundary);
        terrain.AddTriangleCellData(indices, beginWeights, w2, boundaryWeights);
        //terrain.AddTriangleColor(beginColor, c2, boundaryColor);
        //terrain.AddTriangulateTerrainType(types);

        for (int i = 2; i < HexMetrics.terraceSteps; i++)
        {
            Vector3 v1 = v2;
            Color w1 = w2;
            v2 = HexMetrics.Perturb(HexMetrics.TerraceLerp(begin, left, i));
            w2 = HexMetrics.TerraceLerp(beginWeights, leftWeights, i);
            terrain.AddTriangulateUnperturbed(v1, v2, boundary);
            terrain.AddTriangleCellData(indices, w1, w2, boundaryWeights);
            //terrain.AddTriangleColor(c1, c2, boundaryColor);
            //terrain.AddTriangulateTerrainType(types);
        }

        terrain.AddTriangulateUnperturbed(v2, HexMetrics.Perturb(left), boundary);
        terrain.AddTriangleCellData(indices, w2, leftWeights, boundaryWeights);
        //terrain.AddTriangleColor(c2, leftColor, boundaryColor);
        //terrain.AddTriangulateTerrainType(types);
    }
    /// <summary>
    /// 三角化角部连接,当角部三角形的边是CSS与CSC
    /// </summary>
    /// <param name="begin"></param>
    /// <param name="beginCell"></param>
    /// <param name="left"></param>
    /// <param name="leftCell"></param>
    /// <param name="right"></param>
    /// <param name="rightCell"></param>
    void TriangulateCornerCliffTerraces(Vector3 begin, HexCell beginCell, Vector3 left, HexCell leftCell, Vector3 right, HexCell rightCell)
    {
        float b = 1f / (leftCell.Elevation - beginCell.Elevation);
        if (b < 0)
        {
            b = -b;
        }
        Vector3 boundary = Vector3.Lerp(HexMetrics.Perturb(begin), HexMetrics.Perturb(left), b);
        Color boundaryWeights = Color.Lerp(weights1, weights2, b);

        Vector3 indices;
        indices.x = beginCell.Index;
        indices.y = leftCell.Index;
        indices.z = rightCell.Index;

        TriangulateBoundaryTriangle(right, weights3, begin, weights1, boundary, boundaryWeights, indices);

        if (leftCell.GetEdgeType(rightCell) == HexEdgeType.Slope)
        {
            TriangulateBoundaryTriangle(left, weights2, right, weights3, boundary, boundaryWeights, indices);
        }
        else
        {
            terrain.AddTriangulateUnperturbed(HexMetrics.Perturb(left), HexMetrics.Perturb(right), boundary);
            terrain.AddTriangleCellData(indices, weights2, weights3, boundaryWeights);
            //terrain.AddTriangleColor(color2, color3, boundaryColor);
            //terrain.AddTriangulateTerrainType(types);
        }
    }
    /// <summary>
    /// 三角化单元中心和一条边之间的三角扇形
    /// </summary>
    /// <param name="center"></param>
    /// <param name="edge"></param>
    /// <param name="color"></param>
    void TriangulateEdgeFan(Vector3 center, EdgeVertices edge, float index)
    {
        terrain.AddTriangle(center, edge.v1, edge.v2);
        terrain.AddTriangle(center, edge.v2, edge.v3);
        terrain.AddTriangle(center, edge.v3, edge.v4);
        terrain.AddTriangle(center, edge.v4, edge.v5);

        //terrain.AddTriangleColor(color1);
        //terrain.AddTriangleColor(color1);
        //terrain.AddTriangleColor(color1);
        //terrain.AddTriangleColor(color1);

        //Vector3 types;
        //types.x = types.y = types.z = type;
        //terrain.AddTriangulateTerrainType(types);
        //terrain.AddTriangulateTerrainType(types);
        //terrain.AddTriangulateTerrainType(types);
        //terrain.AddTriangulateTerrainType(types);

        Vector3 indices;
        indices.x = indices.y = indices.z = index;
        terrain.AddTriangleCellData(indices, weights1);
        terrain.AddTriangleCellData(indices, weights1);
        terrain.AddTriangleCellData(indices, weights1);
        terrain.AddTriangleCellData(indices, weights1);
    }
    /// <summary>
    /// 三角化两条边之间的四边形
    /// </summary>
    /// <param name="e1"></param>
    /// <param name="c1"></param>
    /// <param name="e2"></param>
    /// <param name="c2"></param>
    void TriangulateEdgeStrip(EdgeVertices e1, Color w1, float index1, EdgeVertices e2, Color w2, float index2, bool hasRoad = false)
    {
        terrain.AddQuad(e1.v1, e1.v2, e2.v1, e2.v2);
        //terrain.AddQuadColor(c1, c2);
        terrain.AddQuad(e1.v2, e1.v3, e2.v2, e2.v3);
        //terrain.AddQuadColor(c1, c2);
        terrain.AddQuad(e1.v3, e1.v4, e2.v3, e2.v4);
        //terrain.AddQuadColor(c1, c2);
        terrain.AddQuad(e1.v4, e1.v5, e2.v4, e2.v5);
        //terrain.AddQuadColor(c1, c2);

        //Vector3 types;
        //types.x = types.z = type1;
        //types.y = type2;
        //terrain.AddQuadTerrainType(types);
        //terrain.AddQuadTerrainType(types);
        //terrain.AddQuadTerrainType(types);
        //terrain.AddQuadTerrainType(types);

        Vector3 indices;
        indices.x = indices.z = index1;
        indices.y = index2;
        terrain.AddQuadCellData(indices, w1, w2);
        terrain.AddQuadCellData(indices, w1, w2);
        terrain.AddQuadCellData(indices, w1, w2);
        terrain.AddQuadCellData(indices, w1, w2);

        if (hasRoad)
        {
            TriangulateRoadSegment(e1.v2, e1.v3, e1.v4, e2.v2, e2.v3, e2.v4, w1, w2, indices);
        }
    }


    /// <summary>
    /// 三角化穿过的河道
    /// </summary>
    /// <param name="direction"></param>
    /// <param name="cell"></param>
    /// <param name="center"></param>
    /// <param name="e"></param>
    void TriangulateWithRiver(HexDirection direction, HexCell cell, Vector3 center, EdgeVertices e)
    {
        Vector3 centerL, centerR;
        if (cell.HasRiverThroughEdge(direction.Opposite()))
        {
            centerL = center +
                HexMetrics.GetFirstSolidCorner(direction.Previous()) * 0.25f;
            centerR = center +
                HexMetrics.GetSecondSolidCorner(direction.Next()) * 0.25f;
        }
        else if (cell.HasRiverThroughEdge(direction.Next()))
        {
            centerL = center;
            centerR = Vector3.Lerp(center, e.v5, 2f / 3f);
        }
        else if (cell.HasRiverThroughEdge(direction.Previous()))
        {
            centerL = Vector3.Lerp(center, e.v1, 2f / 3f);
            centerR = center;
        }
        else if (cell.HasRiverThroughEdge(direction.Next2()))
        {
            centerL = center;
            centerR = center +
                HexMetrics.GetSolidEdgeMiddle(direction.Next()) *
                (0.5f * HexMetrics.innerToOuter);
        }
        else
        {
            centerL = center +
                HexMetrics.GetSolidEdgeMiddle(direction.Previous()) *
                (0.5f * HexMetrics.innerToOuter);
            centerR = center;
        }
        center = Vector3.Lerp(centerL, centerR, 0.5f);
        EdgeVertices m = new EdgeVertices(Vector3.Lerp(centerL, e.v1, 0.5f), Vector3.Lerp(centerR, e.v5, 0.5f), 1f / 6f);
        m.v3.y = center.y = e.v3.y;
        //TriangulateEdgeStrip(m, cell.Color, e, cell.Color);
        TriangulateEdgeStrip(m, weights1, cell.Index, e, weights1, cell.Index);

        terrain.AddTriangle(centerL, m.v1, m.v2);
        //terrain.AddTriangleColor(cell.Color);
        terrain.AddQuad(centerL, center, m.v2, m.v3);
        //terrain.AddQuadColor(cell.Color);
        terrain.AddQuad(center, centerR, m.v3, m.v4);
        //terrain.AddQuadColor(cell.Color);
        terrain.AddTriangle(centerR, m.v4, m.v5);
        //terrain.AddTriangleColor(cell.Color);

        //terrain.AddTriangleColor(color1);
        //terrain.AddQuadColor(color1);
        //terrain.AddQuadColor(color1);
        //terrain.AddTriangleColor(color1);

        //Vector3 types;
        //types.x = types.y = types.z = cell.TerrainTypeIndex;
        //terrain.AddTriangulateTerrainType(types);
        //terrain.AddQuadTerrainType(types);
        //terrain.AddQuadTerrainType(types);
        //terrain.AddTriangulateTerrainType(types);

        Vector3 indices;
        indices.x = indices.y = indices.z = cell.Index;
        terrain.AddTriangleCellData(indices, weights1);
        terrain.AddQuadCellData(indices, weights1);
        terrain.AddQuadCellData(indices, weights1);
        terrain.AddTriangleCellData(indices, weights1);

        if (!cell.IsUnderwater)
        {
            bool reversed = cell.IncomingRiver == direction;
            TriangulateRiverQuad(centerL, centerR, m.v2, m.v4, cell.RiverSurfaceY, 0.4f, reversed, indices);
            TriangulateRiverQuad(m.v2, m.v4, e.v2, e.v4, cell.RiverSurfaceY, 0.6f, reversed, indices);
        }
    }
    /// <summary>
    /// 三角化河流的开始或结尾
    /// </summary>
    /// <param name="direction"></param>
    /// <param name="cell"></param>
    /// <param name="center"></param>
    /// <param name="e"></param>
    void TriangulateWithRiverBeginOrEnd(HexDirection direction, HexCell cell, Vector3 center, EdgeVertices e)
    {

        EdgeVertices m = new EdgeVertices(
            Vector3.Lerp(center, e.v1, 0.5f),
            Vector3.Lerp(center, e.v5, 0.5f)
            );
        m.v3.y = e.v3.y;


        //TriangulateEdgeStrip(m, color1, e, color1);
        TriangulateEdgeStrip(m, weights1, cell.Index, e, weights1, cell.Index);
        TriangulateEdgeFan(center, m, cell.Index);

        if (!cell.IsUnderwater)
        {
            bool reversed = cell.HasIncomingRiver;
            Vector3 indices;
            indices.x = indices.y = indices.z = cell.Index;
            TriangulateRiverQuad(m.v2, m.v4, e.v2, e.v4, cell.RiverSurfaceY, 0.6f, reversed, indices);
            center.y = m.v2.y = m.v4.y = cell.RiverSurfaceY;
            rivers.AddTriangle(center, m.v2, m.v4);
            if (reversed)
            {
                rivers.AddTriangulateUV(new Vector2(0.5f, 0.4f), new Vector2(1f, 0.2f), new Vector2(0f, 0.2f));
            }
            else
            {
                rivers.AddTriangulateUV(new Vector2(0.5f, 0.4f), new Vector2(0f, 0.6f), new Vector2(1f, 0.6f));
            }
            rivers.AddTriangleCellData(indices, weights1);
        }
    }
    /// <summary>
    /// 三角化河流经过时单元格内没有河流的方向
    /// </summary>
    /// <param name="direction"></param>
    /// <param name="cell"></param>
    /// <param name="center"></param>
    /// <param name="e"></param>
    void TriangulateAdjacentToRiver(HexDirection direction, HexCell cell, Vector3 center, EdgeVertices e)
    {
        if (cell.HasRoads)
        {
            TriangulateRoadAdjacentToRiver(direction, cell, center, e);
        }
        if (cell.HasRiverThroughEdge(direction.Next()))
        {
            if (cell.HasRiverThroughEdge(direction.Previous()))
            {
                center += HexMetrics.GetSolidEdgeMiddle(direction) *
                    (HexMetrics.innerToOuter * 0.5f);
            }
            else if (
                cell.HasRiverThroughEdge(direction.Previous2())
            )
            {
                center += HexMetrics.GetFirstSolidCorner(direction) * 0.25f;
            }
        }
        else if (
            cell.HasRiverThroughEdge(direction.Previous()) &&
            cell.HasRiverThroughEdge(direction.Next2())
        )
        {
            center += HexMetrics.GetSecondSolidCorner(direction) * 0.25f;
        }

        EdgeVertices m = new EdgeVertices(
            Vector3.Lerp(center, e.v1, 0.5f),
            Vector3.Lerp(center, e.v5, 0.5f)
            );

        //TriangulateEdgeStrip(m, color1, e, color1);
        TriangulateEdgeStrip(m, weights1, cell.Index, e, weights1, cell.Index);
        TriangulateEdgeFan(center, m, cell.Index);

        if (!cell.IsUnderwater && !cell.HasRoadThroughEdge(direction))
        {
            features.AddFeature(cell, (center + e.v1 + e.v5) * (1f / 3f));
        }
    }
    /// <summary>
    /// 三角化河流四边形
    /// </summary>
    /// <param name="v1"></param>
    /// <param name="v2"></param>
    /// <param name="v3"></param>
    /// <param name="v4"></param>
    /// <param name="y"></param>
    void TriangulateRiverQuad(Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4, float y1, float y2, float v, bool reversed, Vector3 indices)
    {
        v1.y = v2.y = y1;
        v3.y = v4.y = y2;
        rivers.AddQuad(v1, v2, v3, v4);
        if (reversed)
        {
            rivers.AddQuadUV(1f, 0f, 0.8f - v, 0.6f - v);
        }
        else
        {
            rivers.AddQuadUV(0f, 1f, v, v + 0.2f);
        }
        rivers.AddQuadCellData(indices, weights1, weights2);
    }
    /// <summary>
    /// 三角化河流四边形,高度相同
    /// </summary>
    /// <param name="v1"></param>
    /// <param name="v2"></param>
    /// <param name="v3"></param>
    /// <param name="v4"></param>
    /// <param name="y"></param>
    /// <param name="reversed"></param>
    void TriangulateRiverQuad(Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4, float y, float v, bool reversed, Vector3 indices)
    {
        TriangulateRiverQuad(v1, v2, v3, v4, y, y, v, reversed, indices);
    }
    /// <summary>
    /// 三角化道路
    /// </summary>
    /// <param name="v1"></param>
    /// <param name="v2"></param>
    /// <param name="v3"></param>
    /// <param name="v4"></param>
    /// <param name="v5"></param>
    /// <param name="v6"></param>
    void TriangulateRoadSegment(Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4, Vector3 v5, Vector3 v6, Color w1, Color w2, Vector3 indices)
    {
        roads.AddQuad(v1, v2, v4, v5);
        roads.AddQuad(v2, v3, v5, v6);
        roads.AddQuadUV(0f, 1f, 0f, 0f);
        roads.AddQuadUV(1f, 0f, 0f, 0f);
        roads.AddQuadCellData(indices, w1, w2);
        roads.AddQuadCellData(indices, w1, w2);
    }
    /// <summary>
    /// 三角化道路
    /// </summary>
    /// <param name="center"></param>
    /// <param name="mL"></param>
    /// <param name="mR"></param>
    /// <param name="e"></param>
    /// <param name="hasRoadThroughCellEdge"></param>
    void TriangulateRoad(Vector3 center, Vector3 mL, Vector3 mR, EdgeVertices e, bool hasRoadThroughCellEdge, float index)
    {
        if (hasRoadThroughCellEdge)
        {
            Vector3 indices;
            indices.x = indices.y = indices.z = index;
            Vector3 mC = Vector3.Lerp(mL, mR, 0.5f);
            TriangulateRoadSegment(mL, mC, mR, e.v2, e.v3, e.v4, weights1, weights1, indices);
            roads.AddTriangle(center, mL, mC);
            roads.AddTriangle(center, mC, mR);
            roads.AddTriangulateUV(new Vector2(1f, 0f), new Vector2(0f, 0f), new Vector2(1f, 0f));
            roads.AddTriangulateUV(new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(0f, 0f));
            roads.AddTriangleCellData(indices, weights1);
            roads.AddTriangleCellData(indices, weights1);
        }
        else
        {
            TriangulateRoadEdge(center, mL, mR, index);
        }
    }
    /// <summary>
    /// 三角化道路边缘
    /// </summary>
    /// <param name="center"></param>
    /// <param name="mL"></param>
    /// <param name="mR"></param>
    void TriangulateRoadEdge(Vector3 center, Vector3 mL, Vector3 mR, float index)
    {
        roads.AddTriangle(center, mL, mR);
        roads.AddTriangulateUV(new Vector2(1f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f));

        Vector3 indices;
        indices.x = indices.y = indices.z = index;
        roads.AddTriangleCellData(indices, weights1);
    }
    /// <summary>
    /// 获得道路插值
    /// </summary>
    /// <param name="direction"></param>
    /// <param name="cell"></param>
    /// <returns></returns>
    Vector2 GetRoadInterpolators(HexDirection direction, HexCell cell)
    {
        Vector2 interpolators;
        if (cell.HasRoadThroughEdge(direction))
        {
            interpolators.x = interpolators.y = 0.5f;
        }
        else
        {
            interpolators.x = cell.HasRoadThroughEdge(direction.Previous()) ? 0.5f : 0.25f;
            interpolators.y = cell.HasRoadThroughEdge(direction.Next()) ? 0.5f : 0.25f;
        }
        return interpolators;
    }
    /// <summary>
    /// 三角化无河流部分
    /// </summary>
    /// <param name="direction"></param>
    /// <param name="cell"></param>
    /// <param name="center"></param>
    /// <param name="e"></param>
    void TriangulateWithoutRiver(HexDirection direction, HexCell cell, Vector3 center, EdgeVertices e)
    {
        TriangulateEdgeFan(center, e, cell.Index);

        if (cell.HasRoads)
        {
            Vector2 interpolators = GetRoadInterpolators(direction, cell);
            TriangulateRoad(
                center,
                Vector3.Lerp(center, e.v1, interpolators.x),
                Vector3.Lerp(center, e.v5, interpolators.y),
                e, cell.HasRoadThroughEdge(direction), cell.Index
            );
        }
    }

    /// <summary>
    /// 三角化道路,临近河流时
    /// </summary>
    /// <param name="direction"></param>
    /// <param name="cell"></param>
    /// <param name="center"></param>
    /// <param name="e"></param>
    void TriangulateRoadAdjacentToRiver(HexDirection direction, HexCell cell, Vector3 center, EdgeVertices e)
    {
        bool hasRoadThroughEdge = cell.HasRoadThroughEdge(direction);
        bool previousHasRiver = cell.HasRiverThroughEdge(direction.Previous());
        bool nextHasRiver = cell.HasRiverThroughEdge(direction.Next());
        Vector2 interpolators = GetRoadInterpolators(direction, cell);
        Vector3 roadCenter = center;

        if (cell.HasRiverBeginOrEnd)
        {
            roadCenter += HexMetrics.GetSolidEdgeMiddle(cell.RiverBeginOrEndDirection.Opposite()) * (1f / 3f);
        }
        else if (cell.IncomingRiver == cell.OutgoingRiver.Opposite())
        {
            Vector3 corner;
            if (previousHasRiver)
            {
                if (!hasRoadThroughEdge && !cell.HasRoadThroughEdge(direction.Next()))
                {
                    return;
                }
                corner = HexMetrics.GetSecondSolidCorner(direction);
            }
            else
            {
                if (!hasRoadThroughEdge && !cell.HasRoadThroughEdge(direction.Previous()))
                {
                    return;
                }
                corner = HexMetrics.GetFirstSolidCorner(direction);
            }
            roadCenter += corner * 0.5f;
            //直线河流架设桥梁
            if (
                cell.IncomingRiver == direction.Next() &&
                (cell.HasRoadThroughEdge(direction.Next2()) ||
                cell.HasRoadThroughEdge(direction.Opposite()))
                )
            {
                features.AddBridge(roadCenter, center - center * 0.5f);
            }
            center += corner * 0.25f;
        }
        else if (cell.IncomingRiver == cell.OutgoingRiver.Previous())
        {
            roadCenter -= HexMetrics.GetSecondCorner(cell.IncomingRiver) * 0.2f;
        }
        else if (cell.IncomingRiver == cell.OutgoingRiver.Next())
        {
            roadCenter -= HexMetrics.GetFirstCorner(cell.IncomingRiver) * 0.2f;
        }
        else if (previousHasRiver && nextHasRiver)
        {
            if (!hasRoadThroughEdge)
            {
                return;
            }
            Vector3 offset = HexMetrics.GetSolidEdgeMiddle(direction) * HexMetrics.innerToOuter;
            roadCenter += offset * 0.7f;
            center += offset * 0.5f;
        }
        else
        {
            HexDirection middle;
            if (previousHasRiver)
            {
                middle = direction.Next();
            }
            else if (nextHasRiver)
            {
                middle = direction.Previous();
            }
            else
            {
                middle = direction;
            }

            if (
                !cell.HasRoadThroughEdge(middle) &&
                !cell.HasRoadThroughEdge(middle.Previous()) &&
                !cell.HasRoadThroughEdge(middle.Next())
                )
            {
                return;
            }

            Vector3 offset = HexMetrics.GetSolidEdgeMiddle(middle);
            //roadCenter += HexMetrics.GetSolidEdgeMiddle(middle) * 0.25f;
            roadCenter += offset * 0.25f;
            //弯曲河流加入桥梁
            if (
                direction == middle &&
                cell.HasRoadThroughEdge(direction.Opposite())
                )
            {
                features.AddBridge(roadCenter, center - offset * (HexMetrics.innerToOuter * 0.7f));
            }
        }

        Vector3 mL = Vector3.Lerp(roadCenter, e.v1, interpolators.x);
        Vector3 mR = Vector3.Lerp(roadCenter, e.v5, interpolators.y);
        TriangulateRoad(roadCenter, mL, mR, e, hasRoadThroughEdge, cell.Index);

        if (previousHasRiver)
        {
            TriangulateRoadEdge(roadCenter, center, mL, cell.Index);
        }
        if (nextHasRiver)
        {
            TriangulateRoadEdge(roadCenter, mR, center, cell.Index);
        }
    }
    /// <summary>
    /// 三角化水体单元
    /// </summary>
    /// <param name="direction"></param>
    /// <param name="cell"></param>
    /// <param name="center"></param>
    void TriangulateWater(HexDirection direction, HexCell cell, Vector3 center)
    {
        center.y = cell.WaterSurfaceY;

        HexCell neighbor = cell.GetNeighbor(direction);
        if (neighbor != null && !neighbor.IsUnderwater)
        {
            TriangulateWaterShore(direction, cell, neighbor, center);
        }
        else
        {
            TriangulateOpenWater(direction, cell, neighbor, center);
        }

    }
    /// <summary>
    /// 三角化开阔水体单元
    /// </summary>
    /// <param name="direction"></param>
    /// <param name="cell"></param>
    /// <param name="neighbor"></param>
    /// <param name="center"></param>
    void TriangulateOpenWater(HexDirection direction, HexCell cell, HexCell neighbor, Vector3 center)
    {
        Vector3 c1 = center + HexMetrics.GetFirstWaterCorner(direction);
        Vector3 c2 = center + HexMetrics.GetSecondWaterCorner(direction);

        water.AddTriangle(center, c1, c2);
        Vector3 indices;
        indices.x = indices.y = indices.z = cell.Index;
        water.AddTriangleCellData(indices, weights1);

        if (direction <= HexDirection.SE && neighbor != null)
        {
            //HexCell neighbor = cell.GetNeighbor(direction);
            //if (neighbor == null || !neighbor.IsUnderwater)
            //{
            //    return;
            //}

            Vector3 bridge = HexMetrics.GetWaterBridge(direction);
            Vector3 e1 = c1 + bridge;
            Vector3 e2 = c2 + bridge;

            water.AddQuad(c1, c2, e1, e2);
            indices.y = neighbor.Index;
            water.AddQuadCellData(indices, weights1, weights2);

            if (direction <= HexDirection.E)
            {
                HexCell nextNeighbor = cell.GetNeighbor(direction.Next());
                if (nextNeighbor == null || !nextNeighbor.IsUnderwater)
                {
                    return;
                }
                water.AddTriangle(c2, e2, c2 + HexMetrics.GetWaterBridge(direction.Next()));
                indices.z = nextNeighbor.Index;
                water.AddTriangleCellData(indices, weights1, weights2, weights3);
            }
        }
    }
    /// <summary>
    /// 三角化沿岸水体单元
    /// </summary>
    /// <param name="direction"></param>
    /// <param name="cell"></param>
    /// <param name="neighbor"></param>
    /// <param name="center"></param>
    void TriangulateWaterShore(HexDirection direction, HexCell cell, HexCell neighbor, Vector3 center)
    {
        EdgeVertices e1 = new EdgeVertices(
            center + HexMetrics.GetFirstWaterCorner(direction),
            center + HexMetrics.GetSecondWaterCorner(direction)
            );
        water.AddTriangle(center, e1.v1, e1.v2);
        water.AddTriangle(center, e1.v2, e1.v3);
        water.AddTriangle(center, e1.v3, e1.v4);
        water.AddTriangle(center, e1.v4, e1.v5);
        Vector3 indices;
        //indices.x = indices.y = indices.z = cell.Index;
        indices.x = indices.z = cell.Index;
        indices.y = neighbor.Index;
        water.AddTriangleCellData(indices, weights1);
        water.AddTriangleCellData(indices, weights1);
        water.AddTriangleCellData(indices, weights1);
        water.AddTriangleCellData(indices, weights1);

        //Vector3 bridge = HexMetrics.GetWaterBridge(direction);
        Vector3 center2 = neighbor.Position;
        if (neighbor.ColumnIndex < cell.ColumnIndex - 1)
        {
            center2.x += HexMetrics.wrapSize * HexMetrics.innerDiameter;
        }
        else if (neighbor.ColumnIndex > cell.ColumnIndex + 1)
        {
            center2.x -= HexMetrics.wrapSize * HexMetrics.innerDiameter;
        }
        center2.y = center.y;
        EdgeVertices e2 = new EdgeVertices(
            center2 + HexMetrics.GetSecondSolidCorner(direction.Opposite()),
            center2 + HexMetrics.GetFirstSolidCorner(direction.Opposite())
            );
        if (cell.HasRiverThroughEdge(direction))
        {
            TriangulateEstuary(e1, e2, cell.HasIncomingRiver && cell.IncomingRiver == direction, indices);
        }
        else
        {
            waterShore.AddQuad(e1.v1, e1.v2, e2.v1, e2.v2);
            waterShore.AddQuad(e1.v2, e1.v3, e2.v2, e2.v3);
            waterShore.AddQuad(e1.v3, e1.v4, e2.v3, e2.v4);
            waterShore.AddQuad(e1.v4, e1.v5, e2.v4, e2.v5);
            waterShore.AddQuadUV(0f, 0f, 0f, 1f);
            waterShore.AddQuadUV(0f, 0f, 0f, 1f);
            waterShore.AddQuadUV(0f, 0f, 0f, 1f);
            waterShore.AddQuadUV(0f, 0f, 0f, 1f);
            waterShore.AddQuadCellData(indices, weights1, weights2);
            waterShore.AddQuadCellData(indices, weights1, weights2);
            waterShore.AddQuadCellData(indices, weights1, weights2);
            waterShore.AddQuadCellData(indices, weights1, weights2);
        }

        HexCell nextNeighbor = cell.GetNeighbor(direction.Next());
        if (nextNeighbor != null)
        {
            //Vector3 center3 = nextNeighbor.Position;
            //center3.y = center.y;
            Vector3 center3 = nextNeighbor.Position;
            if (nextNeighbor.ColumnIndex < cell.ColumnIndex - 1)
            {
                center3.x += HexMetrics.wrapSize * HexMetrics.innerDiameter;
            }
            else if (nextNeighbor.ColumnIndex > cell.ColumnIndex + 1)
            {
                center3.x -= HexMetrics.wrapSize * HexMetrics.innerDiameter;
            }
            Vector3 v3 = center3 + (nextNeighbor.IsUnderwater ?
                HexMetrics.GetFirstWaterCorner(direction.Previous()) :
                HexMetrics.GetFirstSolidCorner(direction.Previous())
                );
            v3.y = center.y;
            waterShore.AddTriangle(e1.v5, e2.v5, v3);
            waterShore.AddTriangulateUV(new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, nextNeighbor.IsUnderwater ? 0f : 1f));
            indices.z = nextNeighbor.Index;
            waterShore.AddTriangleCellData(indices, weights1, weights2, weights3);
        }
    }
    /// <summary>
    /// 三角化瀑布
    /// </summary>
    /// <param name="v1"></param>
    /// <param name="v2"></param>
    /// <param name="v3"></param>
    /// <param name="v4"></param>
    /// <param name="y1"></param>
    /// <param name="y2"></param>
    /// <param name="waterY"></param>
    void TriangulateWaterfallInWater(Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4, float y1, float y2, float waterY, Vector3 indices)
    {
        v1.y = v2.y = y1;
        v3.y = v4.y = y2;
        v1 = HexMetrics.Perturb(v1);
        v2 = HexMetrics.Perturb(v2);
        v3 = HexMetrics.Perturb(v3);
        v4 = HexMetrics.Perturb(v4);
        float t = (waterY - y2) / (y1 - y2);
        v3 = Vector3.Lerp(v3, v1, t);
        v4 = Vector3.Lerp(v4, v2, t);
        rivers.AddQuadUnperturbed(v1, v2, v3, v4);
        rivers.AddQuadUV(0f, 1f, 0.8f, 1f);
        rivers.AddQuadCellData(indices, weights1, weights2);
    }
    /// <summary>
    /// 三角化河口
    /// </summary>
    /// <param name="e1"></param>
    /// <param name="e2"></param>
    void TriangulateEstuary(EdgeVertices e1, EdgeVertices e2, bool incomingRiver, Vector3 indices)
    {
        waterShore.AddTriangle(e2.v1, e1.v2, e1.v1);
        waterShore.AddTriangle(e2.v5, e1.v5, e1.v4);
        waterShore.AddTriangulateUV(
            new Vector2(0f, 1f), new Vector2(0f, 0f), new Vector2(0f, 0f)
            );
        waterShore.AddTriangulateUV(
            new Vector2(0f, 1f), new Vector2(0f, 0f), new Vector2(0f, 0f)
            );
        waterShore.AddTriangleCellData(indices, weights2, weights1, weights1);
        waterShore.AddTriangleCellData(indices, weights2, weights1, weights1);

        estuaries.AddQuad(e2.v1, e1.v2, e2.v2, e1.v3);
        estuaries.AddTriangle(e1.v3, e2.v2, e2.v4);
        estuaries.AddQuad(e1.v3, e1.v4, e2.v4, e2.v5);

        estuaries.AddQuadUV(new Vector2(0f, 1f), new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0f, 0f));
        estuaries.AddTriangulateUV(new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(1f, 1f));
        estuaries.AddQuadUV(new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0f, 1f));

        estuaries.AddQuadCellData(indices, weights2, weights1, weights2, weights1);
        estuaries.AddTriangleCellData(indices, weights1, weights2, weights2);
        estuaries.AddQuadCellData(indices, weights1, weights2);

        if (incomingRiver)
        {
            estuaries.AddQuadUV2(
                new Vector2(1.5f, 1f), new Vector2(0.7f, 1.15f),
                new Vector2(1f, 0.8f), new Vector2(0.5f, 1.1f)
            );
            estuaries.AddTriangleUV2(
                new Vector2(0.5f, 1.1f),
                new Vector2(1f, 0.8f),
                new Vector2(0f, 0.8f)
            );
            estuaries.AddQuadUV2(
                new Vector2(0.5f, 1.1f), new Vector2(0.3f, 1.15f),
                new Vector2(0f, 0.8f), new Vector2(-0.5f, 1f)
            );
        }
        else
        {
            estuaries.AddQuadUV2(
                new Vector2(-0.5f, -0.2f), new Vector2(0.3f, -0.35f),
                new Vector2(0f, 0f), new Vector2(0.5f, -0.3f)
            );
            estuaries.AddTriangleUV2(
                new Vector2(0.5f, -0.3f),
                new Vector2(0f, 0f),
                new Vector2(1f, 0f)
            );
            estuaries.AddQuadUV2(
                new Vector2(0.5f, -0.3f), new Vector2(0.7f, -0.35f),
                new Vector2(1f, 0f), new Vector2(1.5f, -0.2f)
            );
        }
    }
}
