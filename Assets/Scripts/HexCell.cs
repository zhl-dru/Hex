using UnityEngine;
using System.IO;
using UnityEngine.UI;

/// <summary>
/// 单元
/// </summary>
public class HexCell : MonoBehaviour
{
    public HexCoordinates coordinates;
    //private Color color;
    /// <summary>
    /// 地形索引
    /// </summary>
    int terrainTypeIndex;
    /// <summary>
    /// 存储单元格的高度级别
    /// </summary>
    public int elevation = int.MinValue;
    /// <summary>
    /// UI标签的引用
    /// </summary>
    public RectTransform uiRect;
    /// <summary>
    /// 所在网格块的引用
    /// </summary>
    public HexGridChunk chunk;
    /// <summary>
    /// 存储单元格的邻居,每个单元格都维护与自己相邻的所有单元格的信息
    /// </summary>
    [SerializeField]
    HexCell[] neighbors;
    /// <summary>
    /// 是否拥有道路
    /// </summary>
    [SerializeField]
    bool[] roads;
    /// <summary>
    /// 是否包含流入/流出的河流
    /// </summary>
    private bool hasIncomingRiver, hasOutgoingRiver;
    /// <summary>
    /// 河流的方向
    /// </summary>
    HexDirection incomingRiver, outgoingRiver;
    /// <summary>
    /// 水平面
    /// </summary>
    int waterLevel;
    /// <summary>
    /// 特征等级
    /// </summary>
    int urbanLevel, farmLevel, plantLevel;
    /// <summary>
    /// 是否被围墙围住
    /// </summary>
    bool walled;
    /// <summary>
    /// 特殊地形索引
    /// </summary>
    int specialIndex;
    /// <summary>
    /// 与选中单元格间的距离
    /// </summary>
    int distance;
    /// <summary>
    /// 可见性
    /// </summary>
    int visibility;
    /// <summary>
    /// 可探索性
    /// </summary>
    bool explored;

    /// <summary>
    /// 单元格索引
    /// </summary>
    public int Index { get; set; }
    /// <summary>
    /// 单元格着色器数据
    /// </summary>
    public HexCellShaderData ShaderData { get; set; }
    /// <summary>
    /// 路径
    /// </summary>
    public HexCell PathFrom { get; set; }
    /// <summary>
    /// 估计路径值
    /// </summary>
    public int SearchHeuristic { get; set; }
    /// <summary>
    /// 搜索优先级
    /// </summary>
    public int SearchPriority
    {
        get { return distance + SearchHeuristic; }
    }

    public HexCell NextWithSamePriority { get; set; }
    /// <summary>
    /// 单元搜索阶段
    /// </summary>
    public int SearchPhase { get; set; }
    /// <summary>
    /// 单位
    /// </summary>
    public HexUnit Unit { get; set; }
    /// <summary>
    /// 是否被探索
    /// </summary>
    public bool IsExplored { get { return explored && Explorable; } private set { explored = value; } }
    /// <summary>
    /// 是否可探索
    /// </summary>
    public bool Explorable { get; set; }
    /// <summary>
    /// 列索引
    /// </summary>
    public int ColumnIndex { get; set; }


