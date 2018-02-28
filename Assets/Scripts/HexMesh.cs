using UnityEngine;
using System.Collections.Generic;
using System;
//需要含有组件MeshFilter和MeshRenderer
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
//不再特定用于渲染整个六边形网格地图，通用于陆地河道等所有网格，其他代码搬运到特定类中去
public class HexMesh : MonoBehaviour {

    Mesh hexMesh;
    //由于会有多个HexMesh进行绘制，river gird等
    //使用ListPool
    [NonSerialized] List<Vector3> vertices;//顶点
    [NonSerialized]
    List<Vector3> terrainTypes;//顶点贴图采样索引,X Y Z分别代表3个顶点的贴图类型索引
    [NonSerialized] List<int> triangles;//三角形
    [NonSerialized] List<Color> colors;

    [NonSerialized] List<Vector2> uvs,uv2s;//纹理坐标，for water
    //添加一个网格碰撞器
    MeshCollider meshCollider;
    //unity 支持4个UV坐标，分别向通道0，1添加UV坐标
    public bool useCollider, useColors, useUVCoordinates, useUV2Coordinates;
    //是否使用地形贴图,通道2
	public bool useTerrainTypes;

	void Awake() {
	    GetComponent<MeshFilter>().mesh = hexMesh = new Mesh();
        //获取网格碰撞器组件
        if(useCollider)
        {
            meshCollider = gameObject.AddComponent<MeshCollider>();
        }
		hexMesh.name = "Hex Mesh";
	}

    //给3个顶点着色颜色
    public void AddTriangleColor(Color color)
    {
        colors.Add(color);
        colors.Add(color);
        colors.Add(color);
    }
    //着不同颜色，c1中心点颜色
    public void AddTriangleColor(Color c1, Color c2, Color c3)
    {
        colors.Add(c1);
        colors.Add(c2);
        colors.Add(c3);
    }
    //添加三角形顶点和，索引
    public void AddTriangle(Vector3 v1, Vector3 v2, Vector3 v3)
    {
        int vertexIndex = vertices.Count;
        //扰动后的顶点
        vertices.Add(HexMetrics.Perturb(v1));
        vertices.Add(HexMetrics.Perturb(v2));
        vertices.Add(HexMetrics.Perturb(v3));
        triangles.Add(vertexIndex);
        triangles.Add(vertexIndex + 1);
        triangles.Add(vertexIndex + 2);
    }
    //不被扰动的顶点
    public void AddTriangleUnperturbed(Vector3 v1, Vector3 v2, Vector3 v3)
    {
        int vertexIndex = vertices.Count;
        vertices.Add(v1);
        vertices.Add(v2);
        vertices.Add(v3);
        triangles.Add(vertexIndex);
        triangles.Add(vertexIndex + 1);
        triangles.Add(vertexIndex + 2);
    }
    //添加四边形，边界框，每个三角形的外面一个梯形
    public void AddQuad(Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4)
    {
        int vertexIndex = vertices.Count;
        vertices.Add(HexMetrics.Perturb(v1));
        vertices.Add(HexMetrics.Perturb(v2));
        vertices.Add(HexMetrics.Perturb(v3));
        vertices.Add(HexMetrics.Perturb(v4));
        //顶点索引
        triangles.Add(vertexIndex);
        triangles.Add(vertexIndex + 2);
        triangles.Add(vertexIndex + 1);
        triangles.Add(vertexIndex + 1);
        triangles.Add(vertexIndex + 2);
        triangles.Add(vertexIndex + 3);
    }
    //未扰动的AddQuad方法
    public void AddQuadUnperturbed(Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4)
    {
        int vertexIndex = vertices.Count;
        vertices.Add(v1);
        vertices.Add(v2);
        vertices.Add(v3);
        vertices.Add(v4);
        //顶点索引
        triangles.Add(vertexIndex);     //V1
        triangles.Add(vertexIndex + 2); //V3
        triangles.Add(vertexIndex + 1); //V2
        triangles.Add(vertexIndex + 1); //V2
        triangles.Add(vertexIndex + 2); //V3
        triangles.Add(vertexIndex + 3); //V4
    }
    //四边形4个顶点着色
    public void AddQuadColor(Color c1, Color c2, Color c3, Color c4)
    {
        colors.Add(c1);
        colors.Add(c2);
        colors.Add(c3);
        colors.Add(c4);
    }
    public void AddQuadColor(Color c1, Color c2)
    {
        colors.Add(c1);
        colors.Add(c1);
        colors.Add(c2);
        colors.Add(c2);
    }
    public void AddQuadColor(Color color)
    {
        colors.Add(color);
        colors.Add(color);
        colors.Add(color);
        colors.Add(color);
    }
    public void AddTriangleUV(Vector2 uv1, Vector2 uv2, Vector3 uv3)
    {
        uvs.Add(uv1);
        uvs.Add(uv2);
        uvs.Add(uv3);
    }

