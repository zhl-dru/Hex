using UnityEngine;

/// <summary>
/// 单元格性质
/// </summary>
public static class HexMetrics
{
    /// <summary>
    /// 外径转换内径因子
    /// </summary>
    public const float outerToInner = 0.866025404f;
    /// <summary>
    /// 内径转换外径因子
    /// </summary>
    public const float innerToOuter = 1f / outerToInner;
    /// <summary>
    /// 外半径
    /// </summary>
    public const float outerRadius = 10f;
    /// <summary>
    /// 内半径
    /// </summary>
    public const float innerRadius = outerRadius * outerToInner;
    /// <summary>
    /// 内直径
    /// </summary>
    public const float innerDiameter = innerRadius * 2f;
    /// <summary>
    /// 六边形六个角的坐标
    /// </summary>
    public static Vector3[] corners =
    {
        new Vector3(0f,0f,outerRadius),
        new Vector3(innerRadius,0f,0.5f*outerRadius),
        new Vector3(innerRadius,0f,-0.5f*outerRadius),
        new Vector3(0f,0f,-outerRadius),
        new Vector3(-innerRadius,0f,-0.5f*outerRadius),
        new Vector3(-innerRadius,0f,0.5f*outerRadius),
        new Vector3(0f,0f,outerRadius)
    };

    /// <summary>
    /// 网格固定区域
    /// </summary>
    public const float solidFactor = 0.8f;
    /// <summary>
    /// 网格边缘混合区域的大小比例
    /// </summary>
    public const float blendFactor = 1f - solidFactor;
    /// <summary>
    /// 定义阶梯间高度
    /// </summary>
    public const float elevationStep = 3f;
    /// <summary>
    /// 定义阶地的数量
    /// </summary>
    public const int terracePerSlope = 2;
    /// <summary>
    /// 定义三角化步骤的数量
    /// </summary>
    public const int terraceSteps = terracePerSlope * 2 + 1;
    /// <summary>
    /// 水平阶梯大小
    /// </summary>
    public const float horizontalTerraceStepSize = 1f / terraceSteps;
    /// <summary>
    /// 垂直阶梯大小
    /// </summary>
    public const float verticalTerraceStepSize = 1f / (terracePerSlope + 1);
    /// <summary>
    /// 噪音纹理
    /// </summary>
    public static Texture2D noiseSource;
    /// <summary>
    /// 微扰强度
    /// </summary>
    public const float cellPerturbStrength = 4f;
    /// <summary>
    /// 噪音取样大小
    /// </summary>
    public const float noiseScale = 0.003f;
    /// <summary>
    /// 高度微扰强度
    /// </summary>
    public const float elevationPerturbStrength = 1.5f;
    /// <summary>
    /// 定义网格块的大小
    /// </summary>
    public const int chunkSizeX = 5, chunkSizeZ = 5;
    /// <summary>
    /// 定义河床高度
    /// </summary>
    public const float streamBedElevationOffset = -1.75f;
    /// <summary>
    /// 定义水体表面偏移
    /// </summary>
    //public const float riverSurfaceElevationOffset = -0.5f;
    public const float waterElevationOffset = -0.5f;
    /// <summary>
    /// 水面因子
    /// </summary>
    public const float waterFactor = 0.6f;
    /// <summary>
    /// 水面混合因子
    /// </summary>
    public const float waterBlendFactor = 1f - waterFactor;
    /// <summary>
    /// 网格尺寸
    /// </summary>
    public const int hashGridSize = 256;
    /// <summary>
    /// 散列网格
    /// </summary>
    static HexHash[] hashGrid;
    /// <summary>
    /// 网格规模
    /// </summary>
    public const float hashGridScale = 0.25f;
    /// <summary>
    /// 特征门槛
    /// </summary>
    static float[][] featureThresholds =
    {
        new float[]{0.0f,0.0f,0.4f},
        new float[]{0.0f,0.4f,0.6f},
        new float[]{0.4f,0.6f,0.8f}
    };
    /// <summary>
    /// 塔楼门槛
    /// </summary>
    public const float wallTowerThreshold = 0.5f;
    /// <summary>
    /// 围墙高度
    /// </summary>
    public const float wallHeight = 4f;
    /// <summary>
    /// 围墙厚度
    /// </summary>
    public const float wallThickness = 0.75f;
    /// <summary>
    /// 围墙高度偏移量
    /// </summary>
    public const float wallElevationOffset = verticalTerraceStepSize;
    /// <summary>
    /// 围墙Y偏移量
    /// </summary>
    public const float wallYOffset = -1f;
    /// <summary>
    /// 默认桥梁长度
    /// </summary>
    public const float bridgeDesignLength = 7f;
    /// <summary>
    /// 包装指标
    /// </summary>
    public static int wrapSize;

