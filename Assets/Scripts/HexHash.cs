using UnityEngine;
using System.Collections;
/*
 * 一个结构体，用于构成Hash表的一个单元，每个单元有多个值，随机初始化
 */
public struct HexHash{
    //abc值分别决定地表是否放置一个Urban，Farm，Plant
    //
    //e决定朝向
    public float a, b ,c, d ,e;

    public static HexHash Create()
    {
        HexHash hash;
        hash.a = Random.value * 0.999f;
        hash.b = Random.value * 0.999f;
        hash.c = Random.value * 0.999f;
        hash.d = Random.value * 0.999f;
        hash.e = Random.value * 0.999f;
        return hash;
    }

}
