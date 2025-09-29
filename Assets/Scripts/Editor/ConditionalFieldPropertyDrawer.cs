using UnityEngine;
using UnityEditor;

/// <summary>
/// 条件字段属性绘制器
/// 根据条件字段的值来控制属性的显示
/// </summary>
[CustomPropertyDrawer(typeof(ConditionalFieldAttribute))]
public class ConditionalFieldPropertyDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        ConditionalFieldAttribute conditionalField = attribute as ConditionalFieldAttribute;
        
        // 检查条件是否满足
        bool shouldShow = ShouldShowProperty(property, conditionalField);
        
        if (shouldShow)
        {
            EditorGUI.PropertyField(position, property, label, true);
        }
    }
    
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        ConditionalFieldAttribute conditionalField = attribute as ConditionalFieldAttribute;
        
        // 检查条件是否满足
        bool shouldShow = ShouldShowProperty(property, conditionalField);
        
        if (shouldShow)
        {
            return EditorGUI.GetPropertyHeight(property, label, true);
        }
        else
        {
            return 0f; // 不显示时高度为0
        }
    }
    
    /// <summary>
    /// 检查是否应该显示属性
    /// </summary>
    private bool ShouldShowProperty(SerializedProperty property, ConditionalFieldAttribute conditionalField)
    {
        // 获取父对象
        SerializedProperty parentProperty = property.serializedObject.FindProperty(property.propertyPath.Split('.')[0]);
        
        // 检查所有条件字段
        foreach (string conditionalFieldName in conditionalField.ConditionalSourceFields)
        {
            SerializedProperty conditionalProperty = property.serializedObject.FindProperty(conditionalFieldName);
            
            if (conditionalProperty == null)
            {
                Debug.LogWarning($"ConditionalField: 找不到条件字段 '{conditionalFieldName}'");
                return true; // 找不到字段时默认显示
            }
            
            // 检查条件是否满足
            bool conditionMet = CheckCondition(conditionalProperty, conditionalField.CompareValues);
            
            if (conditionalField.Inverse)
            {
                conditionMet = !conditionMet;
            }
            
            if (!conditionMet)
            {
                return false; // 有一个条件不满足就不显示
            }
        }
        
        return true; // 所有条件都满足
    }
    
    /// <summary>
    /// 检查条件是否满足
    /// </summary>
    private bool CheckCondition(SerializedProperty conditionalProperty, object[] compareValues)
    {
        if (compareValues == null || compareValues.Length == 0)
        {
            return true; // 没有比较值，默认满足
        }
        
        // 检查是否与任何比较值匹配
        foreach (object compareValue in compareValues)
        {
            if (ComparePropertyValue(conditionalProperty, compareValue))
            {
                return true;
            }
        }
        
        return false;
    }
    
    /// <summary>
    /// 比较属性值与比较值
    /// </summary>
    private bool ComparePropertyValue(SerializedProperty property, object compareValue)
    {
        switch (property.propertyType)
        {
            case SerializedPropertyType.Boolean:
                return property.boolValue == (bool)compareValue;
                
            case SerializedPropertyType.Integer:
                return property.intValue == (int)compareValue;
                
            case SerializedPropertyType.Float:
                return Mathf.Approximately(property.floatValue, (float)compareValue);
                
            case SerializedPropertyType.String:
                return property.stringValue == (string)compareValue;
                
            case SerializedPropertyType.Enum:
                return property.enumValueIndex == (int)compareValue;
                
            default:
                Debug.LogWarning($"ConditionalField: 不支持的条件字段类型 {property.propertyType}");
                return true; // 不支持的类型默认满足
        }
    }
}
