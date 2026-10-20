#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 奶茶店内容编辑器：集中管理章节日程、测试跳进度、资产文件名对齐。
/// 解决三个痛点：
/// 1. 章节下的天和出场条目在美术库 Inspector 里层级太深，这里用两步折叠导航，客人字段直接显示"客人名"而不是资产文件名；
/// 2. 测试不必每次从第一天开始：选好章/天一键设为游戏起始进度；
/// 3. Inspector 对象槽只显示文件名：一键把客人/角色资产文件名改成与其内部名称一致。
/// 菜单：奶茶店 Demo → 内容编辑器。
/// </summary>
public sealed class MilkTeaContentEditor : EditorWindow
{
    private MilkTeaArtLibrary art;
    private Vector2 scroll;
    private readonly List<bool> chapterFoldouts = new List<bool>();
    private readonly List<List<bool>> dayFoldouts = new List<List<bool>>();
    private bool syncJumpFromArt = true;
    private int jumpChapter;
    private int jumpDay;

    [MenuItem("奶茶店 Demo/内容编辑器")]
    public static void Open()
    {
        MilkTeaContentEditor window = GetWindow<MilkTeaContentEditor>("内容编辑器");
        window.minSize = new Vector2(560f, 480f);
    }

    private void OnGUI()
    {
        art = EditorGUILayout.ObjectField("美术库", art, typeof(MilkTeaArtLibrary), false) as MilkTeaArtLibrary;
        if (art == null)
        {
            EditorGUILayout.HelpBox("先拖入美术库（Assets/Resources/MilkTeaArtLibrary）。", MessageType.Info);
            return;
        }

        EditorGUILayout.Space(4);
        scroll = EditorGUILayout.BeginScrollView(scroll);

        DrawScheduleSection();
        EditorGUILayout.Space(8);
        DrawJumpSection();
        EditorGUILayout.Space(8);
        DrawRenameSection();

        EditorGUILayout.EndScrollView();
    }

    // ======== 章节日程编辑 ========

