using UnityEngine;
using System.Collections;
using System.IO;
/*
 * 六边形网格的基础cell，附在prefeb上，表示一个六边形，
 * 记录cell上的各种地表数据，
 * 优先级 water 》river 》specialfeature 》road 》feature
 */
public class HexCell : MonoBehaviour {
    //当前格子的坐标
    public HexCoordinates coordinates;
    //坐标显示的位置
    public RectTransform uiRect;
    //邻居六边形
    [SerializeField]
    HexCell[] neighbors;
    //roads情况
    [SerializeField]
    bool[] roads;
    //cell所属块
    public HexGridChunk chunk;
    //地形类型 目录
    int terrainTypeIndex;

     //是否有出入河流，方向
    bool hasIncomingRiver, hasOutgoingRiver;

    HexDirection incomingRiver, outgoingRiver;
    public bool HasIncomingRiver{
        get{
            return hasIncomingRiver;
        }
    }
    public bool HasOutgoingRiver{
        get{
            return hasOutgoingRiver;
        }
    }
    public HexDirection IncomingRiver{
        get{
            return incomingRiver;
        }
    }

    public HexDirection OutgoingRiver{
        get{
            return outgoingRiver;
        }
    }
    public bool HasRiver{
        get{
            return hasIncomingRiver || hasOutgoingRiver;
        }
    }
    public bool HasRiverBeginOrEnd{
        get{
            return hasIncomingRiver != hasOutgoingRiver;
        }
    }
    //direction方向是否有河流出入
    public bool HasRiverThroughEdge(HexDirection direction){
        return
            hasIncomingRiver && incomingRiver == direction ||
            hasOutgoingRiver && outgoingRiver == direction;
    }
    public HexDirection RiverBeginOrEndDirection
    {
        get
        {
            return hasIncomingRiver ? incomingRiver : outgoingRiver;
        }
    }
    //自身高于neighbor并且水面和neighbor的高度一致
    bool IsValidRiverDestination(HexCell neighbor)
    {
        return neighbor && (
            elevation >= neighbor.elevation || waterLevel == neighbor.elevation
        );
    }
    /*
     * 道路相关操作
     */
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

    public void AddRoad(HexDirection direction){
        if (!roads[(int)direction] && !HasRiverThroughEdge(direction) &&
            !IsSpecial && !GetNeighbor(direction).IsSpecial &&
            GetElevationDifference(direction) <= 1)
        {
            SetRoad((int)direction, true);
        }
    }
    //删除所有方向的road
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

    void SetRoad(int index, bool state)
    {
        roads[index] = state;
        neighbors[index].roads[(int)((HexDirection)index).Opposite()] = state;
        neighbors[index].RefreshSelfOnly();
        RefreshSelfOnly();
    }

    public bool HasRoadThroughEdge(HexDirection direction)
    {
        return roads[(int)direction];
    }
    public int GetElevationDifference(HexDirection direction)
    {
        int difference = elevation - GetNeighbor(direction).elevation;
        return difference >= 0 ? difference : -difference;
    }
    /*
     * 水体的变量
     */
    public int WaterLevel
    {
        get
        {
            return waterLevel;
        }
        set
        {
            if (waterLevel == value)
            {
                return;
            }
            waterLevel = value;
            ValidateRivers();
            Refresh();
        }
    }
    int waterLevel;

    public bool IsUnderwater
    {
        get
        {
            return waterLevel > elevation;
        }
    }
    //颜色
    //没有了，用地形贴图替代

    //地形类型
    public int TerrainTypeIndex
    {
        get
        {
            return terrainTypeIndex;
        }
        set
        {
            if(terrainTypeIndex != value)
            {
                terrainTypeIndex = value;
                Refresh();
            }
        }
    }

    int elevation = int.MinValue;
    /*
     * 海拔，设置GET,SET方便扩展
     */
    public int Elevation
    {
        get
        {
            return elevation;
        }
        set//当变量被改变时候
        {
            if (elevation == value)
            {
                return;
            }
            elevation = value;
            RefreshPosition();
            //防止出现Uphill River
            ValidateRivers();
            //当高度变高时，判断是否删除上面的路
            for (int i = 0; i < roads.Length; i++)
            {
                if (roads[i] && GetElevationDifference((HexDirection)i) > 1)
                {
                    SetRoad(i, false);
                }
            }

            //刷新整个块
            Refresh();
        }
    }
    