    /// <summary>
    /// 包装指标
    /// </summary>
    public static bool Wrapping
    {
        get
        {
            return wrapSize > 0;
        }
    }




    /// <summary>
    /// 获得第一个角
    /// </summary>
    /// <param name="direction">方向</param>
    /// <returns></returns>
    public static Vector3 GetFirstCorner(HexDirection direction)
    {
        return corners[(int)direction];
    }

    /// <summary>
    /// 获得第二个角
    /// </summary>
    /// <param name="direction">方向</param>
    /// <returns></returns>
    public static Vector3 GetSecondCorner(HexDirection direction)
    {
        return corners[(int)direction + 1];
    }

    /// <summary>
    /// 获得固定区域第一个角
    /// </summary>
    /// <param name="direction">方向</param>
    /// <returns></returns>
    public static Vector3 GetFirstSolidCorner(HexDirection direction)
    {
        return corners[(int)direction] * solidFactor;
    }

    /// <summary>
    /// 获得固定区域第二个角
    /// </summary>
    /// <param name="direction">方向</param>
    /// <returns></returns>
    public static Vector3 GetSecondSolidCorner(HexDirection direction)
    {
        return corners[(int)direction + 1] * solidFactor;
    }

    /// <summary>
    /// 获得网格间的桥
    /// </summary>
    /// <param name="direction">方向</param>
    /// <returns></returns>
    public static Vector3 GetBridge(HexDirection direction)
    {
        return (corners[(int)direction] + corners[(int)direction + 1]) * blendFactor;
    }
    /// <summary>
    /// 边缘插值,用于斜坡
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <param name="step"></param>
    /// <returns></returns>
    public static Vector3 TerraceLerp(Vector3 a, Vector3 b, int step)
    {
        float h = step * HexMetrics.horizontalTerraceStepSize;
        a.x += (b.x - a.x) * h;
        a.z += (b.z - a.z) * h;
        float v = ((step + 1) / 2) * HexMetrics.verticalTerraceStepSize;
        a.y += (b.y - a.y) * v;
        return a;
    }
    /// <summary>
    /// 边缘插值,用于斜坡的颜色
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <param name="step"></param>
    /// <returns></returns>
    public static Color TerraceLerp(Color a, Color b, int step)
    {
        float h = step * HexMetrics.horizontalTerraceStepSize;
        return Color.Lerp(a, b, h);
    }

