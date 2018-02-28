/*
 * 2017/6/6 添加
 * 创建新地图脚本
 */

using UnityEngine;
using System.Collections;

public class NewMapMenu : MonoBehaviour {

    public HexGrid hexGrid;


    public void Open()
    {
        HexMapCamera.Locked = true;
        gameObject.SetActive(true);
    }

    public void Close()
    {
        HexMapCamera.Locked= false;
        gameObject.SetActive(false);
    }

    void CreateMap(int x, int z)
    {
        hexGrid.CreateMap(x, z);
        HexMapCamera.ValidatePosition();
        Close();
    }
    public void CreateSmallMap()
    {
        CreateMap(20, 15);
    }

    public void CreateMediumMap()
    {
        CreateMap(40, 30);
    }

    public void CreateLargeMap()
    {
        CreateMap(80, 60);
    }
}
