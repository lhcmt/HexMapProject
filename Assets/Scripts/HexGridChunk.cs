using UnityEngine;
using UnityEngine.UI;
/*
 * 网格块的绘制管理
 */
public class HexGridChunk : MonoBehaviour
{
    HexCell[] cells;//块内的cell
    Canvas gridCanvas;//

    public HexMesh terrain, rivers, roads, water, waterShore, estuaries;//块网格绘制
    public HexFeatureManager features;
    /*
     * 2017/6/17添加
     * 替换cell.color，分别代表三角形三个顶点颜色
     * 修改地图贴图方案
     */
    static Color color1 = new Color(1f, 0f, 0f);//红
    static Color color2 = new Color(0f, 1f, 0f);//绿
    static Color color3 = new Color(0f, 0f, 1f);//蓝

    void Awake()
    {
        gridCanvas = GetComponentInChildren<Canvas>();
        //terrain = GetComponentInChildren<HexMesh>();

        cells = new HexCell[HexMetrics.chunkSizeX * HexMetrics.chunkSizeZ];
        ShowUI(false);
    }

    void LateUpdate()
    {
        //当前帧结束后刷新,不必再更改后马上刷新
        //enabled是unity Behaviours类的参数，为true时才会被Update
        Triangulate();
        enabled = false;
    }
    public void AddCell(int index, HexCell cell)
    {
        cells[index] = cell;
        cell.chunk = this;
        cell.transform.SetParent(transform, false);
        cell.uiRect.SetParent(gridCanvas.transform, false);
    }
    public void Refresh()
    {
        //需要被刷新，enabled是unity Behaviours类的参数，为true时才会被刷新
        enabled = true;
    }

    public void ShowUI(bool visible)
    {
        gridCanvas.gameObject.SetActive(visible);
    }

