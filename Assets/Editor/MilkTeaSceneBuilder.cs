#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;

/// <summary>
/// 一键把奶茶店 Demo 的完整界面生成为场景中的真实、可编辑 GameObject。
/// 生成后可在场景里自由拖拽调整位置、挂 Animator 做动画；游戏逻辑由
/// 运行时的 <see cref="MilkTeaDemoController"/> 通过引用驱动，不再靠代码创建 UI。
/// 菜单：奶茶店 Demo → 构建演示场景界面。可重复执行（会先清掉旧的一份）。
/// </summary>
public static class MilkTeaSceneBuilder
{
    private const string ScenePath = "Assets/Scenes/MilkTeaDemo.unity";
    private const string LibraryPath = "Assets/Resources/MilkTeaArtLibrary.asset";
    private const string RootName = "Milk Tea Demo Canvas";

    private static readonly Color background = Hex("#111827");
    private static readonly Color panel = Hex("#243247");
    private static readonly Color panelLight = Hex("#33445E");
    private static readonly Color cream = Hex("#FFF4D6");
    private static readonly Color mint = Hex("#69D8C5");
    private static readonly Color coral = Hex("#F28B82");
    private static readonly Color yellow = Hex("#F3C84B");
    private static readonly Color blue = Hex("#62B5F5");
    private static readonly Color gray = Hex("#657184");
    private static readonly Color dark = Hex("#101722");

    private static MilkTeaArtLibrary art;
    private static Font font;
    private static MilkTeaDemoController controller;

    [MenuItem("奶茶店 Demo/构建演示场景界面")]
    public static void BuildScene()
    {
        Scene scene = EnsureSceneOpen();
        art = AssetDatabase.LoadAssetAtPath<MilkTeaArtLibrary>(LibraryPath);
        font = art != null && art.uiFont != null ? art.uiFont : GetFallbackFont();

        RemoveExisting(scene);
        EnsureCamera();
        EnsureEventSystem();
        BuildInterface();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Selection.activeObject = controller;
        EditorGUIUtility.PingObject(controller);
        Debug.Log("[奶茶店 Demo] 已在场景 " + ScenePath + " 中生成界面。现在可拖拽调整位置 / 挂 Animator，按 ▶ 运行即可。");
    }