    public void AddQuadUV(Vector2 uv1, Vector2 uv2, Vector3 uv3, Vector3 uv4)
    {
        uvs.Add(uv1);
        uvs.Add(uv2);
        uvs.Add(uv3);
        uvs.Add(uv4);
    }
    public void AddQuadUV(float uMin, float uMax, float vMin, float vMax)
    {
        uvs.Add(new Vector2(uMin, vMin));//左下
        uvs.Add(new Vector2(uMax, vMin));//右下
        uvs.Add(new Vector2(uMin, vMax));
        uvs.Add(new Vector2(uMax, vMax));
    }
    //第二通道添加方法
    public void AddTriangleUV2(Vector2 uv1, Vector2 uv2, Vector3 uv3)
    {
        uv2s.Add(uv1);
        uv2s.Add(uv2);
        uv2s.Add(uv3);
    }

    public void AddQuadUV2(Vector2 uv1, Vector2 uv2, Vector3 uv3, Vector3 uv4)
    {
        uv2s.Add(uv1);
        uv2s.Add(uv2);
        uv2s.Add(uv3);
        uv2s.Add(uv4);
    }
    public void AddQuadUV2(float uMin, float uMax, float vMin, float vMax)
    {
        uv2s.Add(new Vector2(uMin, vMin));//左下
        uv2s.Add(new Vector2(uMax, vMin));//右下
        uv2s.Add(new Vector2(uMin, vMax));
        uv2s.Add(new Vector2(uMax, vMax));
    }

    /*
     * 添加三角形顶点的地形贴图索引
     */
    public void AddTriangleTerrainTypes(Vector3 types)
    {
        terrainTypes.Add(types);
        terrainTypes.Add(types);
        terrainTypes.Add(types);
    }
    /*
     * 添加四边形顶点的地形贴图索引
    */
    public void AddQuadTerrainTypes(Vector3 types)
    {
        terrainTypes.Add(types);
        terrainTypes.Add(types);
        terrainTypes.Add(types);
        terrainTypes.Add(types);
    }

    //清除列表中之前内容
    public void Clear()
    {
        hexMesh.Clear();
        vertices = ListPool<Vector3>.Get();
        if (useColors){
            colors = ListPool<Color>.Get();
        }
        if(useUVCoordinates){
            uvs = ListPool<Vector2>.Get();
        }
        if (useUV2Coordinates)
        {
            uv2s = ListPool<Vector2>.Get();
        }
        if (useTerrainTypes)
        {
            terrainTypes = ListPool<Vector3>.Get();
        }
        triangles = ListPool<int>.Get();
    }

    public void Apply()
    {
        hexMesh.SetVertices(vertices);
        ListPool<Vector3>.Add(vertices);//回收List到pool中，下同
        if(useColors)
        {
            hexMesh.SetColors(colors);
            ListPool<Color>.Add(colors);
        }
        hexMesh.SetTriangles(triangles, 0);
        ListPool<int>.Add(triangles);
        if (useUVCoordinates)
        {
            hexMesh.SetUVs(0, uvs);
            ListPool<Vector2>.Add(uvs);
        }
        if (useUV2Coordinates)
        {
            hexMesh.SetUVs(1, uv2s);
            ListPool<Vector2>.Add(uv2s);
        }

        if (useTerrainTypes)
        {
            hexMesh.SetUVs(2, terrainTypes);
            ListPool<Vector3>.Add(terrainTypes);
        }
        hexMesh.RecalculateNormals();
        if(useCollider)
            meshCollider.sharedMesh = hexMesh;
    }
  
}
