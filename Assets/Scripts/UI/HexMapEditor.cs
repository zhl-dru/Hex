using UnityEngine;
using UnityEngine.EventSystems;
using System.IO;


/// <summary>
/// 地图编辑器
/// </summary>
public class HexMapEditor : MonoBehaviour
{
    /// <summary>
    /// 地形类型索引
    /// </summary>
    int activeTerrainTypeIndex;
    //public Color[] colors;
    public HexGrid hexGrid;
    //private Color activeColor;
    /// <summary>
    /// 有效高度
    /// </summary>
    int activeElevation;
    /// <summary>
    /// 有效水平面
    /// </summary>
    int activeWaterLevel;
    //bool applyColor;
    /// <summary>
    /// 高度是否应用到单元
    /// </summary>
    bool applyElevation = true;
    /// <summary>
    /// 水平面是否应用到单元
    /// </summary>
    bool applyWaterLevel = true;
    /// <summary>
    /// 特征密度是否应用到单元
    /// </summary>
    bool applyUrbanLevel, applyFarmLevel, applyPlantLevel, applySpecialIndex;
    /// <summary>
    /// 拖拽是否有效
    /// </summary>
    bool isDrag;
    /// <summary>
    /// 拖拽方向
    /// </summary>
    HexDirection dragDirection;
    /// <summary>
    /// 拖拽前的单元
    /// </summary>
    HexCell previousCell;
    /// <summary>
    /// 刷子大小
    /// </summary>
    int brushSize;
    /// <summary>
    /// 编辑模式
    /// </summary>
    enum OptionalToggle
    {
        Ignore, Yes, No
    }
    /// <summary>
    /// 编辑模式
    /// </summary>
    OptionalToggle riverMode, roadMode, walledMode;
    /// <summary>
    /// 特征密度
    /// </summary>
    int activeUrbanLevel, activeFarmLevel, activePlantLevel, activeSpecialIndex;
    /// <summary>
    /// Terrain材质引用
    /// </summary>
    public Material terrainMaterial;
    /// <summary>
    /// 是否处于编辑模式
    /// </summary>
    //bool editMode;
    /// <summary>
    /// 单位
    /// </summary>
    //public HexUnit unitPrefab;




    private void Awake()
    {
        terrainMaterial.DisableKeyword("GRID_ON");
        Shader.EnableKeyword("HEX_MAP_EDIT_MODE");
        SetEditMode(true);
    }

