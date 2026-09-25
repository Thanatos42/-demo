using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 一段对话数据。按顺序播放每一行，点按钮进入下一行，最后一行点按钮触发后续流程。
/// 台词与说话人支持占位符：{customer}=顾客名、{drink}=饮品名、{sugar}=糖度、{ice}=冰度。
/// 可在 Unity 中右键 Create → 奶茶店 Demo → 对话 创建并在 Inspector 里编辑。
/// </summary>
[CreateAssetMenu(fileName = "Dialogue", menuName = "奶茶店 Demo/对话", order = 2)]
public sealed class MilkTeaDialogue : ScriptableObject
{
    [Serializable]
    public sealed class Line
    {
        [Tooltip("说话人，可用 {customer} 表示当前顾客名")]
        public string speaker = "顾客";

        [TextArea(2, 4)]
        [Tooltip("台词，可用 {drink}/{sugar}/{ice}/{customer} 占位符")]
        public string text = string.Empty;

        [Tooltip("推进到下一句的按钮文字，留空默认“继续”")]
        public string buttonLabel = "继续";
    }

    public List<Line> lines = new List<Line>();
}