    /// <summary>
    /// 地形索引
    /// </summary>
    public int TerrainTypeIndex
    {
        get { return terrainTypeIndex; }
        set
        {
            if (terrainTypeIndex != value)
            {
                terrainTypeIndex = value;
                //Refresh();
                ShaderData.RefreshTerrain(this);
            }
        }
    }
    /// <summary>
    /// 高度
    /// </summary>
    public int Elevation
    {
        get
        {
            return elevation;
        }
        set
        {
            if (elevation == value)
            {
                return;
            }

            int originalViewElevation = ViewElevation;
            elevation = value;
            if (ViewElevation != originalViewElevation)
            {
                ShaderData.ViewElevationChanged();
            }

            RefreshPosition();

            //当高度变化导致河流不合法时删除河流

            ValidateRivers();

            //当高度变化导致道路不合法时删除道路
            for (int i = 0; i < roads.Length; i++)
            {
                if (roads[i] && GetElevationDifference((HexDirection)i) > 1)
                {
                    SetRoad(i, false);
                }
            }

            Refresh();
        }
    }
    /// <summary>
    /// 视野高度
    /// </summary>
    public int ViewElevation
    {
        get
        {
            return elevation >= waterLevel ? elevation : waterLevel;
        }
    }
    /// <summary>
    /// 自身位置
    /// </summary>
    public Vector3 Position
    {
        get { return transform.localPosition; }
    }
    /// <summary>
    /// 水平面
    /// </summary>
    public int WaterLevel
    {
        get { return waterLevel; }
        set
        {
            if (waterLevel == value)
            {
                return;
            }
            int originalViewElevation = ViewElevation;
            waterLevel = value;
            if (ViewElevation != originalViewElevation)
            {
                ShaderData.ViewElevationChanged();
            }
            ValidateRivers();
            Refresh();
        }
    }
    /// <summary>
    /// 城市特征等级
    /// </summary>
    public int UrbanLevel
    {
        get { return urbanLevel; }
        set
        {
            if (urbanLevel != value)
            {
                urbanLevel = value;
                RefreshSelfOnly();
            }
        }
    }
    /// <summary>
    /// 农田特征等级
    /// </summary>
    public int FarmLevel
    {
        get { return farmLevel; }
        set
        {
            if (farmLevel != value)
            {
                farmLevel = value;
                RefreshSelfOnly();
            }
        }
    }
    /// <summary>
    /// 植物特征等级
    /// </summary>
    public int PlantLevel
    {
        get { return plantLevel; }
        set
        {
            if (plantLevel != value)
            {
                plantLevel = value;
                RefreshSelfOnly();
            }
        }
    }
    /// <summary>
    /// 特殊地形特征
    /// </summary>
    public int SpecialIndex
    {
        get { return specialIndex; }
        set
        {
            if (specialIndex != value && !HasRiver)
            {
                specialIndex = value;

                //加入特殊地形时删除单元内的道路
                RemoveRoads();
                RefreshSelfOnly();
            }
        }
    }
    /// <summary>
    /// 与选中单元的距离
    /// </summary>
    public int Distance
    {
        get { return distance; }
        set
        {
            distance = value;
            //UpdateDistanceLabel();
        }
    }

    /// <summary>
    /// 是否有特殊地形特征
    /// </summary>
    public bool IsSpecial
    {
        get { return specialIndex > 0; }
    }

    /// <summary>
    /// 是否被围墙围住
    /// </summary>
    public bool Walled
    {
        get { return walled; }
        set
        {
            if (walled != value)
            {
                walled = value;
                Refresh();
            }
        }
    }
    /// <summary>
    /// 是否有流入河流
    /// </summary>
    public bool HasIncomingRiver
    {
        get { return hasIncomingRiver; }
    }
    /// <summary>
    /// 是否有流出河流
    /// </summary>
    public bool HasOutgoingRiver
    {
        get { return hasOutgoingRiver; }
    }
    /// <summary>
    /// 流入河流方向
    /// </summary>
    public HexDirection IncomingRiver
    {
        get { return incomingRiver; }
    }
    /// <summary>
    /// 流出河流方向
    /// </summary>
    public HexDirection OutgoingRiver
    {
        get { return outgoingRiver; }
    }
    /// <summary>
    /// 单元中是否至少有一条路
    /// </summary>
    public bool HasRoads
    {
        get
        {
            for (int i = 0; i < roads.Length; i++)
            {
                if (roads[i])
                {
                    return true;
                }
            }
            return false;
        }
    }
    /// <summary>
    /// 单元格是否有河流流过
    /// </summary>
    public bool HasRiver
    {
        get { return hasIncomingRiver || hasOutgoingRiver; }
    }
    /// <summary>
    /// 单元格是否拥有河流的首端或尾端
    /// </summary>
    public bool HasRiverBeginOrEnd
    {
        get { return hasIncomingRiver != hasOutgoingRiver; }
    }
    /// <summary>
    /// 单元在输入方向上是否有一条路
    /// </summary>
    /// <param name="direction"></param>
    /// <returns></returns>
    public bool HasRoadThroughEdge(HexDirection direction)
    {
        return roads[(int)direction];
    }

