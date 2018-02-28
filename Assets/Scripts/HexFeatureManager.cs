﻿using UnityEngine;
using System.Collections;
/*
 * 负责单个块的Feature管理
 * 类似于HexMesh，拥有Clear，Apply等方法
 */
public class HexFeatureManager : MonoBehaviour {
    
    //wall使用mesh添加 
    public HexMesh walls;
    //使用prefeb的方式添加
    public HexFeatureCollection[] 
        urbanCollections, farmCollections , plantCollections;

    public Transform wallTower,bridge;
    //场景特殊物体预制体
    public Transform[] special;
    //feature容器
    Transform container;

    public void Clear(){
        if (container)
        {
            Destroy(container.gameObject);
        }
        container = new GameObject("Feature Container").transform;
        container.SetParent(transform, false);
        walls.Clear();
    }

    public void Apply() {
        walls.Apply();
    }

    public void AddFeature(HexCell cell,Vector3 position) 
    {
        if (cell.IsSpecial)
        {
            return;
        }
        HexHash hash = HexMetrics.SampleHashGrid(position);

        Transform prefab = PickPrefab(urbanCollections,cell.UrbanLevel, hash.a,hash.d);

        Transform otherPrefab = PickPrefab(
          farmCollections, cell.FarmLevel, hash.b, hash.d);
        //如果同时选取2种以上类型的地表物体，取hash值小的
        float usedHash = hash.a;
        if (prefab){
            if (otherPrefab && hash.b < hash.a){
                prefab = otherPrefab;
                usedHash = hash.b;
            }
        }
        else if (otherPrefab){
            prefab = otherPrefab;
            usedHash = hash.b;
        }

        otherPrefab = PickPrefab(
            plantCollections, cell.PlantLevel, hash.c, hash.d
        );
        if (prefab){
            if (otherPrefab && hash.c < usedHash){
                prefab = otherPrefab;
            }
        }
        else if (otherPrefab){
            prefab = otherPrefab;
        }
        else{
            return;
        }

        Transform instance = Instantiate(prefab);

        //position.y += instance.localScale.y * 0.5f;

        instance.localPosition = HexMetrics.Perturb(position);
        instance.localRotation = Quaternion.Euler(0f,360f * hash.e,0f);
        instance.SetParent(container, false);
    }

    Transform PickPrefab(
        HexFeatureCollection[] collection, 
        int level, float hash, float choice)
    {
        if (level > 0)
        {
            float[] thresholds = HexMetrics.GetFeatureThresholds(level - 1);
            for (int i = 0; i < thresholds.Length; i++)
            {
                if (hash < thresholds[i])
                {
                    return collection[i].Pick(choice);
                }
            }
        }
        return null;
    }
    /*
     *被HexGridChunk调用
     */
    public void AddWall(
    EdgeVertices near, HexCell nearCell,
    EdgeVertices far, HexCell farCell,
    bool hasRiver, bool hasRoad 
    ){
        if(nearCell.Walled != farCell.Walled
            && !nearCell.IsUnderwater && !farCell.IsUnderwater
            && nearCell.GetEdgeType(farCell) != HexEdgeType.Cliff
        ){
            AddWallSegment(near.v1, far.v1, near.v2, far.v2);
            if(hasRiver ||hasRoad)
            {
                AddWallCap(near.v2, far.v2);
                AddWallCap(near.v4, far.v4);
            }
            else
            {
                AddWallSegment(near.v2, far.v2, near.v3, far.v3);
                AddWallSegment(near.v3, far.v3, near.v4, far.v4);
            }
            AddWallSegment(near.v4, far.v4, near.v5, far.v5);
        }
    }