    private void DrawScheduleSection()
    {
        EditorGUILayout.LabelField("章节日程", EditorStyles.boldLabel);
        if (art.chapters == null || art.chapters.Count == 0)
        {
            EditorGUILayout.HelpBox(
                "还没有章节。右键 Create → 奶茶店 Demo → 章节 创建后拖进下方列表。\n" +
                "未配置章节时游戏维持旧流程（按客人出场表循环）。", MessageType.Info);
        }

        SyncFoldoutSizes();

        SerializedObject serializedObject = new SerializedObject(art);
        SerializedProperty chaptersProp = serializedObject.FindProperty("chapters");

        // 新增章节
        if (GUILayout.Button("+ 新增章节（在列表末尾创建并挂上）", GUILayout.Width(260f)))
        {
            MilkTeaChapter chapter = CreateAsset<MilkTeaChapter>("Assets/Dialogue", "Chapter", "新章节");
            if (chapter != null)
            {
                chaptersProp.InsertArrayElementAtIndex(chaptersProp.arraySize);
                SerializedProperty element = chaptersProp.GetArrayElementAtIndex(chaptersProp.arraySize - 1);
                element.objectReferenceValue = chapter;
                chapterFoldouts.Add(false);
                dayFoldouts.Add(new List<bool>());
            }
        }

        for (int i = 0; i < chaptersProp.arraySize; i++)
        {
            SerializedProperty chapterProp = chaptersProp.GetArrayElementAtIndex(i);
            MilkTeaChapter chapter = chapterProp.objectReferenceValue as MilkTeaChapter;
            string title = chapter != null && !string.IsNullOrEmpty(chapter.chapterName)
                ? chapter.chapterName
                : "章节 " + (i + 1);

            EditorGUILayout.BeginHorizontal();
            chapterFoldouts[i] = EditorGUILayout.Foldout(chapterFoldouts[i], title, true);
            if (GUILayout.Button("－ 删除", GUILayout.Width(70f)))
            {
                chaptersProp.DeleteArrayElementAtIndex(i);
                serializedObject.ApplyModifiedProperties();
                SyncFoldoutSizes();
                return;
            }

            EditorGUILayout.EndHorizontal();
            if (!chapterFoldouts[i] || chapter == null)
            {
                continue;
            }

            SerializedObject chapterObject = new SerializedObject(chapter);
            SerializedProperty nameProp = chapterObject.FindProperty("chapterName");
            SerializedProperty bgmProp = chapterObject.FindProperty("bgm");
            SerializedProperty daysProp = chapterObject.FindProperty("days");

            nameProp.stringValue = EditorGUILayout.TextField("章节名", nameProp.stringValue);
            EditorGUILayout.PropertyField(bgmProp, new GUIContent("章节 BGM（覆盖营业期默认曲）"));

            EditorGUILayout.Space(2);
            if (GUILayout.Button("+ 新增一天", GUILayout.Width(140f)))
            {
                daysProp.InsertArrayElementAtIndex(daysProp.arraySize);
                SerializedProperty day = daysProp.GetArrayElementAtIndex(daysProp.arraySize - 1);
                day.FindPropertyRelative("dayName").stringValue = "第 " + (daysProp.arraySize) + " 天";
                dayFoldouts[i].Add(false);
            }

            SyncDayFoldouts(i, daysProp.arraySize);

            for (int j = 0; j < daysProp.arraySize; j++)
            {
                SerializedProperty dayProp = daysProp.GetArrayElementAtIndex(j);
                SerializedProperty dayNameProp = dayProp.FindPropertyRelative("dayName");
                SerializedProperty appearancesProp = dayProp.FindPropertyRelative("appearances");

                EditorGUILayout.BeginHorizontal();
                dayFoldouts[i][j] = EditorGUILayout.Foldout(dayFoldouts[i][j],
                    "  " + (string.IsNullOrEmpty(dayNameProp.stringValue) ? "第 " + (j + 1) + " 天" : dayNameProp.stringValue)
                    + "（" + appearancesProp.arraySize + " 位客人）", true);
                if (GUILayout.Button("－", GUILayout.Width(28f)))
                {
                    daysProp.DeleteArrayElementAtIndex(j);
                    chapterObject.ApplyModifiedProperties();
                    dayFoldouts[i].RemoveAt(j);
                    return;
                }

                EditorGUILayout.EndHorizontal();
                if (!dayFoldouts[i][j])
                {
                    continue;
                }

                EditorGUI.indentLevel++;
                dayNameProp.stringValue = EditorGUILayout.TextField("天名（标题栏显示）", dayNameProp.stringValue);

                EditorGUILayout.Space(2);
                if (GUILayout.Button("+ 新增出场", GUILayout.Width(140f)))
                {
                    appearancesProp.InsertArrayElementAtIndex(appearancesProp.arraySize);
                    SerializedProperty appearance = appearancesProp.GetArrayElementAtIndex(appearancesProp.arraySize - 1);
                    appearance.FindPropertyRelative("sugarLevel").intValue = 1;
                    appearance.FindPropertyRelative("iceLevel").intValue = 1;
                }

                for (int k = 0; k < appearancesProp.arraySize; k++)
                {
                    if (DrawAppearance(appearancesProp.GetArrayElementAtIndex(k), k, appearancesProp))
                    {
                        // 删除后立即应用并重建折叠状态，避免后续绘制访问越界
                        chapterObject.ApplyModifiedProperties();
                        return;
                    }
                }

                EditorGUI.indentLevel--;
                chapterObject.ApplyModifiedProperties();
            }

            chapterObject.ApplyModifiedProperties();
        }

        serializedObject.ApplyModifiedProperties();
    }

