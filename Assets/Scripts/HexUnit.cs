using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

/// <summary>
/// 单位
/// </summary>
public class HexUnit : MonoBehaviour
{
    /// <summary>
    /// 单位位置
    /// </summary>
    HexCell location, currentTravelLocation;
    /// <summary>
    /// 单位朝向
    /// </summary>
    float orientation;

    public static HexUnit unitPrefab;
    /// <summary>
    /// 应该行进的路径
    /// </summary>
    List<HexCell> pathToTravel;
    /// <summary>
    /// 移动速度
    /// </summary>
    const float travelSpeed = 4f;
    /// <summary>
    /// 转向速度
    /// </summary>
    const float rotationSpeed = 180f;
    /// <summary>
    /// 视野范围
    /// </summary>
    public int VisionRange
    {
        get
        {
            return 3;
        }
    }




    private void OnEnable()
    {
        if (location)
        {
            transform.localPosition = location.Position;
            if (currentTravelLocation)
            {
                Grid.IncreaseVisibility(location, VisionRange);
                Grid.DecreaseVisibility(currentTravelLocation, VisionRange);
                currentTravelLocation = null;
            }
        }
    }



    /// <summary>
    /// 单位位置
    /// </summary>
    public HexCell Location
    {
        get { return location; }
        set
        {
            if (location)
            {
                //location.DecreaseVisibility();
                Grid.DecreaseVisibility(location, VisionRange);
                location.Unit = null;
            }
            location = value;
            value.Unit = this;
            //value.IncreaseVisibility();
            Grid.IncreaseVisibility(value, VisionRange);
            transform.localPosition = value.Position;
            Grid.MakeChildOfColumn(transform, value.ColumnIndex);
        }
    }
    /// <summary>
    /// 单位朝向
    /// </summary>
    public float Orientation
    {
        get { return orientation; }
        set
        {
            orientation = value;
            transform.localRotation = Quaternion.Euler(0f, value, 0f);
        }
    }
    /// <summary>
    /// 网格
    /// </summary>
    public HexGrid Grid { get; set; }
    /// <summary>
    /// 单位速度
    /// </summary>
    public int Speed { get { return 24; } }







    /// <summary>
    /// 验证位置
    /// </summary>
    public void ValidateLocation()
    {
        transform.localPosition = location.Position;
    }
    /// <summary>
    /// 销毁单位
    /// </summary>
    public void Die()
    {
        if (location)
        {
            //location.DecreaseVisibility();
            Grid.IncreaseVisibility(location, VisionRange);
        }
        location.Unit = null;
        Destroy(gameObject);
    }
    /// <summary>
    /// 保存单位
    /// </summary>
    /// <param name="writer"></param>
    public void Save(BinaryWriter writer)
    {
        location.coordinates.Save(writer);
        writer.Write(orientation);
    }
    /// <summary>
    /// 加载单位
    /// </summary>
    /// <param name="reader"></param>
    /// <param name="grid"></param>
    public static void Load(BinaryReader reader, HexGrid grid)
    {
        HexCoordinates coordinates = HexCoordinates.Load(reader);
        float orientation = reader.ReadSingle();
        grid.AddUnit(Instantiate(unitPrefab), grid.GetCell(coordinates), orientation);
    }
    /// <summary>
    /// 无效目的地
    /// </summary>
    /// <param name="cell"></param>
    /// <returns></returns>
    public bool IsValidDestination(HexCell cell)
    {
        return cell.IsExplored && !cell.IsUnderwater && !cell.Unit;
    }

    public void Traval(List<HexCell> path)
    {
        //Location = path[path.Count - 1];
        location.Unit = null;
        location = path[path.Count - 1];
        location.Unit = this;
        pathToTravel = path;
        StopAllCoroutines();
        StartCoroutine(TravelPath());
    }