    public bool IsVisible
    {
        get { return visibility > 0 && Explorable; }
    }


    /// <summary>
    /// 获得河床的垂直高度
    /// </summary>
    public float StreamBedY
    {
        get
        {
            return
                (elevation + HexMetrics.streamBedElevationOffset) *
                HexMetrics.elevationStep;
        }
    }
    /// <summary>
    /// 获得河流表面的垂直位置
    /// </summary>
    public float RiverSurfaceY
    {
        get
        {
            return
                (elevation + HexMetrics.waterElevationOffset) *
                HexMetrics.elevationStep;
        }
    }
    /// <summary>
    /// 获得水体单元表面的垂直位置
    /// </summary>
    public float WaterSurfaceY
    {
        get
        {
            return
                (waterLevel + HexMetrics.waterElevationOffset) *
                HexMetrics.elevationStep;
        }
    }
    /// <summary>
    /// 获得河流流入或流出的方向
    /// </summary>
    public HexDirection RiverBeginOrEndDirection
    {
        get
        {
            return hasIncomingRiver ? incomingRiver : outgoingRiver;
        }
    }


    /// <summary>
    /// 河流是否流经指定的边
    /// </summary>
    /// <param name="direction"></param>
    /// <returns></returns>
    public bool HasRiverThroughEdge(HexDirection direction)
    {
        return
            hasIncomingRiver && incomingRiver == direction ||
            hasOutgoingRiver && outgoingRiver == direction;
    }
    /// <summary>
    /// 单元是否在水下
    /// </summary>
    public bool IsUnderwater
    {
        get
        {
            return waterLevel > elevation;
        }
    }

    /// <summary>
    /// 获取单元格的指定邻居
    /// </summary>
    /// <param name="direction">方向</param>
    /// <returns></returns>
    public HexCell GetNeighbor(HexDirection direction)
    {
        return neighbors[(int)direction];
    }

    /// <summary>
    /// 设置单元格的邻居
    /// </summary>
    /// <param name="direction">方向</param>
    /// <param name="cell">单元格的实例</param>
    public void SetNeighbor(HexDirection direction, HexCell cell)
    {
        neighbors[(int)direction] = cell;
        cell.neighbors[(int)direction.Opposite()] = this;
    }

    /// <summary>
    /// 获取指定方向的边缘类型
    /// </summary>
    /// <param name="direction">方向</param>
    /// <returns></returns>
    public HexEdgeType GetEdgeType(HexDirection direction)
    {
        return HexMetrics.GetEdgeType(elevation, neighbors[(int)direction].elevation);
    }
    /// <summary>
    /// 获得与指定单元格间的边缘类型
    /// </summary>
    /// <param name="otherCell"></param>
    /// <returns></returns>
    public HexEdgeType GetEdgeType(HexCell otherCell)
    {
        return HexMetrics.GetEdgeType(elevation, otherCell.elevation);
    }

    void Refresh()
    {
        if (chunk)
        {
            chunk.Refresh();
            for (int i = 0; i < neighbors.Length; i++)
            {
                HexCell neighbor = neighbors[i];
                if (neighbor != null && neighbor.chunk != chunk)
                {
                    neighbor.chunk.Refresh();
                }
            }
            if (Unit)
            {
                Unit.ValidateLocation();
            }
        }
    }
    /// <summary>
    /// 获得指定方向上的高度差
    /// </summary>
    /// <param name="direction"></param>
    /// <returns></returns>
    public int GetElevationDifference(HexDirection direction)
    {
        int difference = elevation - GetNeighbor(direction).elevation;
        return difference >= 0 ? difference : -difference;
    }

