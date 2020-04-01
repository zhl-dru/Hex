using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.IO;
using System;

public class SaveLoadMenu : MonoBehaviour
{
    public HexGrid hexGrid;
    public Text menuLabel, actionButtonLabel;
    public InputField nameInput;
    private bool saveMode;
    public RectTransform listContent;
    public SaveLoadItem itemPrefab;
    const int mapFileVersion = 3;

    public void Open(bool saveMode)
    {
        this.saveMode = saveMode;
        if (saveMode)
        {
            menuLabel.text = "保存地图";
            actionButtonLabel.text = "保存";
        }
        else
        {
            menuLabel.text = "加载地图";
            actionButtonLabel.text = "加载";
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
    /// <summary>
    /// 获得路径
    /// </summary>
    /// <returns></returns>
    string GetSelectedPath()
    {
        string mapName = nameInput.text;
        if (mapName.Length == 0)
        {
            return null;
        }
        return Path.Combine(Application.persistentDataPath, mapName + ".map");
    }

    public void SelectItem(string name)
    {
        nameInput.text = name;
    }
    /// <summary>
    /// 文件列表
    /// </summary>
    void FillList()
    {
        for (int i = 0; i < listContent.childCount; i++)
        {
            Destroy(listContent.GetChild(i).gameObject);
        }

        string[] paths = Directory.GetFiles(Application.persistentDataPath, "*.map");
        //按字母顺序排列
        Array.Sort(paths);

        for (int i = 0; i < paths.Length; i++)
        {
            SaveLoadItem item = Instantiate(itemPrefab);
            item.menu = this;
            item.MapName = Path.GetFileNameWithoutExtension(paths[i]);
            item.transform.SetParent(listContent, false);
        }
    }

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



    /// <summary>
    /// 保存
    /// </summary>
    void Save(string path)
    {
        //string path = Path.Combine(Application.persistentDataPath, "test.map");
        using (
            BinaryWriter writer = new BinaryWriter(File.Open(path, FileMode.Create))
            )
        {
            writer.Write(mapFileVersion);
            hexGrid.Save(writer);
        }
    }
    /// <summary>
    /// 加载
    /// </summary>
    void Load(string path)
    {
        //string path = Path.Combine(Application.persistentDataPath, "test.map");
        if (!File.Exists(path))
        {
            Debug.LogError("File does not exist" + path);
            return;
        }
        using (
            BinaryReader reader = new BinaryReader(File.OpenRead(path))
            )
        {
            int header = reader.ReadInt32();
            if (header <= mapFileVersion)
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
    /// <summary>
    /// 删除文件
    /// </summary>
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
        //清除Name Input并刷新文件列表
        nameInput.text = "";
        FillList();
    }
}
