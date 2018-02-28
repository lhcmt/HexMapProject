using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

using System.IO;
//map编辑器，以后可以用作界面
public class HexMapEditor : MonoBehaviour
{
    enum OptionalToggle
    {
        Ignore, Yes, No
    }

    public HexGrid hexGrid;

    public Slider slider;
    //接收界面Slider参数
    int activeElevation;
    int activeWaterLevel;
    int activeUrbanLevel,activeFarmLevel,activePlantLevel,activeSpecialIndex;
    //接收Toggle是否修改,海拔，水体，地面物体等
    bool applyElevation;
    bool applyWaterLevel;
    bool applyUrbanLevel, applyFarmLevel, applyPlantLevel,applySpecialIndex;
    int brushSize;
    //取代颜色，界面颜色设置存储为目录
    int activeTerrainTypeIndex;

    OptionalToggle riverMode, roadMode, walledMode;
    //用于检测编辑河流时候的鼠标拖拽
    bool isDrag;
    HexDirection dragDirection;
    HexCell previousCell;


    void Update()
    {   //当鼠标在UI上时，不处理被UI遮挡的物体
        if (Input.GetMouseButton(0) && !EventSystem.current.IsPointerOverGameObject())
        {
            HandleInput();
        }
        else
        {
            previousCell = null;
        }
    }

    void HandleInput()
    {
        Ray inputRay = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        if (Physics.Raycast(inputRay, out hit))
        {
            HexCell currentCell = hexGrid.GetCell(hit.point);
            if (previousCell && previousCell != currentCell)
            {
                ValidateDrag(currentCell);
            }
            else{
                isDrag = false;
            }
            EditCells(currentCell);
            previousCell = currentCell;
        }
        else
        {
            previousCell = null;
        }
    }
    void EditCell(HexCell cell)
    {
        if (cell)
        {
            //UI面板上被选中的选项，会修改其对应的值 ：颜色，海拔，水体，地面建筑等
            if(activeTerrainTypeIndex >= 0)
            {
                cell.TerrainTypeIndex = activeTerrainTypeIndex;
            }
            if (applyElevation){
                cell.Elevation = activeElevation;
            }
            if (applyWaterLevel){
                cell.WaterLevel = activeWaterLevel;
            }
            if (applySpecialIndex)
            {
                cell.SpecialIndex = activeSpecialIndex;
            }
            if(applyUrbanLevel){
                cell.UrbanLevel = activeUrbanLevel;
            }
            if (applyFarmLevel){
                cell.FarmLevel = activeFarmLevel;
            }
            if (applyPlantLevel){
                cell.PlantLevel = activePlantLevel;
            }

            if (riverMode == OptionalToggle.No){//删除River
                cell.RemoveRiver();
            }
            if (roadMode == OptionalToggle.No){//删除Road
                cell.RemoveRoads();
            }
            if (walledMode != OptionalToggle.Ignore){
                cell.Walled = walledMode == OptionalToggle.Yes;
            }
            if(isDrag){
                HexCell otherCell = cell.GetNeighbor(dragDirection.Opposite());
                if (otherCell){
                    if (riverMode == OptionalToggle.Yes){
                        otherCell.SetOutgoingRiver(dragDirection);
                    }
                    if (roadMode == OptionalToggle.Yes){
                        otherCell.AddRoad(dragDirection);
                    }
                }

            }
        }
    }
    //根据brushSize区域编辑
    void EditCells(HexCell center)
    {
        int centerX = center.coordinates.X;
        int centerZ = center.coordinates.Z;
        //从最底层的Z开始
        for (int r = 0, z = centerZ - brushSize; z <= centerZ; z++, r++){
            //从底部到中间,从左到右
            for (int x = centerX - r; x <= centerX + brushSize; x++){
                EditCell(hexGrid.GetCell(new HexCoordinates(x, z)));
            }
        }
        for (int r = 0, z = centerZ + brushSize; z > centerZ; z--, r++){
            //从顶部到中间
            for (int x = centerX + r; x >= centerX - brushSize; x--)
            {
                EditCell(hexGrid.GetCell(new HexCoordinates(x, z)));
            }
        }
    }
    //有效拖动，查询从哪个方向拖动过来的previousCell->currentCell
    void ValidateDrag(HexCell currentCell)
    {
        for (dragDirection = HexDirection.NE;dragDirection <= HexDirection.NW;dragDirection++)
        {
            if (previousCell.GetNeighbor(dragDirection) == currentCell)
            {
                isDrag = true;
                return;
            }
        }
        isDrag = false;
    }
    //显示/隐藏每个网格的坐标UI
    public void ShowUI(bool visible){
        hexGrid.ShowUI(visible);
    }



    /*
     * 界面按钮调用方法
     */

    public void SetElevation(){
        activeElevation = (int)slider.value;
    }
    public void SetApplyElevation(bool toggle){
        applyElevation = toggle;
    }
    public void SetApplyWaterLevel(bool toggle){
        applyWaterLevel = toggle;
    }
    public void SetWaterLevel(float level){
        activeWaterLevel = (int)level;
    }
    public void SetBrushSize(float size){
        brushSize = (int)size;
    }
    public void SetRiverMode(int mode){
        riverMode = (OptionalToggle)mode;
    }
    
    public void SetRoadMode(int mode){
        roadMode = (OptionalToggle)mode;
    }
    public void SetWalledMode(int mode)
    {
        walledMode = (OptionalToggle)mode;
    }
    public void SetApplyUrbanLevel(bool toggle){
        applyUrbanLevel = toggle;
    }
    public void SetUrbanLevel(float level){
        activeUrbanLevel = (int)level;
    }
    public void SetApplyFarmLevel(bool toggle){
        applyFarmLevel = toggle;
    }

    public void SetFarmLevel(float level){
        activeFarmLevel = (int)level;
    }

    public void SetApplyPlantLevel(bool toggle){
        applyPlantLevel = toggle;
    }
    public void SetPlantLevel(float level){
        activePlantLevel = (int)level;
    }
    public void SetApplySpecialIndex(bool toggle)
    {
        applySpecialIndex = toggle;
    }

    public void SetSpecialIndex(float index)
    {
        activeSpecialIndex = (int)index;
    }
    public void SetTerrainTypeIndex(int index)
    {
        activeTerrainTypeIndex = index;
    }
}