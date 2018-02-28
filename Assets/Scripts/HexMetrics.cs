using UnityEngine;
public enum HexEdgeType
{
    //平面，坡，悬崖
    Flat, Slope, Cliff
}
//六边形网格的定义
public static class HexMetrics
{
    //六边形的顶点数组
    static Vector3[] corners = {
		new Vector3(0f, 0f, outerRadius),
		new Vector3(innerRadius, 0f, 0.5f * outerRadius),
		new Vector3(innerRadius, 0f, -0.5f * outerRadius),
		new Vector3(0f, 0f, -outerRadius),
		new Vector3(-innerRadius, 0f, -0.5f * outerRadius),
		new Vector3(-innerRadius, 0f, 0.5f * outerRadius),
        new Vector3(0f, 0f, outerRadius)
    };


    //内圈/外圈
    public const float outerToInner = 0.866025404f;
    public const float innerToOuter = 1f / outerToInner;
    //外圈半径
    public const float outerRadius = 10f;
    //内半径
    public const float innerRadius = outerRadius * outerToInner;
    //六边形，纯色区域比率
    public const float solidFactor = 0.8f;
    //六边形，边框区域比率
    public const float blendFactor = 1f - solidFactor;
    //3个海拔高度
    public const float elevationStep = 3f;
    //一个斜坡有几个露台（terraces）
    public const int terracesPerSlope = 1;
    //平面数量
    public const int terraceSteps = terracesPerSlope * 2 + 1;
    //水平插值步长和垂直插值步长，垂直差值步长
    public const float horizontalTerraceStepSize = 1f / terraceSteps;
    public const float verticalTerraceStepSize = 1f / (terracesPerSlope + 1);

    //噪音样本
    public static Texture2D noiseSource;
    //扰动力度
    public const float cellPerturbStrength = 4f;//4f
    public const float elevationPerturbStrength = 1.5f;
    //噪音采样规模
    public const float noiseScale = 0.003f;
    //块大小
    public const int chunkSizeX = 5, chunkSizeZ = 5;
    //河床垂直偏移
    public const float streamBedElevationOffset = -1f;
    //水面高度
    public const float waterElevationOffset = -0.5f;
    //水面比率因子
    public const float waterFactor = 0.6f;
    public const float waterBlendFactor = 1f - waterFactor;

    //256*256的hash表，用于在256*256的噪音贴图采样
    public const int hashGridSize = 256;
    static HexHash[] hashGrid;
    public const float hashGridScale = 0.25f;
    //墙高度
    public const float wallHeight = 4f;
    //围墙下沉量
    public const float wallYOffset = -1f;
    //围墙厚度
    public const float wallThickness = 0.75f;

    public const float wallElevationOffset = verticalTerraceStepSize;

    public const float wallTowerThreshold = 0.5f;
    //桥的长度
    public const float bridgeDesignLength = 7f;
    //初始化Hash表
    public static void InitializeHashGrid(int seed)
    {
        hashGrid = new HexHash[hashGridSize * hashGridSize];
        Random.State currentState = Random.state;
        Random.InitState(seed);
        for (int i = 0; i < hashGrid.Length; i++)
        {
            hashGrid[i] = HexHash.Create();
        }
        Random.state = currentState;
    }
    //建筑群升级的阀值
    static float[][] featureThresholds = {
		new float[] {0.0f, 0.0f, 0.4f},
		new float[] {0.0f, 0.4f, 0.6f},
		new float[] {0.4f, 0.6f, 0.8f}
	};

    public static float[] GetFeatureThresholds(int level)
    {
        return featureThresholds[level];
    }

    public static HexEdgeType GetEdgeType(int elevation1, int elevation2)
    {
        if (elevation1 == elevation2)//同一平面上
        {
            return HexEdgeType.Flat;
        }
        int delta = elevation2 - elevation1;//高度差
        if (delta == 1 || delta == -1)//高度差为1
        {
            return HexEdgeType.Slope;//坡
        }
        return HexEdgeType.Cliff;//其他情况为悬崖
    }

