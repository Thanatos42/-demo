using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 休息界面手机的内容库：朋友圈动态与 CG 图鉴。
/// 朋友圈：每条动态带"解锁锚点"（第几章·第几天），进度到达后自动出现在手机里；
/// CG：平时隐藏，由对话行的 ShowCg 演出事件按 targetName（即 id）解锁，解锁后进相册。
/// 资产拖进 <see cref="MilkTeaArtLibrary"/> 的 socialContent 槽生效。
/// 可在 Unity 中右键 Create → 奶茶店 Demo → 社交内容 创建。
/// </summary>
[CreateAssetMenu(fileName = "SocialContent", menuName = "奶茶店 Demo/社交内容", order = 6)]
public sealed class MilkTeaSocialContent : ScriptableObject
{
    /// <summary>一条朋友圈动态：作者 + 文案 + 可选配图 + 解锁锚点。</summary>
    [Serializable]
    public sealed class Post
    {
        [Tooltip("动态 ID（存档用，建议唯一，如 xue_day1")]
        public string id = "post";

        [Tooltip("发动态的角色（取其名字与立绘做头像）")]
        public MilkTeaCharacter author;

        [TextArea(2, 4)]
        [Tooltip("动态文案；支持 {customer}/{drink} 等占位符（休息界面按当前进度填充）")]
        public string text = "今天奶茶不错！";

        [Tooltip("配图（可选）")]
        public Sprite image;

        [Header("解锁锚点：进度到达该章·天后显示")]
        [Tooltip("解锁所需章节索引（美术库 chapters 下标，0 = 第一章）")]
        public int unlockChapterIndex;

        [Tooltip("解锁所需天索引（章节 days 下标，0 = 该章第 1 天）")]
        public int unlockDayIndex;
    }

    /// <summary>一张 CG：由对话行 ShowCg 事件按 id 解锁，解锁后在手机相册查看。</summary>
    [Serializable]
    public sealed class Cg
    {
        [Tooltip("CG 的 ID；对话行 ShowCg 事件的 targetName 填这个名即解锁并展示本图")]
        public string id = "cg";

        [Tooltip("CG 插图")]
        public Sprite image;

        [TextArea(1, 3)]
        [Tooltip("图注（相册里显示在图下方，可选）")]
        public string caption = string.Empty;
    }

    [Header("朋友圈动态")]
    public List<Post> posts = new List<Post>();

    [Header("CG 图鉴")]
    public List<Cg> cgs = new List<Cg>();

    /// <summary>按 ID 找 CG；找不到返回 null。</summary>
    public Cg FindCg(string cgId)
    {
        if (string.IsNullOrEmpty(cgId) || cgs == null)
        {
            return null;
        }

        for (int i = 0; i < cgs.Count; i++)
        {
            Cg cg = cgs[i];
            if (cg != null && cg.id == cgId)
            {
                return cg;
            }
        }

        return null;
    }
}