    /// <summary>
    /// 设置流出河流
    /// </summary>
    /// <param name="direction"></param>
    public void SetOutgoingRiver(HexDirection direction)
    {
        if (hasOutgoingRiver && outgoingRiver == direction)
        {
            return;
        }
        //指定方向必须有邻居并且高度不高于当前单元
        HexCell neighbor = GetNeighbor(direction);
        if (!IsValidRiverDestination(neighbor))
        {
            return;
        }

        RemoveOutgoingRiver();
        if (hasIncomingRiver && incomingRiver == direction)
        {
            RemoveIncomingRiver();
        }

        hasOutgoingRiver = true;
        outgoingRiver = direction;

        //添加河流时删除单元内的特殊地形
        specialIndex = 0;

        //设置相应方向邻居的流入河流
        neighbor.RemoveIncomingRiver();
        neighbor.hasIncomingRiver = true;
        neighbor.incomingRiver = direction.Opposite();

        //设置河流时删除覆盖道路
        SetRoad((int)direction, false);
    }
    /// <summary>
    /// 设置道路
    /// </summary>
    /// <param name="index"></param>
    /// <param name="state"></param>
    void SetRoad(int index, bool state)
    {
        roads[index] = state;
        //禁用邻居的相应道路
        neighbors[index].roads[(int)((HexDirection)index).Opposite()] = state;
        neighbors[index].RefreshSelfOnly();
        RefreshSelfOnly();
    }
    /// <summary>
    /// 删除河流
    /// </summary>
    public void RemoveRiver()
    {
        RemoveOutgoingRiver();
        RemoveIncomingRiver();
    }
    /// <summary>
    /// 删除河流流出部分
    /// </summary>
    public void RemoveOutgoingRiver()
    {
        if (!hasOutgoingRiver)
        {
            return;
        }
        hasOutgoingRiver = false;
        RefreshSelfOnly();

        //删除邻居单元的流入
        HexCell neighbor = GetNeighbor(outgoingRiver);
        neighbor.hasIncomingRiver = false;
        neighbor.RefreshSelfOnly();
    }
    /// <summary>
    /// 删除河流流入部分
    /// </summary>
    public void RemoveIncomingRiver()
    {
        if (!hasIncomingRiver)
        {
            return;
        }
        hasIncomingRiver = false;
        RefreshSelfOnly();

        //删除邻居单元的流出
        HexCell neighbor = GetNeighbor(incomingRiver);
        neighbor.hasOutgoingRiver = false;
        neighbor.RefreshSelfOnly();
    }
    /// <summary>
    /// 删除道路
    /// </summary>
    public void RemoveRoads()
    {
        for (int i = 0; i < neighbors.Length; i++)
        {
            if (roads[i])
            {
                SetRoad(i, false);
            }
        }
    }
    /// <summary>
    /// 添加道路
    /// </summary>
    /// <param name="direction"></param>
    public void AddRoads(HexDirection direction)
    {
        if (
            !roads[(int)direction] &&
            !HasRiverThroughEdge(direction) &&
            !IsSpecial && !GetNeighbor(direction).IsSpecial &&
            GetElevationDifference(direction) <= 1
            )
        {
            SetRoad((int)direction, true);
        }
    }
    /// <summary>
    /// 输入邻居是否是一条流出河流的有效目的地
    /// </summary>
    /// <param name="neighbor"></param>
    /// <returns></returns>
    bool IsValidRiverDestination(HexCell neighbor)
    {
        return neighbor && (
            elevation >= neighbor.elevation || waterLevel == neighbor.elevation
            );
    }
    /// <summary>
    /// 验证河流有效性
    /// </summary>
    void ValidateRivers()
    {
        if (hasOutgoingRiver && !IsValidRiverDestination(GetNeighbor(outgoingRiver)))
        {
            RemoveOutgoingRiver();
        }
        if (hasIncomingRiver && !GetNeighbor(incomingRiver).IsValidRiverDestination(this))
        {
            RemoveIncomingRiver();
        }
    }

    /// <summary>
    /// 设置字符串
    /// </summary>
    /// <param name="text"></param>
    public void SetLabel(string text)
    {
        Text label = uiRect.GetComponent<Text>();
        label.text = text;
    }


    /// <summary>
    /// 仅刷新自身
    /// </summary>
    void RefreshSelfOnly()
    {
        chunk.Refresh();
        if (Unit)
        {
            Unit.ValidateLocation();
        }
    }
    /// <summary>
    /// 刷新位置
    /// </summary>
    void RefreshPosition()
    {
        Vector3 position = transform.localPosition;
        position.y = elevation * HexMetrics.elevationStep;
        //应用高度微扰
        position.y += (HexMetrics.SampleNoise(position).y * 2f - 1f) * HexMetrics.elevationPerturbStrength;
        transform.localPosition = position;

        Vector3 uiPosition = uiRect.localPosition;
        uiPosition.z = -position.y;
        uiRect.localPosition = uiPosition;
    }


