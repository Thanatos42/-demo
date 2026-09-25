using UnityEngine;

/// <summary>
/// 糖度 / 冰度方块标记：isSugar 为真表示糖度块，否则为冰度块；index 为方块序号(0/1)。
/// 由建场工具挂到每个进度方块按钮上，运行时控制器据此自动接线。
/// </summary>
public sealed class MilkTeaLevelBlock : MonoBehaviour
{
    public bool isSugar;
    public int index;
}
