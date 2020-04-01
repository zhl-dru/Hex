using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HexMapCamera : MonoBehaviour
{
    Transform swivel, stick;
    /// <summary>
    /// 焦距
    /// </summary>
    float zoom = 1f;
    /// <summary>
    /// 定义相机高度
    /// </summary>
    public float stickMinZoom, stickMaxZoom;
    /// <summary>
    /// 定义旋转接头
    /// </summary>
    public float swivelMinZoom, swivelMaxZoom;
    /// <summary>
    /// 定义移动速度
    /// </summary>
    //public float moveSpeed;
    public float moveSpeedMinZoom, moveSpeedMaxZoom;
    /// <summary>
    /// 旋转速度
    /// </summary>
    public float rotationSpeed;
    /// <summary>
    /// 旋转角度
    /// </summary>
    float rotationAngle;

    public HexGrid grid;

    static HexMapCamera instance;



    private void Awake()
    {
        instance = this;
        swivel = transform.GetChild(0);
        stick = swivel.GetChild(0);
    }

    private void Update()
    {
        float zoomDelta = Input.GetAxis("Mouse ScrollWheel");
        if (zoomDelta != 0f)
        {
            AdjustZoom(zoomDelta);
        }

        float rotationDelta = Input.GetAxis("Rotation");
        if (rotationDelta != 0f)
        {
            AdjustRotation(rotationDelta);
        }

        float xDelta = Input.GetAxis("Horizontal");
        float zDelta = Input.GetAxis("Vertical");
        if (xDelta != 0f || zDelta != 0f)
        {
            AdjustPosition(xDelta, zDelta);
        }
    }

    private void OnEnable()
    {
        instance = this;
        ValidatePosition();
    }



    /// <summary>
    /// 调整缩放
    /// </summary>
    /// <param name="delta"></param>
    void AdjustZoom(float delta)
    {
        zoom = Mathf.Clamp01(zoom + delta);

        float distance = Mathf.Lerp(stickMinZoom, stickMaxZoom, zoom);
        stick.localPosition = new Vector3(0f, 0f, distance);

        float angle = Mathf.Lerp(swivelMinZoom, swivelMaxZoom, zoom);
        swivel.localRotation = Quaternion.Euler(angle, 0f, 0f);
    }
    /// <summary>
    /// 调整相机位置
    /// </summary>
    /// <param name="xDelta"></param>
    /// <param name="zDelta"></param>
    void AdjustPosition(float xDelta, float zDelta)
    {
        Vector3 direction = transform.localRotation * new Vector3(xDelta, 0f, zDelta).normalized;
        //阻尼系数
        float damping = Mathf.Max(Mathf.Abs(xDelta), Mathf.Abs(zDelta));
        //float distance = moveSpeed * damping * Time.deltaTime;
        float distance = Mathf.Lerp(moveSpeedMinZoom, moveSpeedMaxZoom, zoom) * damping * Time.deltaTime;

        Vector3 position = transform.localPosition;
        position += direction * distance;
        transform.localPosition = grid.wrapping ? WrapPosition(position) : ClampPosition(position);
    }
    /// <summary>
    /// 控制相机边界
    /// </summary>
    /// <param name="position"></param>
    /// <returns></returns>
    Vector3 ClampPosition(Vector3 position)
    {
        float xMax = (grid.cellCountX - 0.5f) * HexMetrics.innerDiameter;
        position.x = Mathf.Clamp(position.x, 0f, xMax);

        float zMax = (grid.cellCountZ - 1) * (1.5f * HexMetrics.outerRadius);
        position.z = Mathf.Clamp(position.z, 0f, zMax);

        return position;
    }
    /// <summary>
    /// 调整旋转角度
    /// </summary>
    /// <param name="delta"></param>
    void AdjustRotation(float delta)
    {
        rotationAngle += delta * rotationSpeed * Time.deltaTime;
        if (rotationAngle < 0f)
        {
            rotationAngle += 360f;
        }
        else if (rotationAngle >= 360f)
        {
            rotationAngle -= 360f;
        }
        transform.localRotation = Quaternion.Euler(0f, rotationAngle, 0f);
    }

    public static bool Locked
    {
        set
        {
            instance.enabled = !value;
        }
    }
    /// <summary>
    /// 重置摄像机
    /// </summary>
    public static void ValidatePosition()
    {
        instance.AdjustPosition(0f, 0f);
    }
    /// <summary>
    /// 包裹位置
    /// </summary>
    /// <param name="position"></param>
    /// <returns></returns>
    Vector3 WrapPosition(Vector3 position)
    {
        //float xMax = (grid.cellCountX - 0.5f) * HexMetrics.innerDiameter;
        //position.x = Mathf.Clamp(position.x, 0f, xMax);
        float width = grid.cellCountX * HexMetrics.innerDiameter;
        while (position.x < 0f)
        {
            position.x += width;
        }
        while (position.x > width)
        {
            position.x -= width;
        }

        float zMax = (grid.cellCountZ - 1) * (1.5f * HexMetrics.outerRadius);
        position.z = Mathf.Clamp(position.z, 0f, zMax);

        grid.CenterMap(position.x);
        return position;
    }
}
