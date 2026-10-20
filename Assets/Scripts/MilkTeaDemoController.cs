using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.Video;

/// <summary>
/// 原料分类。定义在此文件中，供各 marker 组件与建场工具共同引用。
/// </summary>
public enum IngredientCategory
{
    Tea,
    Milk,
    Topping
}

/// <summary>
/// 奶茶店 Demo 运行时逻辑控制器（方案：预制体 / 场景）。
/// 本脚本不再创建任何 UI，只持有对场景中已有 UI 组件的引用，并驱动
/// 对话流程、原料选择、糖冰调节、配方手册翻页与拉杆提交等逻辑。
/// 界面由 <c>MilkTeaSceneBuilder</c> 在编辑器里一次性生成，之后可在
/// 场景中自由调整位置、添加 Animator 做动画。
/// </summary>
public sealed class MilkTeaDemoController : MonoBehaviour
{
    private sealed class Recipe
    {
        public string Name;
        public string Tea;
        public string Milk;
        public string Topping;
        public string IconLabel;
        public Sprite IconSprite;
        public Sprite Portrait;
        public string CustomerName = "顾客";
    }

    private const string UnlockPrefix = "MilkTeaDemo.Unlock.";
    private const string ArtLibraryResource = "MilkTeaArtLibrary";
    private const string VolumePref = "MilkTeaDemo.Settings.Volume";
    private const string ResolutionPref = "MilkTeaDemo.Settings.ResolutionIndex";
    private const string LanguagePref = "MilkTeaDemo.Settings.LanguageIndex";

    private struct ResolutionOption
    {
        public int Width;
        public int Height;
        public bool Fullscreen;
        public string Label;
    }

    private static readonly ResolutionOption[] ResolutionOptions =
    {
        new ResolutionOption { Width = 1280, Height = 720, Fullscreen = false, Label = "1280 × 720 窗口" },
        new ResolutionOption { Width = 1600, Height = 900, Fullscreen = false, Label = "1600 × 900 窗口" },
        new ResolutionOption { Width = 1920, Height = 1080, Fullscreen = false, Label = "1920 × 1080 窗口" },
        new ResolutionOption { Width = 1920, Height = 1080, Fullscreen = true, Label = "1920 × 1080 全屏" }
    };

    private static readonly string[] LanguageOptions = { "简体中文", "English" };

    [Header("屏幕根节点")]
    public GameObject dialogueScreen;
    public GameObject mixingScreen;

    [Header("对话界面")]
    public Image portraitImage;
    [Tooltip("立绘序列帧播放器（挂在立绘 Image 上）；客人有 ≥2 帧时播放动画")]
    public SpriteSequenceAnimator portraitAnimator;
    public Text portraitLabel;
    [Tooltip("背景中固定显示的主角 Q 版小人")]
    public Image protagonistChibiImage;
    [Tooltip("柜台前随当前客人切换的 Q 版小人")]
    public Image customerChibiImage;
    public Text dialogueSpeaker;
    public Text dialogueLine;
    public Button dialogueButton;
    [Tooltip("语音专用 AudioSource；留空时运行时自动创建（独占通道，新语音打断旧语音）")]
    public AudioSource voiceSource;
    [Tooltip("效果音 AudioSource；留空时运行时自动创建")]
    public AudioSource sfxSource;
    public Text dialogueButtonLabel;

    [Header("调配 · 需求提示")]
    public Text orderSpeaker;
    public Text orderLine;

    [Header("调配 · 配方手册")]
    public Text recipeIcon;
    public Image recipeIconImage;
    public Text recipeDetails;
    public Text recipePage;
    public Button prevRecipeButton;
    public Button nextRecipeButton;

    [Header("调配 · 原料选择")]
    public Text categoryTitle;
    public Image categoryIcon;
    public Text selectionSummary;
    public Button prevCategoryButton;
    public Button nextCategoryButton;

    [Header("调配 · 提交")]
    public Text machineStatus;
    public Button shakeButton;
    public Text shakeButtonLabel;

    [Header("调配 · 糖冰状态显示")]
    public Text sugarStateLabel;
    public Text iceStateLabel;

    [Header("结算界面")]
    public GameObject settlementScreen;
    public Text settlementTitle;
    public Text settlementBody;
    public Button settlementButton;
    public Text settlementButtonLabel;

    [Header("休息界面")]
    public GameObject restScreen;
    public Button nextDayButton;
    public Text nextDayButtonLabel;
    public Button stayButton;
    public Text stayButtonLabel;
    public Text restHint;
    [Tooltip("手机功能入口按钮（角色/相册/音乐/笔记/设置，顺序固定）")]
    public Button[] restAppButtons;
    [Tooltip("休息界面手机 · 朋友圈面板（角色App，默认隐藏，由建场工具生成）")]
    public GameObject appMomentsPanel;
    [Tooltip("休息界面手机 · 相册面板（相册App，默认隐藏）")]
    public GameObject appAlbumPanel;
    [Tooltip("朋友圈条目容器（滚动区 Content）")]
    public RectTransform appMomentsContent;
    [Tooltip("相册格子容器（滚动区 Content）")]
    public RectTransform appAlbumContent;
    [Tooltip("营业中徽章上的 DAY 与时间文字")]
    public Text restBadgeText;
    [Tooltip("手机状态栏时间文字")]
    public Text restPhoneClock;
    [Tooltip("休息界面固定的休息时间（暂无时间系统，后续可接）")]
    private const string RestClockText = "22:00";

    [Header("对话 · 全屏 CG 演出（由建场工具生成）")]
    [Tooltip("全屏 CG 浮层根节点（对话行 ShowCg 事件时弹出，点击按钮关闭）")]
    public GameObject cgOverlay;
    [Tooltip("全屏 CG 图片")]
    public Image cgOverlayImage;
    [Tooltip("全屏 CG 底部提示文字")]
    public Text cgOverlayHint;

    [Header("背景音乐（留空时运行时自动创建通道）")]
    [Tooltip("BGM 专用 AudioSource（循环播放，切界面换曲）")]
    public AudioSource bgmSource;

    [Header("每日循环")]
    public Text dayTitle;
    [Tooltip("随机点单模式下每天的客人数；排期模式自动使用客人列表长度")]
    public int customersPerDay = 3;

    [Header("开始界面")]
    public GameObject startScreen;
    public Button startGameButton;
    public Button loadGameButton;
    public Button quitGameButton;
    public Button settingsButton;

    [Header("设置面板")]
    public GameObject settingsPanel;
    public Button settingsCloseButton;
    public Text volumeValueLabel;
    public Button volumeDownButton;
    public Button volumeUpButton;
    public Text resolutionValueLabel;
    public Button resolutionPrevButton;
    public Button resolutionNextButton;
    public Text languageValueLabel;
    public Button languagePrevButton;
    public Button languageNextButton;

    [Header("开场动画")]
    public GameObject animationScreen;
    [Tooltip("开场视频播放器；将视频拖到其 Video Clip 即可。未指定时自动回退为倒计时占位")]
    public VideoPlayer introVideo;
    [Tooltip("显示视频画面的 RawImage")]
    public RawImage introVideoImage;
    public Text countdownLabel;
    public Button skipButton;
    public Text skipButtonLabel;
    [Tooltip("开场动画/倒计时时长（秒），未指定视频时用倒计时占位")]
    public float introCountdownSeconds = 10f;

    // 运行时状态反馈用色（仅用于选中高亮 / 糖冰档位着色）
    private readonly Color panel = Hex("#243247");
    private readonly Color panelLight = Hex("#33445E");
    private readonly Color cream = Hex("#FFF4D6");
    private readonly Color mint = Hex("#69D8C5");
    private readonly Color coral = Hex("#F28B82");
    private readonly Color yellow = Hex("#F3C84B");
    private readonly Color blue = Hex("#62B5F5");
    private readonly Color gray = Hex("#657184");

    private MilkTeaArtLibrary art;
    private Recipe[] recipes;
    private RectTransform shakeRect;

    private readonly Dictionary<IngredientCategory, GameObject> categoryPanels =
        new Dictionary<IngredientCategory, GameObject>();
    private readonly Dictionary<IngredientCategory, Dictionary<string, Image>> choiceImages =
        new Dictionary<IngredientCategory, Dictionary<string, Image>>();

    private IngredientCategory currentCategory;
    private string selectedTea = string.Empty;
    private string selectedMilk = string.Empty;
    private string selectedTopping = string.Empty;
    private int sugarLevel;
    private int iceLevel;
    private readonly Image[] sugarBlocks = new Image[2];
    private readonly Image[] iceBlocks = new Image[2];
    private bool isSubmitting;

    private int orderIndex;
    private int orderSugar;
    private int orderIce;
    private int manualIndex;
    private Recipe activeRecipe;
    private MilkTeaCustomer currentCustomer;
    private int customerCursor = -1;
    private int dayNumber = 1;

    // ---- 章节日程状态机：日程模式按 章→天→出场条目 推进；未配置日程时这些游标不动 ----
    // 当天出场序号直接用 servedToday（已完成单数）：第 N 位客人对应出场表第 N-1 条
    private int scheduleChapterIndex;
    private int scheduleDayIndex;
    private MilkTeaChapter.Appearance currentAppearance;

    // ---- 存档与解锁：CG/朋友圈等已解锁内容 ID 集合，随自动存档持久化 ----
    private readonly HashSet<string> unlockedContentIds = new HashSet<string>();

    // ---- 全屏 CG 演出与 BGM 运行态 ----
    private bool cgOverlayOpen;
    private AudioClip currentBgm;

    private int servedToday;
    private int perfectToday;
    private int mistakesToday;
    private int earningsToday;
    private bool currentOrderHadMistake;
    private float masterVolume = 1f;
    private int resolutionIndex = 2;
    private int languageIndex;
    private Coroutine introRoutine;
    private bool introFinished;

    /// <summary>开场动画“已看过”的存档键；重置入口：菜单「奶茶店 Demo/重置开场动画标记」。</summary>
    public const string IntroPlayedKey = "MilkTeaIntroPlayed";