    IEnumerator TravelPath()
    {
        Vector3 a, b, c = pathToTravel[0].Position;
        //transform.localPosition = c;
        yield return LookAt(pathToTravel[1].Position);
        //Grid.DecreaseVisibility(
        //    currentTravelLocation ? currentTravelLocation : pathToTravel[0],
        //    VisionRange);
        if (!currentTravelLocation)
        {
            currentTravelLocation = pathToTravel[0];
        }
        Grid.DecreaseVisibility(currentTravelLocation, VisionRange);
        int currentColumn = currentTravelLocation.ColumnIndex;

        float t = Time.deltaTime * travelSpeed;
        for (int i = 1; i < pathToTravel.Count; i++)
        {
            //Vector3 a = pathToTravel[i - 1].Position;
            //Vector3 b = pathToTravel[i].Position;
            currentTravelLocation = pathToTravel[i];
            a = c;
            b = pathToTravel[i - 1].Position;
            //c = (b + pathToTravel[i].Position) * 0.5f;
            //Grid.IncreaseVisibility(pathToTravel[i], VisionRange);
            int nextColumn = currentTravelLocation.ColumnIndex;
            if (currentColumn != nextColumn)
            {
                if (nextColumn < currentColumn - 1)
                {
                    a.x -= HexMetrics.innerDiameter * HexMetrics.wrapSize;
                    b.x -= HexMetrics.innerDiameter * HexMetrics.wrapSize;
                }
                else if (nextColumn > currentColumn + 1)
                {
                    a.x += HexMetrics.innerDiameter * HexMetrics.wrapSize;
                    b.x += HexMetrics.innerDiameter * HexMetrics.wrapSize;
                }
                Grid.MakeChildOfColumn(transform, nextColumn);
                currentColumn = nextColumn;
            }
            c = (b + currentTravelLocation.Position) * 0.5f;
            Grid.IncreaseVisibility(pathToTravel[i], VisionRange);
            for (; t < 1f; t += Time.deltaTime * travelSpeed)
            {

                transform.localPosition = Bezier.GetPoint(a, b, c, t);
                Vector3 d = Bezier.GetDerivative(a, b, c, t);
                //上下斜坡避免不合法的朝向
                d.y = 0f;
                transform.localRotation = Quaternion.LookRotation(d);
                yield return null;
            }
            Grid.DecreaseVisibility(pathToTravel[i], VisionRange);
            t -= 1f;
        }
        currentTravelLocation = null;
        a = c;
        //b = pathToTravel[pathToTravel.Count - 1].Position;
        b = location.Position;
        c = b;
        Grid.IncreaseVisibility(location, VisionRange);
        for (; t < 1f; t += Time.deltaTime * travelSpeed)
        {
            transform.localPosition = Bezier.GetPoint(a, b, c, t);
            Vector3 d = Bezier.GetDerivative(a, b, c, t);
            d.y = 0f;
            transform.localRotation = Quaternion.LookRotation(d);
            yield return null;
        }
        transform.localPosition = location.Position;
        orientation = transform.localRotation.eulerAngles.y;

        ListPool<HexCell>.Add(pathToTravel);
        pathToTravel = null;
    }
    /// <summary>
    /// 使单位朝向坐标
    /// </summary>
    /// <param name="point"></param>
    /// <returns></returns>
    IEnumerator LookAt(Vector3 point)
    {
        if (HexMetrics.Wrapping)
        {
            float xDistance = point.x - transform.localPosition.x;
            if (xDistance < -HexMetrics.innerRadius * HexMetrics.wrapSize)
            {
                point.x += HexMetrics.innerDiameter * HexMetrics.wrapSize;
            }
            else if (xDistance > HexMetrics.innerRadius * HexMetrics.wrapSize)
            {
                point.x -= HexMetrics.innerDiameter * HexMetrics.wrapSize;
            }
        }


        point.y = transform.localPosition.y;

        Quaternion fromRotation = transform.localRotation;
        Quaternion toRotation = Quaternion.LookRotation(point - transform.localPosition);

        float angle = Quaternion.Angle(fromRotation, toRotation);
        float speed = rotationSpeed / angle;

        if (angle > 0f)
        {
            for (float t = Time.deltaTime; t < 1f; t += Time.deltaTime)
            {
                transform.localRotation = Quaternion.Slerp(fromRotation, toRotation, t);
                yield return null;
            }
            transform.LookAt(point);
            orientation = transform.localRotation.eulerAngles.y;
        }
    }


    public int GetMoveCost(HexCell fromCell,HexCell toCell,HexDirection direction)
    {
        HexEdgeType edgeType = fromCell.GetEdgeType(toCell);
        if (edgeType == HexEdgeType.Cliff)
        {
            return -1;
        }
        int moveCost;
        if (fromCell.HasRoadThroughEdge(direction))
        {
            moveCost = 1;
        }
        else if (fromCell.Walled != toCell.Walled)
        {
            return -1;
        }
        else
        {
            moveCost = edgeType == HexEdgeType.Flat ? 5 : 10;
            moveCost +=
                toCell.UrbanLevel + toCell.FarmLevel + toCell.PlantLevel;
        }
        return moveCost;
    }
}