    /// <summary>绘制一条出场；返回 true 表示该条已被删除（调用方需立即 Apply 并退出本轮绘制）。</summary>
    private bool DrawAppearance(SerializedProperty appearance, int index, SerializedProperty listProp)
    {
        SerializedProperty customerProp = appearance.FindPropertyRelative("customer");
        MilkTeaCustomer customer = customerProp.objectReferenceValue as MilkTeaCustomer;
        string customerLabel = customer != null && !string.IsNullOrEmpty(customer.customerName)
            ? customer.customerName
            : "未填";

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("出场 " + (index + 1) + " ｜ " + customerLabel, GUILayout.Width(150f));

        customerProp.objectReferenceValue = (MilkTeaCustomer)EditorGUILayout.ObjectField(
            customer, typeof(MilkTeaCustomer), false);

        bool deleted = GUILayout.Button("－", GUILayout.Width(28f));
        if (deleted)
        {
            listProp.DeleteArrayElementAtIndex(index);
        }

        EditorGUILayout.EndHorizontal();
        if (deleted)
        {
            return true;
        }

        EditorGUILayout.BeginVertical(GUI.skin.box);
        EditorGUILayout.PropertyField(appearance.FindPropertyRelative("orderOverride"),
            new GUIContent("本场点单（剧情覆盖）"));
        EditorGUILayout.PropertyField(appearance.FindPropertyRelative("sugarLevel"), new GUIContent("糖度"));
        EditorGUILayout.PropertyField(appearance.FindPropertyRelative("iceLevel"), new GUIContent("冰度"));
        EditorGUILayout.PropertyField(appearance.FindPropertyRelative("openingOverride"), new GUIContent("本场开场对话"));
        EditorGUILayout.PropertyField(appearance.FindPropertyRelative("servingOverride"), new GUIContent("本场满意对话"));
        EditorGUILayout.PropertyField(appearance.FindPropertyRelative("servingBadOverride"), new GUIContent("本场失望对话"));
        EditorGUILayout.EndVertical();
        return false;
    }

    // ======== 测试跳进度 ========

    private void DrawJumpSection()
    {
        EditorGUILayout.LabelField("测试跳进度", EditorStyles.boldLabel);
        if (syncJumpFromArt)
        {
            jumpChapter = Mathf.Max(0, art.startChapterIndex);
            jumpDay = Mathf.Max(0, art.startDayIndex);
            syncJumpFromArt = false;
        }

        int chapterCount = art.chapters != null ? art.chapters.Count : 0;
        if (chapterCount == 0)
        {
            EditorGUILayout.HelpBox("还没有章节，跳进度不可用。", MessageType.None);
            return;
        }

        jumpChapter = Mathf.Clamp(jumpChapter, 0, chapterCount - 1);
        MilkTeaChapter chapter = art.chapters[jumpChapter];
        int dayCount = chapter != null && chapter.days != null ? chapter.days.Count : 0;
        string chapterTitle = chapter != null && !string.IsNullOrEmpty(chapter.chapterName) ? chapter.chapterName : "章节 " + (jumpChapter + 1);

        jumpChapter = EditorGUILayout.IntSlider("章（" + chapterTitle + "）", jumpChapter, 0, chapterCount - 1);
        if (dayCount > 0)
        {
            jumpDay = EditorGUILayout.IntSlider("天", jumpDay, 0, dayCount - 1);
        }
        else
        {
            jumpDay = 0;
        }

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("设为游戏起始进度（Play 后从这天开始）"))
        {
            art.startChapterIndex = jumpChapter;
            art.startDayIndex = jumpDay;
            EditorUtility.SetDirty(art);
            AssetDatabase.SaveAssets();
            Debug.Log("[奶茶店 Demo] 起始进度已设为：" + chapterTitle + " · 第 " + (jumpDay + 1) + " 天。");
        }

        if (GUILayout.Button("清零（从第一章第 1 天开始）"))
        {
            art.startChapterIndex = 0;
            art.startDayIndex = 0;
            jumpChapter = 0;
            jumpDay = 0;
            EditorUtility.SetDirty(art);
            AssetDatabase.SaveAssets();
        }

