using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using System.IO;
public class HexGrid : MonoBehaviour {
    //不再使用长宽，cell数量=块数*块内cell数量
    //public int width = 6;
    //public int height = 6;
    int chunkCountX, chunkCountZ;

    //cell的数量
    public int cellCountX = 20 , cellCountZ = 15;
    //六边形Prefab
    public HexCell cellPrefab;
    //HexCell数组，保存所有cell
    HexCell[] cells;
    //用于显示网格坐标
    public Text cellLabelPrefab;
    //网格块
    public HexGridChunk chunkPrefab;
    //噪声贴图
    public Texture2D noiseSource;
    //hashGrid
    public int seed;

    //手动渲染六边形网格
    HexGridChunk[] chunks;

    void Awake()
    {
        HexMetrics.noiseSource = noiseSource;
        HexMetrics.InitializeHashGrid(seed);
        CreateMap(cellCountX, cellCountZ);

        
    }
    void OnEnable()
    {
        if (!HexMetrics.noiseSource)
        {
            HexMetrics.noiseSource = noiseSource;
            HexMetrics.InitializeHashGrid(seed);
        }
    }
    void Start()
    {
        //hexMesh绘制在HexGridChunk中
    }
    /*
     * public 方法，被HexMapEditor调用
     * 清除原地图数据，创建地图
     */
    public bool CreateMap(int x,int z)
    {
        if(x <= 0 || x % HexMetrics.chunkSizeX != 0||
            z<= 0 || z %HexMetrics.chunkSizeZ != 0)
        {
            Debug.LogError("UnsupportMapSize");
            return false;
        }

        if(chunks != null)
        {
            for(int i = 0; i<chunks.Length;i++ )
            {
                Destroy(chunks[i].gameObject);
            }
        }
        //cell总数
        cellCountX = x;
        cellCountZ = z;
        chunkCountX = cellCountX / HexMetrics.chunkSizeX;
        chunkCountZ = cellCountZ / HexMetrics.chunkSizeZ;
        //创建块，在Start函数中开始渲染
        CreateChunks();
        //创建cell,这个是一个一个格子
        CreateCells();
        return true;
    }
    /*
     * 根据参数创建多个块
     */
    void CreateChunks()
    {
        chunks = new HexGridChunk[chunkCountX * chunkCountZ];

        for (int z = 0, i = 0; z < chunkCountZ; z++)
        {
            for (int x = 0; x < chunkCountX; x++)
            {
                //实例化一个块
                HexGridChunk chunk = chunks[i++] = Instantiate(chunkPrefab);
                chunk.transform.SetParent(transform);
            }
        }
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



    //创建一个CELL，参数给与坐标和编号
    void CreateCell(int x, int z, int i)
    {
        Vector3 position;
        //cell的XYZ本地坐标 0123不是world position
        position.x = (x + z * 0.5f - z / 2) * (HexMetrics.innerRadius * 2f);
        position.y = 0f;
        position.z = z * (HexMetrics.outerRadius * 1.5f);
        //实例化一个CELL，
        HexCell cell = cells[i] = Instantiate<HexCell>(cellPrefab);
        //此操作添加到块中了AddCell方法中了
        //cell.transform.SetParent(transform, false);
        cell.transform.localPosition = position;
        cell.coordinates = HexCoordinates.FromOffsetCoordinates(x, z);
        
        //创建顺序从左到右，从上到下，然后一次设置邻居
        //当前六边形的左邻居为上一个创建的六边形
        if (x > 0)
        {
            cell.SetNeighbor(HexDirection.W, cells[i - 1]);
        }
        if (z > 0)
        {   //大于0的奇数行，东南邻居
            if ((z & 1) == 0)
            {
                cell.SetNeighbor(HexDirection.SE, cells[i - cellCountX]);
                if (x > 0)//第二格开始才有西南邻居
                {
                    cell.SetNeighbor(HexDirection.SW, cells[i - cellCountX - 1]);
                }
            }
            else//偶数行，
            {   //左下
                cell.SetNeighbor(HexDirection.SW, cells[i - cellCountX]);
                if (x < cellCountX - 1)
                {   //右下
                    cell.SetNeighbor(HexDirection.SE, cells[i - cellCountX + 1]);
                }
            }
        }
        //实例化一个Text
        Text label = Instantiate<Text>(cellLabelPrefab);
        label.rectTransform.anchoredPosition =
                new Vector2(position.x, position.z);
        label.text = cell.coordinates.ToStringOnSeparateLines();
        cell.uiRect = label.rectTransform;//初始化的同时，也在inspector中绑定了uiRect和text
        cell.Elevation = 0;
        
        AddCellToChunk(x, z, cell);
    }
    //添加cell到块中
    void AddCellToChunk(int x, int z, HexCell cell)
    {
        //Cell所在块
        int chunkX = x / HexMetrics.chunkSizeX;
        int chunkZ = z / HexMetrics.chunkSizeZ;
        //                          列               行
        HexGridChunk chunk = chunks[chunkX + chunkZ * chunkCountX];
        //块中cell位置
        int localX = x - chunkX * HexMetrics.chunkSizeX;
        int localZ = z - chunkZ * HexMetrics.chunkSizeZ;
        chunk.AddCell(localX + localZ * HexMetrics.chunkSizeX, cell);
    }

    public HexCell GetCell(Vector3 position)
    {
        position = transform.InverseTransformPoint(position);
        HexCoordinates coordinates = HexCoordinates.FromPosition(position);
        int index = coordinates.X + coordinates.Z * cellCountX + coordinates.Z / 2;
        return cells[index];
    }


    public HexCell GetCell(HexCoordinates coordinates)
    {
        
        int z = coordinates.Z;
        if(z < 0 || z >= cellCountZ)
        {
            return null;//防止超出边界
        }
        int x = coordinates.X + z / 2;
        if (x < 0 || x >= cellCountX)
        {
            return null;
        }
        return cells[x + z * cellCountX];
    }
    //关闭显示所有块的UI
    public void ShowUI(bool visible)
    {
        for (int i = 0; i < chunks.Length; i++)
        {
            chunks[i].ShowUI(visible);
        }
    }

    /*
     * save and load
     * 保存和载入单个Grid的数据
     * 被HexMapEditor调用
     */
    public void Save(BinaryWriter writer)
    {
        writer.Write(cellCountX);
        writer.Write(cellCountZ);
       for (int i = 0; i < cells.Length; i++) {
			cells[i].Save(writer);
       }
    }

    public void Load(BinaryReader reader,int header)
    {
        int x = 20, z = 15;
        if(header >= 1)
        {
            x = reader.ReadInt32();
            z = reader.ReadInt32();
        }
        if(x != cellCountX || z !=cellCountZ)
        {
            if (!CreateMap(x, z))
            {
                return;
            }
        }


        for (int i = 0; i < cells.Length; i++){
            cells[i].Load(reader);
        }
        //读取数据后刷新地图
        for (int i = 0; i < chunks.Length; i++){
            chunks[i].Refresh();
        }
    }
}
