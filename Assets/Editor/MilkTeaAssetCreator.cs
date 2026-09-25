#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 一键生成方案 B 所需的数据资产：10 款配方 + 美术库 + 默认对话，并放到 Resources 目录，
/// 供运行时 <see cref="MilkTeaDemoController"/> 通过 Resources.Load 读取。
/// 生成后即可在 Inspector 中逐个拖拽替换图标 / 立绘 / 场景等 Sprite，或编辑对话台词。
/// 菜单：奶茶店 Demo → 生成/更新数据资产。
/// </summary>
public static class MilkTeaAssetCreator
{
    private const string ResourcesFolder = "Assets/Resources";
    private const string RecipeFolder = "Assets/Resources/Recipes";
    private const string DialogueFolder = "Assets/Resources/Dialogues";
    private const string CustomerFolder = "Assets/Resources/Customers";
    private const string LibraryPath = "Assets/Resources/MilkTeaArtLibrary.asset";

    private static readonly string[][] RecipeTable =
    {
        // displayName, iconLabel, teaBase, milkBase, topping, customerName
        new[] { "经典珍珠奶茶", "珍珠", "红茶", "鲜奶", "珍珠", "上班族顾客" },
        new[] { "茉莉奶绿", "奶绿", "茉莉绿茶", "鲜奶", "无", "学生顾客" },
        new[] { "黑糖脏脏奶茶", "黑糖", "红茶", "鲜奶", "黑糖珍珠", "熟客" },
        new[] { "芝士奶盖茉莉", "奶盖", "茉莉绿茶", "无奶", "芝士奶盖", "白领顾客" },
        new[] { "芋泥波波奶茶", "芋泥", "红茶", "鲜奶", "芋泥", "甜品爱好者" },
        new[] { "抹茶红豆奶", "抹茶", "抹茶", "鲜奶", "红豆", "抹茶控" },
        new[] { "杨枝甘露", "杨枝", "茉莉绿茶", "椰奶", "芒果西米", "夏日顾客" },
        new[] { "草莓芝芝", "草莓", "茉莉绿茶", "无奶", "草莓芝士", "少女顾客" },
        new[] { "百香果双响炮", "百香", "茉莉绿茶", "无奶", "百香果椰果", "健身顾客" },
        new[] { "桂花乌龙拿铁", "桂花", "乌龙", "鲜奶", "桂花糖浆", "老茶客" },
    };

    private static readonly string[] IngredientNames =
    {
        "红茶", "茉莉绿茶", "抹茶", "乌龙",
        "鲜奶", "椰奶", "无奶",
        "珍珠", "黑糖珍珠", "芝士奶盖", "芋泥", "红豆", "芒果西米",
        "草莓芝士", "百香果椰果", "桂花糖浆", "无"
    };

    [MenuItem("奶茶店 Demo/生成或更新数据资产")]
    public static void CreateAssets()
    {
        EnsureFolder(ResourcesFolder);
        EnsureFolder(RecipeFolder);

        MilkTeaArtLibrary library = AssetDatabase.LoadAssetAtPath<MilkTeaArtLibrary>(LibraryPath);
        if (library == null)
        {
            library = ScriptableObject.CreateInstance<MilkTeaArtLibrary>();
            AssetDatabase.CreateAsset(library, LibraryPath);
        }

        library.recipes.Clear();
        for (int i = 0; i < RecipeTable.Length; i++)
        {
            string[] row = RecipeTable[i];
            string assetPath = RecipeFolder + "/" + (i + 1).ToString("00") + "_" + row[0] + ".asset";
            MilkTeaRecipe recipe = AssetDatabase.LoadAssetAtPath<MilkTeaRecipe>(assetPath);
            if (recipe == null)
            {
                recipe = ScriptableObject.CreateInstance<MilkTeaRecipe>();
                AssetDatabase.CreateAsset(recipe, assetPath);
            }

            recipe.displayName = row[0];
            recipe.iconLabel = row[1];
            recipe.teaBase = row[2];
            recipe.milkBase = row[3];
            recipe.topping = row[4];
            recipe.customerName = row[5];
            EditorUtility.SetDirty(recipe);
            library.recipes.Add(recipe);
        }

        library.ingredientIcons.Clear();
        foreach (string name in IngredientNames)
        {
            library.ingredientIcons.Add(new MilkTeaArtLibrary.NamedSprite { name = name });
        }

        EnsureFolder(DialogueFolder);
        if (library.defaultOpeningDialogue == null)
        {
            library.defaultOpeningDialogue = EnsureDialogue(DialogueFolder + "/OpeningDialogue.asset", new[]
            {
                new MilkTeaDialogue.Line { speaker = "{customer}", text = "你好，我想要一杯{drink}，{ice}、{sugar}。", buttonLabel = "回应" },
                new MilkTeaDialogue.Line { speaker = "主角", text = "了解了，请稍候。", buttonLabel = "开始调配" }
            });
        }

        if (library.defaultServingDialogue == null)
        {
            library.defaultServingDialogue = EnsureDialogue(DialogueFolder + "/ServingDialogue.asset", new[]
            {
                new MilkTeaDialogue.Line { speaker = "主角", text = "您的{drink}做好了，请慢用。", buttonLabel = "继续" },
                new MilkTeaDialogue.Line { speaker = "{customer}", text = "谢谢，做得很棒！", buttonLabel = "下一位" }
            });
        }

        EditorUtility.SetDirty(library);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Selection.activeObject = library;
        EditorGUIUtility.PingObject(library);
        Debug.Log("[奶茶店 Demo] 已生成 " + RecipeTable.Length + " 款配方与美术库：" + LibraryPath +
                  "。现在可在 Inspector 中拖拽替换各处 Sprite。");
    }

