using UnityEngine;
using UnityEngine.UI;
using System.IO;
using System.Collections;
using System.Collections.Generic;

public class HexGrid : MonoBehaviour
{
    public int cellCountX = 20, cellCountZ = 15;
    int chunkCountX, chunkCountZ;
    int searchFrontierPhase;
    bool currentPathExists;
    int currentCenterColumnIndex = -1;

    public HexCell cellPrefab;
    public Text cellLabelPrefab;
    public HexGridChunk chunkPrefab;
    HexCell currentPathFrom, currentPathTo;
    List<HexUnit> units = new List<HexUnit>();
    public HexUnit unitPrefab;
    public bool wrapping;



    public Texture2D noiseSource;
    /// <summary>
    /// 随机种子
    /// </summary>
    public int seed;
    /// <summary>
    /// 设置地图大小
    /// </summary>

    HexGridChunk[] chunks;
    HexCell[] cells;
    HexCellPriorityQueue searchFrontier;
    HexCellShaderData cellShaderData;
    Transform[] columns;

    public bool HasPath
    {
        get { return currentPathExists; }
    }

    private void Awake()
    {
        HexMetrics.noiseSource = noiseSource;
        HexMetrics.InitializeHashGrid(seed);
        HexUnit.unitPrefab = unitPrefab;
        cellShaderData = gameObject.AddComponent<HexCellShaderData>();
        cellShaderData.Grid = this;

        CreateMap(cellCountX, cellCountZ, wrapping);
    }


    private void OnEnable()
    {
        if (!HexMetrics.noiseSource)
        {
            HexMetrics.noiseSource = noiseSource;
            HexMetrics.InitializeHashGrid(seed);
            HexUnit.unitPrefab = unitPrefab;
            HexMetrics.wrapSize = wrapping ? cellCountX : 0;
            ResetVisibility();
        }
    }


    /// <summary>
    /// 创建单元格
    /// </summary>
    /// <param name="x">在地图宽度的位置</param>
    /// <param name="z">在地图高度的位置</param>
    /// <param name="i">在单元格列表中的位置</param>
    private void CreateCell(int x, int z, int i)
    {
        Vector3 position;
        position.x = (x + z * 0.5f - z / 2) * HexMetrics.innerDiameter;
        position.y = 0f;
        position.z = z * (HexMetrics.outerRadius * 1.5f);

        HexCell cell = cells[i] = Instantiate<HexCell>(cellPrefab);
        cell.transform.localPosition = position;
        cell.coordinates = HexCoordinates.FromOffSetCoordinates(x, z);
        cell.Index = i;
        cell.ColumnIndex = x / HexMetrics.chunkSizeX;
        cell.ShaderData = cellShaderData;

        if (wrapping)
        {
            cell.Explorable = z > 0 && z < cellCountZ - 1;
        }
        else
        {
            cell.Explorable = x > 0 && z > 0 && x < cellCountX - 1 && z < cellCountZ - 1;
        }

        //初始化邻居关系
        //初始化东西方向的关系
        if (x > 0)
        {
            cell.SetNeighbor(HexDirection.W, cells[i - 1]);
            if (wrapping && x == cellCountX - 1)
            {
                cell.SetNeighbor(HexDirection.E, cells[i - x]);
            }
        }
        //初始化其余方向的关系
        if (z > 0)
        {
            //除第0行外索引为偶数的行
            if ((z & 1) == 0)
            {
                //东南及相应单元格的反方向
                cell.SetNeighbor(HexDirection.SE, cells[i - cellCountX]);
                //行内除第1个外其他单元格
                if (x > 0)
                {
                    //东北及相应单元格的反方向
                    cell.SetNeighbor(HexDirection.SW, cells[i - cellCountX - 1]);
                }
                else if (wrapping)
                {
                    cell.SetNeighbor(HexDirection.SW, cells[i - 1]);
                }
            }
            //索引为奇数的行
            else
            {
                //东北及相应单元格的反方向
                cell.SetNeighbor(HexDirection.SW, cells[i - cellCountX]);
                //行内除最后1个外其他单元格
                if (x < cellCountX - 1)
                {
                    //东南及相应单元格的反方向
                    cell.SetNeighbor(HexDirection.SE, cells[i - cellCountX + 1]);
                }
                else if (wrapping)
                {
                    cell.SetNeighbor(
                        HexDirection.SE, cells[i - cellCountX * 2 + 1]
                    );
                }
            }
        }

        Text label = Instantiate<Text>(cellLabelPrefab);
        label.rectTransform.anchoredPosition = new Vector2(position.x, position.z);
        cell.uiRect = label.rectTransform;
        cell.Elevation = 0;

        AddCellToChunk(x, z, cell);
    }