    public Vector3 Position
    {
        get
        {
            return transform.localPosition;
        }
    }
    public float StreamBedY
    {
        get
        {
            return
                (elevation + HexMetrics.streamBedElevationOffset) *
                HexMetrics.elevationStep;
        }
    }
    public float RiverSurfaceY
    {
        get
        {
            return
                (elevation + HexMetrics.waterElevationOffset) *
                HexMetrics.elevationStep;
        }
    }
    //河床垂直偏移高度Y
    public float WaterSurfaceY
    {
        get
        {
            return
                (waterLevel + HexMetrics.waterElevationOffset) *
                HexMetrics.elevationStep;
        }
    }


    /*
     * 提取出来的evelvation参数改变后的刷新方法
     */
    void RefreshPosition()
    {
        Vector3 position = transform.localPosition;
        position.y = elevation * HexMetrics.elevationStep;
        position.y +=
            (HexMetrics.SampleNoise(position).y * 2f - 1f) *
            HexMetrics.elevationPerturbStrength;
        transform.localPosition = position;
        //调节坐标的显示
        Vector3 uiPosition = uiRect.localPosition;
        uiPosition.z = -position.y;
        uiRect.localPosition = uiPosition;
    }

    /*
     * 地面上的建筑群参数
     * 把红色cube当作城市建筑，
     */
    int urbanLevel, farmLevel, plantLevel;
    public int UrbanLevel
    {
        get
        {
            return urbanLevel;
        }
        set
        {
            if (urbanLevel != value)
            {
                urbanLevel = value;
                RefreshSelfOnly();
            }
        }
    }

    /*
    * 地面上的农场群参数
    */
    public int FarmLevel
    {
        get
        {
            return farmLevel;
        }
        set
        {
            if (farmLevel != value)
            {
                farmLevel = value;
                RefreshSelfOnly();
            }
        }
    }
    /*
    * 地表植物群参数
    */
    public int PlantLevel
    {
        get
        {
            return plantLevel;
        }
        set
        {
            if (plantLevel != value)
            {
                plantLevel = value;
                RefreshSelfOnly();
            }
        }
    }

    public bool Walled
    {
        get { return walled; }
        set
        {
            if(walled !=value)
            {
                walled = value;
                Refresh();
            }
        }
    }
    bool walled;

