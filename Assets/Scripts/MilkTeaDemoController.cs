using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

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
    public Text portraitLabel;
    public Text dialogueSpeaker;
    public Text dialogueLine;
    public Button dialogueButton;
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
    public Text selectionSummary;
    public Button prevCategoryButton;
    public Button nextCategoryButton;

    [Header("调配 · 打包机")]
    public Text machineStatus;
    public Button leverButton;

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

    [Header("每日循环")]
    public Text dayTitle;
    [Tooltip("随机点单模式下每天的客人数；排期模式自动使用客人列表长度")]
    public int customersPerDay = 3;

    [Header("开始界面")]
    public GameObject startScreen;
    public Button startGameButton;
    public Button loadGameButton;
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
    public Text countdownLabel;
    public Button skipButton;
    public Text skipButtonLabel;
    [Tooltip("开场动画/倒计时时长（秒），动画素材就绪前用倒计时占位")]
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
    private RectTransform leverRect;

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

    private void Awake()
    {
        art = Resources.Load<MilkTeaArtLibrary>(ArtLibraryResource);
        recipes = BuildRecipes();
        ApplyRuntimeFont();
        WireChoices();
        WireLevels();
        WireNavigation();
        LoadSettings();
        ShowStartScreen();
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

        if (leverButton != null)
        {
            leverRect = leverButton.GetComponent<RectTransform>();
            leverButton.onClick.AddListener(BeginSubmit);
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
        ResolveNextOrder();
        UpdateRecipeManual();
        UpdateCustomerPortrait(activeRecipe);
        PlayOpeningDialogue();
    }

    private void ResolveNextOrder()
    {
        currentOrderHadMistake = false;
        if (art != null && art.customers != null && art.customers.Count > 0)
        {
            customerCursor = (customerCursor + 1) % art.customers.Count;
            MilkTeaCustomer customer = art.customers[customerCursor];
            if (customer != null && customer.order != null)
            {
                currentCustomer = customer;
                activeRecipe = new Recipe
                {
                    Name = customer.order.displayName,
                    Tea = customer.order.teaBase,
                    Milk = customer.order.milkBase,
                    Topping = customer.order.topping,
                    IconLabel = string.IsNullOrEmpty(customer.order.iconLabel) ? "?" : customer.order.iconLabel,
                    IconSprite = customer.order.icon,
                    Portrait = customer.portrait != null ? customer.portrait : customer.order.customerPortrait,
                    CustomerName = string.IsNullOrEmpty(customer.customerName) ? "顾客" : customer.customerName
                };
                orderSugar = Mathf.Clamp(customer.sugarLevel, 0, 2);
                orderIce = Mathf.Clamp(customer.iceLevel, 0, 2);
                manualIndex = FindManualIndex(activeRecipe.Name);
                return;
            }
        }

        currentCustomer = null;
        orderIndex = UnityEngine.Random.Range(0, recipes.Length);
        activeRecipe = recipes[orderIndex];
        orderSugar = UnityEngine.Random.Range(1, 3); // 客人只点少糖或多糖
        orderIce = UnityEngine.Random.Range(0, 3);   // 去冰 / 少冰 / 多冰
        manualIndex = orderIndex;
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
        if (leverButton != null)
        {
            leverButton.interactable = true;
        }

        ClearSelections();
        RestoreOrderReminder();
    }

    private void PlayOpeningDialogue()
    {
        MilkTeaDialogue dialogue = currentCustomer != null && currentCustomer.openingDialogue != null
            ? currentCustomer.openingDialogue
            : (art != null ? art.defaultOpeningDialogue : null);
        if (dialogue != null && dialogue.lines != null && dialogue.lines.Count > 0)
        {
            PlayDialogue(dialogue, EnterMixing);
            return;
        }

        ConfigureDialogue(activeRecipe.CustomerName, Format("你好，我想要一杯{drink}，{ice}、{sugar}。"), "回应",
            delegate { ConfigureDialogue("主角", "了解了。", "开始调配", EnterMixing); });
    }

    private void ShowServingDialogue()
    {
        mixingScreen.SetActive(false);
        dialogueScreen.SetActive(true);
        UpdateCustomerPortrait(activeRecipe);
        MilkTeaDialogue dialogue = currentCustomer != null && currentCustomer.servingDialogue != null
            ? currentCustomer.servingDialogue
            : (art != null ? art.defaultServingDialogue : null);
        if (dialogue != null && dialogue.lines != null && dialogue.lines.Count > 0)
        {
            PlayDialogue(dialogue, AdvanceAfterServe);
            return;
        }

        ConfigureDialogue("主角", "您的奶茶做好了。", "继续",
            delegate { ConfigureDialogue(activeRecipe.CustomerName, "谢谢！", "下一位", AdvanceAfterServe); });
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

        ConfigureDialogue(Format(line.speaker), Format(line.text), button, next);
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
            dayTitle.text = "第 " + dayNumber + " 天 · 营业中";
        }

        ShowOpeningDialogue();
    }

    private int CustomersToday()
    {
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
            restHint.text = "忙碌的一天结束了，回到出租屋歇一歇。\n准备好了就开始新的一天吧。";
        }
    }

    private void GoNextDay()
    {
        dayNumber++;
        BeginDay();
    }

    private void Stay()
    {
        if (restHint != null)
        {
            restHint.text = "再歇一会儿……\n想开始营业时，点「进入下一天」。";
        }
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
    }

    private void OnStartGame()
    {
        if (startScreen != null)
        {
            startScreen.SetActive(false);
        }

        ShowIntro();
    }

    private void OnLoadGame()
    {
        // 目前 Demo 不持久化每日进度，读取存档直接以已解锁配方继续营业，并跳过开场动画。
        if (startScreen != null)
        {
            startScreen.SetActive(false);
        }

        BeginDay();
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
        if (countdownLabel != null)
        {
            countdownLabel.text = Mathf.CeilToInt(introCountdownSeconds > 0f ? introCountdownSeconds : 10f).ToString();
        }

        if (introRoutine != null)
        {
            StopCoroutine(introRoutine);
        }

        introRoutine = StartCoroutine(IntroCountdown());
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
        dialogueSpeaker.text = speaker;
        dialogueLine.text = line;
        dialogueButtonLabel.text = buttonText;
        dialogueButton.onClick.RemoveAllListeners();
        dialogueButton.interactable = action != null;
        if (action != null)
        {
            dialogueButton.onClick.AddListener(delegate { action(); });
        }
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
        UpdateChoiceVisuals();
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
        leverButton.interactable = false;
        machineStatus.text = "正在封装饮品…";

        float duration = 0.22f;
        for (float elapsed = 0f; elapsed < duration; elapsed += Time.deltaTime)
        {
            float progress = elapsed / duration;
            if (leverRect != null)
            {
                leverRect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(0f, -28f, progress));
            }

            yield return null;
        }

        yield return new WaitForSeconds(0.12f);
        if (leverRect != null)
        {
            leverRect.localRotation = Quaternion.identity;
        }

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
            leverButton.interactable = true;
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
        machineStatus.text = "请选择原料后拉动拉杆";
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
        bool unlocked = IsUnlocked(manualIndex);
        bool showSprite = unlocked && recipe.IconSprite != null;
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
            recipeIcon.text = unlocked ? recipe.IconLabel : "?";
            recipeIcon.fontSize = unlocked ? 30 : 66;
            recipeIcon.color = unlocked ? mint : yellow;
        }

        recipeDetails.text = recipe.Name + "\n茶底：" + recipe.Tea + "\n奶底：" + recipe.Milk +
                             "\n配料：" + recipe.Topping;
        recipePage.text = (manualIndex + 1) + " / " + recipes.Length;
    }

    private void UpdateCustomerPortrait(Recipe recipe)
    {
        if (portraitImage == null)
        {
            return;
        }

        Sprite portrait = recipe != null && recipe.Portrait != null
            ? recipe.Portrait
            : (art != null ? art.defaultCustomerPortrait : null);
        if (ApplySprite(portraitImage, portrait))
        {
            if (portraitLabel != null)
            {
                portraitLabel.gameObject.SetActive(false);
            }
        }
        else
        {
            portraitImage.color = panel;
            if (portraitLabel != null)
            {
                portraitLabel.gameObject.SetActive(true);
                portraitLabel.text = (recipe != null ? recipe.CustomerName : "顾客") + "\n临时立绘";
            }
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