    private void Awake()
    {
        art = Resources.Load<MilkTeaArtLibrary>(ArtLibraryResource);
        EnsureAudioChannels();
        recipes = BuildRecipes();
        // 测试跳进度：起始章/天取美术库配置（编辑器窗口可改）
        scheduleChapterIndex = art != null ? Mathf.Max(0, art.startChapterIndex) : 0;
        scheduleDayIndex = art != null ? Mathf.Max(0, art.startDayIndex) : 0;
        // 恢复已解锁内容（CG 等）；进度本身在点"读取存档"时才恢复
        MilkTeaSaveSystem.SaveData cached = MilkTeaSaveSystem.Load(MilkTeaSaveSystem.AutoSaveSlot);
        if (cached != null && cached.unlockedIds != null)
        {
            unlockedContentIds.UnionWith(cached.unlockedIds);
        }
        ApplyRuntimeFont();
        ApplyDialogueVisualSettings();
        WireChoices();
        WireLevels();
        WireNavigation();
        LoadSettings();
        ShowStartScreen();
    }

    private void Update()
    {
#if UNITY_EDITOR
        // 测试快捷键：F9 直接结束当天进结算；F10 推进到下一天（回到开始界面）
        if (Input.GetKeyDown(KeyCode.F9))
        {
            servedToday = CustomersToday();
            ShowSettlement();
            return;
        }

        if (Input.GetKeyDown(KeyCode.F10))
        {
            GoNextDay();
            return;
        }
#endif

        if (!Input.GetKeyDown(KeyCode.Space) || dialogueScreen == null || !dialogueScreen.activeInHierarchy)
        {
            return;
        }

        if (settingsPanel != null && settingsPanel.activeInHierarchy)
        {
            return;
        }

        GameObject selected = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
        if (selected != null && selected.GetComponentInParent<InputField>() != null)
        {
            return;
        }

        if (dialogueButton != null && dialogueButton.IsActive() && dialogueButton.IsInteractable())
        {
            dialogueButton.onClick.Invoke();
        }
    }

    private void ApplyRuntimeFont()
    {
        Font font = art != null && art.uiFont != null ? art.uiFont : CreateChineseFont();
        if (font == null)
        {
            return;
        }

        Text[] texts = GetComponentsInChildren<Text>(true);
        foreach (Text text in texts)
        {
            if (text.font == null || !text.font.dynamic)
            {
                text.font = font;
            }
        }
    }

    private void WireChoices()
    {
        MilkTeaChoiceButton[] markers = GetComponentsInChildren<MilkTeaChoiceButton>(true);
        foreach (MilkTeaChoiceButton marker in markers)
        {
            IngredientCategory category = marker.category;
            string option = marker.option;
            if (!choiceImages.ContainsKey(category))
            {
                choiceImages[category] = new Dictionary<string, Image>();
            }

            choiceImages[category][option] = marker.GetComponent<Image>();
            Button button = marker.GetComponent<Button>();
            if (button != null)
            {
                IngredientCategory capturedCategory = category;
                string capturedOption = option;
                button.onClick.AddListener(delegate { SelectIngredient(capturedCategory, capturedOption); });
            }
        }

        MilkTeaCategoryPanel[] panels = GetComponentsInChildren<MilkTeaCategoryPanel>(true);
        foreach (MilkTeaCategoryPanel marker in panels)
        {
            categoryPanels[marker.category] = marker.gameObject;
        }
    }

    private void WireLevels()
    {
        MilkTeaLevelBlock[] markers = GetComponentsInChildren<MilkTeaLevelBlock>(true);
        foreach (MilkTeaLevelBlock marker in markers)
        {
            if (marker.index < 0 || marker.index > 1)
            {
                continue;
            }

            Image image = marker.GetComponent<Image>();
            if (marker.isSugar)
            {
                sugarBlocks[marker.index] = image;
            }
            else
            {
                iceBlocks[marker.index] = image;
            }

            Button button = marker.GetComponent<Button>();
            if (button != null)
            {
                bool isSugar = marker.isSugar;
                int index = marker.index;
                button.onClick.AddListener(delegate { ChangeLevel(isSugar, index); });
            }
        }
    }

    private void WireNavigation()
    {
        if (prevRecipeButton != null)
        {
            prevRecipeButton.onClick.AddListener(delegate { ChangeManualPage(-1); });
        }

        if (nextRecipeButton != null)
        {
            nextRecipeButton.onClick.AddListener(delegate { ChangeManualPage(1); });
        }

        if (prevCategoryButton != null)
        {
            prevCategoryButton.onClick.AddListener(delegate { ChangeCategory(-1); });
        }

        if (nextCategoryButton != null)
        {
            nextCategoryButton.onClick.AddListener(delegate { ChangeCategory(1); });
        }

        if (shakeButton != null)
        {
            shakeRect = shakeButton.GetComponent<RectTransform>();
            shakeButton.onClick.AddListener(BeginSubmit);
        }

        if (settlementButton != null)
        {
            settlementButton.onClick.AddListener(ShowRest);
        }

        if (nextDayButton != null)
        {
            nextDayButton.onClick.AddListener(GoNextDay);
        }

        if (stayButton != null)
        {
            stayButton.onClick.AddListener(Stay);
        }

        if (restAppButtons != null)
        {
            string[] appNames = { "角色", "相册", "音乐", "笔记", "设置" };
            for (int i = 0; i < restAppButtons.Length && i < appNames.Length; i++)
            {
                if (restAppButtons[i] == null)
                {
                    continue;
                }

                string appName = appNames[i];
                restAppButtons[i].onClick.AddListener(() => OnRestAppClicked(appName));
            }
        }

        BindAppCloseButton(appMomentsPanel);
        BindAppCloseButton(appAlbumPanel);

        if (startGameButton != null)
        {
            startGameButton.onClick.AddListener(OnStartGame);
        }

        if (loadGameButton != null)
        {
            loadGameButton.onClick.AddListener(OnLoadGame);
        }

        if (settingsButton != null)
        {
            settingsButton.onClick.AddListener(OpenSettings);
        }

        if (quitGameButton != null)
        {
            quitGameButton.onClick.AddListener(QuitGame);
        }

        if (settingsCloseButton != null)
        {
            settingsCloseButton.onClick.AddListener(CloseSettings);
        }

        if (volumeDownButton != null)
        {
            volumeDownButton.onClick.AddListener(delegate { ChangeVolume(-1); });
        }

        if (volumeUpButton != null)
        {
            volumeUpButton.onClick.AddListener(delegate { ChangeVolume(1); });
        }

        if (resolutionPrevButton != null)
        {
            resolutionPrevButton.onClick.AddListener(delegate { ChangeResolution(-1); });
        }

        if (resolutionNextButton != null)
        {
            resolutionNextButton.onClick.AddListener(delegate { ChangeResolution(1); });
        }

        if (languagePrevButton != null)
        {
            languagePrevButton.onClick.AddListener(delegate { ChangeLanguage(-1); });
        }

        if (languageNextButton != null)
        {
            languageNextButton.onClick.AddListener(delegate { ChangeLanguage(1); });
        }

        if (skipButton != null)
        {
            skipButton.onClick.AddListener(SkipIntro);
        }
    }

    private Recipe[] BuildRecipes()
    {
        if (art != null && art.recipes != null && art.recipes.Count > 0)
        {
            List<Recipe> list = new List<Recipe>();
            foreach (MilkTeaRecipe source in art.recipes)
            {
                if (source == null)
                {
                    continue;
                }

                list.Add(new Recipe
                {
                    Name = source.displayName,
                    Tea = source.teaBase,
                    Milk = source.milkBase,
                    Topping = source.topping,
                    IconLabel = string.IsNullOrEmpty(source.iconLabel) ? "?" : source.iconLabel,
                    IconSprite = source.icon,
                    Portrait = source.customerPortrait,
                    CustomerName = string.IsNullOrEmpty(source.customerName) ? "顾客" : source.customerName
                });
            }

            if (list.Count > 0)
            {
                return list.ToArray();
            }
        }

        return BuildDefaultRecipes();
    }

    private static Recipe[] BuildDefaultRecipes()
    {
        return new[]
        {
            MakeRecipe("经典珍珠奶茶", "红茶", "鲜奶", "珍珠", "珍珠"),
            MakeRecipe("茉莉奶绿", "茉莉绿茶", "鲜奶", "无", "奶绿"),
            MakeRecipe("黑糖脏脏奶茶", "红茶", "鲜奶", "黑糖珍珠", "黑糖"),
            MakeRecipe("芝士奶盖茉莉", "茉莉绿茶", "无奶", "芝士奶盖", "奶盖"),
            MakeRecipe("芋泥波波奶茶", "红茶", "鲜奶", "芋泥", "芋泥"),
            MakeRecipe("抹茶红豆奶", "抹茶", "鲜奶", "红豆", "抹茶"),
            MakeRecipe("杨枝甘露", "茉莉绿茶", "椰奶", "芒果西米", "杨枝"),
            MakeRecipe("草莓芝芝", "茉莉绿茶", "无奶", "草莓芝士", "草莓"),
            MakeRecipe("百香果双响炮", "茉莉绿茶", "无奶", "百香果椰果", "百香"),
            MakeRecipe("桂花乌龙拿铁", "乌龙", "鲜奶", "桂花糖浆", "桂花"),
        };
    }

    private static Recipe MakeRecipe(string name, string tea, string milk, string topping, string iconLabel)
    {
        return new Recipe
        {
            Name = name,
            Tea = tea,
            Milk = milk,
            Topping = topping,
            IconLabel = iconLabel
        };
    }

    private void ShowOpeningDialogue()
    {
        if (settlementScreen != null)
        {
            settlementScreen.SetActive(false);
        }

        if (restScreen != null)
        {
            restScreen.SetActive(false);
        }

        if (startScreen != null)
        {
            startScreen.SetActive(false);
        }

        if (animationScreen != null)
        {
            animationScreen.SetActive(false);
        }

        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
        }