    /*
     * 地表特殊物体,接收来自界面的参数activeSpecialIndex
     */
    int specialIndex;
    public int SpecialIndex
    {
        get
        {
            return specialIndex;
        }
        set
        {
            if(specialIndex != value && !HasRiver)
            {
                specialIndex = value;
                RemoveRoads();
                RefreshSelfOnly();
            }
        }
    }
    public bool IsSpecial
    {
        get
        {
            return specialIndex > 0;
        }
    }
    //返回指定的邻居格子，右上角为1，顺时针
    public HexCell GetNeighbor(HexDirection direction)
    {
        return neighbors[(int)direction];
    }
    //设置指定的邻居关系
    public void SetNeighbor(HexDirection direction, HexCell cell)
    {
        neighbors[(int)direction] = cell;
        cell.neighbors[(int)direction.Opposite()] = this;
    }
    //当前六边形某一方向的边界类型
    public HexEdgeType GetEdgeType(HexDirection direction)
    {
        return HexMetrics.GetEdgeType(
            elevation, neighbors[(int)direction].elevation
        );
    }
    //重载一个方法
    public HexEdgeType GetEdgeType(HexCell otherCell)
    {
        return HexMetrics.GetEdgeType(
            elevation, otherCell.elevation
        );
    }
    //刷新cell
    void Refresh(){
        if (chunk){
            //刷新自己的块和this块上边界的HexCell.chunk,否则在块的边界会出现问题
            chunk.Refresh();
            for (int i = 0; i < neighbors.Length; i++){
                HexCell neighbor = neighbors[i];
                if (neighbor != null && neighbor.chunk != chunk){
                    neighbor.chunk.Refresh();
                }
            }
        }
    }
    //仅刷新自己的块
    void RefreshSelfOnly()
    {
        chunk.Refresh();
    }
    /******************删除River方法*******************/
    //删除cell上出去的河流
    public void RemoveOutgoingRiver()
    {
        if (!hasOutgoingRiver)
        {
            return;
        }
        hasOutgoingRiver = false;
        RefreshSelfOnly();
        //把neighbor的入河去掉
        HexCell neighbor = GetNeighbor(outgoingRiver);
        neighbor.hasIncomingRiver = false;
        neighbor.RefreshSelfOnly();
    }
    //删除cell上的Incoming河流
    public void RemoveIncomingRiver()
    {
        if (!hasIncomingRiver)
        {
            return;
        }
        hasIncomingRiver = false;
        RefreshSelfOnly();
        //把neighbor的入河去掉
        HexCell neighbor = GetNeighbor(IncomingRiver);
        neighbor.hasOutgoingRiver = false;
        neighbor.RefreshSelfOnly();
    }
    //删除cell上的River
    public void RemoveRiver()
    {
        RemoveOutgoingRiver();
        RemoveIncomingRiver();
    }
    //验证河流是否有效
    void ValidateRivers()
    {
        if (
            hasOutgoingRiver &&
            !IsValidRiverDestination(GetNeighbor(outgoingRiver))
        )
        {
            RemoveOutgoingRiver();
        }
        if (
            hasIncomingRiver &&
            !GetNeighbor(incomingRiver).IsValidRiverDestination(this)
        )
        {
            RemoveIncomingRiver();
        }
    }
    /*
    * 添加river
    *删除thiscell上的别的方向的river出口，以及流入的river，添加direction出去的river
    *并且和direction方向上的neighbor接上，删除其本来的incomingRiver，添加新的incomingRiver
    */
    public void SetOutgoingRiver(HexDirection direction)
    {
        if (hasOutgoingRiver && outgoingRiver == direction){
            return;
        }
        HexCell neighbor = GetNeighbor(direction);
        //neighbor必须存在，而且高度低于this cell
        if (!IsValidRiverDestination(neighbor)){
            return;
        }
        RemoveOutgoingRiver();
        if (hasIncomingRiver && incomingRiver == direction){
            RemoveIncomingRiver();
        }
        hasOutgoingRiver = true;
        outgoingRiver = direction;
        specialIndex = 0;

        //和OutgoingRiver方向上的neighbor接上
        neighbor.RemoveIncomingRiver();
        neighbor.hasIncomingRiver = true;
        neighbor.incomingRiver = direction.Opposite();
        neighbor.specialIndex = 0;

        SetRoad((int)direction, false);
    }
    /*
     * save and load
     * 保存和载入单个cell的数据
     * 用于被HexGrid调用
     * 保存读取顺序要对应
     */
    public void Save(BinaryWriter writer)
    {
        //整型数据，0-255
        writer.Write((byte)terrainTypeIndex);
        writer.Write((byte)elevation);
        writer.Write((byte)waterLevel);
        writer.Write((byte)urbanLevel);
        writer.Write((byte)farmLevel);
        writer.Write((byte)plantLevel);
        writer.Write((byte)specialIndex);
        //bool和enum数据
        writer.Write(walled);
        //由于incomingRiver和outgoingRiver只需要用到3位，用一个byte的第八位表示是否有river
        //如果不存在river 则保存0
        if (hasIncomingRiver){
            writer.Write((byte)(incomingRiver + 128));
        }
        else{
            writer.Write((byte)0);
        }
        if (hasOutgoingRiver){
            writer.Write((byte)(outgoingRiver + 128));
        }
        else{
            writer.Write((byte)0);
        }
        //使用一个byte存储6个bool
        //0011111，后六位分别654321这6个方向
        int roadFlags = 0;
        for (int i = 0; i < roads.Length; i++){
            if (roads[i]){
                roadFlags |= 1 << i;
            }
        }
        writer.Write((byte)roadFlags);
    }

    public void Load(BinaryReader reader)
    {
        //整型数据
        terrainTypeIndex = reader.ReadByte();
        elevation = reader.ReadByte();
        //elevation 需要单独刷新
        //或者直接改变Elevation，但是会引起其他操作
        RefreshPosition();
        waterLevel = reader.ReadByte();
        urbanLevel = reader.ReadByte();
        farmLevel = reader.ReadByte();
        plantLevel = reader.ReadByte();
        specialIndex = reader.ReadByte();

        //bool和enum数据
        walled = reader.ReadBoolean();
        //由于incomingRiver和outgoingRiver只需要用到3位，用一个byte的第八位表示是否有river
        //如果不存在river 则保存0
        byte riverData = reader.ReadByte();
        if (riverData >= 128){
            hasIncomingRiver = true;
            incomingRiver = (HexDirection)(riverData - 128);
        }
        else{
            hasIncomingRiver = false;
        }
        riverData = reader.ReadByte();
        if (riverData >= 128){
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
    }
}