    private static MilkTeaDialogue EnsureDialogue(string path, MilkTeaDialogue.Line[] defaultLines)
    {
        MilkTeaDialogue dialogue = AssetDatabase.LoadAssetAtPath<MilkTeaDialogue>(path);
        if (dialogue == null)
        {
            dialogue = ScriptableObject.CreateInstance<MilkTeaDialogue>();
            dialogue.lines = new List<MilkTeaDialogue.Line>(defaultLines);
            AssetDatabase.CreateAsset(dialogue, path);
            EditorUtility.SetDirty(dialogue);
        }

        return dialogue;
    }

    [MenuItem("奶茶店 Demo/生成示例客人")]
    public static void CreateSampleCustomers()
    {
        MilkTeaArtLibrary library = AssetDatabase.LoadAssetAtPath<MilkTeaArtLibrary>(LibraryPath);
        if (library == null || library.recipes == null || library.recipes.Count == 0)
        {
            Debug.LogError("[奶茶店 Demo] 未找到配方数据，请先执行「生成或更新数据资产」。");
            return;
        }

        EnsureFolder(DialogueFolder);
        EnsureFolder(CustomerFolder);

        library.customers.Clear();

        library.customers.Add(BuildSampleCustomer("Customer_小林", "上班族小林", "经典珍珠奶茶", 1, 1,
            BuildDialogue(DialogueFolder + "/Opening_小林.asset", new[]
            {
                new MilkTeaDialogue.Line { speaker = "{customer}", text = "老板，来杯{drink}，我赶时间！", buttonLabel = "回应" },
                new MilkTeaDialogue.Line { speaker = "主角", text = "好嘞，{sugar}、{ice}，对吧？", buttonLabel = "确认" },
                new MilkTeaDialogue.Line { speaker = "{customer}", text = "对对，麻烦快一点～", buttonLabel = "好的" },
                new MilkTeaDialogue.Line { speaker = "主角", text = "收到，马上给你做！", buttonLabel = "开始调配" }
            }),
            BuildDialogue(DialogueFolder + "/Serving_小林.asset", new[]
            {
                new MilkTeaDialogue.Line { speaker = "主角", text = "你的{drink}好了，拿稳～", buttonLabel = "递过去" },
                new MilkTeaDialogue.Line { speaker = "{customer}", text = "谢啦，全靠这杯续命！", buttonLabel = "下一位" }
            })));

        library.customers.Add(BuildSampleCustomer("Customer_阿绿", "抹茶控阿绿", "抹茶红豆奶", 0, 0,
            BuildDialogue(DialogueFolder + "/Opening_阿绿.asset", new[]
            {
                new MilkTeaDialogue.Line { speaker = "{customer}", text = "我要一杯{drink}，记得{sugar}、{ice}哦。", buttonLabel = "回应" },
                new MilkTeaDialogue.Line { speaker = "主角", text = "抹茶控没跑了，红豆给你多留点？", buttonLabel = "好呀" },
                new MilkTeaDialogue.Line { speaker = "{customer}", text = "那敢情好，谢谢！", buttonLabel = "开始调配" }
            }),
            BuildDialogue(DialogueFolder + "/Serving_阿绿.asset", new[]
            {
                new MilkTeaDialogue.Line { speaker = "主角", text = "抹茶红豆奶来啦，慢用。", buttonLabel = "递上" },
                new MilkTeaDialogue.Line { speaker = "{customer}", text = "嗯～这抹茶香气真正！", buttonLabel = "下一位" }
            })));

        library.customers.Add(BuildSampleCustomer("Customer_小满", "夏日少女小满", "杨枝甘露", 2, 2,
            BuildDialogue(DialogueFolder + "/Opening_小满.asset", new[]
            {
                new MilkTeaDialogue.Line { speaker = "{customer}", text = "天气好热，来杯{drink}！", buttonLabel = "回应" },
                new MilkTeaDialogue.Line { speaker = "主角", text = "没问题，{sugar}、{ice}，透心凉～", buttonLabel = "确认" },
                new MilkTeaDialogue.Line { speaker = "{customer}", text = "就要这种冰爽感觉！", buttonLabel = "开始调配" }
            }),
            BuildDialogue(DialogueFolder + "/Serving_小满.asset", new[]
            {
                new MilkTeaDialogue.Line { speaker = "主角", text = "你的{drink}，别喝太急哦。", buttonLabel = "递给她" },
                new MilkTeaDialogue.Line { speaker = "{customer}", text = "谢谢老板，太满足啦！", buttonLabel = "下一位" }
            })));

        EditorUtility.SetDirty(library);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Selection.activeObject = library;
        EditorGUIUtility.PingObject(library);
        Debug.Log("[奶茶店 Demo] 已生成 " + library.customers.Count + " 位示例客人并写入出场表，运行后会按顺序循环出场。如需恢复随机点单，清空美术库的 customers 列表即可。");
    }