    /// <summary>
    /// 保存单元
    /// </summary>
    /// <param name="writer"></param>
    public void Save(BinaryWriter writer)
    {
        writer.Write((byte)terrainTypeIndex);
        writer.Write((byte)elevation + 127);
        writer.Write((byte)waterLevel);
        writer.Write((byte)urbanLevel);
        writer.Write((byte)farmLevel);
        writer.Write((byte)plantLevel);
        writer.Write((byte)specialIndex);
        writer.Write(walled);

        if (hasIncomingRiver)
        {
            writer.Write((byte)(IncomingRiver + 128));
        }
        else
        {
            writer.Write((byte)0);
        }
        if (hasOutgoingRiver)
        {
            writer.Write((byte)(outgoingRiver + 128));
        }
        else
        {
            writer.Write((byte)0);
        }

        int roadFlags = 0;
        for (int i = 0; i < roads.Length; i++)
        {
            if (roads[i])
            {
                roadFlags |= 1 << i;
            }
        }
        writer.Write((byte)roadFlags);
        writer.Write(IsExplored);
    }
    /// <summary>
    /// 读取单元
    /// </summary>
    /// <param name="reader"></param>
    public void Load(BinaryReader reader)
    {
        terrainTypeIndex = reader.ReadByte();
        ShaderData.RefreshTerrain(this);
        elevation = reader.ReadByte();
        elevation -= 127;
        RefreshPosition();
        waterLevel = reader.ReadByte();
        urbanLevel = reader.ReadByte();
        farmLevel = reader.ReadByte();
        plantLevel = reader.ReadByte();
        specialIndex = reader.ReadByte();
        walled = reader.ReadBoolean();

        byte riverData = reader.ReadByte();
        if (riverData >= 128)
        {
            hasIncomingRiver = true;
            incomingRiver = (HexDirection)(riverData - 128);
        }
        else
        {
            hasIncomingRiver = false;
        }
        riverData = reader.ReadByte();
        if (riverData >= 128)
        {
            hasOutgoingRiver = true;
            outgoingRiver = (HexDirection)(riverData - 128);
        }
        else
        {
            hasOutgoingRiver = false;
        }

        int roadFlags = reader.ReadByte();
        for (int i = 0; i < roads.Length; i++)
        {
            roads[i] = (roadFlags & (1 << i)) != 0;
        }

        IsExplored = reader.ReadBoolean();
        ShaderData.RefreshVisibility(this);
    }


    /// <summary>
    /// 关闭轮廓高亮
    /// </summary>
    public void DisableHighlight()
    {
        Image highlight = uiRect.GetChild(0).GetComponent<Image>();
        highlight.enabled = false;
    }
    /// <summary>
    /// 启用轮廓高亮
    /// </summary>
    public void EnableHighlight()
    {
        Image highlight = uiRect.GetChild(0).GetComponent<Image>();
        highlight.enabled = true;
    }
    /// <summary>
    /// 轮廓高亮颜色
    /// </summary>
    /// <param name="color"></param>
    public void EnableHighlight(Color color)
    {
        Image highlight = uiRect.GetChild(0).GetComponent<Image>();
        highlight.color = color;
        highlight.enabled = true;
    }

    /// <summary>
    /// 增加可见性
    /// </summary>
    public void IncreaseVisibility()
    {
        visibility += 1;
        if (visibility == 1)
        {
            IsExplored = true;
            ShaderData.RefreshVisibility(this);
        }
    }
    /// <summary>
    /// 减少可见性
    /// </summary>
    public void DecreaseVisibility()
    {
        visibility -= 1;
        if (visibility == 0)
        {
            ShaderData.RefreshVisibility(this);
        }
    }

    public void ResetVisibility()
    {
        if (visibility > 0)
        {
            visibility = 0;
            ShaderData.RefreshVisibility(this);
        }
    }

    public void SetMapData(float data)
    {
        ShaderData.SetMapData(this, data);
    }

}
