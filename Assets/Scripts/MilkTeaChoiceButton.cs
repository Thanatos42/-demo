using UnityEngine;

/// <summary>
/// 原料选择按钮标记：记录该按钮属于哪一类原料以及对应的选项名称。
/// 由建场工具在编辑器里挂到每个原料按钮上，运行时控制器据此自动接线。
/// </summary>
public sealed class MilkTeaChoiceButton : MonoBehaviour
{
    public IngredientCategory category;
    public string option;
}
