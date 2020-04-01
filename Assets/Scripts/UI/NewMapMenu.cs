using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NewMapMenu : MonoBehaviour
{
    public HexGrid hexGrid;
    public HexMapGenerator mapGenerator;
    /// <summary>
    /// 是否使用地图生成
    /// </summary>
    bool generateMaps = true;
    /// <summary>
    /// 是否使用包装
    /// </summary>
    bool wrapping = true;


    public void Open()
    {
        gameObject.SetActive(true);
        HexMapCamera.Locked = true;
    }

    public void Close()
    {
        gameObject.SetActive(false);
        HexMapCamera.Locked = false;
    }
    /// <summary>
    /// 创建地图
    /// </summary>
    /// <param name="x"></param>
    /// <param name="z"></param>
    void CreateMap(int x, int z)
    {
        if (generateMaps)
        {
            mapGenerator.GenerateMap(x, z, wrapping);
        }
        else
        {
            hexGrid.CreateMap(x, z, wrapping);
        }
        HexMapCamera.ValidatePosition();
        Close();
    }
    /// <summary>
    /// 创建小地图
    /// </summary>
    public void CreateSmallMap()
    {
        CreateMap(20, 15);
    }
    /// <summary>
    /// 创建中地图
    /// </summary>
    public void CreateMediumMap()
    {
        CreateMap(40, 30);
    }
    /// <summary>
    /// 创建大地图
    /// </summary>
    public void CreateLargeMap()
    {
        CreateMap(80, 60);
    }

    /// <summary>
    /// 控制是否生成地图
    /// </summary>
    /// <param name="toggle"></param>
    public void ToggleMapGeneration(bool toggle)
    {
        generateMaps = toggle;
    }
    /// <summary>
    /// 控制是否使用包装
    /// </summary>
    /// <param name="toggle"></param>
    public void ToggleWrapping(bool toggle)
    {
        wrapping = toggle;
    }
}