        EditorGUILayout.EndHorizontal();
        EditorGUILayout.HelpBox(
            "Play 中还有快捷键：F9 = 直接结束当天进结算；F10 = 推进到下一天。\n" +
            "读档按钮会回到自动存档（每天进入休息界面时写入）的进度。", MessageType.None);
    }

    // ======== 资产文件名对齐 ========

    private void DrawRenameSection()
    {
        EditorGUILayout.LabelField("资产文件名对齐", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Inspector 的对象槽只显示资产文件名。此操作把客人/角色资产文件改成与其内部名称一致\n" +
            "（如 Customer → 小雪），美术库列表和日程条目立刻可读。", MessageType.None);
        if (GUILayout.Button("统一客人/角色资产文件名", GUILayout.Width(240f)))
        {
            int renamed = 0;
            renamed += RenameAssetsByInternalName(art.customers, c => c.customerName);
            renamed += RenameAssetsByInternalName(art.characters, c => c.characterName);
            AssetDatabase.SaveAssets();
            Debug.Log("[奶茶店 Demo] 已重命名 " + renamed + " 个资产文件。");
        }
    }

    private static int RenameAssetsByInternalName<T>(IEnumerable<T> assets, System.Func<T, string> nameGetter) where T : Object
    {
        int renamed = 0;
        if (assets == null)
        {
            return 0;
        }

        foreach (T asset in assets)
        {
            if (asset == null)
            {
                continue;
            }

            string target = nameGetter(asset);
            if (string.IsNullOrEmpty(target) || asset.name == target)
            {
                continue;
            }

            string path = AssetDatabase.GetAssetPath(asset);
            if (string.IsNullOrEmpty(path))
            {
                continue;
            }

            string error = AssetDatabase.RenameAsset(path, target);
            if (string.IsNullOrEmpty(error))
            {
                renamed++;
            }
            else
            {
                Debug.LogWarning("[奶茶店 Demo] 重命名失败：" + path + " → " + target + "（" + error + "）");
            }
        }

        return renamed;
    }

    // ======== 工具 ========

    private static T CreateAsset<T>(string folder, string baseName, string fallbackInternalName) where T : ScriptableObject
    {
        if (!AssetDatabase.IsValidFolder(folder))
        {
            string parent = System.IO.Path.GetDirectoryName(folder);
            string leaf = System.IO.Path.GetFileName(folder);
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
            {
                AssetDatabase.CreateFolder("Assets", parent.Substring("Assets/".Length));
            }

            AssetDatabase.CreateFolder(string.IsNullOrEmpty(parent) ? "Assets" : parent, leaf);
        }

        string path = AssetDatabase.GenerateUniqueAssetPath(folder + "/" + baseName + ".asset");
        T asset = ScriptableObject.CreateInstance<T>();
        AssetDatabase.CreateAsset(asset, path);

        // 顺手把文件名对齐内部名称
        if (asset is MilkTeaChapter chapterAsset)
        {
            chapterAsset.chapterName = fallbackInternalName;
            AssetDatabase.RenameAsset(path, fallbackInternalName);
        }

        AssetDatabase.SaveAssets();
        EditorGUIUtility.PingObject(asset);
        return asset;
    }

    private void SyncFoldoutSizes()
    {
        int count = art.chapters != null ? art.chapters.Count : 0;
        while (chapterFoldouts.Count < count)
        {
            chapterFoldouts.Add(false);
            dayFoldouts.Add(new List<bool>());
        }

        while (chapterFoldouts.Count > count)
        {
            chapterFoldouts.RemoveAt(chapterFoldouts.Count - 1);
            dayFoldouts.RemoveAt(dayFoldouts.Count - 1);
        }
    }

    private void SyncDayFoldouts(int chapterIndex, int dayCount)
    {
        List<bool> foldouts = dayFoldouts[chapterIndex];
        while (foldouts.Count < dayCount)
        {
            foldouts.Add(false);
        }

        while (foldouts.Count > dayCount)
        {
            foldouts.RemoveAt(foldouts.Count - 1);
        }
    }
}
#endif