    //斜坡的插值方法，在ab之间插值
    public static Vector3 TerraceLerp(Vector3 a, Vector3 b, int step)
    {
        float h = step * HexMetrics.horizontalTerraceStepSize;
        a.x += (b.x - a.x) * h;
        a.z += (b.z - a.z) * h;
        //step奇数时候+1
        float v = ((step + 1) / 2) * HexMetrics.verticalTerraceStepSize;
        a.y += (b.y - a.y) * v;
        return a;
    }

    //颜色差值方法
    public static Color TerraceLerp(Color a, Color b, int step)
    {
        float h = step * HexMetrics.horizontalTerraceStepSize;
        return Color.Lerp(a, b, h);
    }
    public static Vector3 GetFirstCorner(HexDirection direction)
    {
        return corners[(int)direction];
    }
    public static Vector3 GetSecondCorner(HexDirection direction)
    {
        return corners[(int)direction + 1];
    }
     //返回边框顶点坐标
    public static Vector3 GetFirstSolidCorner(HexDirection direction)
    {
        return corners[(int)direction] * solidFactor;
    }

    public static Vector3 GetSecondSolidCorner(HexDirection direction)
    {
        return corners[(int)direction + 1] * solidFactor;
    }
    public static Vector3 GetBridge(HexDirection direction)
    {
        return (corners[(int)direction] + corners[(int)direction + 1]) *
             blendFactor;
    }
    //返回两个顶点中间点
    public static Vector3 GetSolidEdgeMiddle(HexDirection direction)
    {
        return (corners[(int)direction] + corners[(int)direction + 1]) *
            (0.5f * solidFactor);
    }

    public static Vector3 GetFirstWaterCorner(HexDirection direction)
    {
        return corners[(int)direction] * waterFactor;
    }

    public static Vector3 GetSecondWaterCorner(HexDirection direction)
    {
        return corners[(int)direction + 1] * waterFactor;
    }

    public static Vector3 GetWaterBridge(HexDirection direction)
    {
        return (corners[(int)direction] + corners[(int)direction + 1]) *
            waterBlendFactor;
    }
    /**************************************************/
    /***********双线性滤波采样函数*********************/
    public static Vector4 SampleNoise(Vector3 position)
    {
        return noiseSource.GetPixelBilinear(
            position.x * noiseScale, 
            position.y * noiseScale);
    }
    /*******************************************************************/
    //扰动方法，用来扰动Grid的顶点，sample的范围在-1-1
    public static Vector3 Perturb(Vector3 position)
    {
        Vector4 sample = SampleNoise(position);
        position.x += (sample.x * 2f - 1f) * cellPerturbStrength;
        position.z += (sample.z * 2f - 1f) * cellPerturbStrength;
        return position;
    }
    /*
     * SampleHashGrid
     */
    public static HexHash SampleHashGrid(Vector3 position)
    {
        int x = (int)(position.x * hashGridScale) % hashGridSize;
        if (x < 0) 
            x += hashGridSize; 
        int z = (int)(position.z * hashGridScale) % hashGridSize;
        if (z < 0) 
            z += hashGridSize; 
        return hashGrid[x + z * hashGridSize];
    }
    /*
     * 返回一个Vector3，表示墙面厚度
     */
    public static Vector3 WallThicknessOffset(Vector3 near, Vector3 far)
    {
        Vector3 offset;
        offset.x = far.x - near.x;
        offset.y = 0f;
        offset.z = far.z - near.z;
        return offset.normalized * (wallThickness *0.5f);
    }
    //墙体插值方法，用于下降墙体，使得墙体高度对齐near 和far中低的一方
    public static Vector3 WallLerp(Vector3 near, Vector3 far)
    {
        near.x += (far.x - near.x) * 0.5f;
        near.z += (far.z - near.z) * 0.5f;
        float v =
            near.y < far.y ? wallElevationOffset : (1f - wallElevationOffset);
        near.y += (far.y - near.y) * v + wallYOffset;
        return near;
    }
}