    public void AddWall(
    Vector3 c1, HexCell cell1,
    Vector3 c2, HexCell cell2,
    Vector3 c3, HexCell cell3
    ){
        if (cell1.Walled)
        {
            if (cell2.Walled)
            {
                if (!cell3.Walled)
                {   //cell3在墙外
                    AddWallSegment(c3, cell3, c1, cell1, c2, cell2);
                }

            }
            else if (cell3.Walled){
                //cell2在墙外
                AddWallSegment(c2, cell2, c3, cell3, c1, cell1);
            }
            else
            {   //cell23在墙外
                AddWallSegment(c1, cell1, c2, cell2, c3, cell3);
            }
        }
        else if (cell2.Walled)
        {
            if (cell3.Walled)
            {   //cell1墙外
                AddWallSegment(c1, cell1, c2, cell2, c3, cell3);
            }
            else
            {   //cell 1,3墙外
                AddWallSegment(c2, cell2, c3, cell3, c1, cell1);
            }
        }
        else if (cell3.Walled)
        {   //cell 2,3墙外
            AddWallSegment(c3, cell3, c1, cell1, c2, cell2);
        }
    }
    /*
    *在connetion区域添加围墙
    */
    void AddWallSegment(
    Vector3 nearLeft, Vector3 farLeft, Vector3 nearRight, Vector3 farRight,
        bool addTower = false
    ){
        nearLeft = HexMetrics.Perturb(nearLeft);
        farLeft = HexMetrics.Perturb(farLeft);
        nearRight = HexMetrics.Perturb(nearRight);
        farRight = HexMetrics.Perturb(farRight);

        Vector3 left = HexMetrics.WallLerp(nearLeft, farLeft);
        Vector3 right = HexMetrics.WallLerp(nearRight, farRight);

        Vector3 leftThicknessOffset =
            HexMetrics.WallThicknessOffset(nearLeft, farLeft);
        Vector3 rightThicknessOffset =
            HexMetrics.WallThicknessOffset(nearRight, farRight);

        float leftTop = left.y + HexMetrics.wallHeight;
        float rightTop = right.y + HexMetrics.wallHeight;

        //墙向外面
        Vector3 v1, v2, v3, v4;
        v1 = v3 = left - leftThicknessOffset;
        v2 = v4 = right - rightThicknessOffset;
        v3.y = leftTop;
        v4.y = rightTop;
        walls.AddQuadUnperturbed(v1, v2, v3, v4);

        Vector3 t1 = v3, t2 = v4;

        //墙向内面
        v1 = v3 = left + leftThicknessOffset;
        v2 = v4 = right + rightThicknessOffset;
        v3.y = leftTop;
        v4.y = rightTop;
        walls.AddQuadUnperturbed(v2, v1, v4, v3);
        walls.AddQuadUnperturbed(t1, t2, v3, v4);
        if (addTower)
        {
            Transform towerInstance = Instantiate(wallTower);
            towerInstance.transform.localPosition = (left + right) * 0.5f;
            Vector3 rightDirection = right - left;
            rightDirection.y = 0f;
            towerInstance.transform.right = rightDirection;
            towerInstance.SetParent(container, false);
        }
    }
    /*
    *在三角形连接区域区域添加围墙
    */
    void AddWallSegment(
    Vector3 pivot, HexCell pivotCell,
    Vector3 left, HexCell leftCell,
    Vector3 right, HexCell rightCell
    )
    {
        if (pivotCell.IsUnderwater)
            return;

        bool hasLeftWall = !leftCell.IsUnderwater &&
			pivotCell.GetEdgeType(leftCell) != HexEdgeType.Cliff;
		bool hasRighWall = !rightCell.IsUnderwater &&
			pivotCell.GetEdgeType(rightCell) != HexEdgeType.Cliff;

        if (hasLeftWall)
        {
            if (hasRighWall)
            {
                bool hasTower = false;//no tower on slope
                if (leftCell.Elevation == rightCell.Elevation)
                {
                    HexHash hash = HexMetrics.SampleHashGrid(
                        (pivot + left + right) * (1f / 3f));
                    hasTower = hash.e < HexMetrics.wallTowerThreshold;
                }
                AddWallSegment(pivot, left, pivot, right, hasTower);
                
            }
            else if (leftCell.Elevation < rightCell.Elevation)
            {
                AddWallWedge(pivot, left, right);
            }
            else
            {
                AddWallCap(pivot, left);
            }
        }
        else if(hasRighWall)
            if(rightCell.Elevation < leftCell.Elevation)
            {
                AddWallWedge(right, pivot, left);
            }
            else
            {
                AddWallCap(pivot, right);
            }
    }
    //合拢被水路阻断的墙壁
    void AddWallCap(Vector3 near, Vector3 far)
    {
        near = HexMetrics.Perturb(near);
        far = HexMetrics.Perturb(far);

        Vector3 center = HexMetrics.WallLerp(near, far);
        Vector3 thickness = HexMetrics.WallThicknessOffset(near, far);

        Vector3 v1, v2, v3, v4;

        v1 = v3 = center - thickness;
        v2 = v4 = center + thickness;
        v3.y = v4.y = center.y + HexMetrics.wallHeight;
        walls.AddQuadUnperturbed(v1, v2, v3, v4);
    }
    //合拢墙壁和悬崖边缘
    void AddWallWedge(Vector3 near, Vector3 far,Vector3 point)
    {
        near = HexMetrics.Perturb(near);
        far = HexMetrics.Perturb(far);
        point = HexMetrics.Perturb(point);

        Vector3 center = HexMetrics.WallLerp(near, far);
        Vector3 thickness = HexMetrics.WallThicknessOffset(near, far);

        Vector3 v1, v2, v3, v4;
        Vector3 pointTop = point;
        point.y = center.y;

        v1 = v3 = center - thickness;
        v2 = v4 = center + thickness;
        v3.y = v4.y = pointTop.y = center.y + HexMetrics.wallHeight;
        walls.AddQuadUnperturbed(v1, point, v3, pointTop);
        walls.AddQuadUnperturbed(point, v2, pointTop, v4);
        walls.AddTriangleUnperturbed(pointTop, v3, v4);
    }

    /*
     * 添加桥,参数roadCenter1，roadCenter2为Unperturbed Vector3
     */
    public void AddBridge(Vector3 roadCenter1, Vector3 roadCenter2)
    {
        roadCenter1 = HexMetrics.Perturb(roadCenter1);
        roadCenter2 = HexMetrics.Perturb(roadCenter2);
        Transform instance = Instantiate(bridge);
        instance.localPosition = (roadCenter1 + roadCenter2) * 0.5f;
        instance.forward = roadCenter2 - roadCenter1;//调整朝向
        //根据河的宽度调整桥的长度
        float length = Vector3.Distance(roadCenter1, roadCenter2);
        instance.localScale = new Vector3(
            1f, 1f, length * (1f / HexMetrics.bridgeDesignLength)
        );
        instance.SetParent(container,false);
        
    }

    /*
     * 添加Special Feature
     */
    public void AddSpecialFeature(HexCell cell, Vector3 position)
    {
        Transform instance = Instantiate(special[cell.SpecialIndex - 1]);
        //hash调整朝向
        HexHash hash = HexMetrics.SampleHashGrid(position);
        instance.localRotation = Quaternion.Euler(0f, 360f * hash.e, 0f);

        instance.localPosition = position;
        instance.SetParent(container, false);

    }
}