    /***********************网格块绘制方法********************************/
    public void Triangulate()
    {
        terrain.Clear();
        rivers.Clear();
        roads.Clear();
        water.Clear();
        waterShore.Clear();
        estuaries.Clear();
        features.Clear();
        for (int i = 0; i < cells.Length; i++)
        {
            //绘制cells[i]六边形
            Triangulate(cells[i]);
        }
        //复制List到数组到mesh
        terrain.Apply();
        rivers.Apply();
        roads.Apply();
        water.Apply();
        waterShore.Apply();
        estuaries.Apply();
        features.Apply();
    }
    /*
    * 根据相邻关系绘制三角形，右上角开始1-6
    *    6  1
    *   5    2
    *    4  3
    */
    void Triangulate(HexCell cell)
    {
        for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++)
        {
            Triangulate(d, cell);
        }
        if (!cell.IsUnderwater)
        {
            if (!cell.HasRiver && !cell.HasRoads)
            {
                features.AddFeature(cell, cell.Position);
            }
            if (cell.IsSpecial)
            {
                features.AddSpecialFeature(cell, cell.Position);
            }
        }
        
    }
    /*
     * 被Triangulate(HexCell cell)调用
     * 绘制cell 的direction方向的三角形
     */
    void Triangulate(HexDirection direction, HexCell cell)
    {
        Vector3 center = cell.Position;
        //内边
        EdgeVertices e = new EdgeVertices(
            center + HexMetrics.GetFirstSolidCorner(direction),
            center + HexMetrics.GetSecondSolidCorner(direction)
        );
        //根据是否有河流，不同中心区域的绘制方法
        if (cell.HasRiver)
        {
            if (cell.HasRiverThroughEdge(direction))
            {
                e.v3.y = cell.StreamBedY;
                if (cell.HasRiverBeginOrEnd)
                {
                    TriangulateWithRiverBeginOrEnd(direction, cell, center, e);
                }
                else
                {
                    TriangulateWithRiver(direction, cell, center, e);
                }
            }
            else
            {
                TriangulateAdjacentToRiver(direction, cell, center, e);
            }

        }
        else
        {
            //绘制有路的情况，以及中心的纯色三角形区域,
            TriangulateWithoutRiver(direction, cell, center, e);
            if (!cell.IsUnderwater && !cell.HasRoadThroughEdge(direction))
            {
                features.AddFeature(cell, (center + e.v1 + e.v5) * (1f / 3f));
            }
        }
        //为了减少重叠，只有3个方向需要绘制连接区域
        if (direction <= HexDirection.SE)
        {
            TriangulateConnection(direction, cell, e);
        }
        //如果cell上面存在water，绘制水体
        if(cell.IsUnderwater)
        {
            TriangulateWater(direction, cell, center);
        }
    }

    //六边形之间的连接区域
    void TriangulateConnection(HexDirection direction, HexCell cell, EdgeVertices e1)
    {
        HexCell neighbor = cell.GetNeighbor(direction);
        if (neighbor == null)
        {
            return;
        }
        //四边形外面2个顶点，bridge边
        Vector3 bridge = HexMetrics.GetBridge(direction);
        bridge.y = neighbor.Position.y - cell.Position.y;
        //四边形bridge外面边
        EdgeVertices e2 = new EdgeVertices(
            e1.v1 + bridge,
            e1.v5 + bridge
        );

        bool hasRiver = cell.HasRiverThroughEdge(direction);
        bool hasRoad = cell.HasRoadThroughEdge(direction);
        /*
         * 如果方向上存在河流，则绘制河道
         */
        if (hasRiver)
        {
            e2.v3.y = neighbor.StreamBedY;
            if (!cell.IsUnderwater) { 
                //river都不在水里
                if(!neighbor.IsUnderwater)
                {
                    TriangulateRiverQuad(
                        e1.v2, e1.v4, e2.v2, e2.v4,
                        cell.RiverSurfaceY, neighbor.RiverSurfaceY, 0.8f,
                        cell.HasIncomingRiver && cell.IncomingRiver == direction);
                }
                //river进入water,存在瀑布
                else if (cell.Elevation > neighbor.WaterLevel)
                {
                    TriangulateWaterfallInWater(
                        e1.v2, e1.v4, e2.v2, e2.v4,
                        cell.RiverSurfaceY, neighbor.RiverSurfaceY,
                        neighbor.WaterSurfaceY);
                }
            }
            //反过来，cell在水底，waterfall 方向向外
            else if (!neighbor.IsUnderwater &&neighbor.Elevation > cell.WaterLevel)
                {
                    TriangulateWaterfallInWater(
                        e2.v4, e2.v2, e1.v4, e1.v2,
                        neighbor.RiverSurfaceY, cell.RiverSurfaceY,
                        cell.WaterSurfaceY);
                }
        }

        /*
         * 根据不同连接边界类型
         * 插值方式绘制三角形的边界bridge    
         */
        if (cell.GetEdgeType(direction) == HexEdgeType.Slope)
        {
            TriangulateEdgeTerraces(e1, cell, e2, neighbor,hasRoad);
        }
        else//Flat, Cliff情况
        {
            TriangulateEdgeStrip(e1, color1, cell.TerrainTypeIndex,
                                e2, color2, neighbor.TerrainTypeIndex, hasRoad);
        }

        //在connection区域加上围墙
        features.AddWall(e1, cell, e2, neighbor,hasRiver,hasRoad);

        //被3个六边形共享的三角形区域
        HexCell nextNeighbor = cell.GetNeighbor(direction.Next());
        if (direction <= HexDirection.E && nextNeighbor != null)
        {
            //添加Y轴
            Vector3 v5 = e1.v5 + HexMetrics.GetBridge(direction.Next());
            v5.y = nextNeighbor.Position.y;
            //如果当前cell是最低的，
            if (cell.Elevation <= neighbor.Elevation)
            {
                if (cell.Elevation <= nextNeighbor.Elevation)
                {
                    TriangulateCorner(e1.v5, cell, e2.v5, neighbor, v5, nextNeighbor);
                }
                else
                {//内层判断失败，nextNeighbor最低,逆时针旋转三角形
                    TriangulateCorner(v5, nextNeighbor, e1.v5, cell, e2.v5, neighbor);
                }
            }//这么做是为了方便在TriangulateCorner方法中拓展
            else if (neighbor.Elevation <= nextNeighbor.Elevation)
            {
                TriangulateCorner(e2.v5, neighbor, v5, nextNeighbor, e1.v5, cell);
            }
            else
            {
                TriangulateCorner(v5, nextNeighbor, e1.v5, cell, e2.v5, neighbor);
            }

        }
    }

    //梯田绘制方法，参数为开始边，到结束边
    void TriangulateEdgeTerraces(
        EdgeVertices begin, HexCell beginCell,
        EdgeVertices end, HexCell endCell,
        bool hasRoad = false)
    {
        //step1插值
        EdgeVertices e2 = EdgeVertices.TerraceLerp(begin, end, 1);
        Color c2 = HexMetrics.TerraceLerp(color1, color2, 1);

        float t1 = beginCell.TerrainTypeIndex;
        float t2 = endCell.TerrainTypeIndex;

        TriangulateEdgeStrip(begin, color1, t1, e2, color2, t2, hasRoad);
        //2到4
        for (int i = 2; i < HexMetrics.terraceSteps; i++)
        {
            EdgeVertices e1 = e2;
            Color c1 = c2;
            e2 = EdgeVertices.TerraceLerp(begin, end, 2);
            c2 = HexMetrics.TerraceLerp(color1, color2, i);
            TriangulateEdgeStrip(e1, c1, t1, e2, c2 ,t2, hasRoad);
        }
        TriangulateEdgeStrip(e2,c2, t1, end,color2, t2, hasRoad);
    }
    /*
     * 绘制三角形的连接区域
     * 根据边界类型调用其他Corner绘制方法
     * 每一个三角形都连接3个cell
     * 
     * 颜色对应
     * red bottom vertex, 
     * green left vertex
     * blue  right vertex
     */
    void TriangulateCorner(
        Vector3 bottom, HexCell bottomCell,
        Vector3 left, HexCell leftCell,
        Vector3 right, HexCell rightCell
        ){
        HexEdgeType leftEdgeType = bottomCell.GetEdgeType(leftCell);
        HexEdgeType rightEdgeType = bottomCell.GetEdgeType(rightCell);
        if (leftEdgeType == HexEdgeType.Slope)
        {
            if (rightEdgeType == HexEdgeType.Slope)
            {   //slope-slope-flat,类型
                TriangulateCornerTerraces(
                    bottom, bottomCell, left, leftCell, right, rightCell);
            }
            //slope-flat-slope,类型（底-左-右）
            else if (rightEdgeType == HexEdgeType.Flat)
            {
                TriangulateCornerTerraces(
                    left, leftCell, right, rightCell, bottom, bottomCell);
            }
            //
            else TriangulateCornerTerracesCliff(
                    bottom, bottomCell, left, leftCell, right, rightCell);
        }
        else if (rightEdgeType == HexEdgeType.Slope)
        {
            if (leftEdgeType == HexEdgeType.Flat)
            {
                //flat-slope-slope,类型（底-左-右）
                TriangulateCornerTerraces(
                    right, rightCell, bottom, bottomCell, left, leftCell
                );
            }//The Mirror Cases镜像情况
            else TriangulateCornerCliffTerraces(
                bottom, bottomCell, left, leftCell, right, rightCell);
        }
        //Double Cliffs，双悬崖情况
        else if (leftCell.GetEdgeType(rightCell) == HexEdgeType.Slope)
        {
            if (leftCell.Elevation < rightCell.Elevation)
            {
                TriangulateCornerCliffTerraces(
                    right, rightCell, bottom, bottomCell, left, leftCell
                );
            }
            else
            {
                TriangulateCornerTerracesCliff(
                    left, leftCell, right, rightCell, bottom, bottomCell
                );
            }
        }
        else
        {

            terrain.AddTriangle(bottom, left, right);
            terrain.AddTriangleColor(color1, color2, color3);
            //添加地形贴图UV
            Vector3 types;
            types.x = bottomCell.TerrainTypeIndex;
            types.y = leftCell.TerrainTypeIndex;
            types.z = rightCell.TerrainTypeIndex;
            terrain.AddTriangleTerrainTypes(types);
        }

        features.AddWall(bottom, bottomCell, left, leftCell, right, rightCell);
    }
    /*
     * 三个平面都不在一个高度时候的三角形插值绘制
     */
    void TriangulateCornerTerracesCliff(
    Vector3 begin, HexCell beginCell,
    Vector3 left, HexCell leftCell,
    Vector3 right, HexCell rightCell)
    {
        float b = 1f / (rightCell.Elevation - beginCell.Elevation);
        //双悬崖情况
        if (b < 0)
        {
            b = -b;
        }
        Vector3 boundary = Vector3.Lerp(
            HexMetrics.Perturb(begin), HexMetrics.Perturb(right), b);
        Color boundaryColor = Color.Lerp(color1, color3, b);
        //地形贴图UV索引
        Vector3 types;
        types.x = beginCell.TerrainTypeIndex;
        types.y = leftCell.TerrainTypeIndex;
        types.z = rightCell.TerrainTypeIndex;

        TriangulateBoundaryTriangle(
            begin, color1, left, color2, boundary, boundaryColor,types
            );

        if (leftCell.GetEdgeType(rightCell) == HexEdgeType.Slope)
        {
            TriangulateBoundaryTriangle(
                left, color2, right, color3, boundary, boundaryColor, types
            );
        }
        else//如果剩下一半右边是悬崖
        {
            terrain.AddTriangleUnperturbed(
                HexMetrics.Perturb(left), HexMetrics.Perturb(right), boundary);
            terrain.AddTriangleColor(color2, color3, boundaryColor);
            terrain.AddTriangleTerrainTypes(types);
        }
    }
    /*
     * 上面一种方法的对称方法
     */
    void TriangulateCornerCliffTerraces(
    Vector3 begin, HexCell beginCell,
    Vector3 left, HexCell leftCell,
    Vector3 right, HexCell rightCell
)
    {
        float b = 1f / (leftCell.Elevation - beginCell.Elevation);
        //双悬崖情况
        if (b < 0)
        {
            b = -b;
        }
        //地形贴图UV索引
        Vector3 types;
        types.x = beginCell.TerrainTypeIndex;
        types.y = leftCell.TerrainTypeIndex;
        types.z = rightCell.TerrainTypeIndex;

        Vector3 boundary = Vector3.Lerp(HexMetrics.Perturb(begin), HexMetrics.Perturb(left), b);
        Color boundaryColor = Color.Lerp(color1, color2, b);

        TriangulateBoundaryTriangle(
            right, color3, begin, color1, boundary, boundaryColor, types
        );

        if (leftCell.GetEdgeType(rightCell) == HexEdgeType.Slope)
        {
            TriangulateBoundaryTriangle(
                left, color2, right, color3, boundary, boundaryColor, types
            );
        }
        else
        {
            terrain.AddTriangleUnperturbed(
                HexMetrics.Perturb(left), HexMetrics.Perturb(right), boundary);
            terrain.AddTriangleColor(color2, color3, boundaryColor);
            terrain.AddTriangleTerrainTypes(types);
        }
    }
    //add顶点索引，颜色，UV
    //
    void TriangulateBoundaryTriangle(
    Vector3 begin, Color beginColor,
    Vector3 left, Color leftColor,
    Vector3 boundary, Color boundaryColor,Vector3 types)
    {
        Vector3 v2 = HexMetrics.TerraceLerp(begin, left, 1);
        Color c2 = HexMetrics.TerraceLerp(beginColor, leftColor, 1);
        //R扰乱除了边界点boundary以外的点
        terrain.AddTriangleUnperturbed(HexMetrics.Perturb(begin), HexMetrics.Perturb(v2), boundary);
        terrain.AddTriangleColor(beginColor, c2, boundaryColor);
        terrain.AddTriangleTerrainTypes(types);
        for (int i = 2; i < HexMetrics.terraceSteps; i++)
        {
            Vector3 v1 = v2;
            Color c1 = c2;
            v2 = HexMetrics.TerraceLerp(begin, left, i);
            c2 = HexMetrics.TerraceLerp(beginColor, leftColor, i);
            terrain.AddTriangleUnperturbed(HexMetrics.Perturb(v1), HexMetrics.Perturb(v2), boundary);
            terrain.AddTriangleColor(c1, c2, boundaryColor);
            terrain.AddTriangleTerrainTypes(types);
        }
        terrain.AddTriangleUnperturbed(HexMetrics.Perturb(v2), HexMetrics.Perturb(left), boundary);
        terrain.AddTriangleColor(c2, leftColor, boundaryColor);
        terrain.AddTriangleTerrainTypes(types);
    }
    /*
     * 连接三角形  针对left right都是梯田的
     */
    void TriangulateCornerTerraces(
        Vector3 begin, HexCell beginCell,
        Vector3 left, HexCell leftCell,
        Vector3 right, HexCell rightCell)
    {
        //在slope-slope-flat类型中，和平台的插值TriangulateEdgeTerraces方法一样
        Vector3 v3 = HexMetrics.TerraceLerp(begin, left, 1);
        Vector3 v4 = HexMetrics.TerraceLerp(begin, right, 1);
        Color c3 = HexMetrics.TerraceLerp(color1, color2, 1);
        Color c4 = HexMetrics.TerraceLerp(color1, color3, 1);
        //添加地形贴图UV
        Vector3 types;
        types.x = beginCell.TerrainTypeIndex;
        types.y = leftCell.TerrainTypeIndex;
        types.z = rightCell.TerrainTypeIndex;
        
        terrain.AddTriangle(begin, v3, v4);
        terrain.AddTriangleColor(color1, c3, c4);
        terrain.AddTriangleTerrainTypes(types);

        for (int i = 2; i < HexMetrics.terraceSteps; i++)
        {
            Vector3 v1 = v3;
            Vector3 v2 = v4;
            Color c1 = c3;
            Color c2 = c4;
            v3 = HexMetrics.TerraceLerp(begin, left, i);
            v4 = HexMetrics.TerraceLerp(begin, right, i);
            c3 = HexMetrics.TerraceLerp(color1, color2, i);
            c4 = HexMetrics.TerraceLerp(color1, color3, i);
            terrain.AddQuad(v1, v2, v3, v4);
            terrain.AddQuadColor(c1, c2, c3, c4);
            terrain.AddQuadTerrainTypes(types);
        }
        terrain.AddQuad(v3, v4, left, right);
        terrain.AddQuadColor(c3, c4, color2, color3);
        terrain.AddQuadTerrainTypes(types);
    }

    /*
     *单独的三角形绘制方法，用于重新绘制中心的纯色六边形
     *参数为六边形中心center， 六边形边缘edge ，地形类型
     */
    void TriangulateEdgeFan(Vector3 center, EdgeVertices edge, float type)
    {
        terrain.AddTriangle(center, edge.v1, edge.v2);
        terrain.AddTriangle(center, edge.v2, edge.v3);
        terrain.AddTriangle(center, edge.v3, edge.v4);
        terrain.AddTriangle(center, edge.v4, edge.v5);

        terrain.AddTriangleColor(color1);
        terrain.AddTriangleColor(color1);
        terrain.AddTriangleColor(color1);
        terrain.AddTriangleColor(color1);

        Vector3 types;
        types.x = types.y = types.z = type;
        terrain.AddTriangleTerrainTypes(types);
        terrain.AddTriangleTerrainTypes(types);
        terrain.AddTriangleTerrainTypes(types);
        terrain.AddTriangleTerrainTypes(types);
    }
    /*
     * 绘制两个六边形的连接矩形 “bridge”
     * bridge 变成了4个矩形，条形
     */
    void TriangulateEdgeStrip(
        EdgeVertices e1, Color c1,float type1, 
        EdgeVertices e2, Color c2,float type2,
        bool hasRoad = false)
    {
        terrain.AddQuad(e1.v1, e1.v2, e2.v1, e2.v2);        
        terrain.AddQuad(e1.v2, e1.v3, e2.v2, e2.v3);        
        terrain.AddQuad(e1.v3, e1.v4, e2.v3, e2.v4);  
        terrain.AddQuad(e1.v4, e1.v5, e2.v4, e2.v5);

        terrain.AddQuadColor(c1, c2);
        terrain.AddQuadColor(c1, c2);
        terrain.AddQuadColor(c1, c2);
        terrain.AddQuadColor(c1, c2);

        Vector3 types;
        types.x = types.z = type1;
        types.y = type2;
        terrain.AddQuadTerrainTypes(types);
        terrain.AddQuadTerrainTypes(types);
        terrain.AddQuadTerrainTypes(types);
        terrain.AddQuadTerrainTypes(types);

        if (hasRoad)
        {
            TriangulateRoadSegment(e1.v2, e1.v3, e1.v4, e2.v2, e2.v3, e2.v4);
        }
    }
    //绘制河道贯穿的整个cell的情况
    void TriangulateWithRiver(
    HexDirection direction, HexCell cell, Vector3 center, EdgeVertices e)
    {
        Vector3 centerL, centerR;
        if (cell.HasRiverThroughEdge(direction.Opposite()))
        {
            //河流中心的左右岸，*0.25是因为边界是4个矩形，河流占0.5
            centerL = center +
                HexMetrics.GetFirstSolidCorner(direction.Previous()) * 0.25f;
            centerR = center +
                HexMetrics.GetSecondSolidCorner(direction.Next()) * 0.25f;
        }
        //当cell河流存在转角的时候
        else if (cell.HasRiverThroughEdge(direction.Previous()))
        {
            centerR = center;
            centerL = Vector3.Lerp(center, e.v1, 2f / 3f);
        }
        else if (cell.HasRiverThroughEdge(direction.Next()))
        {
            centerL = center;
            centerR = Vector3.Lerp(center, e.v5, 2f / 3f);
        }
        else if (cell.HasRiverThroughEdge(direction.Next2()))
        {
            centerL = center;
            centerR = center +
                HexMetrics.GetSolidEdgeMiddle(direction.Next()) * (0.5f * HexMetrics.innerToOuter);
        }
        else
        {
            centerL = center +
                HexMetrics.GetSolidEdgeMiddle(direction.Previous()) * (0.5f * HexMetrics.innerToOuter);
            centerR = center;
        }
        center = Vector3.Lerp(centerL, centerR, 0.5f);

        EdgeVertices m = new EdgeVertices(Vector3.Lerp(centerL, e.v1, 0.5f),
            Vector3.Lerp(centerR, e.v5, 0.5f), 1f / 6f);
        m.v3.y = center.y = e.v3.y;
        //绘制上半个梯形
        TriangulateEdgeStrip(m, color1,cell.TerrainTypeIndex, 
                            e, color1,cell.TerrainTypeIndex);
        //添加三角形面片
        terrain.AddTriangle(centerL, m.v1, m.v2);
        terrain.AddQuad(centerL, center, m.v2, m.v3);
        terrain.AddQuad(center, centerR, m.v3, m.v4);
        terrain.AddTriangle(centerR, m.v4, m.v5);
        //顺序添加顶点颜色
        terrain.AddTriangleColor(color1);
        terrain.AddQuadColor(color1);
        terrain.AddQuadColor(color1);
        terrain.AddTriangleColor(color1);

        //地形贴图索引
        Vector3 types;
        types.x = types.y = types.z = cell.TerrainTypeIndex;
        terrain.AddTriangleTerrainTypes(types);
        terrain.AddQuadTerrainTypes(types);
        terrain.AddQuadTerrainTypes(types);
        terrain.AddTriangleTerrainTypes(types);

        if (!cell.IsUnderwater)
        {
            //添加水面
            bool reversed = cell.IncomingRiver == direction;
            TriangulateRiverQuad(centerL, centerR, m.v2, m.v4, cell.RiverSurfaceY, 0.4f, reversed);
            TriangulateRiverQuad(m.v2, m.v4, e.v2, e.v4, cell.RiverSurfaceY, 0.6f, reversed);
        }
    }

    void TriangulateWithRiverBeginOrEnd(HexDirection direction, HexCell cell,
            Vector3 center, EdgeVertices e)
    {
        EdgeVertices m = new EdgeVertices(
            Vector3.Lerp(center, e.v1, 0.5f),
            Vector3.Lerp(center, e.v5, 0.5f));
        m.v3.y = e.v3.y;
        TriangulateEdgeStrip(m, color1, cell.TerrainTypeIndex, 
                            e, color1,cell.TerrainTypeIndex);
        TriangulateEdgeFan(center, m, cell.TerrainTypeIndex);
        if (!cell.IsUnderwater)
        {
            //添加起止水面的前半部分
            bool reversed = cell.HasIncomingRiver;
            TriangulateRiverQuad(m.v2, m.v4, e.v2, e.v4, cell.RiverSurfaceY, 0.6f, reversed);
            //河水的头尾前半部分
            center.y = m.v2.y = m.v4.y = cell.RiverSurfaceY;
            rivers.AddTriangle(center, m.v2, m.v4);
            if (reversed)
            {
                rivers.AddTriangleUV(
                    new Vector2(0.5f, 0.4f), new Vector2(1f, 0.2f), new Vector2(0f, 0.2f)
                );
            }
            else
            {
                rivers.AddTriangleUV(
                    new Vector2(0.5f, 0.4f), new Vector2(0f, 0.6f), new Vector2(1f, 0.6f)
                );
            }
        }
    }
    //其余没有河道的方向
    void TriangulateAdjacentToRiver(
        HexDirection direction, HexCell cell, Vector3 center, EdgeVertices e
    )
    {
        if (cell.HasRoads)
        {
            TriangulateRoadAdjacentToRiver(direction, cell, center, e);
        }
        if (cell.HasRiverThroughEdge(direction.Next()))
        {
            if (cell.HasRiverThroughEdge(direction.Previous()))
            { //120度河道
                center += HexMetrics.GetSolidEdgeMiddle(direction) *
                    (HexMetrics.innerToOuter * 0.5f);
            }
            else if (//180度河道
                cell.HasRiverThroughEdge(direction.Previous2())
            )
            {
                center += HexMetrics.GetFirstSolidCorner(direction) * 0.25f;
            }
        }
        else if (
            cell.HasRiverThroughEdge(direction.Previous()) &&
            cell.HasRiverThroughEdge(direction.Next2())
        )
        {
            center += HexMetrics.GetSecondSolidCorner(direction) * 0.25f;
        }

        EdgeVertices m = new EdgeVertices(
            Vector3.Lerp(center, e.v1, 0.5f),
            Vector3.Lerp(center, e.v5, 0.5f)
        );

        TriangulateEdgeStrip(m, color1,cell.TerrainTypeIndex, 
                            e, color1,cell.TerrainTypeIndex);
        TriangulateEdgeFan(center, m, cell.TerrainTypeIndex);
        //如果存在feature，绘制
        if (!cell.IsUnderwater && !cell.HasRoadThroughEdge(direction))
        {
            features.AddFeature(cell,(center + e.v1 + e.v5) * (1f / 3f));
        }
    }
    /***********************水体****************************/
    void TriangulateRiverQuad(Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4,
        float y1, float y2, float v, bool reversed)
    {
        v1.y = v2.y = y1;
        v3.y = v4.y = y2;
        rivers.AddQuad(v1, v2, v3, v4);
        if(reversed)
        {
            rivers.AddQuadUV(1f, 0f, 0.8f-v, 0.6f-v);
        }
        else
        rivers.AddQuadUV(0f, 1f, v, v+0.2f);
    }
    //重载，高度Y1Y2相同
    void TriangulateRiverQuad(
    Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4,
    float y, float v, bool reversed)
    {
        TriangulateRiverQuad(v1, v2, v3, v4, y, y,v, reversed);
    }

    /*************************** road **************************************/
    //绘制路面区域，贴图坐标
    //添加顶点，索引，UV坐标
    void TriangulateRoadSegment (
		Vector3 v1, Vector3 v2, Vector3 v3,
		Vector3 v4, Vector3 v5, Vector3 v6
	) {
		roads.AddQuad(v1, v2, v4, v5);
		roads.AddQuad(v2, v3, v5, v6);
        roads.AddQuadUV(0f, 1f, 0f, 0f);
        roads.AddQuadUV(1f, 0f, 0f, 0f);
	}
    //
    void TriangulateRoad(
    Vector3 center, Vector3 mL, Vector3 mR, EdgeVertices e, 
        bool hasRoadThroughCellEdge)
    {
        if (hasRoadThroughCellEdge)
        {
            //靠近边界的矩形
            Vector3 mC = Vector3.Lerp(mL, mR, 0.5f);
            TriangulateRoadSegment(mL, mC, mR, e.v2, e.v3, e.v4);
            //靠近中心的三角形
            roads.AddTriangle(center, mL, mC);
            roads.AddTriangle(center, mC, mR);
            roads.AddTriangleUV(
                new Vector2(1f, 0f), new Vector2(0f, 0f), new Vector2(1f, 0f));
            roads.AddTriangleUV(
                new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(0f, 0f));
        }
        else
        {
            TriangulateRoadEdge(center, mL, mR);
        }
    }
    //绘制没有河流的情况
    void TriangulateWithoutRiver(
    HexDirection direction, HexCell cell, Vector3 center, EdgeVertices e)
    {
        //绘制中心的纯色三角形区域,
        TriangulateEdgeFan(center, e, cell.TerrainTypeIndex);
        //如果存在路面 ，覆盖上一层路
        if (cell.HasRoads)
        {
            Vector2 interpolators = GetRoadInterpolators(direction, cell);
            TriangulateRoad(
                center,
                Vector3.Lerp(center, e.v1, interpolators.x),
                Vector3.Lerp(center, e.v5, interpolators.y),
                e, 
                cell.HasRoadThroughEdge(direction)
            );
        }
    }
    //添加路的边界
    void TriangulateRoadEdge(Vector3 center, Vector3 mL, Vector3 mR)
    {
        roads.AddTriangle(center, mL, mR);
        roads.AddTriangleUV(
            new Vector2(1f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f)
        );
    }
    //让路变得光滑，某一方向没有路的，则edge变窄
    Vector2 GetRoadInterpolators(HexDirection direction, HexCell cell)
    {
        Vector2 interpolators;
        if (cell.HasRoadThroughEdge(direction))
        {
            interpolators.x = interpolators.y = 0.5f;
        }
        else
        {
            interpolators.x =
                cell.HasRoadThroughEdge(direction.Previous()) ? 0.5f : 0.25f;
            interpolators.y =
                cell.HasRoadThroughEdge(direction.Next()) ? 0.5f : 0.25f;
        }
        return interpolators;
    }
    //同时有road经过river
    void TriangulateRoadAdjacentToRiver(
    HexDirection direction, HexCell cell, Vector3 center, EdgeVertices e)
    {
        bool hasRoadThroughEdge = cell.HasRoadThroughEdge(direction);
        bool previousHasRiver = cell.HasRiverThroughEdge(direction.Previous());
        bool nextHasRiver = cell.HasRiverThroughEdge(direction.Next());
        Vector2 interpolators = GetRoadInterpolators(direction, cell);
        Vector3 roadCenter = center;

        if (cell.HasRiverBeginOrEnd)
        {
            roadCenter += HexMetrics.GetSolidEdgeMiddle(
                cell.RiverBeginOrEndDirection.Opposite()
            ) * (1f / 3f);
        }
        //road横跨直线河道
        else if (cell.IncomingRiver == cell.OutgoingRiver.Opposite())
        {
            Vector3 corner;
            if (previousHasRiver){
                if (
                    !hasRoadThroughEdge &&
                    !cell.HasRoadThroughEdge(direction.Next())
                   ){
                    return;
                    }
                corner = HexMetrics.GetSecondSolidCorner(direction);
            }
            else{
                if (
                    !hasRoadThroughEdge &&
                    !cell.HasRoadThroughEdge(direction.Previous())
                   ){
                    return;
                    }
                corner = HexMetrics.GetFirstSolidCorner(direction);
                }

            roadCenter += corner * 0.5f;
            if (cell.IncomingRiver == direction.Next() && (
                cell.HasRoadThroughEdge(direction.Next2()) ||
                cell.HasRoadThroughEdge(direction.Opposite())
            ))
            {
                features.AddBridge(roadCenter, center - corner * 0.5f);
            }
            center += corner * 0.25f;
        }
        //河流一折角
        else if (cell.IncomingRiver == cell.OutgoingRiver.Previous())
        {
            roadCenter -= HexMetrics.GetSecondCorner(cell.IncomingRiver) * 0.2f;
        }
        else if (cell.IncomingRiver == cell.OutgoingRiver.Next())
        {
            roadCenter -= HexMetrics.GetFirstCorner(cell.IncomingRiver) * 0.2f;
        }
        //河流二折角的情况
        else if (previousHasRiver && nextHasRiver)
        {
            if (!hasRoadThroughEdge)
            {
                return;
            }
            Vector3 offset = HexMetrics.GetSolidEdgeMiddle(direction) *
                HexMetrics.innerToOuter;
            roadCenter += offset * 0.7f;
            center += offset * 0.5f;
        }
        else
        {
            //河流转角的中间方向
            HexDirection middle;
            if (previousHasRiver)
            {
                middle = direction.Next();
            }
            else if (nextHasRiver)
            {
                middle = direction.Previous();
            }
            else
            {
                middle = direction;
            }

            if (
                !cell.HasRoadThroughEdge(middle) &&
                !cell.HasRoadThroughEdge(middle.Previous()) &&
                !cell.HasRoadThroughEdge(middle.Next())
                ){
                return;
            }
            Vector3 offest = HexMetrics.GetSolidEdgeMiddle(middle);
            roadCenter += offest * 0.25f;
            //架桥
            if(
                direction == middle&&
                cell.HasRoadThroughEdge(direction.Opposite())
            ){
                features.AddBridge(roadCenter, 
                    center - offest * (HexMetrics.innerToOuter * 0.7f));
            }
        }
        Vector3 mL = Vector3.Lerp(roadCenter, e.v1, interpolators.x);
        Vector3 mR = Vector3.Lerp(roadCenter, e.v5, interpolators.y);

        TriangulateRoad(roadCenter, mL, mR, e, hasRoadThroughEdge);
        if (previousHasRiver)
        {
            TriangulateRoadEdge(roadCenter, center, mL);
        }
        if (nextHasRiver)
        {
            TriangulateRoadEdge(roadCenter, mR, center);
        }

    }
    //cell上的water水体的绘制方法
    void TriangulateWater(
    HexDirection direction, HexCell cell, Vector3 center)
    {

        center.y = cell.WaterSurfaceY;
        HexCell neighbor = cell.GetNeighbor(direction);
        //分别针对岸边的水体和开放水体，进行绘制
        if (neighbor != null && !neighbor.IsUnderwater)
        {
            TriangulateWaterShore(direction, cell, neighbor, center);
        }
        else
        {
            TriangulateOpenWater(direction, cell, neighbor, center);
        }
    }
    void TriangulateOpenWater(
        HexDirection direction,HexCell cell,HexCell neighbor,Vector3 center)
    {
        Vector3 c1 = center + HexMetrics.GetFirstWaterCorner(direction);
        Vector3 c2 = center + HexMetrics.GetSecondWaterCorner(direction);
        //注意，这是cell的六分之一
        water.AddTriangle(center, c1, c2);
        //水体这一方向上的连接区域,只画右半边 NE E SE
        if (direction <= HexDirection.SE && neighbor != null)
        {
            Vector3 bridge = HexMetrics.GetWaterBridge(direction);
            Vector3 e1 = c1 + bridge;
            Vector3 e2 = c2 + bridge;
            water.AddQuad(c1, c2, e1, e2);
            //connect三角形
            if (direction <= HexDirection.E)
            {
                HexCell nextNeighbor = cell.GetNeighbor(direction.Next());
                if (nextNeighbor == null || !nextNeighbor.IsUnderwater)
                {
                    return;
                }
                water.AddTriangle(
                    c2, e2, c2 + HexMetrics.GetWaterBridge(direction.Next())
                );
            }
        }
    }
    //水体的边缘
    void TriangulateWaterShore(
    HexDirection direction, HexCell cell, HexCell neighbor, Vector3 center)
    {
        EdgeVertices e1 = new EdgeVertices(
            center + HexMetrics.GetFirstWaterCorner(direction),
            center + HexMetrics.GetSecondWaterCorner(direction));
        water.AddTriangle(center, e1.v1, e1.v2);
        water.AddTriangle(center, e1.v2, e1.v3);
        water.AddTriangle(center, e1.v3, e1.v4);
        water.AddTriangle(center, e1.v4, e1.v5);
        //add bridge

        //Vector3 bridge = HexMetrics.GetWaterBridge(direction);
        Vector3 center2 = neighbor.Position;
        center2.y = center.y;
        EdgeVertices e2 = new EdgeVertices(
            center2 + HexMetrics.GetSecondSolidCorner(direction.Opposite()),
			center2 + HexMetrics.GetFirstSolidCorner(direction.Opposite())
            );
        //河岸direction方向有river进
        if(cell.HasRiverThroughEdge(direction))
        {
            TriangulateEstuary(e1, e2,cell.IncomingRiver == direction);
        }
        //如果没有river进入水体
        else
        {
            waterShore.AddQuad(e1.v1, e1.v2, e2.v1, e2.v2);
            waterShore.AddQuad(e1.v2, e1.v3, e2.v2, e2.v3);
            waterShore.AddQuad(e1.v3, e1.v4, e2.v3, e2.v4);
            waterShore.AddQuad(e1.v4, e1.v5, e2.v4, e2.v5);
            waterShore.AddQuadUV(0f, 0f, 0f, 1f);
            waterShore.AddQuadUV(0f, 0f, 0f, 1f);
            waterShore.AddQuadUV(0f, 0f, 0f, 1f);
            waterShore.AddQuadUV(0f, 0f, 0f, 1f);
        }
        //add triangle
        HexCell nextNeighbor = cell.GetNeighbor(direction.Next());

        if (nextNeighbor != null)
        {

            Vector3 v3 = nextNeighbor.Position + (nextNeighbor.IsUnderwater ?
                HexMetrics.GetFirstWaterCorner(direction.Previous()) :
                HexMetrics.GetFirstSolidCorner(direction.Previous()));
            v3.y = center.y;

			waterShore.AddTriangle(e1.v5, e2.v5, v3);
            waterShore.AddTriangleUV(
                new Vector2(0f, 0f),
                new Vector2(0f, 1f),
                new Vector2(0f, nextNeighbor.IsUnderwater ? 0f : 1f));
        }
    }
    //河流进入水体，v1,v2高处，v3v4低处
    void TriangulateWaterfallInWater(
    Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4,
    float y1, float y2, float waterY)
    {
        v1.y = v2.y = y1;
        v3.y = v4.y = y2;
        //水进入水面就消失
        v1 = HexMetrics.Perturb(v1);
        v2 = HexMetrics.Perturb(v2);
        v3 = HexMetrics.Perturb(v3);
        v4 = HexMetrics.Perturb(v4);
        float t = (waterY - y2) / (y1 - y2);
        v3 = Vector3.Lerp(v3, v1, t);
        v4 = Vector3.Lerp(v4, v2, t);

        rivers.AddQuadUnperturbed(v1, v2, v3, v4);
        rivers.AddQuadUV(0f, 1f, 0.8f, 1f);
    }
    //瀑布Triangulate,第三个参数为方向
    void TriangulateEstuary(EdgeVertices e1,EdgeVertices e2, bool incomingRiver)
    {
        //两侧的三角形
        waterShore.AddTriangle(e2.v1, e1.v2, e1.v1);
        waterShore.AddTriangle(e2.v5, e1.v5, e1.v4);
        waterShore.AddTriangleUV(
            new Vector2(0f, 1f), new Vector2(0f, 0f), new Vector2(0f, 0f)
        );
        waterShore.AddTriangleUV(
            new Vector2(0f, 1f), new Vector2(0f, 0f), new Vector2(0f, 0f)
        );
        //中间瀑布区域
        estuaries.AddQuad(e2.v1, e1.v2, e2.v2, e1.v3);
        estuaries.AddTriangle(e1.v3, e2.v2, e2.v4);
        estuaries.AddQuad(e1.v3, e1.v4, e2.v4, e2.v5);

        estuaries.AddQuadUV(
            new Vector2(0f, 1f), new Vector2(0f, 0f),
            new Vector2(1f, 1f), new Vector2(0f, 0f));
        estuaries.AddTriangleUV(
            new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(1f, 1f)
        );
        estuaries.AddQuadUV(
            new Vector2(0f, 0f), new Vector2(0f, 0f),
            new Vector2(1f, 1f), new Vector2(0f, 1f));
        //通道二的UV坐标，uv坐标会按照mesh中顶点添加的数组匹配
        if (incomingRiver)
        {
            estuaries.AddQuadUV2(
                new Vector2(1.5f, 1f), new Vector2(0.7f, 1.15f),
                new Vector2(1f, 0.8f), new Vector2(0.5f, 1.1f));
            estuaries.AddTriangleUV2(
                new Vector2(0.5f, 1.1f),
                new Vector2(1f, 0.8f),
                new Vector2(0f, 0.8f)
            );
            estuaries.AddQuadUV2(
                new Vector2(0.5f, 1.1f), new Vector2(0.3f, 1.15f),
                new Vector2(0f, 0.8f), new Vector2(-0.5f, 1f)
            );
        }
        else//水从湖泊流出
        {
            estuaries.AddQuadUV2(
                new Vector2(-0.5f, -0.2f), new Vector2(0.3f, -0.35f),
                new Vector2(0f, 0f), new Vector2(0.5f, -0.3f));
            estuaries.AddTriangleUV2(
                new Vector2(0.5f, -0.3f),
                new Vector2(0f, 0f),
                new Vector2(1f, 0f)
            );
            estuaries.AddQuadUV2(
                new Vector2(0.5f, -0.3f), new Vector2(0.7f, -0.35f),
                new Vector2(1f, 0f), new Vector2(1.5f, -0.2f)
            );
        }

    }
}