    private static MilkTeaCustomer BuildSampleCustomer(string fileName, string customerName, string recipeName,
        int sugarLevel, int iceLevel, MilkTeaDialogue opening, MilkTeaDialogue serving)
    {
        string path = CustomerFolder + "/" + fileName + ".asset";
        MilkTeaCustomer customer = AssetDatabase.LoadAssetAtPath<MilkTeaCustomer>(path);
        if (customer == null)
        {
            customer = ScriptableObject.CreateInstance<MilkTeaCustomer>();
            AssetDatabase.CreateAsset(customer, path);
        }

        MilkTeaRecipe recipe = FindRecipe(recipeName);
        if (recipe == null)
        {
            Debug.LogWarning("[奶茶店 Demo] 未找到配方资产：" + recipeName + "，请先生成数据资产。");
        }

        customer.customerName = customerName;
        customer.order = recipe;
        customer.sugarLevel = sugarLevel;
        customer.iceLevel = iceLevel;
        customer.openingDialogue = opening;
        customer.servingDialogue = serving;
        EditorUtility.SetDirty(customer);
        return customer;
    }

    private static MilkTeaRecipe FindRecipe(string displayName)
    {
        for (int i = 0; i < RecipeTable.Length; i++)
        {
            if (RecipeTable[i][0] == displayName)
            {
                string path = RecipeFolder + "/" + (i + 1).ToString("00") + "_" + displayName + ".asset";
                return AssetDatabase.LoadAssetAtPath<MilkTeaRecipe>(path);
            }
        }

        return null;
    }

    private static MilkTeaDialogue BuildDialogue(string path, MilkTeaDialogue.Line[] lines)
    {
        MilkTeaDialogue dialogue = AssetDatabase.LoadAssetAtPath<MilkTeaDialogue>(path);
        if (dialogue == null)
        {
            dialogue = ScriptableObject.CreateInstance<MilkTeaDialogue>();
            AssetDatabase.CreateAsset(dialogue, path);
        }

        dialogue.lines = new List<MilkTeaDialogue.Line>(lines);
        EditorUtility.SetDirty(dialogue);
        return dialogue;
    }

    private static void EnsureFolder(string folder)
    {
        if (AssetDatabase.IsValidFolder(folder))
        {
            return;
        }

        string parent = Path.GetDirectoryName(folder).Replace('\\', '/');
        string leaf = Path.GetFileName(folder);
        if (!AssetDatabase.IsValidFolder(parent))
        {
            EnsureFolder(parent);
        }

        AssetDatabase.CreateFolder(parent, leaf);
    }
}
#endif
