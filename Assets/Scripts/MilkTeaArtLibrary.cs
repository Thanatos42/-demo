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
    [Tooltip("奶茶店场景整图，作为对话界面全屏背景")]
    public Sprite shopScene;

    [Header("主角")]
    [Tooltip("主角姓名；对话说话人与该名称或“主角”一致时显示主角立绘")]
    public string protagonistName = "主角";
    public Sprite protagonistPortrait;
    [Tooltip("主角立绘序列帧：≥2 帧时优先循环播放")]
    public List<Sprite> protagonistPortraitFrames = new List<Sprite>();
    public float protagonistPortraitFps = 8f;
    public Sprite protagonistChibi;

    [Header("默认顾客回退")]
    public Sprite defaultCustomerPortrait;
    public Sprite customerChibi;

    [Header("对话界面装饰")]
    [Tooltip("继续按钮图标（如猫爪），设置后覆盖文字“继续”")]
    public Sprite continueButtonIcon;
    [Tooltip("说话人名牌背景（丝带/标签样式），设置后覆盖纯色")]
    public Sprite speakerTagBackground;
    [Tooltip("天数/营业状态徽章背景（顶部标题栏），设置后覆盖纯色")]
    public Sprite dayTitleBackground;

    [Header("对话文字颜色")]
    public Color dialogueTextColor = new Color(0.32f, 0.22f, 0.18f, 1f);
    public Color speakerTextColor = new Color(1f, 0.96f, 0.84f, 1f);
    public Color dayTitleTextColor = new Color(0.25f, 0.18f, 0.14f, 1f);

    [Header("对话场景小人布局（1920 × 1080 坐标）")]
    public Vector2 protagonistChibiPosition = new Vector2(1120f, 600f);
    public Vector2 protagonistChibiSize = new Vector2(150f, 150f);
    public Vector2 customerChibiPosition = new Vector2(1360f, 390f);
    public Vector2 customerChibiSize = new Vector2(180f, 180f);

    [Header("调配场景")]
    [Tooltip("调配界面全屏底图（含右侧操作区装饰和底色），设置后覆盖占位色块")]
    public Sprite mixingBackground;
    [Tooltip("左上后厨俯视整图，叠在底图左侧")]
    public Sprite kitchenScene;

    [Header("调配界面图标")]
    [Tooltip("茶底分类图标（如茶叶）")]
    public Sprite teaCategoryIcon;
    [Tooltip("奶底分类图标（如牛奶）")]
    public Sprite milkCategoryIcon;
    [Tooltip("配料分类图标（如配料碗）")]
    public Sprite toppingCategoryIcon;
    [Tooltip("配方翻页左箭头")]
    public Sprite recipePreviousIcon;
    [Tooltip("配方翻页右箭头")]
    public Sprite recipeNextIcon;
    [Tooltip("原料分类切换上箭头")]
    public Sprite categoryPreviousIcon;
    [Tooltip("原料分类切换下箭头")]
    public Sprite categoryNextIcon;

    [Header("糖冰格子图标")]
    public Sprite sugarIcon;
    public Sprite iceIcon;
    [Tooltip("未选中的空格子")]
    public Sprite levelBlockEmpty;
    [Tooltip("糖度选中格子（粉色）")]
    public Sprite sugarBlockSelected;
    [Tooltip("冰度选中格子（蓝色）")]
    public Sprite iceBlockSelected;

    [Header("调配提交按钮 Shake Button")]
    [Tooltip("开始摇动按钮底图，设置后覆盖纯色")]
    public Sprite shakeButtonBackground;
    [Tooltip("摇杯装饰图标（可选）")]
    public Sprite shakeButtonIcon;

    [Header("休息场景 Rest Scene")]
    [Tooltip("右侧出租屋俯视整图（床上不画主角），设置后覆盖占位色块")]
    public Sprite apartmentScene;
    [Tooltip("床上休息的主角小人（躺姿首帧，后续可扩展序列帧动画）")]
    public Sprite restCharacter;
    [Tooltip("猫爪手机外壳（屏幕区域留空）")]
    public Sprite phoneFrame;
    [Tooltip("手机屏幕壁纸，可更换；状态栏时间由文字动态叠加，不要烧进图")]
    public Sprite phoneWallpaper;

    [Header("手机功能图标 Rest Apps")]
    [Tooltip("角色入口图标")]
    public Sprite appCharacterIcon;
    [Tooltip("相册入口图标")]
    public Sprite appAlbumIcon;
    [Tooltip("音乐入口图标")]
    public Sprite appMusicIcon;
    [Tooltip("笔记入口图标")]
    public Sprite appNotesIcon;
    [Tooltip("设置入口图标")]
    public Sprite appSettingsIcon;

    [Header("休息徽章按钮 Rest Badge")]
    [Tooltip("营业中徽章整图（奶茶杯+底板+营业中字样），DAY与时间由文字叠加")]
    public Sprite openBadge;
    [Tooltip("返回按钮整图（含箭头与返回字样）")]
    public Sprite backButtonIcon;

    [Header("结算界面 Settlement")]
    [Tooltip("结算界面整屏底图，设置后覆盖占位深色")]
    public Sprite settlementBackground;
    [Tooltip("结算卡片底图（含边框装饰），标题数据文字叠加；标题/数据不要烧进图")]
    public Sprite settlementCard;

    [Header("开始界面")]
    [Tooltip("开始菜单背景整图，设置后覆盖占位色块")]
    public Sprite startBackground;
    [Tooltip("游戏 Logo，设置后覆盖占位文字")]
    public Sprite startLogo;
    [Tooltip("设置按钮图标（左上角小图标），设置后覆盖文字“设置”")]
    public Sprite settingsIcon;

    [Header("按钮皮肤（留空则用纯色占位；建议用九宫格 Sprite）")]
    [Tooltip("正向主按钮，如开始游戏/进入下一天（对应青色）")]
    public Sprite buttonPrimary;
    [Tooltip("次要按钮，如读取存档/翻页/再休息一会儿（对应深蓝）")]
    public Sprite buttonSecondary;
    [Tooltip("强调/危险按钮，如跳过/完成/继续（对应珊瑚色）")]
    public Sprite buttonAccent;
    [Tooltip("中性/默认按钮，如设置及其他（对应深灰）")]
    public Sprite buttonNeutral;

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
