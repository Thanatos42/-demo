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
/// 菜单：奶茶店 Demo → 构建演示场景界面。可重复执行（已存在的界面会保留手动调整，
/// 仅创建缺失的界面）。如需强制重建某个界面，先在层级里删掉它再执行。
/// 菜单：奶茶店 Demo → 强制重建全部界面 —— 清空重来。
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
        BuildSceneInternal(forceRebuildAll: false);
    }

    [MenuItem("奶茶店 Demo/强制重建全部界面")]
    public static void ForceRebuildAll()
    {
        BuildSceneInternal(forceRebuildAll: true);
    }

    private static void BuildSceneInternal(bool forceRebuildAll)
    {
        Scene scene = EnsureSceneOpen();
        art = AssetDatabase.LoadAssetAtPath<MilkTeaArtLibrary>(LibraryPath);
        font = art != null && art.uiFont != null ? art.uiFont : GetFallbackFont();

        if (forceRebuildAll)
        {
            RemoveExisting(scene);
        }

        EnsureCamera();
        EnsureEventSystem();
        BuildInterface(scene);

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

    private static void BuildInterface(Scene scene)
    {
        // 查找已有 Canvas，没有才新建
        GameObject canvasObject = null;
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root.name == RootName)
            {
                canvasObject = root;
                break;
            }
        }

        Transform rootTransform;
        if (canvasObject != null)
        {
            controller = canvasObject.GetComponent<MilkTeaDemoController>();
            if (controller == null)
            {
                controller = canvasObject.AddComponent<MilkTeaDemoController>();
            }

            Transform contentFind = canvasObject.transform.Find("16:9 Content");
            rootTransform = contentFind != null ? contentFind : canvasObject.transform;
        }
        else
        {
            canvasObject = new GameObject(RootName, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
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
            rootTransform = root.transform;
        }

        // 对每个界面：已存在则保留手动调整，仅重新接线引用；不存在则新建
        BuildOrPreserveScreen(rootTransform, "Dialogue Screen", background, true, (t) =>
        {
            controller.dialogueScreen = t.gameObject;
            BuildDialogueScreen(t);
        }, (existing) =>
        {
            controller.dialogueScreen = existing.gameObject;
            RewireDialogueScreen(existing);
        });

        BuildOrPreserveScreen(rootTransform, "Mixing Screen", background, true, (t) =>
        {
            controller.mixingScreen = t.gameObject;
            BuildMixingScreen(t);
        }, (existing) =>
        {
            controller.mixingScreen = existing.gameObject;
            RewireMixingScreen(existing);
        });

        BuildOrPreserveScreen(rootTransform, "Settlement Screen", Hex("#0C1524"), false, (t) =>
        {
            controller.settlementScreen = t.gameObject;
            BuildSettlementScreen(t);
            t.gameObject.SetActive(false);
        }, (existing) =>
        {
            controller.settlementScreen = existing.gameObject;
            RewireSettlementScreen(existing);
        });

        BuildOrPreserveScreen(rootTransform, "Rest Screen", Hex("#12100E"), false, (t) =>
        {
            controller.restScreen = t.gameObject;
            BuildRestScreen(t);
            t.gameObject.SetActive(false);
        }, (existing) =>
        {
            controller.restScreen = existing.gameObject;
            RewireRestScreen(existing);
        });

        BuildOrPreserveScreen(rootTransform, "Start Screen", Hex("#0B1622"), true, (t) =>
        {
            controller.startScreen = t.gameObject;
            BuildStartScreen(t);
        }, (existing) =>
        {
            controller.startScreen = existing.gameObject;
            RewireStartScreen(existing);
        });

        BuildOrPreserveScreen(rootTransform, "Intro Screen", Hex("#000000"), false, (t) =>
        {
            controller.animationScreen = t.gameObject;
            BuildAnimationScreen(t);
            t.gameObject.SetActive(false);
        }, (existing) =>
        {
            controller.animationScreen = existing.gameObject;
            RewireIntroScreen(existing);
        });

        BuildOrPreserveScreen(rootTransform, "Settings Panel", new Color(0f, 0f, 0f, 0.72f), false, (t) =>
        {
            controller.settingsPanel = t.gameObject;
            BuildSettingsPanel(t);
            t.gameObject.SetActive(false);
        }, (existing) =>
        {
            controller.settingsPanel = existing.gameObject;
            RewireSettingsPanel(existing);
        });
    }

    private static void BuildOrPreserveScreen(Transform parent, string screenName, Color color,
        bool activeByDefault,
        System.Action<Transform> buildNew,
        System.Action<Transform> rewireExisting)
    {
        Transform existing = parent.Find(screenName);
        if (existing != null)
        {
            rewireExisting(existing);
            return;
        }

        GameObject screen = CreatePanel(screenName, parent, color);
        Stretch(screen.GetComponent<RectTransform>());
        buildNew(screen.transform);
    }

    // ======== 保留已有界面时的引用重新接线 ========

    /// <summary>在 parent 及其子层中按 name 查找组件，找不到返回 null。</summary>
    private static T FindDeep<T>(Transform parent, string name) where T : Component
    {
        foreach (T component in parent.GetComponentsInChildren<T>(true))
        {
            if (component.gameObject.name == name)
            {
                return component;
            }
        }

        return null;
    }

    /// <summary>找按钮内部的 Label 子节点的 Text。</summary>
    private static Text FindButtonLabel(Transform root, string buttonName)
    {
        Button button = FindDeep<Button>(root, buttonName);
        if (button == null)
        {
            return null;
        }

        Transform label = button.transform.Find("Label");
        return label != null ? label.GetComponent<Text>() : null;
    }

    private static void RewireDialogueScreen(Transform root)
    {
        controller.dayTitle = FindDeep<Text>(root, "Title");
        controller.portraitImage = FindDeep<Image>(root, "Customer Portrait");
        controller.portraitAnimator = FindDeep<SpriteSequenceAnimator>(root, "Customer Portrait");
        controller.portraitLabel = FindDeep<Text>(root, "Portrait Label");
        controller.protagonistChibiImage = FindDeep<Image>(root, "Chibi 主角");
        controller.customerChibiImage = FindDeep<Image>(root, "Chibi 顾客");
        controller.dialogueSpeaker = FindDeep<Text>(root, "Speaker");
        controller.dialogueLine = FindDeep<Text>(root, "Line");
        controller.dialogueButton = FindDeep<Button>(root, "Continue");
        controller.dialogueButtonLabel = FindButtonLabel(root, "Continue");
    }

    private static void RewireMixingScreen(Transform root)
    {
        controller.orderSpeaker = FindDeep<Text>(root, "Order Speaker");
        controller.orderLine = FindDeep<Text>(root, "Order Line");
        controller.recipeIcon = FindDeep<Text>(root, "Recipe Icon Text");
        controller.recipeIconImage = FindDeep<Image>(root, "Recipe Icon Image");
        controller.recipeDetails = FindDeep<Text>(root, "Recipe Details");
        controller.recipePage = FindDeep<Text>(root, "Page");
        controller.prevRecipeButton = FindDeep<Button>(root, "Previous Recipe");
        controller.nextRecipeButton = FindDeep<Button>(root, "Next Recipe");
        controller.categoryTitle = FindDeep<Text>(root, "Category Title");
        controller.categoryIcon = FindDeep<Image>(root, "Category Icon");
        controller.selectionSummary = FindDeep<Text>(root, "Selection Summary");
        controller.prevCategoryButton = FindDeep<Button>(root, "Previous Category");
        controller.nextCategoryButton = FindDeep<Button>(root, "Next Category");
        controller.machineStatus = FindDeep<Text>(root, "Machine Status");
        controller.shakeButton = FindDeep<Button>(root, "Shake Button");
        controller.shakeButtonLabel = FindButtonLabel(root, "Shake Button");
        controller.sugarStateLabel = FindDeep<Text>(root, "Sugar State");
        controller.iceStateLabel = FindDeep<Text>(root, "Ice State");
    }

    private static void RewireSettlementScreen(Transform root)
    {
        controller.settlementTitle = FindDeep<Text>(root, "Settlement Title");
        controller.settlementBody = FindDeep<Text>(root, "Settlement Body");
        controller.settlementButton = FindDeep<Button>(root, "Settlement Button");
        controller.settlementButtonLabel = FindButtonLabel(root, "Settlement Button");
    }

    private static void RewireRestScreen(Transform root)
    {
        controller.restHint = FindDeep<Text>(root, "Rest Hint");
        controller.nextDayButton = FindDeep<Button>(root, "Next Day Button");
        controller.nextDayButtonLabel = FindButtonLabel(root, "Next Day Button");
        controller.stayButton = FindDeep<Button>(root, "Stay Button");
        controller.stayButtonLabel = FindButtonLabel(root, "Stay Button");
    }

    private static void RewireStartScreen(Transform root)
    {
        controller.settingsButton = FindDeep<Button>(root, "Settings Button");
        controller.startGameButton = FindDeep<Button>(root, "Start Game");
        controller.loadGameButton = FindDeep<Button>(root, "Load Game");
        controller.quitGameButton = FindDeep<Button>(root, "Quit Game");
    }

    private static void RewireIntroScreen(Transform root)
    {
        controller.introVideo = FindDeep<VideoPlayer>(root, "Video Surface");
        controller.introVideoImage = FindDeep<RawImage>(root, "Video Surface");
        controller.countdownLabel = FindDeep<Text>(root, "Countdown");
        controller.skipButton = FindDeep<Button>(root, "Skip Button");
        controller.skipButtonLabel = FindButtonLabel(root, "Skip Button");
    }

    private static void RewireSettingsPanel(Transform root)
    {
        controller.settingsCloseButton = FindDeep<Button>(root, "Settings Close");
        controller.volumeValueLabel = FindDeep<Text>(root, "Volume Value");
        controller.volumeDownButton = FindDeep<Button>(root, "Volume Prev");
        controller.volumeUpButton = FindDeep<Button>(root, "Volume Next");
        controller.resolutionValueLabel = FindDeep<Text>(root, "Resolution Value");
        controller.resolutionPrevButton = FindDeep<Button>(root, "Resolution Prev");
        controller.resolutionNextButton = FindDeep<Button>(root, "Resolution Next");
        controller.languageValueLabel = FindDeep<Text>(root, "Language Value");
        controller.languagePrevButton = FindDeep<Button>(root, "Language Prev");
        controller.languageNextButton = FindDeep<Button>(root, "Language Next");
    }

    // ======== 各界面构建方法 ========

    private static void BuildDialogueScreen(Transform parent)
    {
        // 全屏店景背景
        GameObject shopPanel = CreatePanel("Shop Overview", parent, panelLight);
        Stretch(shopPanel.GetComponent<RectTransform>());
        Sprite shopScene = art != null ? art.shopScene : null;
        if (shopScene != null)
        {
            ApplySprite(shopPanel.GetComponent<Image>(), shopScene);
        }
        else
        {
            CreateText("Shop Title", shopPanel.transform, "奶茶店 · 俯视场景（拖入背景图到 shopScene 槽位）", 34, cream,
                TextAnchor.MiddleCenter, new Vector2(560, 540), new Vector2(800, 55), true);
        }

        // Q版角色（主角固定；柜台前顾客按当前 MilkTeaCustomer 动态切换）
        Vector2 protagonistPosition = art != null && art.protagonistChibiPosition != Vector2.zero
            ? art.protagonistChibiPosition : new Vector2(1120f, 600f);
        Vector2 protagonistSize = art != null && art.protagonistChibiSize.x > 0f && art.protagonistChibiSize.y > 0f
            ? art.protagonistChibiSize : new Vector2(150f, 150f);
        Vector2 customerPosition = art != null && art.customerChibiPosition != Vector2.zero
            ? art.customerChibiPosition : new Vector2(1360f, 390f);
        Vector2 customerSize = art != null && art.customerChibiSize.x > 0f && art.customerChibiSize.y > 0f
            ? art.customerChibiSize : new Vector2(180f, 180f);
        controller.protagonistChibiImage = CreateChibi(parent, "主角", protagonistPosition, protagonistSize, mint,
            art != null ? art.protagonistChibi : null);
        controller.customerChibiImage = CreateChibi(parent, "顾客", customerPosition, customerSize, coral,
            art != null ? art.customerChibi : null);

        // 顶部天数/营业状态徽章
        GameObject dayBadge = CreatePanel("Day Badge", parent, Hex("#1A1A1A"));
        SetRect(dayBadge.GetComponent<RectTransform>(), 710, 940, 500, 110);
        Sprite dayBg = art != null ? art.dayTitleBackground : null;
        if (dayBg != null)
        {
            Image badgeImage = dayBadge.GetComponent<Image>();
            badgeImage.sprite = dayBg;
            badgeImage.color = Color.white;
            badgeImage.type = dayBg.border != Vector4.zero ? Image.Type.Sliced : Image.Type.Simple;
            badgeImage.preserveAspect = true;
        }

        Color titleTextColor = art != null && art.dayTitleTextColor.a > 0.01f
            ? art.dayTitleTextColor : Hex("#402E24");
        controller.dayTitle = CreateText("Title", dayBadge.transform, "第 1 天 · 营业中", 36, titleTextColor,
            TextAnchor.MiddleCenter, Vector2.zero, new Vector2(500, 110), true);

        // 角色立绘（透明背景浮层，覆盖在左侧）
        GameObject portraitPanel = CreatePanel("Customer Portrait", parent, new Color(0f, 0f, 0f, 0f));
        SetRect(portraitPanel.GetComponent<RectTransform>(), 0, 80, 700, 900);
        Image portraitImage = portraitPanel.GetComponent<Image>();
        portraitImage.raycastTarget = false;
        controller.portraitImage = portraitImage;
        controller.portraitAnimator = portraitPanel.AddComponent<SpriteSequenceAnimator>();
        controller.portraitLabel = CreateText("Portrait Label", portraitPanel.transform, "顾客\n临时立绘", 72, cream,
            TextAnchor.MiddleCenter, new Vector2(80, 250), new Vector2(540, 300), true);

        // 对话框（底部加宽）
        GameObject dialogueBox = CreatePanel("Dialogue Box", parent, Hex("#172033"));
        SetRect(dialogueBox.GetComponent<RectTransform>(), 80, 30, 1760, 300);
        ApplySprite(dialogueBox.GetComponent<Image>(), art != null ? art.dialogueBoxBackground : null);

        // 说话人名牌
        GameObject speakerTag = CreatePanel("Speaker Tag", parent, coral);
        SetRect(speakerTag.GetComponent<RectTransform>(), 150, 280, 260, 60);
        Sprite tagBg = art != null ? art.speakerTagBackground : null;
        if (tagBg != null)
        {
            Image tagImage = speakerTag.GetComponent<Image>();
            tagImage.sprite = tagBg;
            tagImage.color = Color.white;
            tagImage.type = tagBg.border != Vector4.zero ? Image.Type.Sliced : Image.Type.Simple;
            tagImage.preserveAspect = false;
        }

        Color speakerTextColor = art != null && art.speakerTextColor.a > 0.01f
            ? art.speakerTextColor : cream;
        controller.dialogueSpeaker = CreateText("Speaker", speakerTag.transform, string.Empty, 32, speakerTextColor,
            TextAnchor.MiddleCenter, Vector2.zero, new Vector2(260, 60), true);

        Color dialogueTextColor = art != null && art.dialogueTextColor.a > 0.01f
            ? art.dialogueTextColor : Hex("#52382E");
        controller.dialogueLine = CreateText("Line", dialogueBox.transform, string.Empty, 36, dialogueTextColor,
            TextAnchor.UpperLeft, new Vector2(50, 50), new Vector2(1500, 180), false);

        // 继续按钮（支持图标替换，如猫爪）
        Text continueLabel;
        controller.dialogueButton = CreateButton("Continue", dialogueBox.transform, "继续", new Vector2(1530, 30),
            new Vector2(180, 100), coral, out continueLabel);
        Sprite continueIcon = art != null ? art.continueButtonIcon : null;
        if (continueIcon != null)
        {
            Image btnImage = controller.dialogueButton.GetComponent<Image>();
            btnImage.sprite = continueIcon;
            btnImage.color = Color.white;
            btnImage.type = Image.Type.Simple;
            btnImage.preserveAspect = true;

            // 图标按钮保持素材原色，避免沿用 CreateButton 的珊瑚红 Color Tint。
            ColorBlock colors = controller.dialogueButton.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.92f, 0.92f, 0.92f, 1f);
            colors.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
            colors.selectedColor = Color.white;
            colors.disabledColor = new Color(0.6f, 0.6f, 0.6f, 1f);
            controller.dialogueButton.colors = colors;
            continueLabel.gameObject.SetActive(false);
        }

        controller.dialogueButtonLabel = continueLabel;
    }

    private static void BuildMixingScreen(Transform parent)
    {
        // 全屏底图（含右侧操作区装饰和奶油粉底色）
        Sprite mixingBg = art != null ? art.mixingBackground : null;
        if (mixingBg != null)
        {
            ApplySprite(parent.GetComponent<Image>(), mixingBg);
        }

        // 左上后厨场景（叠在底图左侧）
        GameObject kitchenPanel = CreatePanel("Kitchen Overview", parent, panel);
        SetRect(kitchenPanel.GetComponent<RectTransform>(), 30, 340, 630, 710);
        Sprite kitchenScene = art != null ? art.kitchenScene : null;
        if (kitchenScene != null)
        {
            ApplySprite(kitchenPanel.GetComponent<Image>(), kitchenScene);
        }
        else
        {
            CreateText("Kitchen Title", kitchenPanel.transform, "后厨 · 俯视场景", 34, cream,
                TextAnchor.MiddleCenter, new Vector2(100, 530), new Vector2(430, 55), true);
        }

        CreateChibi(kitchenPanel.transform, "主角", new Vector2(250, 80), mint, art != null ? art.protagonistChibi : null);

        // 左下对话框（复用 dialogueBoxBackground）
        GameObject orderBox = CreatePanel("Order Reminder", parent, Hex("#172033"));
        SetRect(orderBox.GetComponent<RectTransform>(), 30, 30, 630, 280);
        ApplySprite(orderBox.GetComponent<Image>(), art != null ? art.dialogueBoxBackground : null);

        // 说话人名牌（复用 speakerTagBackground）
        GameObject orderTag = CreatePanel("Order Tag", parent, coral);
        SetRect(orderTag.GetComponent<RectTransform>(), 60, 260, 200, 50);
        Sprite tagBg = art != null ? art.speakerTagBackground : null;
        if (tagBg != null)
        {
            Image tagImage = orderTag.GetComponent<Image>();
            tagImage.sprite = tagBg;
            tagImage.color = Color.white;
            tagImage.type = tagBg.border != Vector4.zero ? Image.Type.Sliced : Image.Type.Simple;
        }

        Color speakerColor = art != null && art.speakerTextColor.a > 0.01f
            ? art.speakerTextColor : cream;
        controller.orderSpeaker = CreateText("Order Speaker", orderTag.transform, "顾客需求", 28, speakerColor,
            TextAnchor.MiddleCenter, Vector2.zero, new Vector2(200, 50), true);

        Color dialogueColor = art != null && art.dialogueTextColor.a > 0.01f
            ? art.dialogueTextColor : Hex("#52382E");
        controller.orderLine = CreateText("Order Line", orderBox.transform,
            "经典珍珠奶茶 · 少糖 · 少冰\n茶底、奶底和配料需要由你完成。", 30, dialogueColor,
            TextAnchor.UpperLeft, new Vector2(40, 40), new Vector2(550, 170), false);

        // 猫爪装饰（复用 continueButtonIcon，仅装饰）
        Sprite continueIcon = art != null ? art.continueButtonIcon : null;
        if (continueIcon != null)
        {
            GameObject paw = CreatePanel("Order Paw", orderBox.transform, Color.white);
            SetRect(paw.GetComponent<RectTransform>(), 530, 10, 80, 80);
            Image pawImage = paw.GetComponent<Image>();
            pawImage.sprite = continueIcon;
            pawImage.type = Image.Type.Simple;
            pawImage.preserveAspect = true;
            pawImage.raycastTarget = false;
        }

        // 右侧操作区容器（透明，仅做布局容器）
        GameObject operationPanel = CreatePanel("Operation Panel", parent, Color.clear);
        SetRect(operationPanel.GetComponent<RectTransform>(), 700, 0, 1220, 1080);
        operationPanel.GetComponent<Image>().raycastTarget = false;
        BuildRecipePanel(operationPanel.transform);
        BuildIngredientPanel(operationPanel.transform);
        BuildSugarIcePanel(operationPanel.transform);
        BuildShakePanel(operationPanel.transform);
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

        // 左上角设置入口（有图标则用图标，否则文字）
        Text gearLabel;
        controller.settingsButton = CreateButton("Settings Button", parent, "设置", new Vector2(45, 945),
            new Vector2(120, 90), panel, out gearLabel);
        gearLabel.fontSize = 30;
        Sprite settingsIcon = art != null ? art.settingsIcon : null;
        if (settingsIcon != null)
        {
            Image settingsBg = controller.settingsButton.GetComponent<Image>();
            settingsBg.sprite = settingsIcon;
            settingsBg.color = Color.white;
            settingsBg.type = Image.Type.Simple;
            settingsBg.preserveAspect = true;
            gearLabel.gameObject.SetActive(false);
        }
        else
        {
            AddFrame(controller.settingsButton.transform, mint);
        }

        // 右侧上方 Logo
        GameObject logo = CreatePanel("Logo", parent, new Color(0f, 0f, 0f, 0.35f));
        SetRect(logo.GetComponent<RectTransform>(), 1090, 600, 760, 420);
        if (!ApplySprite(logo.GetComponent<Image>(), art != null ? art.startLogo : null))
        {
            CreateText("Logo Text", logo.transform, "奶茶店\n模拟经营", 96, cream, TextAnchor.MiddleCenter,
                Vector2.zero, new Vector2(760, 420), true);
        }

        // 右侧下方三个按钮（等距分布）
        Text startLabel;
        controller.startGameButton = CreateButton("Start Game", parent, "新的游戏", new Vector2(1150, 470),
            new Vector2(560, 110), mint, out startLabel);
        startLabel.color = dark;
        startLabel.fontSize = 38;

        Text loadLabel;
        controller.loadGameButton = CreateButton("Load Game", parent, "读取存档", new Vector2(1150, 330),
            new Vector2(560, 110), panelLight, out loadLabel);
        loadLabel.fontSize = 38;

        Text quitLabel;
        controller.quitGameButton = CreateButton("Quit Game", parent, "退出游戏", new Vector2(1150, 190),
            new Vector2(560, 110), panel, out quitLabel);
        quitLabel.fontSize = 38;

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
        GameObject recipePanel = CreatePanel("Recipe Manual", parent, new Color(0f, 0f, 0f, 0.02f));
        SetRect(recipePanel.GetComponent<RectTransform>(), 20, 700, 1180, 360);
        recipePanel.GetComponent<Image>().raycastTarget = false;

        CreateText("Recipe Header", recipePanel.transform, "配方详情", 34, Hex("#52382E"),
            TextAnchor.MiddleCenter, new Vector2(20, 290), new Vector2(220, 50), true);

        // 配方文字信息
        controller.recipeDetails = CreateText("Recipe Details", recipePanel.transform,
            string.Empty, 28, Hex("#52382E"),
            TextAnchor.UpperLeft, new Vector2(20, 160), new Vector2(450, 130), true);

        // 奶茶成品图
        GameObject iconCard = CreatePanel("Recipe Icon", recipePanel.transform, new Color(0f, 0f, 0f, 0f));
        SetRect(iconCard.GetComponent<RectTransform>(), 780, 30, 360, 300);
        iconCard.GetComponent<Image>().raycastTarget = false;
        controller.recipeIcon = CreateText("Recipe Icon Text", iconCard.transform, "?", 66, yellow,
            TextAnchor.MiddleCenter, Vector2.zero, new Vector2(360, 300), true);
        GameObject iconImageObject = CreatePanel("Recipe Icon Image", iconCard.transform, Color.white);
        SetRect(iconImageObject.GetComponent<RectTransform>(), 0, 0, 360, 300);
        controller.recipeIconImage = iconImageObject.GetComponent<Image>();
        controller.recipeIconImage.preserveAspect = true;
        controller.recipeIconImage.raycastTarget = false;
        iconImageObject.SetActive(false);

        // 左右箭头
        Text unused;
        controller.prevRecipeButton = CreateButton("Previous Recipe", recipePanel.transform, "◀", new Vector2(700, 140),
            new Vector2(60, 60), panelLight, out unused);
        ApplyArrowIcon(controller.prevRecipeButton, art != null ? art.recipePreviousIcon : null);
        controller.nextRecipeButton = CreateButton("Next Recipe", recipePanel.transform, "▶", new Vector2(1100, 140),
            new Vector2(60, 60), panelLight, out unused);
        ApplyArrowIcon(controller.nextRecipeButton, art != null ? art.recipeNextIcon : null);

        controller.recipePage = CreateText("Page", recipePanel.transform, "1 / 10", 22, Hex("#52382E"), TextAnchor.MiddleCenter,
            new Vector2(880, 10), new Vector2(120, 30), true);
    }

    private static void BuildIngredientPanel(Transform parent)
    {
        GameObject ingredientPanel = CreatePanel("Ingredients", parent, new Color(0f, 0f, 0f, 0.02f));
        SetRect(ingredientPanel.GetComponent<RectTransform>(), 20, 200, 740, 480);
        ingredientPanel.GetComponent<Image>().raycastTarget = false;

        controller.categoryTitle = CreateText("Category Title", ingredientPanel.transform, "原料选择 · 茶底", 30, Hex("#52382E"),
            TextAnchor.MiddleLeft, new Vector2(60, 420), new Vector2(400, 46), true);

        // 分类图标（茶叶/牛奶/配料）
        GameObject categoryIconObj = CreatePanel("Category Icon", ingredientPanel.transform, Color.clear);
        SetRect(categoryIconObj.GetComponent<RectTransform>(), 10, 418, 46, 46);
        controller.categoryIcon = categoryIconObj.GetComponent<Image>();
        controller.categoryIcon.preserveAspect = true;
        controller.categoryIcon.raycastTarget = false;
        Sprite teaIcon = art != null ? art.teaCategoryIcon : null;
        if (teaIcon != null)
        {
            controller.categoryIcon.sprite = teaIcon;
            controller.categoryIcon.color = Color.white;
        }

        // 上下箭头
        Text unused;
        controller.prevCategoryButton = CreateButton("Previous Category", ingredientPanel.transform, "▲", new Vector2(570, 432),
            new Vector2(56, 48), panelLight, out unused);
        ApplyArrowIcon(controller.prevCategoryButton, art != null ? art.categoryPreviousIcon : null);
        controller.nextCategoryButton = CreateButton("Next Category", ingredientPanel.transform, "▼", new Vector2(640, 432),
            new Vector2(56, 48), panelLight, out unused);
        ApplyArrowIcon(controller.nextCategoryButton, art != null ? art.categoryNextIcon : null);

        BuildChoicePanel(ingredientPanel.transform, IngredientCategory.Tea,
            new[] { "红茶", "茉莉绿茶", "抹茶", "乌龙" }, 4);
        BuildChoicePanel(ingredientPanel.transform, IngredientCategory.Milk,
            new[] { "鲜奶", "椰奶", "无奶" }, 3);
        BuildChoicePanel(ingredientPanel.transform, IngredientCategory.Topping,
            new[] { "珍珠", "黑糖珍珠", "芝士奶盖", "芋泥", "红豆", "芒果西米", "草莓芝士", "百香果椰果", "桂花糖浆", "无" }, 3);

        controller.selectionSummary = CreateText("Selection Summary", ingredientPanel.transform,
            "已选：茶底 — ｜ 奶底 — ｜ 配料 —", 22, Hex("#52382E"),
            TextAnchor.MiddleLeft, new Vector2(20, 0), new Vector2(700, 40), true);
    }

    private static void BuildSugarIcePanel(Transform parent)
    {
        GameObject sugarIcePanel = CreatePanel("Sugar Ice Panel", parent, new Color(0f, 0f, 0f, 0.02f));
        SetRect(sugarIcePanel.GetComponent<RectTransform>(), 780, 200, 400, 480);
        sugarIcePanel.GetComponent<Image>().raycastTarget = false;

        // 糖浆
        Sprite sugarIconSprite = art != null ? art.sugarIcon : null;
        if (sugarIconSprite != null)
        {
            GameObject sugarIconObj = CreatePanel("Sugar Icon", sugarIcePanel.transform, Color.white);
            SetRect(sugarIconObj.GetComponent<RectTransform>(), 20, 370, 60, 60);
            ApplySprite(sugarIconObj.GetComponent<Image>(), sugarIconSprite);
            sugarIconObj.GetComponent<Image>().raycastTarget = false;
        }

        CreateText("Sugar Label", sugarIcePanel.transform, "糖浆", 28, Hex("#52382E"),
            TextAnchor.MiddleLeft, new Vector2(90, 375), new Vector2(100, 50), true);
        CreateLevelBlock(sugarIcePanel.transform, "Sugar 1", new Vector2(200, 378), yellow, true, 0);
        CreateLevelBlock(sugarIcePanel.transform, "Sugar 2", new Vector2(270, 378), yellow, true, 1);
        controller.sugarStateLabel = CreateText("Sugar State", sugarIcePanel.transform, "无糖", 24, Hex("#52382E"),
            TextAnchor.MiddleRight, new Vector2(220, 325), new Vector2(150, 40), true);

        // 冰块
        Sprite iceIconSprite = art != null ? art.iceIcon : null;
        if (iceIconSprite != null)
        {
            GameObject iceIconObj = CreatePanel("Ice Icon", sugarIcePanel.transform, Color.white);
            SetRect(iceIconObj.GetComponent<RectTransform>(), 20, 220, 60, 60);
            ApplySprite(iceIconObj.GetComponent<Image>(), iceIconSprite);
            iceIconObj.GetComponent<Image>().raycastTarget = false;
        }

        CreateText("Ice Label", sugarIcePanel.transform, "冰块", 28, Hex("#52382E"),
            TextAnchor.MiddleLeft, new Vector2(90, 225), new Vector2(100, 50), true);
        CreateLevelBlock(sugarIcePanel.transform, "Ice 1", new Vector2(200, 228), blue, false, 0);
        CreateLevelBlock(sugarIcePanel.transform, "Ice 2", new Vector2(270, 228), blue, false, 1);
        controller.iceStateLabel = CreateText("Ice State", sugarIcePanel.transform, "去冰", 24, Hex("#52382E"),
            TextAnchor.MiddleRight, new Vector2(220, 175), new Vector2(150, 40), true);
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

    private static void BuildShakePanel(Transform parent)
    {
        // 状态文字
        controller.machineStatus = CreateText("Machine Status", parent, "请选择原料后点击开始摇动", 24, Hex("#52382E"),
            TextAnchor.MiddleCenter, new Vector2(310, 120), new Vector2(600, 40), true);

        // 开始摇动按钮
        Text shakeLabel;
        controller.shakeButton = CreateButton("Shake Button", parent, "开始摇动！", new Vector2(310, 20),
            new Vector2(600, 90), coral, out shakeLabel);
        shakeLabel.fontSize = 36;
        shakeLabel.color = Color.white;
        controller.shakeButtonLabel = shakeLabel;

        Sprite shakeBg = art != null ? art.shakeButtonBackground : null;
        if (shakeBg != null)
        {
            Image btnImage = controller.shakeButton.GetComponent<Image>();
            btnImage.sprite = shakeBg;
            btnImage.color = Color.white;
            btnImage.type = shakeBg.border != Vector4.zero ? Image.Type.Sliced : Image.Type.Simple;
            btnImage.preserveAspect = false;

            ColorBlock colors = controller.shakeButton.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.92f, 0.92f, 0.92f, 1f);
            colors.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
            colors.selectedColor = Color.white;
            colors.disabledColor = new Color(0.6f, 0.6f, 0.6f, 1f);
            controller.shakeButton.colors = colors;
        }

        Sprite shakeIcon = art != null ? art.shakeButtonIcon : null;
        if (shakeIcon != null)
        {
            GameObject iconObj = CreatePanel("Shake Icon", controller.shakeButton.transform, Color.white);
            SetRect(iconObj.GetComponent<RectTransform>(), 20, 10, 70, 70);
            Image iconImage = iconObj.GetComponent<Image>();
            iconImage.sprite = shakeIcon;
            iconImage.type = Image.Type.Simple;
            iconImage.preserveAspect = true;
            iconImage.raycastTarget = false;
        }
    }

    private static void ApplyArrowIcon(Button button, Sprite icon)
    {
        if (button == null || icon == null)
        {
            return;
        }

        Image image = button.GetComponent<Image>();
        image.sprite = icon;
        image.color = Color.white;
        image.type = Image.Type.Simple;
        image.preserveAspect = true;

        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(0.92f, 0.92f, 0.92f, 1f);
        colors.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
        colors.selectedColor = Color.white;
        colors.disabledColor = new Color(0.6f, 0.6f, 0.6f, 1f);
        button.colors = colors;

        Text label = button.GetComponentInChildren<Text>();
        if (label != null)
        {
            label.gameObject.SetActive(false);
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

    private static Image CreateChibi(Transform parent, string label, Vector2 position, Color color, Sprite sprite)
    {
        return CreateChibi(parent, label, position, new Vector2(120f, 120f), color, sprite);
    }

    private static Image CreateChibi(Transform parent, string label, Vector2 position, Vector2 dimensions,
        Color color, Sprite sprite)
    {
        GameObject body = CreatePanel("Chibi " + label, parent, color);
        SetRect(body.GetComponent<RectTransform>(), position.x, position.y, dimensions.x, dimensions.y);
        Image image = body.GetComponent<Image>();
        if (!ApplySprite(image, sprite))
        {
            CreateText("Chibi Label", body.transform, label + "\nQ版", 24, dark,
                TextAnchor.MiddleCenter, Vector2.zero, dimensions, true);
        }

        return image;
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
