using UnityEngine;

/// <summary>
/// 单款奶茶的配方数据资源。可在 Unity 中通过右键
/// Create → 奶茶店 Demo → 配方 创建，并在 Inspector 中拖拽替换图标 / 立绘。
/// </summary>
[CreateAssetMenu(fileName = "Recipe", menuName = "奶茶店 Demo/配方", order = 0)]
public sealed class MilkTeaRecipe : ScriptableObject
{
    [Header("基础信息")]
    public string displayName = "新奶茶";

    [Tooltip("配方图标，留空则显示下方文字占位")]
    public Sprite icon;

    [Tooltip("未设置图标时显示的文字占位")]
    public string iconLabel = "?";

    [Header("配方要求（用于判定）")]
    public string teaBase = "红茶";
    public string milkBase = "鲜奶";

    [Tooltip("顶料 / 风味，无顶料请填“无”")]
    public string topping = "珍珠";

    [Header("点单剧情（可选）")]
    [Tooltip("该顾客的立绘，留空则使用美术库中的默认立绘")]
    public Sprite customerPortrait;

    public string customerName = "顾客";
}
