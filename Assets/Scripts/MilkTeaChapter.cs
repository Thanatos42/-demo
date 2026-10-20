using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 一个章节的剧情日程：章节下分天，每天是一个出场表（依次出场的客人 + 本场点单/对话覆盖）。
/// 把章节资产拖进 <see cref="MilkTeaArtLibrary"/> 的 chapters 列表后，游戏按
/// "章 → 天 → 出场条目" 的顺序推进；条目里填了的项覆盖客人资产，没填的回落客人资产。
/// 点单随剧情走：同一角色前期嗜甜、后期戒糖，只需在对应天的条目里填 orderOverride。
/// 可在 Unity 中右键 Create → 奶茶店 Demo → 章节 创建。
/// </summary>
[CreateAssetMenu(fileName = "Chapter", menuName = "奶茶店 Demo/章节", order = 5)]
public sealed class MilkTeaChapter : ScriptableObject
{
    /// <summary>一条出场：本场的客人以及该场专属的点单与对话（留空回落客人资产配置）。</summary>
    [Serializable]
    public sealed class Appearance
    {
        [Tooltip("本场出场的客人（点单与对话未覆盖时用它的配置）")]
        public MilkTeaCustomer customer;

        [Header("本场点单覆盖（可选，剧情指定）")]
        [Tooltip("本场指定的饮品；留空则用客人资产的点单池/固定点单")]
        public MilkTeaRecipe orderOverride;

        [Tooltip("覆盖点单时的糖度：0 无糖 / 1 少糖 / 2 多糖")]
        [Range(0, 2)]
        public int sugarLevel = 1;

        [Tooltip("覆盖点单时的冰度：0 去冰 / 1 少冰 / 2 多冰")]
        [Range(0, 2)]
        public int iceLevel = 1;

        [Header("本场对话覆盖（可选）")]
        [Tooltip("本场进店对话；留空用客人资产里的 openingDialogue")]
        public MilkTeaDialogue openingOverride;

        [Tooltip("本场做对交付对话；留空用客人资产里的 servingDialogue")]
        public MilkTeaDialogue servingOverride;

        [Tooltip("本场做错交付对话；留空用客人资产里的 servingBadDialogue")]
        public MilkTeaDialogue servingBadOverride;
    }

    /// <summary>一天：按顺序出场的客人列表。</summary>
    [Serializable]
    public sealed class Day
    {
        [Tooltip("这一天在标题栏显示的名字")]
        public string dayName = "第 1 天";

        [Tooltip("当天的出场表：按顺序依次出场，通常 3~4 位")]
        public List<Appearance> appearances = new List<Appearance>();
    }

    [Header("章节")]
    [Tooltip("章节名（编辑器窗口与跳进度显示用）")]
    public string chapterName = "第一章";

    [Tooltip("本章的天列表，按顺序推进")]
    public List<Day> days = new List<Day>();

    [Header("章节背景音乐（可选）")]
    [Tooltip("本章营业期间（对话/调配/结算/休息）循环的 BGM；留空用美术库各界面默认曲")]
    public AudioClip bgm;
}
