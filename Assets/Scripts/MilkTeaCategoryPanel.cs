using UnityEngine;

/// <summary>
/// 原料分类容器标记：标明该容器对应茶底 / 奶底 / 配料中的哪一类。
/// 运行时控制器据此在切换分类时显示 / 隐藏对应容器。
/// </summary>
public sealed class MilkTeaCategoryPanel : MonoBehaviour
{
    public IngredientCategory category;
}