    /// <summary>
    /// 获取单元格间的边缘类型
    /// </summary>
    /// <param name="elevation1">高度</param>
    /// <param name="elevation2">高度</param>
    /// <returns>边缘类型</returns>
    public static HexEdgeType GetEdgeType(int elevation1, int elevation2)
    {
        if (elevation1 == elevation2)
        {
            return HexEdgeType.Flat;
        }
        int delta = elevation2 - elevation1;
        if (delta == 1 || delta == -1)
        {
            return HexEdgeType.Slope;
        }
        return HexEdgeType.Cliff;
    }
    /// <summary>
    /// 获得特征门槛
    /// </summary>
    /// <param name="level"></param>
    /// <returns></returns>
    public static float[] GetFeatureThresholds(int level)
    {
        return featureThresholds[level];
    }
    /// <summary>
    /// 噪音取样
    /// </summary>
    /// <param name="position">世界坐标</param>
    /// <returns>可转换为4D向量的颜色</returns>
    public static Vector4 SampleNoise(Vector3 position)
    {
        Vector4 sample = noiseSource.GetPixelBilinear(position.x * noiseScale, position.z * noiseScale);
        if (Wrapping && position.x < innerDiameter * 1.5f)
        {
            Vector4 sample2 = noiseSource.GetPixelBilinear((position.x + wrapSize * innerDiameter) * noiseScale, position.z * noiseScale);
            sample = Vector4.Lerp(sample2, sample, position.x * (1f / innerDiameter) - 0.5f);
        }
        return sample;
    }
    /// <summary>
    /// 对相邻两个角向量取平均数然后乘以纯色因子
    /// </summary>
    /// <param name="direction"></param>
    /// <returns></returns>
    public static Vector3 GetSolidEdgeMiddle(HexDirection direction)
    {
        return (corners[(int)direction] + corners[(int)direction + 1]) * (0.5f * solidFactor);
    }
    /// <summary>
    /// 微扰顶点
    /// </summary>
    /// <param name="position"></param>
    /// <returns></returns>
    public static Vector3 Perturb(Vector3 position)
    {
        Vector4 sample = HexMetrics.SampleNoise(position);
        position.x += (sample.x * 2f - 1f) * HexMetrics.cellPerturbStrength;
        position.z += (sample.z * 2f - 1f) * HexMetrics.cellPerturbStrength;
        return position;
    }
    /// <summary>
    /// 获得水体单元第一个角
    /// </summary>
    /// <param name="direction"></param>
    /// <returns></returns>
    public static Vector3 GetFirstWaterCorner(HexDirection direction)
    {
        return corners[(int)direction] * waterFactor;
    }
    /// <summary>
    /// 获得水体单元第二个角
    /// </summary>
    /// <param name="direction"></param>
    /// <returns></returns>
    public static Vector3 GetSecondWaterCorner(HexDirection direction)
    {
        return corners[(int)direction + 1] * waterFactor;
    }
    /// <summary>
    /// 获得水体单元间的桥
    /// </summary>
    /// <param name="direction"></param>
    /// <returns></returns>
    public static Vector3 GetWaterBridge(HexDirection direction)
    {
        return (corners[(int)direction] + corners[(int)direction + 1]) * waterBlendFactor;
    }
    /// <summary>
    /// 初始化散列网格
    /// </summary>
    public static void InitializeHashGrid(int seed)
    {
        hashGrid = new HexHash[hashGridSize * hashGridSize];
        Random.State currentState = Random.state;
        //加入随机种子
        Random.InitState(seed);
        for (int i = 0; i < hashGrid.Length; i++)
        {
            hashGrid[i] = HexHash.Create();
        }
        Random.state = currentState;
    }
    /// <summary>
    /// 散列索引取样
    /// </summary>
    /// <param name="position"></param>
    /// <returns></returns>
    public static HexHash SamleHashGrid(Vector3 position)
    {
        int x = (int)(position.x * hashGridScale) % hashGridSize;
        if (x < 0)
        {
            x += hashGridSize;
        }
        int z = (int)(position.z * hashGridScale) % hashGridSize;
        if (z < 0)
        {
            z += hashGridSize;
        }
        return hashGrid[x + z * hashGridSize];
    }
    /// <summary>
    /// 围墙偏移向量
    /// </summary>
    /// <param name="near"></param>
    /// <param name="far"></param>
    /// <returns></returns>
    public static Vector3 WallThicknessOffset(Vector3 near, Vector3 far)
    {
        Vector3 offset;
        offset.x = far.x - near.x;
        offset.y = 0f;
        offset.z = far.z - near.z;
        return offset.normalized * (wallThickness * 0.5f);
    }
    /// <summary>
    /// 围墙插值
    /// </summary>
    /// <param name="near"></param>
    /// <param name="far"></param>
    /// <returns></returns>
    public static Vector3 WallLerp(Vector3 near, Vector3 far)
    {
        near.x += (far.x - near.x) * 0.5f;
        near.z += (far.z - near.z) * 0.5f;
        float v = near.y < far.y ? wallElevationOffset : (1f - wallElevationOffset);
        //计算Y坐标时将新的偏移考虑进去
        near.y += (far.y - near.y) * v + wallYOffset;
        return near;
    }
}