    /// <summary>
    /// 获得选择的单元格
    /// </summary>
    /// <param name="position">坐标</param>
    /// <returns>单元格的实例</returns>
    public HexCell GetCell(Vector3 position)
    {
        position = transform.InverseTransformPoint(position);
        HexCoordinates coordinates = HexCoordinates.FromPosition(position);
        //int index = coordinates.X + coordinates.Z * cellCountX + coordinates.Z / 2;
        //return cells[index];
        return GetCell(coordinates);
    }
    /// <summary>
    /// 获得选择的单元格
    /// </summary>
    /// <param name="xOffset">x偏移坐标</param>
    /// <param name="zOffset">z偏移坐标</param>
    /// <returns></returns>
    public HexCell GetCell(int xOffset, int zOffset)
    {
        return cells[xOffset + zOffset * cellCountX];
    }
    /// <summary>
    /// 获得选择的单元格
    /// </summary>
    /// <param name="cellIndex">单元格索引</param>
    /// <returns></returns>
    public HexCell GetCell(int cellIndex)
    {
        return cells[cellIndex];
    }

    void CreateCells()
    {
        cells = new HexCell[cellCountZ * cellCountX];

        for (int z = 0, i = 0; z < cellCountZ; z++)
        {
            for (int x = 0; x < cellCountX; x++)
            {
                CreateCell(x, z, i++);
            }
        }
    }

    void CreateChunks()
    {
        columns = new Transform[chunkCountX];
        for (int x = 0; x < chunkCountX; x++)
        {
            columns[x] = new GameObject("Column").transform;
            columns[x].SetParent(transform, false);
        }


        chunks = new HexGridChunk[chunkCountX * chunkCountZ];
        for (int z = 0, i = 0; z < chunkCountZ; z++)
        {
            for (int x = 0; x < chunkCountX; x++)
            {
                HexGridChunk chunk = chunks[i++] = Instantiate(chunkPrefab);
                chunk.transform.SetParent(columns[x], false);
            }
        }
    }

    void AddCellToChunk(int x, int z, HexCell cell)
    {
        int chunkX = x / HexMetrics.chunkSizeX;
        int chunkZ = z / HexMetrics.chunkSizeZ;
        HexGridChunk chunk = chunks[chunkX + chunkZ * chunkCountX];

        int localX = x - chunkX * HexMetrics.chunkSizeX;
        int localZ = z - chunkZ * HexMetrics.chunkSizeZ;
        chunk.AddCell(localX + localZ * HexMetrics.chunkSizeX, cell);
    }

    public HexCell GetCell(HexCoordinates coordinates)
    {
        int z = coordinates.Z;
        if (z < 0 || z > cellCountZ)
        {
            return null;
        }
        int x = coordinates.X + z / 2;
        if (x < 0 || x >= cellCountX)
        {
            return null;
        }
        return cells[x + z * cellCountX];
    }

    public void ShowUI(bool visible)
    {
        for (int i = 0; i < chunks.Length; i++)
        {
            chunks[i].ShowUI(visible);
        }
    }
    /// <summary>
    /// 创建地图
    /// </summary>
    public bool CreateMap(int x, int z, bool wrapping)
    {
        ClearPath();
        ClearUnits();
        if (
            x <= 0 || x % HexMetrics.chunkSizeX != 0 ||
            z <= 0 || z % HexMetrics.chunkSizeZ != 0
            )
        {
            Debug.LogError("Unsupported map size.");
            return false;
        }

        if (columns != null)
        {
            for (int i = 0; i < columns.Length; i++)
            {
                Destroy(columns[i].gameObject);
            }
        }

        cellCountX = x;
        cellCountZ = z;
        this.wrapping = wrapping;
        currentCenterColumnIndex = -1;
        HexMetrics.wrapSize = wrapping ? cellCountX : 0;

        chunkCountX = cellCountX / HexMetrics.chunkSizeX;
        chunkCountZ = cellCountZ / HexMetrics.chunkSizeZ;

        cellShaderData.Initialize(cellCountX, cellCountZ);

        CreateChunks();
        CreateCells();

        return true;
    }