        mixingScreen.SetActive(false);
        dialogueScreen.SetActive(true);
        PlayBgm(ResolveBgm(art != null ? art.bgmDialogue : null));
        ResolveNextOrder();
        UpdateRecipeManual();
        UpdateDialogueChibis();
        UpdateDialoguePortrait(activeRecipe.CustomerName, null);
        PlayOpeningDialogue();
    }

    private void ResolveNextOrder()
    {
        currentOrderHadMistake = false;
        currentAppearance = null;

        // 日程模式：美术库配置了章节时，按“章→天→出场条目”依次出场，条目字段覆盖客人资产
        if (ScheduleMode && ResolveScheduleOrder())
        {
            return;
        }

        if (art != null && art.customers != null && art.customers.Count > 0)
        {
            customerCursor = (customerCursor + 1) % art.customers.Count;
            MilkTeaCustomer customer = art.customers[customerCursor];
            if (customer != null)
            {
                // 点单池随机抽取实现“每次点单可能不同”；池空或条目无效时回退固定点单
                MilkTeaCustomer.OrderEntry pooled = PickPooledOrder(customer);
                MilkTeaRecipe order = pooled != null ? pooled.order : customer.order;
                if (order != null)
                {
                    ApplyCustomerOrder(
                        customer,
                        order,
                        pooled != null ? pooled.sugarLevel : customer.sugarLevel,
                        pooled != null ? pooled.iceLevel : customer.iceLevel);
                    return;
                }
            }
        }

        currentCustomer = null;
        orderIndex = UnityEngine.Random.Range(0, recipes.Length);
        activeRecipe = recipes[orderIndex];
        orderSugar = UnityEngine.Random.Range(1, 3); // 客人只点少糖或多糖
        orderIce = UnityEngine.Random.Range(0, 3);   // 去冰 / 少冰 / 多冰
        manualIndex = orderIndex;
    }

    /// <summary>是否处于章节日程模式：美术库配置了至少一个章节即接管出场。</summary>
    private bool ScheduleMode
    {
        get { return art != null && art.chapters != null && art.chapters.Count > 0; }
    }

    private MilkTeaChapter CurrentChapter()
    {
        if (!ScheduleMode)
        {
            return null;
        }

        int index = Mathf.Clamp(scheduleChapterIndex, 0, art.chapters.Count - 1);
        return art.chapters[index];
    }

    /// <summary>按日程出场表取当前条目并落单；条目无效时告警并返回 false（落到旧兑底链）。</summary>
    private bool ResolveScheduleOrder()
    {
        MilkTeaChapter chapter = CurrentChapter();
        if (chapter == null || chapter.days == null || chapter.days.Count == 0)
        {
            Debug.LogWarning("章节日程：当前章节无效或没有天，回退随机出场。");
            return false;
        }

        int dayIndex = Mathf.Clamp(scheduleDayIndex, 0, chapter.days.Count - 1);
        MilkTeaChapter.Day day = chapter.days[dayIndex];
        if (day == null || day.appearances == null || day.appearances.Count == 0)
        {
            Debug.LogWarning("章节日程：当前天没有出场条目，回退随机出场。");
            return false;
        }

        // 当天第 N 位客人（servedToday 从 0 起）对应出场表第 N 条
        int appearanceIndex = Mathf.Clamp(servedToday, 0, day.appearances.Count - 1);
        MilkTeaChapter.Appearance appearance = day.appearances[appearanceIndex];
        if (appearance == null || appearance.customer == null)
        {
            Debug.LogWarning("章节日程：出场条目未填客人，回退随机出场。");
            return false;
        }

        currentAppearance = appearance;
        MilkTeaCustomer customer = appearance.customer;
        if (appearance.orderOverride != null)
        {
            // 剧情指定点单：同一角色不同天点不同的单就靠这里
            ApplyCustomerOrder(customer, appearance.orderOverride, appearance.sugarLevel, appearance.iceLevel);
            return true;
        }

        MilkTeaCustomer.OrderEntry pooled = PickPooledOrder(customer);
        MilkTeaRecipe order = pooled != null ? pooled.order : customer.order;
        if (order == null)
        {
            Debug.LogWarning("章节日程：客人“" + customer.customerName + "”没有可用的点单，回退随机出场。");
            currentAppearance = null;
            return false;
        }

        ApplyCustomerOrder(
            customer,
            order,
            pooled != null ? pooled.sugarLevel : customer.sugarLevel,
            pooled != null ? pooled.iceLevel : customer.iceLevel);
        return true;
    }

    /// <summary>把“客人 + 点单 + 糖冰”落到当前订单状态（日程/列表两条出场链共用）。</summary>
    private void ApplyCustomerOrder(MilkTeaCustomer customer, MilkTeaRecipe order, int sugar, int ice)
    {
        currentCustomer = customer;
        activeRecipe = new Recipe
        {
            Name = order.displayName,
            Tea = order.teaBase,
            Milk = order.milkBase,
            Topping = order.topping,
            IconLabel = string.IsNullOrEmpty(order.iconLabel) ? "?" : order.iconLabel,
            IconSprite = order.icon,
            Portrait = customer.portrait != null ? customer.portrait : order.customerPortrait,
            CustomerName = string.IsNullOrEmpty(customer.customerName) ? "顾客" : customer.customerName
        };
        orderSugar = Mathf.Clamp(sugar, 0, 2);
        orderIce = Mathf.Clamp(ice, 0, 2);
        manualIndex = FindManualIndex(activeRecipe.Name);
    }

    /// <summary>从客人点单池随机抽一条；池为空或条目均无效时返回 null 走固定点单。</summary>
    private static MilkTeaCustomer.OrderEntry PickPooledOrder(MilkTeaCustomer customer)
    {
        if (customer == null || customer.orderPool == null || customer.orderPool.Count == 0)
        {
            return null;
        }

        int start = UnityEngine.Random.Range(0, customer.orderPool.Count);
        for (int i = 0; i < customer.orderPool.Count; i++)
        {
            MilkTeaCustomer.OrderEntry entry = customer.orderPool[(start + i) % customer.orderPool.Count];
            if (entry != null && entry.order != null)
            {
                return entry;
            }
        }

        return null;
    }

    private int FindManualIndex(string recipeName)
    {
        for (int i = 0; i < recipes.Length; i++)
        {
            if (recipes[i].Name == recipeName)
            {
                return i;
            }
        }

        return manualIndex;
    }

    private void EnterMixing()
    {
        dialogueScreen.SetActive(false);
        mixingScreen.SetActive(true);
        PlayBgm(ResolveBgm(art != null ? art.bgmMixing : null));
        if (shakeButton != null)
        {
            shakeButton.interactable = true;
        }

        ClearSelections();
        RestoreOrderReminder();
    }

    private void PlayOpeningDialogue()
    {
        // 覆盖链：日程条目 > 客人资产 > 美术库默认
        MilkTeaDialogue dialogue = currentAppearance != null && currentAppearance.openingOverride != null
            ? currentAppearance.openingOverride
            : (currentCustomer != null && currentCustomer.openingDialogue != null
                ? currentCustomer.openingDialogue
                : (art != null ? art.defaultOpeningDialogue : null));
        if (dialogue != null && dialogue.lines != null && dialogue.lines.Count > 0)
        {
            PlayDialogue(dialogue, EnterMixing);
            return;
        }

        ConfigureDialogue(activeRecipe.CustomerName, Format("你好，我想要一杯{drink}，{ice}、{sugar}。"), "回应",
            delegate { ConfigureDialogue(ProtagonistName(), "了解了。", "开始调配", EnterMixing); });
    }

    private void ShowServingDialogue()
    {
        mixingScreen.SetActive(false);
        dialogueScreen.SetActive(true);
        UpdateDialogueChibis();
        UpdateDialoguePortrait(ProtagonistName(), null);
        MilkTeaDialogue dialogue = ResolveServingDialogue();
        if (dialogue != null && dialogue.lines != null && dialogue.lines.Count > 0)
        {
            PlayDialogue(dialogue, AdvanceAfterServe);
            return;
        }

        ConfigureDialogue(ProtagonistName(), "您的奶茶做好了。", "继续",
            delegate { ConfigureDialogue(activeRecipe.CustomerName, "谢谢！", "下一位", AdvanceAfterServe); });
    }

    /// <summary>交付对话解析：本单做错过优先走失望对话；覆盖链 日程条目 > 客人资产 > 美术库默认。</summary>
    private MilkTeaDialogue ResolveServingDialogue()
    {
        if (currentOrderHadMistake)
        {
            if (currentAppearance != null && currentAppearance.servingBadOverride != null)
            {
                return currentAppearance.servingBadOverride;
            }

            if (currentCustomer != null && currentCustomer.servingBadDialogue != null)
            {
                return currentCustomer.servingBadDialogue;
            }
        }

        if (currentAppearance != null && currentAppearance.servingOverride != null)
        {
            return currentAppearance.servingOverride;
        }

        if (currentCustomer != null && currentCustomer.servingDialogue != null)
        {
            return currentCustomer.servingDialogue;
        }

        return art != null ? art.defaultServingDialogue : null;
    }

    private void PlayDialogue(MilkTeaDialogue dialogue, Action onComplete)
    {
        PlayDialogueLine(dialogue, 0, onComplete);
    }

    private void PlayDialogueLine(MilkTeaDialogue dialogue, int index, Action onComplete)
    {
        MilkTeaDialogue.Line line = dialogue.lines[index];
        string button = string.IsNullOrEmpty(line.buttonLabel) ? "继续" : line.buttonLabel;
        Action next;
        if (index >= dialogue.lines.Count - 1)
        {
            next = onComplete;
        }
        else
        {
            int nextIndex = index + 1;
            next = delegate { PlayDialogueLine(dialogue, nextIndex, onComplete); };
        }

        string formattedSpeaker = Format(line.speaker);
        PlayVoice(line.voiceClip);
        PlayLineEvents(line);
        ConfigureDialogue(formattedSpeaker, Format(line.text), button, next, line.expression);
    }

    private string Format(string content)
    {
        if (string.IsNullOrEmpty(content) || activeRecipe == null)
        {
            return content;
        }

        string name = string.IsNullOrEmpty(activeRecipe.CustomerName) ? "顾客" : activeRecipe.CustomerName;
        return content
            .Replace("{customer}", name)
            .Replace("{drink}", activeRecipe.Name)
            .Replace("{sugar}", SugarName(orderSugar))
            .Replace("{ice}", IceName(orderIce));
    }

    private void BeginDay()
    {
        servedToday = 0;
        perfectToday = 0;
        mistakesToday = 0;
        earningsToday = 0;
        customerCursor = -1;
        if (dayTitle != null)
        {
            // 日程模式优先显示剧情天名（如“开端·雨天的初遇”）
            MilkTeaChapter chapter = CurrentChapter();
            MilkTeaChapter.Day day = chapter != null && chapter.days != null && chapter.days.Count > 0
                ? chapter.days[Mathf.Clamp(scheduleDayIndex, 0, chapter.days.Count - 1)]
                : null;
            dayTitle.text = day != null && !string.IsNullOrEmpty(day.dayName)
                ? day.dayName + " · 营业中"
                : "第 " + dayNumber + " 天 · 营业中";
        }

        ShowOpeningDialogue();
    }

    private int CustomersToday()
    {
        // 日程模式：当天出场表条目数决定今天接待几位
        MilkTeaChapter chapter = CurrentChapter();
        if (chapter != null && chapter.days != null && chapter.days.Count > 0)
        {
            MilkTeaChapter.Day day = chapter.days[Mathf.Clamp(scheduleDayIndex, 0, chapter.days.Count - 1)];
            if (day != null && day.appearances != null && day.appearances.Count > 0)
            {
                return day.appearances.Count;
            }
        }

        if (art != null && art.customers != null && art.customers.Count > 0)
        {
            return art.customers.Count;
        }

        return Mathf.Max(1, customersPerDay);
    }

    private void AdvanceAfterServe()
    {
        if (settlementScreen != null && servedToday >= CustomersToday())
        {
            ShowSettlement();
        }
        else
        {
            ShowOpeningDialogue();
        }
    }

    private void ShowSettlement()
    {
        dialogueScreen.SetActive(false);
        mixingScreen.SetActive(false);
        if (restScreen != null)
        {
            restScreen.SetActive(false);
        }

        settlementScreen.SetActive(true);
        PlayBgm(ResolveBgm(art != null ? art.bgmSettlement : null));
        if (settlementTitle != null)
        {
            settlementTitle.text = "第 " + dayNumber + " 天 · 营业结算";
        }

        if (settlementBody != null)
        {
            settlementBody.text = "今日出杯：" + servedToday + " 杯\n" +
                                  "完美制作：" + perfectToday + " 杯\n" +
                                  "操作失误：" + mistakesToday + " 次\n" +
                                  "今日营收：¥" + earningsToday;
        }

        if (settlementButtonLabel != null)
        {
            settlementButtonLabel.text = "回家休息";
        }
    }

    private void ShowRest()
    {
        if (settlementScreen != null)
        {
            settlementScreen.SetActive(false);
        }

        restScreen.SetActive(true);
        if (restHint != null)
        {
            restHint.text = string.Empty;
        }

        if (restBadgeText != null)
        {
            restBadgeText.text = "DAY " + dayNumber + "   " + RestClockText;
        }

        if (restPhoneClock != null)
        {
            restPhoneClock.text = RestClockText;
        }

        CloseRestApps();
        PlayBgm(art != null ? art.bgmRest : null);
        // 每天进入休息界面时自动存档，"读取存档"从此处继续
        AutoSaveProgress();
    }

    private void GoNextDay()
    {
        // 休息界面点"返回"：天数推进，回到开始界面由玩家手动开始新一天
        dayNumber++;
        if (ScheduleMode)
        {
            AdvanceScheduleDay();
        }

        ShowStartScreen();
    }

    /// <summary>日程模式推进到下一天；跨章/通关循环回第一章（Demo 行为）。</summary>
    private void AdvanceScheduleDay()
    {
        scheduleDayIndex++;
        MilkTeaChapter chapter = CurrentChapter();
        if (chapter == null || chapter.days == null || scheduleDayIndex >= chapter.days.Count)
        {
            scheduleDayIndex = 0;
            scheduleChapterIndex++;
            if (scheduleChapterIndex >= art.chapters.Count)
            {
                scheduleChapterIndex = 0;
                Debug.Log("全部章节完成，回到第一章循环。");
            }
            else
            {
                MilkTeaChapter next = art.chapters[Mathf.Clamp(scheduleChapterIndex, 0, art.chapters.Count - 1)];
                Debug.Log("进入章节：" + (next != null ? next.chapterName : "未命名"));
            }
        }
    }

    private void Stay()
    {
        if (restHint != null)
        {
            restHint.text = "再歇一会儿……\n想开始营业时，点「返回」。";
        }
    }

    private void OnRestAppClicked(string appName)
    {
        // 角色 App = 朋友圈；相册 App = CG 图鉴；其余暂开发中
        if (appName == "角色")
        {
            OpenRestApp(appMomentsPanel, BuildMomentsContent);
            return;
        }

        if (appName == "相册")
        {
            OpenRestApp(appAlbumPanel, BuildAlbumContent);
            return;
        }

        if (restHint != null)
        {
            restHint.text = "「" + appName + "」功能开发中，敬请期待！";
        }
    }

    private void OpenRestApp(GameObject panel, Action builder)
    {
        CloseRestApps();
        if (panel == null)
        {
            if (restHint != null)
            {
                restHint.text = "手机面板尚未生成：请跑一次建场菜单「奶茶店 Demo/构建全部界面（保留手动调整）」。";
            }

            return;
        }

        panel.SetActive(true);
        if (builder != null)
        {
            builder();
        }
    }

    private void CloseRestApps()
    {
        if (appMomentsPanel != null)
        {
            appMomentsPanel.SetActive(false);
        }

        if (appAlbumPanel != null)
        {
            appAlbumPanel.SetActive(false);
        }
    }

    /// <summary>绑定手机 App 面板的关闭按钮（节点由建场工具生成，默认隐藏所以 Awake 时要含隐藏查找）。</summary>
    private void BindAppCloseButton(GameObject panel)
    {
        if (panel == null)
        {
            return;
        }

        Transform close = panel.transform.Find("App Close Button");
        Button button = close != null ? close.GetComponent<Button>() : null;
        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(CloseRestApps);
        }
    }

    // ======== 手机 App 内容：朋友圈与相册（运行时按数据动态生成条目） ========

    private void BuildMomentsContent()
    {
        if (appMomentsContent == null)
        {
            return;
        }

        EnsureVerticalContent(appMomentsContent);
        ClearChildren(appMomentsContent);
        MilkTeaSocialContent content = art != null ? art.socialContent : null;
        if (content == null || content.posts == null || content.posts.Count == 0)
        {
            CreateRuntimeLabel(appMomentsContent, "还没有任何动态。\n（把社交内容资产拖进美术库 socialContent 槽）");
            return;
        }

        int shown = 0;
        for (int i = content.posts.Count - 1; i >= 0; i--)
        {
            MilkTeaSocialContent.Post post = content.posts[i];
            if (post == null || !IsPostUnlocked(post))
            {
                continue;
            }

            CreateRuntimePostEntry(appMomentsContent, post);
            shown++;
        }

        if (shown == 0)
        {
            CreateRuntimeLabel(appMomentsContent, "还没有解锁任何动态，继续经营吧。");
        }
    }

    private void BuildAlbumContent()
    {
        if (appAlbumContent == null)
        {
            return;
        }

        EnsureGridContent(appAlbumContent);
        ClearChildren(appAlbumContent);
        MilkTeaSocialContent content = art != null ? art.socialContent : null;
        if (content == null || content.cgs == null || content.cgs.Count == 0)
        {
            CreateRuntimeLabel(appAlbumContent, "相册还是空的。\n（把社交内容资产拖进美术库 socialContent 槽）");
            return;
        }

        for (int i = 0; i < content.cgs.Count; i++)
        {
            CreateRuntimeCgCell(appAlbumContent, content.cgs[i]);
        }
    }

    /// <summary>朋友圈解锁判定：无日程时全显示；有日程时按“章·天”锚点。</summary>
    private bool IsPostUnlocked(MilkTeaSocialContent.Post post)
    {
        if (!ScheduleMode)
        {
            return true;
        }

        return scheduleChapterIndex > post.unlockChapterIndex
            || (scheduleChapterIndex == post.unlockChapterIndex && scheduleDayIndex >= post.unlockDayIndex);
    }

    private void CreateRuntimePostEntry(RectTransform parent, MilkTeaSocialContent.Post post)
    {
        GameObject entry = CreateAutoLayoutItem(parent, "Post", new Color(1f, 1f, 1f, 0.06f), 14, 8);

        // 头像 + 作者行
        GameObject header = CreateHorizontalLayoutItem(entry.transform, "Header", new Color(0f, 0f, 0f, 0f), 10f);

        GameObject avatarHolder = CreateRuntimePanel(header.transform, "Avatar", new Color(0.35f, 0.42f, 0.5f, 1f));
        Image avatar = avatarHolder.GetComponent<Image>();
        avatar.sprite = post.author != null ? post.author.portrait : null;
        avatar.color = post.author != null && post.author.portrait != null ? Color.white : new Color(0.55f, 0.62f, 0.7f, 1f);
        avatar.preserveAspect = true;
        LayoutElement avatarLayout = avatarHolder.AddComponent<LayoutElement>();
        avatarLayout.preferredWidth = 52f;
        avatarLayout.preferredHeight = 52f;
        avatarLayout.minWidth = 52f;
        avatarLayout.minHeight = 52f;

        string authorName = post.author != null ? post.author.characterName : "匿名";
        Text authorText = CreateRuntimeText(header.transform, authorName, 28, cream, TextAnchor.MiddleLeft);
        LayoutElement authorLayout = authorText.GetComponent<LayoutElement>();
        authorLayout.flexibleWidth = 1f;

        CreateRuntimeText(entry.transform, Format(post.text), 26, new Color(0.92f, 0.94f, 0.98f, 1f), TextAnchor.UpperLeft);

        if (post.image != null)
        {
            GameObject imageHolder = CreateAutoLayoutItem(entry.transform, "Photo", new Color(0f, 0f, 0f, 0f), 0, 0);
            Image photo = imageHolder.AddComponent<Image>();
            photo.sprite = post.image;
            photo.color = Color.white;
            photo.preserveAspect = true;
            LayoutElement photoLayout = imageHolder.AddComponent<LayoutElement>();
            photoLayout.preferredHeight = 220f;
        }
    }

    private void CreateRuntimeCgCell(RectTransform parent, MilkTeaSocialContent.Cg cg)
    {
        bool unlocked = cg != null && !string.IsNullOrEmpty(cg.id) && unlockedContentIds.Contains("cg:" + cg.id);
        GameObject cell = CreateRuntimePanel(parent, "Cg Cell",
            unlocked ? new Color(1f, 1f, 1f, 0.1f) : new Color(0f, 0f, 0f, 0.4f));
        Image image = cell.GetComponent<Image>();
        if (unlocked && cg.image != null)
        {
            image.sprite = cg.image;
            image.color = Color.white;
            image.type = Image.Type.Simple;
            image.preserveAspect = true;
            return;
        }

        Text lockLabel = CreateRuntimeText(cell.transform, unlocked ? "未配图" : "?", 40,
            new Color(1f, 1f, 1f, 0.5f), TextAnchor.MiddleCenter);
        Stretch(lockLabel.rectTransform);
    }

    /// <summary>把 RectTransform 拉伸填满父节点（只用于运行时动态 UI）。</summary>
    private static void Stretch(RectTransform rect)
    {
        if (rect == null)
        {
            return;
        }

        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    // ======== 运行时动态 UI 的小工具（只用于手机 App 条目） ========

    private static void ClearChildren(RectTransform container)
    {
        for (int i = container.childCount - 1; i >= 0; i--)
        {
            Destroy(container.GetChild(i).gameObject);
        }
    }

    private static void EnsureVerticalContent(RectTransform content)
    {
        VerticalLayoutGroup layout = content.GetComponent<VerticalLayoutGroup>();
        if (layout == null)
        {
            layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
        }

        layout.spacing = 10f;
        layout.padding = new RectOffset(8, 8, 8, 8);
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        ContentSizeFitter fitter = content.GetComponent<ContentSizeFitter>();
        if (fitter == null)
        {
            fitter = content.gameObject.AddComponent<ContentSizeFitter>();
        }

        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
    }

    private static void EnsureGridContent(RectTransform content)
    {
        GridLayoutGroup grid = content.GetComponent<GridLayoutGroup>();
        if (grid == null)
        {
            grid = content.gameObject.AddComponent<GridLayoutGroup>();
        }

        grid.cellSize = new Vector2(140f, 140f);
        grid.spacing = new Vector2(10f, 10f);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 3;
        grid.childAlignment = TextAnchor.UpperLeft;
        grid.padding = new RectOffset(8, 8, 8, 8);
    }

    private static GameObject CreateAutoLayoutItem(Transform parent, string name, Color background, int padding, int spacing)
    {
        GameObject item = CreateRuntimePanel(parent, name, background);
        VerticalLayoutGroup layout = item.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(padding, padding, padding, padding);
        layout.spacing = spacing;
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        ContentSizeFitter fitter = item.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        return item;
    }

    private static GameObject CreateHorizontalLayoutItem(Transform parent, string name, Color background, float spacing)
    {
        GameObject item = CreateRuntimePanel(parent, name, background);
        HorizontalLayoutGroup layout = item.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = spacing;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
        ContentSizeFitter fitter = item.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        return item;
    }

    private static GameObject CreateRuntimePanel(Transform parent, string name, Color color)
    {
        GameObject panel = new GameObject(name, typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(parent, false);
        panel.GetComponent<Image>().color = color;
        panel.GetComponent<Image>().raycastTarget = false;
        return panel;
    }

    private Text CreateRuntimeText(Transform parent, string content, int fontSize, Color color, TextAnchor anchor)
    {
        GameObject go = new GameObject("Text", typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);
        Text text = go.GetComponent<Text>();
        text.font = art != null && art.uiFont != null ? art.uiFont : CreateChineseFont();
        if (text.font == null)
        {
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }

        text.fontSize = fontSize;
        text.color = color;
        text.alignment = anchor;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.supportRichText = false;
        text.text = content;
        LayoutElement layout = go.AddComponent<LayoutElement>();
        layout.minHeight = fontSize * 1.4f;
        return text;
    }

    private void CreateRuntimeLabel(RectTransform parent, string content)
    {
        CreateRuntimeText(parent, content, 26, new Color(1f, 1f, 1f, 0.6f), TextAnchor.UpperLeft);
    }

    private void ShowStartScreen()
    {
        dialogueScreen.SetActive(false);
        mixingScreen.SetActive(false);
        if (settlementScreen != null)
        {
            settlementScreen.SetActive(false);
        }

        if (restScreen != null)
        {
            restScreen.SetActive(false);
        }

        if (animationScreen != null)
        {
            animationScreen.SetActive(false);
        }

        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
        }

        if (startScreen != null)
        {
            startScreen.SetActive(true);
        }

        PlayBgm(art != null ? art.bgmStart : null);
    }

    private void OnStartGame()
    {
        if (startScreen != null)
        {
            startScreen.SetActive(false);
        }

        // 开场动画只在首次进入游戏时播一次，之后直接开始营业
        if (PlayerPrefs.GetInt(IntroPlayedKey, 0) == 1)
        {
            BeginDay();
            return;
        }

        ShowIntro();
    }

    private void OnLoadGame()
    {
        // 读取自动存档（0 号位）：恢复章/天/天数与已解锁内容；没有存档则从起始进度开始
        MilkTeaSaveSystem.SaveData save = MilkTeaSaveSystem.Load(MilkTeaSaveSystem.AutoSaveSlot);
        if (save != null)
        {
            scheduleChapterIndex = Mathf.Max(0, save.chapterIndex);
            scheduleDayIndex = Mathf.Max(0, save.dayIndex);
            dayNumber = Mathf.Max(1, save.dayNumber);
            if (save.unlockedIds != null)
            {
                unlockedContentIds.UnionWith(save.unlockedIds);
            }

            Debug.Log("已读取存档：" + (string.IsNullOrEmpty(save.label) ? MilkTeaSaveSystem.GetPath(MilkTeaSaveSystem.AutoSaveSlot) : save.label));
        }
        else
        {
            Debug.Log("没有找到存档，从起始进度开始（" + MilkTeaSaveSystem.GetPath(MilkTeaSaveSystem.AutoSaveSlot) + "）");
        }

        if (startScreen != null)
        {
            startScreen.SetActive(false);
        }

        BeginDay();
    }

    private void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void ShowIntro()
    {
        dialogueScreen.SetActive(false);
        mixingScreen.SetActive(false);
        if (animationScreen == null)
        {
            BeginDay();
            return;
        }

        animationScreen.SetActive(true);
        introFinished = false;

        if (introVideo != null && introVideo.clip != null)
        {
            if (countdownLabel != null)
            {
                countdownLabel.gameObject.SetActive(false);
            }

            introVideo.renderMode = VideoRenderMode.APIOnly;
            introVideo.isLooping = false;
            introVideo.playOnAwake = false;
            introVideo.loopPointReached -= OnIntroVideoEnd;
            introVideo.loopPointReached += OnIntroVideoEnd;
            introVideo.prepareCompleted -= OnIntroVideoPrepared;
            introVideo.prepareCompleted += OnIntroVideoPrepared;
            introVideo.Prepare();
            return;
        }

        if (countdownLabel != null)
        {
            countdownLabel.gameObject.SetActive(true);
            countdownLabel.text = Mathf.CeilToInt(introCountdownSeconds > 0f ? introCountdownSeconds : 10f).ToString();
        }

        if (introRoutine != null)
        {
            StopCoroutine(introRoutine);
        }

        introRoutine = StartCoroutine(IntroCountdown());
    }

    private void OnIntroVideoPrepared(VideoPlayer source)
    {
        if (introVideoImage != null)
        {
            introVideoImage.texture = source.texture;
            introVideoImage.color = Color.white;
        }

        source.Play();
    }

    private void OnIntroVideoEnd(VideoPlayer source)
    {
        FinishIntro();
    }

    private IEnumerator IntroCountdown()
    {
        float remaining = introCountdownSeconds > 0f ? introCountdownSeconds : 10f;
        while (remaining > 0f)
        {
            if (countdownLabel != null)
            {
                countdownLabel.text = Mathf.CeilToInt(remaining).ToString();
            }

            remaining -= Time.deltaTime;
            yield return null;
        }

        introRoutine = null;
        FinishIntro();
    }

    private void SkipIntro()
    {
        if (introRoutine != null)
        {
            StopCoroutine(introRoutine);
            introRoutine = null;
        }

        FinishIntro();
    }

    private void FinishIntro()
    {
        if (introFinished)
        {
            return;
        }

        introFinished = true;
        // 无论播完还是手动跳过，都记住“已看过开场”，下次启动直接营业
        PlayerPrefs.SetInt(IntroPlayedKey, 1);
        PlayerPrefs.Save();
        if (introVideo != null)
        {
            introVideo.loopPointReached -= OnIntroVideoEnd;
            introVideo.prepareCompleted -= OnIntroVideoPrepared;
            introVideo.Stop();
        }

        if (animationScreen != null)
        {
            animationScreen.SetActive(false);
        }

        BeginDay();
    }

    private void OpenSettings()
    {
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(true);
            UpdateSettingsLabels();
        }
    }

    private void CloseSettings()
    {
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
        }
    }

    private void LoadSettings()
    {
        masterVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(VolumePref, 1f));
        resolutionIndex = Mathf.Clamp(PlayerPrefs.GetInt(ResolutionPref, 2), 0, ResolutionOptions.Length - 1);
        languageIndex = Mathf.Clamp(PlayerPrefs.GetInt(LanguagePref, 0), 0, LanguageOptions.Length - 1);
        AudioListener.volume = masterVolume;
        if (!Application.isEditor)
        {
            ApplyResolution();
        }

        UpdateSettingsLabels();
    }

    private void ChangeVolume(int direction)
    {
        masterVolume = Mathf.Clamp01(Mathf.Round((masterVolume + direction * 0.1f) * 10f) / 10f);
        AudioListener.volume = masterVolume;
        PlayerPrefs.SetFloat(VolumePref, masterVolume);
        PlayerPrefs.Save();
        UpdateSettingsLabels();
    }

    private void ChangeResolution(int direction)
    {
        int count = ResolutionOptions.Length;
        resolutionIndex = (resolutionIndex + direction + count) % count;
        PlayerPrefs.SetInt(ResolutionPref, resolutionIndex);
        PlayerPrefs.Save();
        ApplyResolution();
        UpdateSettingsLabels();
    }

    private void ChangeLanguage(int direction)
    {
        int count = LanguageOptions.Length;
        languageIndex = (languageIndex + direction + count) % count;
        PlayerPrefs.SetInt(LanguagePref, languageIndex);
        PlayerPrefs.Save();
        UpdateSettingsLabels();
    }

    private void ApplyResolution()
    {
        ResolutionOption option = ResolutionOptions[resolutionIndex];
        Screen.SetResolution(option.Width, option.Height, option.Fullscreen);
    }

    private void UpdateSettingsLabels()
    {
        if (volumeValueLabel != null)
        {
            volumeValueLabel.text = Mathf.RoundToInt(masterVolume * 100f) + "%";
        }

        if (resolutionValueLabel != null)
        {
            resolutionValueLabel.text = ResolutionOptions[resolutionIndex].Label;
        }

        if (languageValueLabel != null)
        {
            languageValueLabel.text = LanguageOptions[languageIndex];
        }
    }

    private void ConfigureDialogue(string speaker, string line, string buttonText, Action action)
    {
        ConfigureDialogue(speaker, line, buttonText, action, null);
    }

    private void ConfigureDialogue(string speaker, string line, string buttonText, Action action, string expression)
    {
        UpdateDialoguePortrait(speaker, expression);
        ApplyDialogueTextColors();
        dialogueSpeaker.text = speaker;
        dialogueLine.text = line;
        dialogueButtonLabel.text = buttonText;
        dialogueButton.onClick.RemoveAllListeners();
        dialogueButton.interactable = action != null;
        if (action != null)
        {
            // 统一走按钮分发：全屏 CG 开着时，第一次点击先关 CG 不推进行
            dialogueButton.onClick.AddListener(delegate { OnDialogueButtonClicked(action); });
        }
    }

    private void OnDialogueButtonClicked(Action action)
    {
        if (cgOverlayOpen)
        {
            CloseCgOverlay();
            return;
        }

        action();
    }

    private void ChangeCategory(int direction)
    {
        int count = Enum.GetValues(typeof(IngredientCategory)).Length;
        int next = ((int)currentCategory + direction + count) % count;
        currentCategory = (IngredientCategory)next;
        UpdateCategoryView();
        RestoreOrderReminder();
    }

    private void UpdateCategoryView()
    {
        foreach (KeyValuePair<IngredientCategory, GameObject> entry in categoryPanels)
        {
            entry.Value.SetActive(entry.Key == currentCategory);
        }

        categoryTitle.text = "原料选择 · " + CategoryName(currentCategory);
        UpdateCategoryIcon();
        UpdateChoiceVisuals();
    }

    private void UpdateCategoryIcon()
    {
        if (categoryIcon == null || art == null)
        {
            return;
        }

        Sprite icon = null;
        switch (currentCategory)
        {
            case IngredientCategory.Tea:
                icon = art.teaCategoryIcon;
                break;
            case IngredientCategory.Milk:
                icon = art.milkCategoryIcon;
                break;
            case IngredientCategory.Topping:
                icon = art.toppingCategoryIcon;
                break;
        }

        if (icon != null)
        {
            categoryIcon.sprite = icon;
            categoryIcon.color = Color.white;
            categoryIcon.enabled = true;
        }
        else
        {
            categoryIcon.enabled = false;
        }
    }

    private void SelectIngredient(IngredientCategory category, string option)
    {
        if (category == IngredientCategory.Tea)
        {
            selectedTea = option;
        }
        else if (category == IngredientCategory.Milk)
        {
            selectedMilk = option;
        }
        else
        {
            selectedTopping = option;
        }

        UpdateChoiceVisuals();
        UpdateSelectionSummary();
        RestoreOrderReminder();
    }

    private void UpdateChoiceVisuals()
    {
        foreach (KeyValuePair<IngredientCategory, Dictionary<string, Image>> group in choiceImages)
        {
            string selected = GetSelectedIngredient(group.Key);
            foreach (KeyValuePair<string, Image> item in group.Value)
            {
                if (item.Value != null)
                {
                    item.Value.color = item.Key == selected ? mint : panelLight;
                }
            }
        }
    }

    private void ChangeLevel(bool isSugar, int clickedIndex)
    {
        if (isSugar)
        {
            sugarLevel = NextLevel(sugarLevel, clickedIndex);
        }
        else
        {
            iceLevel = NextLevel(iceLevel, clickedIndex);
        }

        UpdateLevelBlocks();
        UpdateSelectionSummary();
        RestoreOrderReminder();
    }

    private static int NextLevel(int current, int clickedIndex)
    {
        if (current == 0)
        {
            return clickedIndex == 0 ? 1 : 2;
        }

        if (current == 1)
        {
            return clickedIndex == 0 ? 0 : 2;
        }

        return clickedIndex == 0 ? 1 : 0;
    }

    private void UpdateLevelBlocks()
    {
        for (int i = 0; i < sugarBlocks.Length; i++)
        {
            if (sugarBlocks[i] != null)
            {
                sugarBlocks[i].color = i < sugarLevel ? yellow : gray;
            }
        }

        for (int i = 0; i < iceBlocks.Length; i++)
        {
            if (iceBlocks[i] != null)
            {
                iceBlocks[i].color = i < iceLevel ? blue : gray;
            }
        }

        if (sugarStateLabel != null)
        {
            sugarStateLabel.text = SugarName(sugarLevel);
        }

        if (iceStateLabel != null)
        {
            iceStateLabel.text = IceName(iceLevel);
        }
    }

    private void UpdateSelectionSummary()
    {
        selectionSummary.text = "已选：茶底 " + EmptyAsDash(selectedTea) +
                                " ｜ 奶底 " + EmptyAsDash(selectedMilk) +
                                " ｜ 配料 " + EmptyAsDash(selectedTopping) +
                                "\n糖度 " + SugarName(sugarLevel) + " ｜ 冰度 " + IceName(iceLevel);
    }

    private void BeginSubmit()
    {
        if (!isSubmitting)
        {
            StartCoroutine(SubmitRoutine());
        }
    }

    private IEnumerator SubmitRoutine()
    {
        isSubmitting = true;
        if (shakeButton != null)
        {
            shakeButton.interactable = false;
        }

        machineStatus.text = "正在封装饮品…";

        float duration = 0.3f;
        Vector2 originalPosition = shakeRect != null ? shakeRect.anchoredPosition : Vector2.zero;
        for (float elapsed = 0f; elapsed < duration; elapsed += Time.deltaTime)
        {
            float progress = elapsed / duration;
            if (shakeRect != null)
            {
                // 左右晃动 + 轻微旋转，围绕中心摇动
                float wobble = Mathf.Sin(progress * Mathf.PI * 6f) * 12f;
                float angle = Mathf.Sin(progress * Mathf.PI * 6f) * 5f;
                shakeRect.anchoredPosition = originalPosition + new Vector2(wobble, 0f);
                shakeRect.localRotation = Quaternion.Euler(0f, 0f, angle);
            }

            yield return null;
        }

        if (shakeRect != null)
        {
            shakeRect.anchoredPosition = originalPosition;
            shakeRect.localRotation = Quaternion.identity;
        }

        yield return new WaitForSeconds(0.15f);

        Recipe recipe = activeRecipe;
        bool isCorrect = selectedTea == recipe.Tea && selectedMilk == recipe.Milk &&
                         selectedTopping == recipe.Topping && sugarLevel == orderSugar && iceLevel == orderIce;

        if (isCorrect)
        {
            SetUnlocked(manualIndex);
            UpdateRecipeManual();
            servedToday++;
            earningsToday += currentOrderHadMistake ? 15 : 20;
            if (!currentOrderHadMistake)
            {
                perfectToday++;
            }

            machineStatus.text = "制作正确！";
            machineStatus.color = mint;
            yield return new WaitForSeconds(0.8f);
            ShowServingDialogue();
        }
        else
        {
            mistakesToday++;
            currentOrderHadMistake = true;
            orderSpeaker.text = "主角（内心）";
            orderLine.text = "这么做好像不正确。\n顾客需求：" + recipe.Name + " · " +
                             SugarName(orderSugar) + " · " + IceName(orderIce);
            machineStatus.text = "请重新调配";
            machineStatus.color = coral;
            ClearSelections();
            if (shakeButton != null)
            {
                shakeButton.interactable = true;
            }
        }

        isSubmitting = false;
    }

    private void ClearSelections()
    {
        selectedTea = string.Empty;
        selectedMilk = string.Empty;
        selectedTopping = string.Empty;
        sugarLevel = 0;
        iceLevel = 0;
        currentCategory = IngredientCategory.Tea;
        machineStatus.text = "请选择原料后点击开始摇动";
        machineStatus.color = cream;
        UpdateCategoryView();
        UpdateLevelBlocks();
        UpdateSelectionSummary();
    }

    private void RestoreOrderReminder()
    {
        Recipe recipe = activeRecipe;
        orderSpeaker.text = "顾客需求";
        orderLine.text = recipe.Name + " · " + SugarName(orderSugar) + " · " + IceName(orderIce) +
                         "\n茶底、奶底和配料需要由你完成。";
    }

    private void ChangeManualPage(int direction)
    {
        manualIndex = (manualIndex + direction + recipes.Length) % recipes.Length;
        UpdateRecipeManual();
    }

    private void UpdateRecipeManual()
    {
        Recipe recipe = recipes[manualIndex];
        bool showSprite = recipe.IconSprite != null;
        if (recipeIconImage != null)
        {
            recipeIconImage.gameObject.SetActive(showSprite);
            if (showSprite)
            {
                ApplySprite(recipeIconImage, recipe.IconSprite);
            }
        }

        if (recipeIcon != null)
        {
            recipeIcon.gameObject.SetActive(!showSprite);
            recipeIcon.text = recipe.IconLabel;
            recipeIcon.fontSize = 30;
            recipeIcon.color = mint;
        }

        recipeDetails.text = recipe.Name + "\n茶底：" + recipe.Tea + "\n奶底：" + recipe.Milk +
                             "\n配料：" + recipe.Topping;
        recipePage.text = (manualIndex + 1) + " / " + recipes.Length;
    }

    private string ProtagonistName()
    {
        return art != null && !string.IsNullOrEmpty(art.protagonistName)
            ? art.protagonistName
            : "主角";
    }

    private bool IsProtagonistSpeaker(string speaker)
    {
        if (string.IsNullOrEmpty(speaker))
        {
            return false;
        }

        string protagonistName = ProtagonistName();
        return speaker == "主角" || speaker.StartsWith("主角（", StringComparison.Ordinal)
            || speaker == protagonistName || speaker.StartsWith(protagonistName + "（", StringComparison.Ordinal);
    }

    private void EnsureAudioChannels()
    {
        if (voiceSource == null)
        {
            voiceSource = gameObject.AddComponent<AudioSource>();
            voiceSource.playOnAwake = false;
        }

        if (sfxSource == null)
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.playOnAwake = false;
        }

        if (bgmSource == null)
        {
            bgmSource = gameObject.AddComponent<AudioSource>();
            bgmSource.playOnAwake = false;
            bgmSource.loop = true;
        }
    }

    /// <summary>播语音：独占通道，新语音打断上一句。</summary>
    private void PlayVoice(AudioClip clip)
    {
        if (clip == null)
        {
            return;
        }

        if (voiceSource == null)
        {
            voiceSource = gameObject.AddComponent<AudioSource>();
            voiceSource.playOnAwake = false;
        }

        voiceSource.Stop();
        voiceSource.clip = clip;
        voiceSource.Play();
    }

    /// <summary>触发行内附加演出：音效/全屏CG/立绘序列帧动画。</summary>
    private void PlayLineEvents(MilkTeaDialogue.Line line)
    {
        if (line == null || line.events == null)
        {
            return;
        }

        for (int i = 0; i < line.events.Count; i++)
        {
            MilkTeaDialogue.LineEvent lineEvent = line.events[i];
            if (lineEvent == null)
            {
                continue;
            }

            switch (lineEvent.type)
            {
                case MilkTeaDialogue.LineEventType.PlaySfx:
                    PlaySfx(lineEvent.audioClip);
                    break;
                case MilkTeaDialogue.LineEventType.ShowCg:
                    ShowCgEvent(lineEvent.targetName);
                    break;
                case MilkTeaDialogue.LineEventType.PlayAnimation:
                    PlayPortraitAnimation(lineEvent.targetName);
                    break;
            }
        }
    }

    /// <summary>ShowCg 演出：按 targetName 解锁 CG 并弹全屏展示；点对话按钮先关浮层再推进行。</summary>
    private void ShowCgEvent(string cgId)
    {
        if (string.IsNullOrEmpty(cgId))
        {
            return;
        }

        UnlockContent("cg:" + cgId);
        MilkTeaSocialContent.Cg cg = art != null && art.socialContent != null
            ? art.socialContent.FindCg(cgId)
            : null;
        if (cg == null || cg.image == null || cgOverlay == null || cgOverlayImage == null)
        {
            if (cg == null)
            {
                Debug.LogWarning("ShowCg：社交内容库里找不到 CG“" + cgId + "”，已解锁但未展示。");
            }

            return;
        }

        ApplySprite(cgOverlayImage, cg.image);
        if (cgOverlayHint != null)
        {
            cgOverlayHint.gameObject.SetActive(true);
        }

        cgOverlay.SetActive(true);
        cgOverlayOpen = true;
    }

    private void CloseCgOverlay()
    {
        if (cgOverlay != null)
        {
            cgOverlay.SetActive(false);
        }

        cgOverlayOpen = false;
    }

    /// <summary>PlayAnimation 演出：按名在动画库找序列帧，在立绘区播放；下一行说话时恢复立绘。</summary>
    private void PlayPortraitAnimation(string animationName)
    {
        if (portraitAnimator == null || portraitImage == null || art == null || art.animations == null)
        {
            return;
        }

        for (int i = 0; i < art.animations.Count; i++)
        {
            MilkTeaArtLibrary.NamedFrames animation = art.animations[i];
            if (animation == null || animation.name != animationName)
            {
                continue;
            }

            if (animation.frames == null || animation.frames.Count < 2)
            {
                Debug.LogWarning("PlayAnimation：动画“" + animationName + "”序列帧不足 2 帧。");
                return;
            }

            portraitAnimator.enabled = true;
            portraitAnimator.Play(animation.frames, animation.fps, animation.loop);
            if (portraitLabel != null)
            {
                portraitLabel.gameObject.SetActive(false);
            }

            return;
        }

        Debug.LogWarning("PlayAnimation：动画库里找不到“" + animationName + "”。");
    }

    /// <summary>播一次性效果音。</summary>
    private void PlaySfx(AudioClip clip)
    {
        if (clip == null)
        {
            return;
        }

        if (sfxSource == null)
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.playOnAwake = false;
        }

        sfxSource.PlayOneShot(clip);
    }

    // ======== 背景音乐：切界面换曲，同曲不重播；章节 bgm 覆盖营业期默认曲 ========

    private void PlayBgm(AudioClip clip)
    {
        if (clip == null)
        {
            return;
        }

        if (bgmSource == null)
        {
            EnsureAudioChannels();
        }

        if (currentBgm == clip && bgmSource.isPlaying)
        {
            return;
        }

        currentBgm = clip;
        bgmSource.clip = clip;
        bgmSource.Play();
    }

    /// <summary>营业期（对话/调配/结算/休息）BGM：当前章节配了 bgm 则优先。</summary>
    private AudioClip ResolveBgm(AudioClip fallback)
    {
        MilkTeaChapter chapter = CurrentChapter();
        if (chapter != null && chapter.bgm != null)
        {
            return chapter.bgm;
        }

        return fallback;
    }

    private Coroutine portraitBounceRoutine;
    private Vector2 portraitBounceOrigin;
    private bool portraitBounceOriginReady;

    // 当前立绘所属说话人；用于“表情留空时保持现状”的判断
    private string currentPortraitCharacter;

    private void UpdateDialoguePortrait(string speaker, string expression)
    {
        if (portraitImage == null)
        {
            return;
        }

        // 旁白/环境描写行：没有说话人，立绘区清空隐藏；下一行有说话人时由正常流程恢复
        if (speaker == "旁白")
        {
            if (portraitAnimator != null)
            {
                portraitAnimator.Clear();
            }

            portraitImage.sprite = null;
            portraitImage.color = new Color(0f, 0f, 0f, 0f);
            if (portraitLabel != null)
            {
                portraitLabel.gameObject.SetActive(false);
            }

            currentPortraitCharacter = null;
            return;
        }

        StartPortraitBounce();

        // 新链：说话人命中角色注册表时，按表情名切换差分立绘
        MilkTeaCharacter character = FindCharacter(speaker);
        if (character != null)
        {
            Sprite expressionPortrait = character.GetPortrait(expression);
            if (expressionPortrait != null)
            {
                ApplyPortrait(expressionPortrait, null, 8f, speaker);
                currentPortraitCharacter = speaker;
                return;
            }

            // 表情留空且当前已显示该角色：保持现有表情不动
            if (string.IsNullOrEmpty(expression) && currentPortraitCharacter == speaker)
            {
                return;
            }

            if (character.portrait != null)
            {
                // 表情名未命中差分时回退角色默认立绘
                ApplyPortrait(character.portrait, null, 8f, speaker);
                currentPortraitCharacter = speaker;
                return;
            }

            // 角色未配任何立绘：继续走下方旧回退链
        }
        else if (string.IsNullOrEmpty(expression) && currentPortraitCharacter == speaker)
        {
            // 无角色资产但同名且无新表情：保持当前立绘
            return;
        }

        if (IsProtagonistSpeaker(speaker))
        {
            ApplyPortrait(
                art != null ? art.protagonistPortrait : null,
                art != null ? art.protagonistPortraitFrames : null,
                art != null ? art.protagonistPortraitFps : 8f,
                ProtagonistName());
            currentPortraitCharacter = speaker;
            return;
        }

        Sprite customerPortrait = currentCustomer != null && currentCustomer.portrait != null
            ? currentCustomer.portrait
            : (activeRecipe != null && activeRecipe.Portrait != null
                ? activeRecipe.Portrait
                : (art != null ? art.defaultCustomerPortrait : null));
        ApplyPortrait(
            customerPortrait,
            currentCustomer != null ? currentCustomer.portraitFrames : null,
            currentCustomer != null ? currentCustomer.portraitFps : 8f,
            activeRecipe != null ? activeRecipe.CustomerName : "顾客");
        currentPortraitCharacter = speaker;
    }

    /// <summary>按说话人名在角色注册表中查找角色；主角台词优先用主角角色资产。</summary>
    private MilkTeaCharacter FindCharacter(string speakerName)
    {
        if (art == null || string.IsNullOrEmpty(speakerName))
        {
            return null;
        }

        if (art.protagonistCharacter != null && IsProtagonistSpeaker(speakerName))
        {
            return art.protagonistCharacter;
        }

        if (art.characters == null)
        {
            return null;
        }

        for (int i = 0; i < art.characters.Count; i++)
        {
            MilkTeaCharacter candidate = art.characters[i];
            if (candidate != null && candidate.characterName == speakerName)
            {
                return candidate;
            }
        }

        return null;
    }

    /// <summary>说话人切换时立绘上下弹跳一下；连续说话重复触发会先复位再重新弹。</summary>
    private void StartPortraitBounce()
    {
        RectTransform rect = portraitImage.rectTransform;
        if (portraitBounceRoutine != null)
        {
            StopCoroutine(portraitBounceRoutine);
            rect.anchoredPosition = portraitBounceOrigin;
        }

        if (!portraitBounceOriginReady)
        {
            portraitBounceOrigin = rect.anchoredPosition;
            portraitBounceOriginReady = true;
        }

        portraitBounceRoutine = StartCoroutine(BouncePortraitRoutine());
    }

    private IEnumerator BouncePortraitRoutine()
    {
        RectTransform rect = portraitImage.rectTransform;
        const float duration = 0.25f;
        const float bounceHeight = 18f;
        for (float elapsed = 0f; elapsed < duration; elapsed += Time.deltaTime)
        {
            float progress = elapsed / duration;
            float offset = Mathf.Sin(progress * Mathf.PI) * bounceHeight;
            rect.anchoredPosition = portraitBounceOrigin + new Vector2(0f, offset);
            yield return null;
        }

        rect.anchoredPosition = portraitBounceOrigin;
        portraitBounceRoutine = null;
    }

    private void ApplyPortrait(Sprite staticPortrait, IList<Sprite> frames, float fps, string fallbackName)
    {
        if (portraitAnimator != null)
        {
            if (HasAtLeastTwoSprites(frames))
            {
                portraitAnimator.enabled = true;
                portraitAnimator.Play(frames, fps, true);
                if (portraitLabel != null)
                {
                    portraitLabel.gameObject.SetActive(false);
                }

                return;
            }

            portraitAnimator.Clear();
        }

        if (ApplySprite(portraitImage, staticPortrait))
        {
            if (portraitLabel != null)
            {
                portraitLabel.gameObject.SetActive(false);
            }

            return;
        }

        portraitImage.sprite = null;
        portraitImage.color = new Color(0f, 0f, 0f, 0f);
        if (portraitLabel != null)
        {
            portraitLabel.gameObject.SetActive(true);
            portraitLabel.text = fallbackName + "\n临时立绘";
        }
    }

    private static bool HasAtLeastTwoSprites(IList<Sprite> frames)
    {
        if (frames == null)
        {
            return false;
        }

        int validCount = 0;
        for (int i = 0; i < frames.Count; i++)
        {
            if (frames[i] != null && ++validCount >= 2)
            {
                return true;
            }
        }

        return false;
    }

    private void UpdateDialogueChibis()
    {
        // 布局只在 Awake 的 ApplyDialogueVisualSettings 里套用一次，这里只换图；
        // 反复套用会把 Play 模式下拖动的小人位置冲回美术库旧值，导致同步工具记录不到变化。
        ApplyChibi(protagonistChibiImage, art != null ? art.protagonistChibi : null,
            ProtagonistName(), mint);

        // 顾客小人链：客人资产 > 其角色资产 > 美术库默认
        MilkTeaCharacter customerCharacter = currentCustomer != null ? currentCustomer.character : null;
        Sprite customerChibi = currentCustomer != null && currentCustomer.chibi != null
            ? currentCustomer.chibi
            : (customerCharacter != null && customerCharacter.chibi != null
                ? customerCharacter.chibi
                : (art != null ? art.customerChibi : null));
        string customerName = activeRecipe != null ? activeRecipe.CustomerName : "顾客";
        ApplyChibi(customerChibiImage, customerChibi, customerName, coral);
    }

    private void ApplyDialogueVisualSettings()
    {
        ApplyDialogueTextColors();
        ApplyDialogueChibiLayout();
        ApplyDialogueButtonColors();
    }

    private void ApplyDialogueButtonColors()
    {
        if (dialogueButton == null || art == null || art.continueButtonIcon == null)
        {
            return;
        }

        Image buttonImage = dialogueButton.targetGraphic as Image;
        if (buttonImage != null)
        {
            buttonImage.color = Color.white;
        }

        ColorBlock colors = dialogueButton.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(0.92f, 0.92f, 0.92f, 1f);
        colors.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
        colors.selectedColor = Color.white;
        colors.disabledColor = new Color(0.6f, 0.6f, 0.6f, 1f);
        dialogueButton.colors = colors;
    }

    private void ApplyDialogueTextColors()
    {
        Color lineColor = art != null && art.dialogueTextColor.a > 0.01f
            ? art.dialogueTextColor : Hex("#52382E");
        Color speakerColor = art != null && art.speakerTextColor.a > 0.01f
            ? art.speakerTextColor : cream;
        Color titleColor = art != null && art.dayTitleTextColor.a > 0.01f
            ? art.dayTitleTextColor : Hex("#402E24");
        if (dialogueLine != null)
        {
            dialogueLine.color = lineColor;
        }

        if (dialogueSpeaker != null)
        {
            dialogueSpeaker.color = speakerColor;
        }

        if (dayTitle != null)
        {
            dayTitle.color = titleColor;
        }
    }

    private void ApplyDialogueChibiLayout()
    {
        Vector2 protagonistPosition = art != null && art.protagonistChibiPosition != Vector2.zero
            ? art.protagonistChibiPosition
            : new Vector2(1120f, 600f);
        Vector2 protagonistSize = art != null && art.protagonistChibiSize.x > 0f && art.protagonistChibiSize.y > 0f
            ? art.protagonistChibiSize
            : new Vector2(150f, 150f);
        Vector2 customerPosition = art != null && art.customerChibiPosition != Vector2.zero
            ? art.customerChibiPosition
            : new Vector2(1360f, 390f);
        Vector2 customerSize = art != null && art.customerChibiSize.x > 0f && art.customerChibiSize.y > 0f
            ? art.customerChibiSize
            : new Vector2(180f, 180f);

        ApplyDialogueRect(protagonistChibiImage, protagonistPosition, protagonistSize);
        ApplyDialogueRect(customerChibiImage, customerPosition, customerSize);
    }

    private static void ApplyDialogueRect(Image image, Vector2 position, Vector2 size)
    {
        if (image == null)
        {
            return;
        }

        RectTransform rect = image.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.zero;
        rect.pivot = Vector2.zero;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    private static void ApplyChibi(Image image, Sprite sprite, string fallbackName, Color fallbackColor)
    {
        if (image == null)
        {
            return;
        }

        Transform labelTransform = image.transform.Find("Chibi Label");
        Text label = labelTransform != null ? labelTransform.GetComponent<Text>() : null;
        if (ApplySprite(image, sprite))
        {
            if (label != null)
            {
                label.gameObject.SetActive(false);
            }

            return;
        }

        image.sprite = null;
        image.color = fallbackColor;
        if (label != null)
        {
            label.gameObject.SetActive(true);
            label.text = fallbackName + "\nQ版";
        }
    }

    private static bool IsUnlocked(int index)
    {
        return PlayerPrefs.GetInt(UnlockPrefix + index, 0) == 1;
    }

    private static void SetUnlocked(int index)
    {
        PlayerPrefs.SetInt(UnlockPrefix + index, 1);
        PlayerPrefs.Save();
    }

    // ======== 进度存档与内容解锁 ========

    /// <summary>把当前进度与已解锁内容写入自动存档（0 号位）。</summary>
    private void AutoSaveProgress()
    {
        MilkTeaSaveSystem.Save(MilkTeaSaveSystem.AutoSaveSlot, CaptureProgress());
    }

    private MilkTeaSaveSystem.SaveData CaptureProgress()
    {
        MilkTeaChapter chapter = CurrentChapter();
        return new MilkTeaSaveSystem.SaveData
        {
            chapterIndex = scheduleChapterIndex,
            dayIndex = scheduleDayIndex,
            dayNumber = dayNumber,
            unlockedIds = new List<string>(unlockedContentIds),
            label = ScheduleMode
                ? (chapter != null && !string.IsNullOrEmpty(chapter.chapterName) ? chapter.chapterName : "第 " + (scheduleChapterIndex + 1) + " 章")
                  + " · 第 " + (scheduleDayIndex + 1) + " 天"
                : "第 " + dayNumber + " 天"
        };
    }

    /// <summary>解锁一段内容并立即持久化，避免解锁后退出丢失。</summary>
    private void UnlockContent(string contentId)
    {
        if (string.IsNullOrEmpty(contentId) || !unlockedContentIds.Add(contentId))
        {
            return;
        }

        AutoSaveProgress();
    }

    /// <summary>当前进度是否到达“某章某天”（朋友圈解锁锚点用）。</summary>
    private bool IsProgressReached(int chapterIndex, int dayIndex)
    {
        return scheduleChapterIndex > chapterIndex
            || (scheduleChapterIndex == chapterIndex && scheduleDayIndex >= dayIndex);
    }

    private string GetSelectedIngredient(IngredientCategory category)
    {
        if (category == IngredientCategory.Tea)
        {
            return selectedTea;
        }

        if (category == IngredientCategory.Milk)
        {
            return selectedMilk;
        }

        return selectedTopping;
    }

    private static string CategoryName(IngredientCategory category)
    {
        if (category == IngredientCategory.Tea)
        {
            return "茶底";
        }

        if (category == IngredientCategory.Milk)
        {
            return "奶底";
        }

        return "配料";
    }

    private static string EmptyAsDash(string value)
    {
        return string.IsNullOrEmpty(value) ? "—" : value;
    }

    private static string SugarName(int level)
    {
        return level == 0 ? "无糖" : level == 1 ? "少糖" : "多糖";
    }

    private static string IceName(int level)
    {
        return level == 0 ? "去冰" : level == 1 ? "少冰" : "多冰";
    }

    private static bool ApplySprite(Image image, Sprite sprite)
    {
        if (image == null || sprite == null)
        {
            return false;
        }

        image.sprite = sprite;
        image.color = Color.white;
        image.type = Image.Type.Simple;
        image.preserveAspect = true;
        return true;
    }

    private static Font CreateChineseFont()
    {
        try
        {
            return Font.CreateDynamicFontFromOSFont(
                new[] { "Microsoft YaHei UI", "Microsoft YaHei", "SimHei", "Arial" }, 32);
        }
        catch
        {
            return null;
        }
    }

    private static Color Hex(string value)
    {
        Color color;
        return ColorUtility.TryParseHtmlString(value, out color) ? color : Color.white;
    }
}
