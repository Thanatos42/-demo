using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 奶茶店 Demo 的美术与配方总库。运行时会从 Resources 根目录按名称
/// "MilkTeaArtLibrary" 加载。所有 Sprite 字段留空时会自动回退到纯色占位，
/// 因此可以逐个拖拽替换素材，未替换的部分不受影响。
/// </summary>
[CreateAssetMenu(fileName = "MilkTeaArtLibrary", menuName = "奶茶店 Demo/美术库", order = 1)]
public sealed class MilkTeaArtLibrary : ScriptableObject
{
    [Serializable]
    public sealed class NamedSprite
    {
        [Tooltip("原料名称，需与调配界面按钮文字完全一致，如“红茶”“珍珠”")]
        public string name;
        public Sprite sprite;
    }

    [Header("字体（可选，留空使用系统字体）")]
    public Font uiFont;

    [Header("通用背景 / 面板")]
    public Sprite windowBackground;
    public Sprite dialogueBoxBackground;

    [Header("对话场景")]
    [Tooltip("右侧奶茶店俯视整图，设置后覆盖占位色块")]
    public Sprite shopScene;
    public Sprite defaultCustomerPortrait;
    public Sprite protagonistChibi;
    public Sprite customerChibi;

    [Header("调配场景")]
    [Tooltip("左上后厨俯视整图，设置后覆盖占位色块")]
    public Sprite kitchenScene;
    public Sprite cup;
    public Sprite lever;

    [Header("休息场景")]
    [Tooltip("右侧出租屋俯视整图，设置后覆盖占位色块")]
    public Sprite apartmentScene;

    [Header("开始界面")]
    [Tooltip("开始菜单背景整图，设置后覆盖占位色块")]
    public Sprite startBackground;
    [Tooltip("游戏 Logo，设置后覆盖占位文字")]
    public Sprite startLogo;

    [Header("原料图标（按名称匹配，可选）")]
    public List<NamedSprite> ingredientIcons = new List<NamedSprite>();

    [Header("配方列表（决定 Demo 中出现的奶茶）")]
    public List<MilkTeaRecipe> recipes = new List<MilkTeaRecipe>();

    [Header("客人出场表（留空则从配方中随机点单）")]
    [Tooltip("填入客人后，Demo 会按列表顺序循环让他们出场")]
    public List<MilkTeaCustomer> customers = new List<MilkTeaCustomer>();

    [Header("默认对话（客人未单独设置时使用，留空则用内置台词）")]
    public MilkTeaDialogue defaultOpeningDialogue;
    public MilkTeaDialogue defaultServingDialogue;

    public Sprite GetIngredientIcon(string ingredientName)
    {
        if (ingredientIcons == null)
        {
            return null;
        }

        for (int i = 0; i < ingredientIcons.Count; i++)
        {
            NamedSprite entry = ingredientIcons[i];
            if (entry != null && entry.name == ingredientName)
            {
                return entry.sprite;
            }
        }

        return null;
    }
}
