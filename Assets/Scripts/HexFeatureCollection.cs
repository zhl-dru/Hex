using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 特征集
/// </summary>
[System.Serializable]
public struct HexFeatureCollection
{
    /// <summary>
    /// 预制体
    /// </summary>
    public Transform[] prefabs;
    /// <summary>
    /// 选择
    /// </summary>
    /// <param name="choice"></param>
    /// <returns></returns>
    public Transform Pick(float choice)
    {
        return prefabs[(int)(choice * prefabs.Length)];
    }
}