    private static Scene EnsureSceneOpen()
    {
        Scene active = EditorSceneManager.GetActiveScene();
        if (active.path == ScenePath)
        {
            return active;
        }

        if (System.IO.File.Exists(ScenePath))
        {
            return EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        Scene created = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        System.IO.Directory.CreateDirectory("Assets/Scenes");
        EditorSceneManager.SaveScene(created, ScenePath);
        return created;
    }

    private static void RemoveExisting(Scene scene)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root.name == RootName)
            {
                Object.DestroyImmediate(root);
            }
        }
    }

    private static void BuildInterface()
    {
        GameObject canvasObject = new GameObject(RootName, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        controller = canvasObject.AddComponent<MilkTeaDemoController>();

        GameObject canvasBackground = CreatePanel("Window Background", canvasObject.transform, background);
        Stretch(canvasBackground.GetComponent<RectTransform>());
        ApplySprite(canvasBackground.GetComponent<Image>(), art != null ? art.windowBackground : null);

        GameObject root = CreatePanel("16:9 Content", canvasObject.transform, background);
        Stretch(root.GetComponent<RectTransform>());
        AspectRatioFitter fitter = root.AddComponent<AspectRatioFitter>();
        fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
        fitter.aspectRatio = 16f / 9f;

        GameObject dialogueScreen = CreatePanel("Dialogue Screen", root.transform, background);
        Stretch(dialogueScreen.GetComponent<RectTransform>());
        controller.dialogueScreen = dialogueScreen;
        BuildDialogueScreen(dialogueScreen.transform);

        GameObject mixingScreen = CreatePanel("Mixing Screen", root.transform, background);
        Stretch(mixingScreen.GetComponent<RectTransform>());
        controller.mixingScreen = mixingScreen;
        BuildMixingScreen(mixingScreen.transform);

        GameObject settlementScreen = CreatePanel("Settlement Screen", root.transform, Hex("#0C1524"));
        Stretch(settlementScreen.GetComponent<RectTransform>());
        controller.settlementScreen = settlementScreen;
        BuildSettlementScreen(settlementScreen.transform);
        settlementScreen.SetActive(false);

        GameObject restScreen = CreatePanel("Rest Screen", root.transform, Hex("#12100E"));
        Stretch(restScreen.GetComponent<RectTransform>());
        controller.restScreen = restScreen;
        BuildRestScreen(restScreen.transform);
        restScreen.SetActive(false);

        GameObject startScreen = CreatePanel("Start Screen", root.transform, Hex("#0B1622"));
        Stretch(startScreen.GetComponent<RectTransform>());
        controller.startScreen = startScreen;
        BuildStartScreen(startScreen.transform);

        GameObject introScreen = CreatePanel("Intro Screen", root.transform, Hex("#000000"));
        Stretch(introScreen.GetComponent<RectTransform>());
        controller.animationScreen = introScreen;
        BuildAnimationScreen(introScreen.transform);
        introScreen.SetActive(false);

        GameObject settingsPanel = CreatePanel("Settings Panel", root.transform, new Color(0f, 0f, 0f, 0.72f));
        Stretch(settingsPanel.GetComponent<RectTransform>());
        controller.settingsPanel = settingsPanel;
        BuildSettingsPanel(settingsPanel.transform);
        settingsPanel.SetActive(false);
    }

    private static void BuildDialogueScreen(Transform parent)
    {
        controller.dayTitle = CreateText("Title", parent, "第 1 天 · 营业中", 48, cream, TextAnchor.MiddleCenter,
            new Vector2(710, 995), new Vector2(500, 66), true);

        GameObject portraitPanel = CreatePanel("Customer Portrait", parent, panel);
        SetRect(portraitPanel.GetComponent<RectTransform>(), 60, 285, 760, 675);
        AddFrame(portraitPanel.transform, mint);
        controller.portraitImage = portraitPanel.GetComponent<Image>();
        controller.portraitAnimator = portraitPanel.AddComponent<SpriteSequenceAnimator>();
        controller.portraitLabel = CreateText("Portrait Label", portraitPanel.transform, "顾客\n临时立绘", 82, cream,
            TextAnchor.MiddleCenter, new Vector2(80, 180), new Vector2(600, 300), true);

        GameObject shopPanel = CreatePanel("Shop Overview", parent, panelLight);
        SetRect(shopPanel.GetComponent<RectTransform>(), 860, 285, 1000, 675);
        AddFrame(shopPanel.transform, coral);
        Sprite shopScene = art != null ? art.shopScene : null;
        if (shopScene != null)
        {
            ApplySprite(shopPanel.GetComponent<Image>(), shopScene);
        }
        else
        {
            CreateText("Shop Title", shopPanel.transform, "奶茶店 · 俯视小场景", 34, cream,
                TextAnchor.MiddleCenter, new Vector2(240, 600), new Vector2(520, 55), true);
            CreatePanelAt("Counter", shopPanel.transform, new Vector2(500, 255), new Vector2(400, 150), Hex("#AF7657"));
            CreateText("Counter Text", shopPanel.transform, "吧台", 30, cream, TextAnchor.MiddleCenter,
                new Vector2(500, 300), new Vector2(400, 60), true);
            CreatePanelAt("Table A", shopPanel.transform, new Vector2(90, 350), new Vector2(190, 115), Hex("#765D8A"));
            CreatePanelAt("Table B", shopPanel.transform, new Vector2(110, 120), new Vector2(190, 115), Hex("#765D8A"));
            CreatePanelAt("Kitchen", shopPanel.transform, new Vector2(690, 430), new Vector2(240, 120), Hex("#527A73"));
            CreateText("Kitchen Text", shopPanel.transform, "后厨", 28, cream, TextAnchor.MiddleCenter,
                new Vector2(690, 460), new Vector2(240, 50), true);
        }

        CreateChibi(shopPanel.transform, "主角", new Vector2(535, 420), mint, art != null ? art.protagonistChibi : null);
        CreateChibi(shopPanel.transform, "顾客", new Vector2(360, 300), coral, art != null ? art.customerChibi : null);

        GameObject dialogueBox = CreatePanel("Dialogue Box", parent, Hex("#172033"));
        SetRect(dialogueBox.GetComponent<RectTransform>(), 135, 50, 1650, 270);
        AddFrame(dialogueBox.transform, cream);
        ApplySprite(dialogueBox.GetComponent<Image>(), art != null ? art.dialogueBoxBackground : null);

        controller.dialogueSpeaker = CreateText("Speaker", dialogueBox.transform, string.Empty, 38, mint,
            TextAnchor.MiddleLeft, new Vector2(45, 185), new Vector2(500, 58), true);
        controller.dialogueLine = CreateText("Line", dialogueBox.transform, string.Empty, 38, cream,
            TextAnchor.UpperLeft, new Vector2(45, 72), new Vector2(1370, 110), false);
        Text continueLabel;
        controller.dialogueButton = CreateButton("Continue", dialogueBox.transform, "继续", new Vector2(1400, 35),
            new Vector2(200, 90), coral, out continueLabel);
        controller.dialogueButtonLabel = continueLabel;
    }

    private static void BuildMixingScreen(Transform parent)
    {
        CreateText("Mixing Title", parent, "奶茶调配", 46, cream, TextAnchor.MiddleCenter,
            new Vector2(445, 1000), new Vector2(560, 60), true);

        GameObject kitchenPanel = CreatePanel("Kitchen Overview", parent, panel);
        SetRect(kitchenPanel.GetComponent<RectTransform>(), 45, 355, 1030, 605);
        AddFrame(kitchenPanel.transform, mint);
        Sprite kitchenScene = art != null ? art.kitchenScene : null;
        if (kitchenScene != null)
        {
            ApplySprite(kitchenPanel.GetComponent<Image>(), kitchenScene);
        }
        else
        {
            CreateText("Kitchen Title", kitchenPanel.transform, "后厨 · 俯视场景", 34, cream,
                TextAnchor.MiddleCenter, new Vector2(280, 530), new Vector2(470, 55), true);
            CreatePanelAt("Worktop", kitchenPanel.transform, new Vector2(110, 120), new Vector2(810, 145), Hex("#8F664F"));
            CreatePanelAt("Tea Machine", kitchenPanel.transform, new Vector2(90, 320), new Vector2(210, 140), Hex("#53657A"));
            CreateText("Tea Machine Label", kitchenPanel.transform, "萃茶机", 27, cream,
                TextAnchor.MiddleCenter, new Vector2(90, 360), new Vector2(210, 50), true);
            CreatePanelAt("Milk Station", kitchenPanel.transform, new Vector2(720, 320), new Vector2(210, 140), Hex("#53657A"));
            CreateText("Milk Station Label", kitchenPanel.transform, "奶底区", 27, cream,
                TextAnchor.MiddleCenter, new Vector2(720, 360), new Vector2(210, 50), true);
        }

        CreateChibi(kitchenPanel.transform, "主角", new Vector2(470, 255), mint, art != null ? art.protagonistChibi : null);

        GameObject orderBox = CreatePanel("Order Reminder", parent, Hex("#172033"));
        SetRect(orderBox.GetComponent<RectTransform>(), 45, 45, 1030, 270);
        AddFrame(orderBox.transform, cream);
        controller.orderSpeaker = CreateText("Order Speaker", orderBox.transform, "顾客需求", 34, coral,
            TextAnchor.MiddleLeft, new Vector2(38, 185), new Vector2(460, 55), true);
        controller.orderLine = CreateText("Order Line", orderBox.transform,
            "经典珍珠奶茶 · 少糖 · 少冰\n茶底、奶底和配料需要由你完成。", 33, cream,
            TextAnchor.UpperLeft, new Vector2(38, 55), new Vector2(930, 125), false);

        GameObject operationPanel = CreatePanel("Operation Panel", parent, Hex("#1A2433"));
        SetRect(operationPanel.GetComponent<RectTransform>(), 1110, 25, 765, 1025);
        AddFrame(operationPanel.transform, coral);
        BuildRecipePanel(operationPanel.transform);
        BuildIngredientPanel(operationPanel.transform);
        BuildMachinePanel(operationPanel.transform);
    }

    private static void BuildSettlementScreen(Transform parent)
    {
        GameObject card = CreatePanel("Settlement Card", parent, panel);
        SetRect(card.GetComponent<RectTransform>(), 560, 290, 800, 500);
        AddFrame(card.transform, yellow);

        controller.settlementTitle = CreateText("Settlement Title", card.transform, "第 1 天 · 营业结算", 46, cream,
            TextAnchor.MiddleCenter, new Vector2(100, 400), new Vector2(600, 70), true);
        controller.settlementBody = CreateText("Settlement Body", card.transform,
            "今日出杯：0 杯\n完美制作：0 杯\n操作失误：0 次\n今日营收：¥0", 32, mint,
            TextAnchor.MiddleCenter, new Vector2(100, 150), new Vector2(600, 220), false);

        Text settlementLabel;
        controller.settlementButton = CreateButton("Settlement Button", card.transform, "回家休息",
            new Vector2(280, 40), new Vector2(240, 90), coral, out settlementLabel);
        controller.settlementButtonLabel = settlementLabel;
    }

    private static void BuildRestScreen(Transform parent)
    {
        CreateText("Rest Title", parent, "回到出租屋 · 休息中", 48, cream, TextAnchor.MiddleCenter,
            new Vector2(710, 995), new Vector2(500, 66), true);

        // 左侧：手机（后续功能入口占位）
        GameObject phone = CreatePanel("Phone", parent, Hex("#1B2740"));
        SetRect(phone.GetComponent<RectTransform>(), 130, 150, 470, 820);
        AddFrame(phone.transform, mint);
        CreateText("Phone Clock", phone.transform, "20:30", 34, mint, TextAnchor.MiddleCenter,
            new Vector2(35, 762), new Vector2(400, 50), true);

        GameObject screen = CreatePanel("Phone Screen", phone.transform, Hex("#101A2E"));
        SetRect(screen.GetComponent<RectTransform>(), 35, 55, 400, 700);
        CreateText("Phone Header", screen.transform, "手机", 36, cream, TextAnchor.MiddleCenter,
            new Vector2(0, 610), new Vector2(400, 60), true);
        CreateRestMenuEntry(screen.transform, "相册", 500);
        CreateRestMenuEntry(screen.transform, "角色资料", 410);
        CreateRestMenuEntry(screen.transform, "对话记录", 320);
        CreateText("Phone Note", screen.transform, "更多功能敬请期待……", 24, gray, TextAnchor.MiddleCenter,
            new Vector2(0, 60), new Vector2(400, 60), false);

        // 右侧：出租屋俯视小场景
        GameObject apartment = CreatePanel("Apartment Overview", parent, panelLight);
        SetRect(apartment.GetComponent<RectTransform>(), 700, 250, 1140, 600);
        AddFrame(apartment.transform, coral);
        Sprite apartmentScene = art != null ? art.apartmentScene : null;
        if (apartmentScene != null)
        {
            ApplySprite(apartment.GetComponent<Image>(), apartmentScene);
        }
        else
        {
            CreateText("Apartment Title", apartment.transform, "出租屋 · 俯视小场景", 34, cream,
                TextAnchor.MiddleCenter, new Vector2(320, 520), new Vector2(500, 55), true);
            CreatePanelAt("Bed", apartment.transform, new Vector2(80, 90), new Vector2(360, 240), Hex("#6E5A86"));
            CreateText("Bed Label", apartment.transform, "床", 30, cream, TextAnchor.MiddleCenter,
                new Vector2(80, 300), new Vector2(360, 55), true);
            CreatePanelAt("Desk", apartment.transform, new Vector2(720, 110), new Vector2(340, 170), Hex("#8F664F"));
            CreateText("Desk Label", apartment.transform, "书桌", 30, cream, TextAnchor.MiddleCenter,
                new Vector2(720, 300), new Vector2(340, 55), true);
        }

        CreateChibi(apartment.transform, "主角", new Vector2(520, 150), mint, art != null ? art.protagonistChibi : null);

        controller.restHint = CreateText("Rest Hint", parent, "忙碌的一天结束了，回到出租屋歇一歇。\n准备好了就开始新的一天吧。",
            30, cream, TextAnchor.MiddleCenter, new Vector2(700, 150), new Vector2(1140, 80), false);

        Text nextDayLabel;
        controller.nextDayButton = CreateButton("Next Day Button", parent, "进入下一天",
            new Vector2(1180, 45), new Vector2(320, 96), mint, out nextDayLabel);
        nextDayLabel.color = dark;
        controller.nextDayButtonLabel = nextDayLabel;

        Text stayLabel;
        controller.stayButton = CreateButton("Stay Button", parent, "再休息一会儿",
            new Vector2(1520, 45), new Vector2(320, 96), panelLight, out stayLabel);
        controller.stayButtonLabel = stayLabel;
    }

    private static void CreateRestMenuEntry(Transform parent, string caption, float y)
    {
        Text label;
        Button button = CreateButton("Menu " + caption, parent, caption, new Vector2(30, y),
            new Vector2(340, 70), panel, out label);
        button.interactable = false;
    }

    private static void BuildStartScreen(Transform parent)
    {
        ApplySprite(parent.GetComponent<Image>(), art != null ? art.startBackground : null);

        // 左上角设置入口
        Text gearLabel;
        controller.settingsButton = CreateButton("Settings Button", parent, "设置", new Vector2(45, 945),
            new Vector2(120, 90), panel, out gearLabel);
        gearLabel.fontSize = 30;
        AddFrame(controller.settingsButton.transform, mint);

        // 右侧上方 Logo
        GameObject logo = CreatePanel("Logo", parent, new Color(0f, 0f, 0f, 0.35f));
        SetRect(logo.GetComponent<RectTransform>(), 1130, 620, 720, 400);
        if (!ApplySprite(logo.GetComponent<Image>(), art != null ? art.startLogo : null))
        {
            CreateText("Logo Text", logo.transform, "奶茶店\n模拟经营", 96, cream, TextAnchor.MiddleCenter,
                Vector2.zero, new Vector2(720, 400), true);
        }

        // 右侧下方按钮
        Text startLabel;
        controller.startGameButton = CreateButton("Start Game", parent, "开始游戏\nNEW GAME", new Vector2(1270, 450),
            new Vector2(460, 130), mint, out startLabel);
        startLabel.color = dark;
        startLabel.fontSize = 34;

        Text loadLabel;
        controller.loadGameButton = CreateButton("Load Game", parent, "读取存档\nCONTINUE", new Vector2(1270, 290),
            new Vector2(460, 130), panelLight, out loadLabel);
        loadLabel.fontSize = 34;

        CreateText("Version", parent, "奶茶店模拟经营 · Demo", 24, gray, TextAnchor.MiddleLeft,
            new Vector2(45, 45), new Vector2(560, 40), false);
    }

    private static void BuildAnimationScreen(Transform parent)
    {
        GameObject videoSurface = new GameObject("Video Surface", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
        videoSurface.transform.SetParent(parent, false);
        Stretch(videoSurface.GetComponent<RectTransform>());
        RawImage rawImage = videoSurface.GetComponent<RawImage>();
        rawImage.color = Color.black;
        rawImage.raycastTarget = false;
        controller.introVideoImage = rawImage;

        VideoPlayer videoPlayer = videoSurface.AddComponent<VideoPlayer>();
        videoPlayer.playOnAwake = false;
        videoPlayer.isLooping = false;
        videoPlayer.renderMode = VideoRenderMode.APIOnly;
        videoPlayer.audioOutputMode = VideoAudioOutputMode.Direct;
        controller.introVideo = videoPlayer;

        controller.countdownLabel = CreateText("Countdown", parent, "10", 160, cream, TextAnchor.MiddleCenter,
            new Vector2(660, 460), new Vector2(600, 220), true);
        CreateText("Intro Hint", parent, "开场动画 · 未指定视频时用倒计时占位（把视频拖到 Video Player 的 Video Clip）", 26, gray,
            TextAnchor.MiddleCenter, new Vector2(360, 330), new Vector2(1200, 50), false);

        Text skipLabel;
        controller.skipButton = CreateButton("Skip Button", parent, "跳过 ▶▶", new Vector2(1650, 945),
            new Vector2(230, 92), coral, out skipLabel);
        controller.skipButtonLabel = skipLabel;
    }

    private static void BuildSettingsPanel(Transform parent)
    {
        GameObject card = CreatePanel("Settings Card", parent, panel);
        SetRect(card.GetComponent<RectTransform>(), 560, 240, 800, 600);
        AddFrame(card.transform, mint);

        CreateText("Settings Title", card.transform, "设置", 46, cream, TextAnchor.MiddleCenter,
            new Vector2(100, 500), new Vector2(600, 66), true);

        controller.volumeValueLabel = BuildSettingRow(card.transform, "Volume", "音量", 400,
            out controller.volumeDownButton, out controller.volumeUpButton);
        controller.resolutionValueLabel = BuildSettingRow(card.transform, "Resolution", "分辨率", 290,
            out controller.resolutionPrevButton, out controller.resolutionNextButton);
        controller.languageValueLabel = BuildSettingRow(card.transform, "Language", "语言", 180,
            out controller.languagePrevButton, out controller.languageNextButton);

        Text closeLabel;
        controller.settingsCloseButton = CreateButton("Settings Close", card.transform, "完成",
            new Vector2(300, 45), new Vector2(200, 90), coral, out closeLabel);
    }

    private static Text BuildSettingRow(Transform card, string name, string caption, float y,
        out Button prev, out Button next)
    {
        CreateText(name + " Label", card, caption, 32, cream, TextAnchor.MiddleLeft,
            new Vector2(60, y), new Vector2(200, 60), true);

        Text prevLabel;
        prev = CreateButton(name + " Prev", card, "◀", new Vector2(300, y), new Vector2(64, 60), panelLight, out prevLabel);

        Text valueLabel = CreateText(name + " Value", card, "-", 30, mint, TextAnchor.MiddleCenter,
            new Vector2(374, y), new Vector2(300, 60), true);

        Text nextLabel;
        next = CreateButton(name + " Next", card, "▶", new Vector2(674, y), new Vector2(64, 60), panelLight, out nextLabel);
        return valueLabel;
    }

    private static void BuildRecipePanel(Transform parent)
    {
        GameObject recipePanel = CreatePanel("Recipe Manual", parent, panel);
        SetRect(recipePanel.GetComponent<RectTransform>(), 22, 770, 720, 230);
        CreateText("Recipe Header", recipePanel.transform, "配方手册", 31, cream,
            TextAnchor.MiddleCenter, new Vector2(245, 176), new Vector2(230, 45), true);

        GameObject iconCard = CreatePanel("Recipe Icon", recipePanel.transform, dark);
        SetRect(iconCard.GetComponent<RectTransform>(), 25, 35, 165, 150);
        controller.recipeIcon = CreateText("Recipe Icon Text", iconCard.transform, "?", 66, yellow,
            TextAnchor.MiddleCenter, Vector2.zero, new Vector2(165, 150), true);
        GameObject iconImageObject = CreatePanel("Recipe Icon Image", iconCard.transform, Color.white);
        SetRect(iconImageObject.GetComponent<RectTransform>(), 0, 0, 165, 150);
        controller.recipeIconImage = iconImageObject.GetComponent<Image>();
        controller.recipeIconImage.preserveAspect = true;
        controller.recipeIconImage.raycastTarget = false;
        iconImageObject.SetActive(false);

        controller.recipeDetails = CreateText("Recipe Details", recipePanel.transform,
            string.Empty, 25, cream,
            TextAnchor.MiddleLeft, new Vector2(215, 24), new Vector2(375, 160), true);

        Text unused;
        controller.prevRecipeButton = CreateButton("Previous Recipe", recipePanel.transform, "◀", new Vector2(600, 105),
            new Vector2(48, 56), panelLight, out unused);
        controller.nextRecipeButton = CreateButton("Next Recipe", recipePanel.transform, "▶", new Vector2(656, 105),
            new Vector2(48, 56), panelLight, out unused);
        controller.recipePage = CreateText("Page", recipePanel.transform, "1 / 10", 22, mint, TextAnchor.MiddleCenter,
            new Vector2(600, 48), new Vector2(105, 45), true);
    }

    private static void BuildIngredientPanel(Transform parent)
    {
        GameObject ingredientPanel = CreatePanel("Ingredients", parent, panel);
        SetRect(ingredientPanel.GetComponent<RectTransform>(), 22, 292, 720, 455);

        controller.categoryTitle = CreateText("Category Title", ingredientPanel.transform, "原料选择 · 茶底", 31, cream,
            TextAnchor.MiddleLeft, new Vector2(24, 395), new Vector2(430, 46), true);
        Text unused;
        controller.prevCategoryButton = CreateButton("Previous Category", ingredientPanel.transform, "▲", new Vector2(590, 392),
            new Vector2(48, 48), panelLight, out unused);
        controller.nextCategoryButton = CreateButton("Next Category", ingredientPanel.transform, "▼", new Vector2(650, 392),
            new Vector2(48, 48), panelLight, out unused);

        BuildChoicePanel(ingredientPanel.transform, IngredientCategory.Tea,
            new[] { "红茶", "茉莉绿茶", "抹茶", "乌龙" }, 4);
        BuildChoicePanel(ingredientPanel.transform, IngredientCategory.Milk,
            new[] { "鲜奶", "椰奶", "无奶" }, 3);
        BuildChoicePanel(ingredientPanel.transform, IngredientCategory.Topping,
            new[] { "珍珠", "黑糖珍珠", "芝士奶盖", "芋泥", "红豆", "芒果西米", "草莓芝士", "百香果椰果", "桂花糖浆", "无" }, 3);

        controller.selectionSummary = CreateText("Selection Summary", ingredientPanel.transform,
            "已选：茶底 — ｜ 奶底 — ｜ 配料 —", 22, mint,
            TextAnchor.MiddleLeft, new Vector2(24, 133), new Vector2(670, 40), true);

        CreateText("Sugar Label", ingredientPanel.transform, "糖度", 25, cream,
            TextAnchor.MiddleLeft, new Vector2(25, 73), new Vector2(85, 45), true);
        CreateLevelBlock(ingredientPanel.transform, "Sugar 1", new Vector2(120, 74), yellow, true, 0);
        CreateLevelBlock(ingredientPanel.transform, "Sugar 2", new Vector2(180, 74), yellow, true, 1);
        CreateText("Sugar State", ingredientPanel.transform, "无糖 / 少糖 / 多糖", 20, gray,
            TextAnchor.MiddleLeft, new Vector2(250, 75), new Vector2(235, 40), false);

        CreateText("Ice Label", ingredientPanel.transform, "冰度", 25, cream,
            TextAnchor.MiddleLeft, new Vector2(25, 20), new Vector2(85, 45), true);
        CreateLevelBlock(ingredientPanel.transform, "Ice 1", new Vector2(120, 21), blue, false, 0);
        CreateLevelBlock(ingredientPanel.transform, "Ice 2", new Vector2(180, 21), blue, false, 1);
        CreateText("Ice State", ingredientPanel.transform, "去冰 / 少冰 / 多冰", 20, gray,
            TextAnchor.MiddleLeft, new Vector2(250, 22), new Vector2(235, 40), false);
    }

    private static void BuildChoicePanel(Transform parent, IngredientCategory category, string[] options, int columns)
    {
        GameObject choicePanel = CreatePanel(category + " Choices", parent, Color.clear);
        SetRect(choicePanel.GetComponent<RectTransform>(), 24, 170, 672, 205);
        MilkTeaCategoryPanel categoryMarker = choicePanel.AddComponent<MilkTeaCategoryPanel>();
        categoryMarker.category = category;

        const float gap = 10f;
        float width = (672f - gap * (columns - 1)) / columns;
        int rows = Mathf.CeilToInt(options.Length / (float)columns);
        float height = Mathf.Min(58f, (205f - gap * (rows - 1)) / rows);

        for (int i = 0; i < options.Length; i++)
        {
            string option = options[i];
            int column = i % columns;
            int row = i / columns;
            float x = column * (width + gap);
            float y = 205f - height - row * (height + gap);
            Text label;
            Button button = CreateButton(option, choicePanel.transform, option, new Vector2(x, y),
                new Vector2(width, height), panelLight, out label);
            label.fontSize = options.Length > 6 ? 19 : 23;

            MilkTeaChoiceButton marker = button.gameObject.AddComponent<MilkTeaChoiceButton>();
            marker.category = category;
            marker.option = option;

            Sprite ingredientIcon = art != null ? art.GetIngredientIcon(option) : null;
            if (ingredientIcon != null)
            {
                GameObject iconObject = CreatePanel(option + " Icon", button.transform, Color.white);
                SetRect(iconObject.GetComponent<RectTransform>(), (width - 40f) / 2f, height - 44f, 40, 40);
                Image ingredientImage = iconObject.GetComponent<Image>();
                ingredientImage.raycastTarget = false;
                ApplySprite(ingredientImage, ingredientIcon);
            }
        }
    }

    private static void BuildMachinePanel(Transform parent)
    {
        GameObject machine = CreatePanel("Packing Machine", parent, panel);
        SetRect(machine.GetComponent<RectTransform>(), 22, 22, 720, 247);
        controller.machineStatus = CreateText("Machine Status", machine.transform, "请选择原料后拉动拉杆", 27, cream,
            TextAnchor.MiddleCenter, new Vector2(35, 175), new Vector2(500, 48), true);

        GameObject cup = CreatePanel("Cup", machine.transform, Hex("#D8CBB3"));
        SetRect(cup.GetComponent<RectTransform>(), 100, 35, 250, 125);
        if (!ApplySprite(cup.GetComponent<Image>(), art != null ? art.cup : null))
        {
            CreateText("Cup Label", cup.transform, "饮品打包机", 29, dark,
                TextAnchor.MiddleCenter, Vector2.zero, new Vector2(250, 125), true);
        }

        Text leverLabel;
        controller.leverButton = CreateButton("Lever", machine.transform, "拉杆\n▼", new Vector2(540, 38),
            new Vector2(120, 150), coral, out leverLabel);
        leverLabel.fontSize = 27;
        if (ApplySprite(controller.leverButton.GetComponent<Image>(), art != null ? art.lever : null))
        {
            leverLabel.gameObject.SetActive(false);
        }
    }

    private static void CreateLevelBlock(Transform parent, string name, Vector2 position, Color activeColor,
        bool isSugar, int index)
    {
        Text unused;
        Button button = CreateButton(name, parent, string.Empty, position, new Vector2(48, 42), gray, out unused);
        ColorBlock colors = button.colors;
        colors.highlightedColor = activeColor;
        colors.pressedColor = activeColor * 0.9f;
        button.colors = colors;

        MilkTeaLevelBlock marker = button.gameObject.AddComponent<MilkTeaLevelBlock>();
        marker.isSugar = isSugar;
        marker.index = index;
    }

    private static void CreateChibi(Transform parent, string label, Vector2 position, Color color, Sprite sprite)
    {
        GameObject body = CreatePanel("Chibi " + label, parent, color);
        SetRect(body.GetComponent<RectTransform>(), position.x, position.y, 120, 120);
        if (ApplySprite(body.GetComponent<Image>(), sprite))
        {
            return;
        }

        CreateText("Chibi Label", body.transform, label + "\nQ版", 24, dark,
            TextAnchor.MiddleCenter, Vector2.zero, new Vector2(120, 120), true);
    }

    private static GameObject CreatePanel(string name, Transform parent, Color color)
    {
        GameObject gameObject = new GameObject(name, typeof(RectTransform), typeof(Image));
        gameObject.transform.SetParent(parent, false);
        gameObject.GetComponent<Image>().color = color;
        return gameObject;
    }

    private static GameObject CreatePanelAt(string name, Transform parent, Vector2 position, Vector2 size, Color color)
    {
        GameObject gameObject = CreatePanel(name, parent, color);
        SetRect(gameObject.GetComponent<RectTransform>(), position.x, position.y, size.x, size.y);
        return gameObject;
    }

    private static Text CreateText(string name, Transform parent, string content, int size, Color color,
        TextAnchor alignment, Vector2 position, Vector2 dimensions, bool bold)
    {
        GameObject gameObject = new GameObject(name, typeof(RectTransform), typeof(Text));
        gameObject.transform.SetParent(parent, false);
        SetRect(gameObject.GetComponent<RectTransform>(), position.x, position.y, dimensions.x, dimensions.y);
        Text text = gameObject.GetComponent<Text>();
        text.font = font;
        text.text = content;
        text.fontSize = size;
        text.color = color;
        text.alignment = alignment;
        text.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        return text;
    }

    private static Button CreateButton(string name, Transform parent, string caption, Vector2 position,
        Vector2 dimensions, Color color, out Text label)
    {
        GameObject gameObject = CreatePanel(name, parent, color);
        SetRect(gameObject.GetComponent<RectTransform>(), position.x, position.y, dimensions.x, dimensions.y);
        Image image = gameObject.GetComponent<Image>();
        Button button = gameObject.AddComponent<Button>();
        button.targetGraphic = image;

        ColorBlock colors = button.colors;
        if (ApplyButtonSkin(image, ResolveButtonSkin(color)))
        {
            // 已套用皮肤图：底色改白避免染色，仅保留轻微悬停/按下反馈
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.92f, 0.92f, 0.92f, 1f);
            colors.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
            colors.disabledColor = new Color(0.6f, 0.6f, 0.6f, 1f);
        }
        else
        {
            // 无皮肤图：沿用纯色占位
            colors.normalColor = color;
            colors.highlightedColor = Color.Lerp(color, Color.white, 0.18f);
            colors.pressedColor = Color.Lerp(color, Color.black, 0.18f);
            colors.disabledColor = Color.Lerp(color, gray, 0.55f);
        }

        button.colors = colors;

        label = CreateText("Label", gameObject.transform, caption, 25, cream, TextAnchor.MiddleCenter,
            Vector2.zero, dimensions, true);
        return button;
    }

    // 根据按钮占位色映射到语义皮肤槽，未设置时返回 null（则回退纯色）
    private static Sprite ResolveButtonSkin(Color color)
    {
        if (art == null)
        {
            return null;
        }

        if (color == mint)
        {
            return art.buttonPrimary;
        }

        if (color == coral)
        {
            return art.buttonAccent;
        }

        if (color == panelLight)
        {
            return art.buttonSecondary;
        }

        return art.buttonNeutral;
    }

    // 将皮肤图贴到按钮：有九宫格边框用 Sliced，否则 Simple 拉伸填充（不保比例）
    private static bool ApplyButtonSkin(Image image, Sprite sprite)
    {
        if (image == null || sprite == null)
        {
            return false;
        }

        image.sprite = sprite;
        image.color = Color.white;
        image.type = sprite.border != Vector4.zero ? Image.Type.Sliced : Image.Type.Simple;
        image.preserveAspect = false;
        return true;
    }

    private static void AddFrame(Transform parent, Color color)
    {
        Outline outline = parent.gameObject.AddComponent<Outline>();
        outline.effectColor = color;
        outline.effectDistance = new Vector2(4f, -4f);
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

    private static void SetRect(RectTransform rect, float x, float y, float width, float height)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.zero;
        rect.pivot = Vector2.zero;
        rect.anchoredPosition = new Vector2(x, y);
        rect.sizeDelta = new Vector2(width, height);
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static void EnsureCamera()
    {
        if (Object.FindObjectOfType<Camera>() != null)
        {
            return;
        }

        GameObject cameraObject = new GameObject("Main Camera", typeof(Camera));
        cameraObject.tag = "MainCamera";
        Camera camera = cameraObject.GetComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = Hex("#0B111C");
        camera.orthographic = true;
    }

    private static void EnsureEventSystem()
    {
        if (Object.FindObjectOfType<EventSystem>() != null)
        {
            return;
        }

        new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
    }

    private static Font GetFallbackFont()
    {
        Font legacy = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        return legacy != null ? legacy : Resources.GetBuiltinResource<Font>("Arial.ttf");
    }

    private static Color Hex(string value)
    {
        Color color;
        return ColorUtility.TryParseHtmlString(value, out color) ? color : Color.white;
    }
}
#endif
