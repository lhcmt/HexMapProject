/*
 * 2017/6/7 添加
 * 控制save load menu ，
 * 
 * 6/8
 * 添加显示列表
 */

using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using System;
using System.IO;
public class SaveLoadMenu : MonoBehaviour {

    public HexGrid hexGrid;
    //改变按钮名字界面
    public Text menuLabel, actionButtonLebal;

    public InputField nameInput;
    //显示列表
    public RectTransform listContent;

    public SaveLoadItem itemPrefab;
    //追踪Menu状态
    bool saveMode;

    public void open(bool saveMode)
    {
        
        this.saveMode = saveMode;
        if(saveMode)
        {
            menuLabel.text = "Save Map";
            actionButtonLebal.text = "Save";
        }
        else
        {
            menuLabel.text = "Load Map";
            actionButtonLebal.text = "Load";
        }
        FillList();
        gameObject.SetActive(true);
        HexMapCamera.Locked = true;
    }

    public void Close()
    {
        gameObject.SetActive(false);
        HexMapCamera.Locked = false;
    }
/*
 * 获取map存储路径
 */
    string GetSelectedPath()
    {
        string mapName = nameInput.text;
        if (mapName.Length == 0)
        {
            return null;
        }
        return Path.Combine(Application.persistentDataPath, mapName + ".map");
    }

/*
 * save and load
 */
    void Save(string path)
    {
        //创建或者覆盖文件
        //open data stream, linked to this file
        //BinaryWriter和stream classes都实现了IDisposable接口，离开Using
        //范围会调用dispose方法
        using (
            BinaryWriter writer =
                new BinaryWriter(File.Open(path, FileMode.Create))
        ){
            //header,version
            writer.Write(1);
            //mapbody
            hexGrid.Save(writer);
        }
    }

    void Load(string path)
    {
        if (!File.Exists(path))
        {
            Debug.LogError("File does not exist " + path);
            return;
        }
        using (BinaryReader reader = new BinaryReader(File.OpenRead(path)))
        {
            int header = reader.ReadInt32();
            if (header <= 1)
            {
                hexGrid.Load(reader, header);
                HexMapCamera.ValidatePosition();
            }
            else
            {
                Debug.LogWarning("Unknown map format " + header);
            }
        }
    }
/*
 * 绑定在Action button 上 
 * 获取路径，根据模式执行Save() 或Load()
 */
    public void Action()
    {
        string path = GetSelectedPath();
        if (path == null)
        {
            return;
        }
        if (saveMode)
        {
            Save(path);
        }
        else
        {
            Load(path);
        }
        Close();
    }

    public void SelectItem(string name)
    {
        nameInput.text = name;
    }
    /*
     * 获取目录下.map 文件列表名字
     */
    void FillList()
    {
        //clear old content
        for (int i = 0; i < listContent.childCount; i++)
        {
            Destroy(listContent.GetChild(i).gameObject);
        }

        string[] paths =
            Directory.GetFiles(Application.persistentDataPath, "*.map");
        Array.Sort(paths);
        for(int i = 0; i<paths.Length; i++)
        {
            SaveLoadItem item = Instantiate(itemPrefab);
            item.menu = this;
            item.MapName = Path.GetFileNameWithoutExtension(paths[i]);
            item.transform.SetParent(listContent, false);
        }
    }

    public void Delete()
    {
        string path = GetSelectedPath();
        if (path == null)
        {
            return;
        }
        if (File.Exists(path))
        {
            File.Delete(path);
        }
        nameInput.text = "";
        FillList();
    }
}
