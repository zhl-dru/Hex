using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 哈希
/// </summary>
public struct HexHash
{
    public float a, b, c, d, e;

    /// <summary>
    /// 创建散列值
    /// </summary>
    /// <returns></returns>
    public static HexHash Create()
    {
        HexHash hash;
        //保证不出现1,防止数组越界
        hash.a = Random.value * 0.999f;
        hash.b = Random.value * 0.999f;
        hash.c = Random.value * 0.999f;
        hash.d = Random.value * 0.999f;
        hash.e = Random.value * 0.999f;
        return hash;
    }
}