    public void Save(BinaryWriter writer)
    {
        writer.Write(cellCountX);
        writer.Write(cellCountZ);
        writer.Write(wrapping);

        for (int i = 0; i < cells.Length; i++)
        {
            cells[i].Save(writer);
        }

        writer.Write(units.Count);
        for (int i = 0; i < units.Count; i++)
        {
            units[i].Save(writer);
        }
    }

    public void Load(BinaryReader reader, int header)
    {
        ClearPath();
        ClearUnits();
        StopAllCoroutines();
        int x = 20, z = 15;
        if (header >= 1)
        {
            x = reader.ReadInt32();
            z = reader.ReadInt32();
        }
        bool wrapping = reader.ReadBoolean();
        if (x != cellCountX || z != cellCountZ || this.wrapping != wrapping)
        {
            if (!CreateMap(x, z, wrapping))
            {
                return;
            }
        }

        bool originalImmediateMode = cellShaderData.ImmediateMode;
        cellShaderData.ImmediateMode = true;

        for (int i = 0; i < cells.Length; i++)
        {
            cells[i].Load(reader);
        }
        for (int i = 0; i < chunks.Length; i++)
        {
            chunks[i].Refresh();
        }

        if (header >= 2)
        {
            int unitCount = reader.ReadInt32();
            for (int i = 0; i < unitCount; i++)
            {
                HexUnit.Load(reader, this);
            }
        }

        cellShaderData.ImmediateMode = originalImmediateMode;
    }
    /// <summary>
    /// 寻找路径
    /// </summary>
    /// <param name="fromCell"></param>
    /// <param name="toCell"></param>
    /// <param name="speed"></param>
    public void FindPath(HexCell fromCell, HexCell toCell, HexUnit unit)
    {
        ClearPath();

        currentPathFrom = fromCell;
        currentPathTo = toCell;
        currentPathExists = Search(fromCell, toCell, unit);
        if (currentPathExists)
        {
            ShowPath(unit.Speed);
        }
    }
    /// <summary>
    /// 搜索
    /// </summary>
    /// <param name="fromCell"></param>
    /// <param name="toCell"></param>
    /// <param name="speed"></param>
    /// <returns></returns>
    bool Search(HexCell fromCell, HexCell toCell, HexUnit unit)
    {
        int speed = unit.Speed;
        searchFrontierPhase += 2;

        if (searchFrontier == null)
        {
            searchFrontier = new HexCellPriorityQueue();
        }
        else
        {
            searchFrontier.Clear();
        }

        fromCell.SearchPhase = searchFrontierPhase;
        fromCell.Distance = 0;
        searchFrontier.Enqueue(fromCell);

        while (searchFrontier.Count > 0)
        {
            HexCell current = searchFrontier.Dequeue();
            current.SearchPhase += 1;

            if (current == toCell)
            {
                return true;
            }

            int currentTurn = (current.Distance - 1) / speed;

            for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++)
            {
                HexCell neighbor = current.GetNeighbor(d);

                if (
                    neighbor == null ||
                    neighbor.SearchPhase > searchFrontierPhase
                    )
                {
                    continue;
                }
                //if (neighbor.IsUnderwater || neighbor.Unit)
                //{
                //    continue;
                //}

                //int moveCost;
                //HexEdgeType edgeType = current.GetEdgeType(neighbor);

                //if (edgeType == HexEdgeType.Cliff)
                //{
                //    continue;
                //}
                //if (current.HasRoadThroughEdge(d))
                //{
                //    moveCost = 1;
                //}
                //else if (current.Walled != neighbor.Walled)
                //{
                //    continue;
                //}
                //else
                //{
                //    moveCost = edgeType == HexEdgeType.Flat ? 5 : 10;
                //    moveCost += neighbor.UrbanLevel + neighbor.FarmLevel + neighbor.PlantLevel;
                //}

                if (!unit.IsValidDestination(neighbor))
                {
                    continue;
                }
                int moveCost = unit.GetMoveCost(current, neighbor, d);
                if (moveCost < 0)
                {
                    continue;
                }

                int distance = current.Distance + moveCost;
                int turn = (distance - 1) / speed;
                if (turn > currentTurn)
                {
                    distance = turn * speed + moveCost;
                }

                if (neighbor.SearchPhase < searchFrontierPhase)
                {
                    neighbor.SearchPhase = searchFrontierPhase;
                    neighbor.Distance = distance;
                    neighbor.PathFrom = current;
                    neighbor.SearchHeuristic = neighbor.coordinates.DistanceTo(toCell.coordinates);
                    searchFrontier.Enqueue(neighbor);
                }
                else if (distance < neighbor.Distance)
                {
                    int oldPriority = neighbor.SearchPriority;
                    neighbor.Distance = distance;
                    neighbor.PathFrom = current;
                    searchFrontier.Change(neighbor, oldPriority);
                }
            }
        }
        return false;
    }
    /// <summary>
    /// 路径标签
    /// </summary>
    /// <param name="speed"></param>
    void ShowPath(int speed)
    {
        if (currentPathExists)
        {
            HexCell current = currentPathTo;
            while (current != currentPathFrom)
            {
                int turn = (current.Distance - 1) / speed;
                current.SetLabel(turn.ToString());
                current.EnableHighlight(Color.white);
                current = current.PathFrom;
            }
        }
        currentPathFrom.EnableHighlight(Color.blue);
        currentPathTo.EnableHighlight(Color.black);
    }
    /// <summary>
    /// 清除路径
    /// </summary>
    public void ClearPath()
    {
        if (currentPathExists)
        {
            HexCell current = currentPathTo;
            while (current != currentPathFrom)
            {
                current.SetLabel(null);
                current.DisableHighlight();
                current = current.PathFrom;
            }
            current.DisableHighlight();
            currentPathExists = false;
        }
        currentPathFrom = currentPathTo = null;
    }
    /// <summary>
    /// 清理全部单位
    /// </summary>
    void ClearUnits()
    {
        for (int i = 0; i < units.Count; i++)
        {
            units.Clear();
        }
    }
    /// <summary>
    /// 向网格添加单位
    /// </summary>
    /// <param name="unit"></param>
    /// <param name="location"></param>
    /// <param name="orientation"></param>
    public void AddUnit(HexUnit unit, HexCell location, float orientation)
    {
        units.Add(unit);
        unit.Grid = this;
        //unit.transform.SetParent(transform, false);
        unit.Location = location;
        unit.Orientation = orientation;
    }
    /// <summary>
    /// 删除单位
    /// </summary>
    /// <param name="unit"></param>
    public void RemoveUnit(HexUnit unit)
    {
        units.Remove(unit);
        unit.Die();
    }
    /// <summary>
    /// 获得单元格
    /// </summary>
    /// <param name="ray"></param>
    /// <returns></returns>
    public HexCell GetCell(Ray ray)
    {
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit))
        {
            return GetCell(hit.point);
        }
        return null;
    }
    /// <summary>
    /// 获得路径
    /// </summary>
    /// <returns></returns>
    public List<HexCell> GetPath()
    {
        if (!currentPathExists)
        {
            return null;
        }
        List<HexCell> path = ListPool<HexCell>.Get();
        for (HexCell c = currentPathTo; c != currentPathFrom; c = c.PathFrom)
        {
            path.Add(c);
        }
        path.Add(currentPathFrom);
        path.Reverse();
        return path;
    }


    /// <summary>
    /// 查找视野
    /// </summary>
    /// <param name="fromCell"></param>
    /// <param name="range"></param>
    /// <returns></returns>
    List<HexCell> GetVisibleCells(HexCell fromCell, int range)
    {
        List<HexCell> visibleCells = ListPool<HexCell>.Get();

        searchFrontierPhase += 2;

        if (searchFrontier == null)
        {
            searchFrontier = new HexCellPriorityQueue();
        }
        else
        {
            searchFrontier.Clear();
        }

        range += fromCell.ViewElevation;
        fromCell.SearchPhase = searchFrontierPhase;
        fromCell.Distance = 0;
        searchFrontier.Enqueue(fromCell);

        HexCoordinates fromCoordinates = fromCell.coordinates;
        while (searchFrontier.Count > 0)
        {
            HexCell current = searchFrontier.Dequeue();
            current.SearchPhase += 1;
            visibleCells.Add(current);

            for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++)
            {
                HexCell neighbor = current.GetNeighbor(d);

                if (
                    neighbor == null ||
                    neighbor.SearchPhase > searchFrontierPhase ||
                    !neighbor.Explorable
                    )
                {
                    continue;
                }

                int distance = current.Distance + 1;
                if (distance + neighbor.ViewElevation > range ||
                    distance > fromCoordinates.DistanceTo(neighbor.coordinates)
                    )
                {
                    continue;
                }
                if (distance > range)
                {
                    continue;
                }

                if (neighbor.SearchPhase < searchFrontierPhase)
                {
                    neighbor.SearchPhase = searchFrontierPhase;
                    neighbor.Distance = distance;
                    neighbor.SearchHeuristic = 0;
                    searchFrontier.Enqueue(neighbor);
                }
                else if (distance < neighbor.Distance)
                {
                    int oldPriority = neighbor.SearchPriority;
                    neighbor.Distance = distance;
                    searchFrontier.Change(neighbor, oldPriority);
                }
            }
        }
        return visibleCells;
    }
    /// <summary>
    /// 增加可见性
    /// </summary>
    /// <param name="fromCell"></param>
    /// <param name="range"></param>
    public void IncreaseVisibility(HexCell fromCell, int range)
    {
        List<HexCell> cells = GetVisibleCells(fromCell, range);
        for (int i = 0; i < cells.Count; i++)
        {
            cells[i].IncreaseVisibility();
        }
        ListPool<HexCell>.Add(cells);
    }
    /// <summary>
    /// 减少可见性
    /// </summary>
    /// <param name="fromCell"></param>
    /// <param name="range"></param>
    public void DecreaseVisibility(HexCell fromCell, int range)
    {
        List<HexCell> cells = GetVisibleCells(fromCell, range);
        for (int i = 0; i < cells.Count; i++)
        {
            cells[i].DecreaseVisibility();
        }
        ListPool<HexCell>.Add(cells);
    }

    public void ResetVisibility()
    {
        for (int i = 0; i < cells.Length; i++)
        {
            cells[i].ResetVisibility();
        }
        for (int i = 0; i < units.Count; i++)
        {
            HexUnit unit = units[i];
            IncreaseVisibility(unit.Location, unit.VisionRange);
        }
    }
    /// <summary>
    /// 地图中心
    /// </summary>
    /// <param name="xPosition"></param>
    public void CenterMap(float xPosition)
    {
        int centerColumnIndex = (int)
            (xPosition / (HexMetrics.innerDiameter * HexMetrics.chunkSizeX));
        if (centerColumnIndex == currentCenterColumnIndex)
        {
            return;
        }
        currentCenterColumnIndex = centerColumnIndex;

        int minColumnIndex = centerColumnIndex - chunkCountX / 2;
        int maxColumnIndex = centerColumnIndex + chunkCountX / 2;

        Vector3 position;
        position.y = position.z = 0f;
        for (int i = 0; i < columns.Length; i++)
        {
            if (i < minColumnIndex)
            {
                position.x = chunkCountX * (HexMetrics.innerDiameter * HexMetrics.chunkSizeX);
            }
            else if (i > maxColumnIndex)
            {
                position.x = chunkCountX * -(HexMetrics.innerDiameter * HexMetrics.chunkSizeX);
            }
            else
            {
                position.x = 0f;
            }
            columns[i].localPosition = position;
        }
    }

    public void MakeChildOfColumn(Transform child, int columnIndex)
    {
        child.SetParent(columns[columnIndex], false);
    }
}
