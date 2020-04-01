﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 单元纹理
/// </summary>
public class HexCellShaderData : MonoBehaviour
{
    Texture2D cellTexture;
    Color32[] cellTextureData;
    const float transitionSpeed = 225f;
    bool needsVisibilityReset;

    List<HexCell> transitioningCells = new List<HexCell>();


    public bool ImmediateMode { get; set; }
    public HexGrid Grid { get; set; }


    public void Initialize(int x, int z)
    {
        if (cellTexture)
        {
            cellTexture.Resize(x, z);
        }
        else
        {
            cellTexture = new Texture2D(
                x, z, TextureFormat.RGBA32, false, true);
            cellTexture.filterMode = FilterMode.Point;
            //cellTexture.wrapMode = TextureWrapMode.Clamp;
            cellTexture.wrapModeU = TextureWrapMode.Repeat;
            cellTexture.wrapModeV = TextureWrapMode.Clamp;
            Shader.SetGlobalTexture("_HexCellData", cellTexture);
        }
        Shader.SetGlobalVector("_HexCellData_TexelSize", new Vector4(1f / x, 1f / z, x, z));

        if (cellTextureData == null || cellTextureData.Length != x * z)
        {
            cellTextureData = new Color32[x * z];
        }
        else
        {
            for (int i = 0; i < cellTextureData.Length; i++)
            {
                cellTextureData[i] = new Color32(0, 0, 0, 0);
            }
        }

        transitioningCells.Clear();
        enabled = true;
    }

    public void RefreshTerrain(HexCell cell)
    {
        cellTextureData[cell.Index].a = (byte)cell.TerrainTypeIndex;
        enabled = true;
    }

    public void RefreshVisibility(HexCell cell)
    {
        int index = cell.Index;

        if (ImmediateMode)
        {
            cellTextureData[cell.Index].r = cell.IsVisible ? (byte)255 : (byte)0;
            cellTextureData[cell.Index].g = cell.IsExplored ? (byte)255 : (byte)0;
        }
        else if (cellTextureData[index].b != 255)
        {
            cellTextureData[index].b = 255;
            transitioningCells.Add(cell);
        }
        enabled = true;
    }

    bool UpdateCellData(HexCell cell, int delta)
    {
        int index = cell.Index;
        Color32 data = cellTextureData[index];
        bool stillUpdating = false;

        if (cell.IsExplored && data.g < 255)
        {
            stillUpdating = true;
            int t = data.g + delta;
            data.g = t >= 255 ? (byte)255 : (byte)t;
        }
        if (cell.IsVisible)
        {
            if (data.r < 255)
            {
                stillUpdating = true;
                int t = data.r + delta;
                data.r = t >= 255 ? (byte)255 : (byte)t;
            }
        }
        else if (data.r > 0)
        {
            stillUpdating = true;
            int t = data.r - delta;
            data.r = t < 0 ? (byte)0 : (byte)t;
        }

        if (!stillUpdating)
        {
            data.b = 0;
        }
        cellTextureData[index] = data;
        return stillUpdating;
    }

    private void LateUpdate()
    {
        if (needsVisibilityReset)
        {
            needsVisibilityReset = false;
            Grid.ResetVisibility();
        }

        int delta = (int)(Time.deltaTime * transitionSpeed);
        //在理论上也可以获得非常高的帧速率,加上低转换速度,这可能会导致增量为零,为了保证进度,我们需要强制增量至少为1
        if (delta == 0)
        {
            delta = 1;
        }

        for (int i = 0; i < transitioningCells.Count; i++)
        {
            if (!UpdateCellData(transitioningCells[i], delta))
            {
                transitioningCells[i--] = transitioningCells[transitioningCells.Count - 1];
                transitioningCells.RemoveAt(transitioningCells.Count - 1);
            }
        }
        cellTexture.SetPixels32(cellTextureData);
        cellTexture.Apply();
        enabled = transitioningCells.Count > 0;
    }

    public void ViewElevationChanged()
    {
        needsVisibilityReset = true;
        enabled = true;
    }

    public void SetMapData(HexCell cell,float data)
    {
        cellTextureData[cell.Index].b =
            data < 0f ? (byte)0 : (data < 1f ? (byte)(data * 254f) : (byte)254);
        enabled = true;
    }
}