    private void Update()
    {
        //if (
        //    Input.GetMouseButton(0) &&
        //    !EventSystem.current.IsPointerOverGameObject()
        //    )
        //{
        //    HandleInput();
        //}
        //else
        //{
        //    previousCell = null;
        //}
        if (!EventSystem.current.IsPointerOverGameObject())
        {
            if (Input.GetMouseButton(0))
            {
                HandleInput();
                return;
            }
            if (Input.GetKeyDown(KeyCode.U))
            {
                if (Input.GetKey(KeyCode.LeftShift))
                {
                    DestroyUnit();
                }
                else
                {
                    CreateUnit();
                }
                return;
            }
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Application.Quit();
            }
        }
    }
    /// <summary>
    /// 鼠标输入
    /// </summary>
    void HandleInput()
    {
        //Ray inputRay = Camera.main.ScreenPointToRay(Input.mousePosition);
        //RaycastHit hit;
        //if (Physics.Raycast(inputRay, out hit))
        HexCell currentCell = GetCellUnderCursor();
        if (currentCell)
        {
            //HexCell currentCell = hexGrid.GetCell(hit.point);

            if (previousCell && previousCell != currentCell)
            {
                ValidateDrag(currentCell);
            }
            else
            {
                isDrag = false;
            }

            //if (editMode)
            
            EditCells(currentCell);
            
            //else if (Input.GetKey(KeyCode.LeftShift) && searchToCell != currentCell)
            //{
            //    if (searchFromCell != currentCell)
            //    {
            //        if (searchFromCell)
            //        {
            //            searchFromCell.DisableHighlight();
            //        }
            //        searchFromCell = currentCell;
            //        searchFromCell.EnableHighlight(Color.blue);
            //        if (searchToCell)
            //        {
            //            hexGrid.FindPath(searchFromCell, searchToCell, 24);
            //        }
            //    }
            //}
            //else if (searchFromCell && searchFromCell != currentCell)
            //{
            //    if (searchFromCell != currentCell)
            //    {
            //        searchToCell = currentCell;
            //        hexGrid.FindPath(searchFromCell, searchToCell, 24);
            //    }
            //}
            previousCell = currentCell;
            //isDrag = true;
        }
        else
        {
            previousCell = null;
        }
    }


    



    /// <summary>
    /// 设置高度
    /// </summary>
    /// <param name="elevation"></param>
    public void SetElevation(float elevation)
    {
        activeElevation = (int)elevation;
    }
    /// <summary>
    /// 设置水平面
    /// </summary>
    /// <param name="level"></param>
    public void SetWaterLevel(float level)
    {
        activeWaterLevel = (int)level;
    }
    /// <summary>
    /// 设置城市特征密度
    /// </summary>
    /// <param name="level"></param>
    public void SetUrbanLevel(float level)
    {
        activeUrbanLevel = (int)level;
    }
    /// <summary>
    /// 设置农田特征密度
    /// </summary>
    /// <param name="level"></param>
    public void SetFarmLevel(float level)
    {
        activeFarmLevel = (int)level;
    }
    /// <summary>
    /// 设置植物特征密度
    /// </summary>
    /// <param name="level"></param>
    public void SetPlantLevel(float level)
    {
        activePlantLevel = (int)level;
    }
    /// <summary>
    /// 设置特殊地形的索引
    /// </summary>
    /// <param name="index"></param>
    public void SetSpecialIndex(float index)
    {
        activeSpecialIndex = (int)index;
    }
    /// <summary>
    /// 设置河流模式
    /// </summary>
    /// <param name="mode"></param>
    public void SetRiverMode(int mode)
    {
        riverMode = (OptionalToggle)mode;
    }
    /// <summary>
    /// 设置道路模式
    /// </summary>
    /// <param name="mode"></param>
    public void SetRoadMode(int mode)
    {
        roadMode = (OptionalToggle)mode;
    }
    /// <summary>
    /// 设置围墙模式
    /// </summary>
    /// <param name="mode"></param>
    public void SetWalledMode(int mode)
    {
        walledMode = (OptionalToggle)mode;
    }

    /// <summary>
    /// 编辑单元
    /// </summary>
    /// <param name="cell"></param>
    void EditCell(HexCell cell)
    {
        if (cell)
        {
            //if (applyColor)
            //{
            //    cell.Color = activeColor;
            //}
            if (activeTerrainTypeIndex >= 0)
            {
                cell.TerrainTypeIndex = activeTerrainTypeIndex;
            }
            if (applyElevation)
            {
                cell.Elevation = activeElevation;
            }
            if (applyWaterLevel)
            {
                cell.WaterLevel = activeWaterLevel;
            }
            if (applySpecialIndex)
            {
                cell.SpecialIndex = activeSpecialIndex;
            }
            if (applyUrbanLevel)
            {
                cell.UrbanLevel = activeUrbanLevel;
            }
            if (applyFarmLevel)
            {
                cell.FarmLevel = activeFarmLevel;
            }
            if (applyPlantLevel)
            {
                cell.PlantLevel = activePlantLevel;
            }
            if (riverMode == OptionalToggle.No)
            {
                cell.RemoveRiver();
            }
            if (roadMode == OptionalToggle.No)
            {
                cell.RemoveRoads();
            }
            if (walledMode != OptionalToggle.Ignore)
            {
                cell.Walled = walledMode == OptionalToggle.Yes;
            }
            if (isDrag)
            {
                HexCell otherCell = cell.GetNeighbor(dragDirection.Opposite());
                if (otherCell)
                {
                    if (riverMode == OptionalToggle.Yes)
                    {
                        otherCell.SetOutgoingRiver(dragDirection);
                    }
                    if (roadMode == OptionalToggle.Yes)
                    {
                        otherCell.AddRoads(dragDirection);
                    }
                }
            }
        }
    }

    /// <summary>
    /// 设置是否应用高度
    /// </summary>
    /// <param name="toggle"></param>
    public void SetApplyElevation(bool toggle)
    {
        applyElevation = toggle;
    }
    /// <summary>
    /// 设置是否应用水平面
    /// </summary>
    /// <param name="toggle"></param>
    public void SetApplyWaterLevel(bool toggle)
    {
        applyWaterLevel = toggle;
    }
    /// <summary>
    /// 设置是否应用城市特征密度
    /// </summary>
    /// <param name="toggle"></param>
    public void SetApplyUrbanLevel(bool toggle)
    {
        applyUrbanLevel = toggle;
    }
    /// <summary>
    /// 设置是否应用农田特征密度
    /// </summary>
    /// <param name="toggle"></param>
    public void SetApplyFarmLevel(bool toggle)
    {
        applyFarmLevel = toggle;
    }
    /// <summary>
    /// 设置是否应用植物特征密度
    /// </summary>
    /// <param name="toggle"></param>
    public void SetApplyPlantLevel(bool toggle)
    {
        applyPlantLevel = toggle;
    }
    /// <summary>
    /// 设置是否应用特殊地形特征
    /// </summary>
    /// <param name="toggle"></param>
    public void SetApplySpecialIndex(bool toggle)
    {
        applySpecialIndex = toggle;
    }
    /// <summary>
    /// 设置笔刷大小
    /// </summary>
    /// <param name="size"></param>
    public void SetBrushSize(float size)
    {
        brushSize = (int)size;
    }

    /// <summary>
    /// 编辑单元
    /// </summary>
    /// <param name="center"></param>
    void EditCells(HexCell center)
    {
        int centerX = center.coordinates.X;
        int centerZ = center.coordinates.Z;

        for (int r = 0, z = centerZ - brushSize; z <= centerZ; z++, r++)
        {
            for (int x = centerX - r; x <= centerX + brushSize; x++)
            {
                EditCell(hexGrid.GetCell(new HexCoordinates(x, z)));
            }
        }
        for (int r = 0, z = centerZ + brushSize; z > centerZ; z--, r++)
        {
            for (int x = centerX - brushSize; x <= centerX + r; x++)
            {
                EditCell(hexGrid.GetCell(new HexCoordinates(x, z)));
            }
        }
    }
    /// <summary>
    /// 标签显示
    /// </summary>
    /// <param name="visible"></param>
    //public void ShowUI(bool visible)
    //{
    //    hexGrid.ShowUI(visible);
    //}
    /// <summary>
    /// 检测拖拽
    /// </summary>
    /// <param name="currentCell"></param>
    void ValidateDrag(HexCell currentCell)
    {
        for (
            dragDirection = HexDirection.NE;
            dragDirection <= HexDirection.NW;
            dragDirection++)
        {
            if (previousCell.GetNeighbor(dragDirection) == currentCell)
            {
                isDrag = true;
                return;
            }
        }
        isDrag = false;
    }
    /// <summary>
    /// 控制当前激活的地形类型索引
    /// </summary>
    /// <param name="index"></param>
    public void SetTerrainTypeIndex(int index)
    {
        activeTerrainTypeIndex = index;
    }
    /// <summary>
    /// 设置GRID_ON
    /// </summary>
    /// <param name="visible"></param>
    public void ShowGrid(bool visible)
    {
        if (visible)
        {
            terrainMaterial.EnableKeyword("GRID_ON");
        }
        else
        {
            terrainMaterial.DisableKeyword("GRID_ON");
        }
    }

    public void SetEditMode(bool toggle)
    {
        //editMode = toggle;

        //hexGrid.ShowUI(!toggle);
        enabled = toggle;
    }

    /// <summary>
    /// 获得点击单元格
    /// </summary>
    /// <returns></returns>
    HexCell GetCellUnderCursor()
    {
        //Ray inputRay = Camera.main.ScreenPointToRay(Input.mousePosition);
        //RaycastHit hit;
        //if (Physics.Raycast(inputRay, out hit))
        //{
        //    return hexGrid.GetCell(hit.point);
        //}
        //return null;
        return hexGrid.GetCell(Camera.main.ScreenPointToRay(Input.mousePosition));
    }
    /// <summary>
    /// 添加单位
    /// </summary>
    void CreateUnit()
    {
        HexCell cell = GetCellUnderCursor();
        if (cell && !cell.Unit)
        {
            //HexUnit unit = Instantiate(unitPrefab);
            //Instantiate(unitPrefab);
            //unit.transform.SetParent(hexGrid.transform, false);
            //unit.Location = cell;
            //unit.Orientation = Random.Range(0f, 360f);
            hexGrid.AddUnit(Instantiate(HexUnit.unitPrefab), cell, Random.Range(0f, 360f));
        }
    }
    /// <summary>
    /// 删除单位
    /// </summary>
    void DestroyUnit()
    {
        HexCell cell = GetCellUnderCursor();
        if (cell && cell.Unit)
        {
            //Destroy(cell.Unit.gameObject);
            //cell.Unit.Die();
            hexGrid.RemoveUnit(cell.Unit);
        }
    }

}