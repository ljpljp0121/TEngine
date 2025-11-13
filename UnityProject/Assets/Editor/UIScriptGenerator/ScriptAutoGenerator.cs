using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TEngine.Editor.UI
{
    public partial class UIScriptGenerator
    {
        private static string[] VARIABLE_NAME_REGEX;
        private static TextEditor m_textEditor = new TextEditor();
        private static void CheckVariableNames()
        {
            var cnt = (int)UIFieldCodeStyle.Max;
            VARIABLE_NAME_REGEX = new string[cnt];
            for (int i = 0; i < cnt; i++)
            {
                VARIABLE_NAME_REGEX[i] = GetPrefixNameByCodeStyle((UIFieldCodeStyle)i);
            }
        }

        [MenuItem("GameObject/ScriptGenerator/UIPropertyBindComponent", priority = 84)]
        public static void UIPropertyBindComponent()
        {
            GenerateCSharpScript(false);
        }

        [MenuItem("GameObject/ScriptGenerator/UIPropertyBindComponent", true)]
        public static bool ValidateUIPropertyBindComponent()
        {
            return ScriptGeneratorSetting.Instance.UseBindComponent;
        }

        [MenuItem("GameObject/ScriptGenerator/UIPropertyBindComponent - UniTask", priority = 85)]
        public static void UIPropertyBindComponentUniTask()
        {
            GenerateCSharpScript(false, true);
        }

        [MenuItem("GameObject/ScriptGenerator/UIPropertyBindComponent - UniTask", true)]
        public static bool ValidateUIPropertyBindComponentUniTask()
        {
            return ScriptGeneratorSetting.Instance.UseBindComponent;
        }

        [MenuItem("GameObject/ScriptGenerator/UIPropertyAndListenerBindComponent", priority = 86)]
        public static void UIPropertyAndListenerBindComponent()
        {
            GenerateCSharpScript(true);
        }

        [MenuItem("GameObject/ScriptGenerator/UIPropertyAndListenerBindComponent", true)]
        public static bool ValidateUIPropertyAndListenerBindComponent()
        {
            return ScriptGeneratorSetting.Instance.UseBindComponent;
        }

        [MenuItem("GameObject/ScriptGenerator/UIPropertyAndListenerBindComponentUniTask - UniTask", priority = 87)]
        public static void UIPropertyAndListenerBindComponentUniTask()
        {
            GenerateCSharpScript(true, true);
        }

        [MenuItem("GameObject/ScriptGenerator/UIPropertyAndListenerBindComponentUniTask - UniTask", true)]
        public static bool ValidateUIPropertyAndListenerBindComponentUniTask()
        {
            return ScriptGeneratorSetting.Instance.UseBindComponent;
        }

        private static bool GenerateCSharpScript(bool includeListener, bool isUniTask = false,
            bool isAutoGenerate = false, string savePath = null, bool isAutoDiff = true, bool m_isWidget = false)
        {
            var root = Selection.activeTransform;

            if (root == null)
            {
                Debug.LogWarning("请选中一个物体再进行脚本生成操作");
                return false;
            }

            CheckVariableNames();
            StringBuilder strVar = new StringBuilder();
            StringBuilder strBind = new StringBuilder();
            StringBuilder strOnCreate = new StringBuilder();
            StringBuilder strCallback = new StringBuilder();

            var windowComSufName = ScriptGeneratorSetting.Instance.WindowComponentSuffixName;
            var widgetComSufName = ScriptGeneratorSetting.Instance.WidgetComponentSuffixName;

            var widgetPrefix = GetUIWidgetName();
            var rootName = $"{root.name}{windowComSufName}";
            string fileName = $"{root.name}.cs";
            string uiType = "UIWindow";
            if (root.name.StartsWith(widgetPrefix))
            {
                uiType = "UIWidget";
                fileName = $"{root.name.Replace(GetUIWidgetName(), string.Empty)}.cs";
                rootName = $"{root.name.Replace(GetUIWidgetName(), string.Empty)}{widgetComSufName}";
            }

            if (!isAutoDiff)
            {
                if (m_isWidget)
                {
                    uiType = "UIWidget";
                    rootName = $"{root.name}{widgetComSufName}";
                }
                else
                {
                    uiType = "UIWindow";
                    rootName = $"{root.name}{windowComSufName}";
                }
                fileName = $"{root.name}.cs";
            }

            strVar.AppendLine($"\t\tprivate {rootName} m_bindComponent;");

            strBind.AppendLine($"\t\t\tm_bindComponent = gameObject.GetComponent<{rootName}>();");
            AutoErgodic(root, root, ref strVar, ref strBind, ref strOnCreate, ref strCallback, isUniTask);
            StringBuilder strFile = new StringBuilder();

            if (includeListener)
            {
#if ENABLE_TEXTMESHPRO
                    strFile.AppendLine("using TMPro;");
#endif
                if (isUniTask)
                {
                    strFile.AppendLine("using Cysharp.Threading.Tasks;");
                }

                strFile.AppendLine("using UnityEngine;");
                strFile.AppendLine("using UnityEngine.UI;");
                strFile.AppendLine("using TEngine;");
                strFile.AppendLine();
                strFile.AppendLine($"namespace {ScriptGeneratorSetting.GetUINameSpace()}");
                strFile.AppendLine("{");
                {
                    if (isAutoDiff)
                    {
                        if (root.name.StartsWith(widgetPrefix))
                        {
                            strFile.AppendLine($"\tpublic class {fileName.Replace(".cs", "")} : {uiType}");
                        }
                        else
                        {
                            strFile.AppendLine($"\t[Window(UILayer.UI, location : \"{fileName.Replace(".cs", "")}\")]");
                            strFile.AppendLine($"\tpublic class {fileName.Replace(".cs", "")} : {uiType}");
                        }
                    }
                    else
                    {
                        if (!m_isWidget)
                        {
                            strFile.AppendLine($"\t[Window(UILayer.UI, location : \"{fileName.Replace(".cs", "")}\")]");
                        }
                        strFile.AppendLine($"\tpublic class {fileName.Replace(".cs", "")} : {uiType}");
                    }


                    strFile.AppendLine("\t{");
                }
            }

            // 脚本工具生成的代码
            strFile.AppendLine($"\t\t#region 脚本工具生成的代码");
            strFile.AppendLine();
            strFile.AppendLine(strVar.ToString());
            strFile.AppendLine("\t\tprotected override void ScriptGenerator()");
            strFile.AppendLine("\t\t{");
            {
                strFile.Append(strBind.ToString());
                strFile.Append(strOnCreate.ToString());
            }
            strFile.AppendLine("\t\t}");
            strFile.AppendLine();
            strFile.Append($"\t\t#endregion");
            strFile.AppendLine();

            if (includeListener)
            {
                strFile.AppendLine();
                strFile.AppendLine("\t\t#region 事件");
                strFile.AppendLine();
                strFile.Append(strCallback.ToString());
                strFile.AppendLine($"\t\t#endregion");
                strFile.AppendLine("\t}");
                strFile.AppendLine("}");
            }

            m_textEditor.Delete();
            m_textEditor.text = strFile.ToString();
            m_textEditor.SelectAll();
            m_textEditor.Copy();

            if (isAutoGenerate)
            {
                string path = savePath?.Replace("\\", "/");

                bool isOk = EditorUtility.DisplayDialog("生成脚本确认", $"将在目录: {path} 生成脚本文件: {fileName}", "确认", "取消");

                if (!isOk || string.IsNullOrEmpty(path))
                {
                    return false;
                }

                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }

                var filePath = Path.Combine(path, fileName).Replace("\\", "/");

                if (File.Exists(filePath))
                {
                    bool isOverride = EditorUtility.DisplayDialog("警告", $"目录: {path} 已存在脚本文件: {fileName} 是否覆盖生成？", "确认", "取消");

                    if (isOverride)
                    {
                        File.Delete(filePath);
                        AssetDatabase.Refresh();
                    }
                    else
                    {
                        return false;
                    }
                }

                File.WriteAllText(filePath, strFile.ToString(), Encoding.UTF8);
                AssetDatabase.Refresh();
            }
            else
            {
                Debug.Log($"<color=#1E90FF>脚本已生成到剪贴板，请自行Ctl+V粘贴</color>");
            }

            return true;
        }

        public static void AutoErgodic(Transform root, Transform transform, ref StringBuilder strVar,
            ref StringBuilder strBind, ref StringBuilder strOnCreate, ref StringBuilder strCallback, bool isUniTask)
        {
            for (int i = 0; i < transform.childCount; i++)
            {
                Transform child = transform.GetChild(i);
                WriteAutoScript(root, child, ref strVar, ref strBind, ref strOnCreate, ref strCallback, isUniTask);

                // 跳过 "m_item"
                if (child.name.StartsWith(GetUIWidgetName()))
                {
                    continue;
                }
                AutoErgodic(root, child, ref strVar, ref strBind, ref strOnCreate, ref strCallback, isUniTask);
            }
        }

        private static void WriteAutoScript(Transform root, Transform child, ref StringBuilder strVar,
            ref StringBuilder strBind, ref StringBuilder strOnCreate, ref StringBuilder strCallback, bool isUniTask)
        {
            string varName = child.name;
            // 查找相关的规则定义
            var rule = ScriptGeneratorSetting.GetScriptGenerateRule()
                .Find(r => varName.StartsWith(r.uiElementRegex));

            if (rule == null)
            {
                return;
            }
            var componentName = rule.componentName.ToString();
            if (string.IsNullOrEmpty(componentName))
            {
                return;
            }
            varName = GetVariableName(varName);
            if (string.IsNullOrEmpty(varName))
            {
                return;
            }
            // string varPath = GetRelativePath(child, root);
            strVar.AppendLine($"\t\tprivate {componentName} {varName}{(ScriptGeneratorSetting.Instance.NullableEnable?" = null!;":";")}");
            strBind.AppendLine($"\t\t\t{varName} = m_bindComponent.{varName};");

            switch (rule.componentName)
            {
                case UIComponentName.Button:
                    var btnFuncName = GetButtonFuncName(varName);
                    if (isUniTask)
                    {
                        strOnCreate.AppendLine($"\t\t\t{varName}.onClick.AddListener(UniTask.UnityAction({btnFuncName}));");
                        strCallback.AppendLine($"\t\tprivate async UniTaskVoid {btnFuncName}()");
                        strCallback.AppendLine("\t\t{");
                        strCallback.AppendLine("\t\t\tawait UniTask.Yield();");
                        strCallback.AppendLine("\t\t}");
                    }
                    else
                    {
                        strOnCreate.AppendLine($"\t\t\t{varName}.onClick.AddListener({btnFuncName});");
                        strCallback.AppendLine($"\t\tprivate void {btnFuncName}()");
                        strCallback.AppendLine("\t\t{");
                        strCallback.AppendLine("\t\t}");
                    }
                    strCallback.AppendLine();
                    break;
                case UIComponentName.Toggle:
                    var toggleFuncName = GetToggleFuncName(varName);
                    strOnCreate.AppendLine($"\t\t\t{varName}.onValueChanged.AddListener({toggleFuncName});");
                    strCallback.AppendLine($"\t\tprivate void {toggleFuncName}(bool isOn)");
                    strCallback.AppendLine("\t\t{");
                    strCallback.AppendLine("\t\t}");
                    strCallback.AppendLine();
                    break;
                case UIComponentName.Slider:
                    var sliderFuncName = GetSliderFuncName(varName);
                    strOnCreate.AppendLine($"\t\t\t{varName}.onValueChanged.AddListener({sliderFuncName});");
                    strCallback.AppendLine($"\t\tprivate void {sliderFuncName}(float value)");
                    strCallback.AppendLine("\t\t{");
                    strCallback.AppendLine("\t\t}");
                    strCallback.AppendLine();
                    break;
            }
        }

        private static bool GenerateUIComponentScript(string savePath, bool isAutoDiff = true, bool m_isWidget = false)
        {
            var root = Selection.activeTransform;

            if (root == null)
            {
                Debug.LogWarning("请选中一个物体再进行脚本生成操作");
                return false;
            }

            CheckVariableNames();

            var windowComSufName = ScriptGeneratorSetting.Instance.WindowComponentSuffixName;
            var widgetComSufName = ScriptGeneratorSetting.Instance.WidgetComponentSuffixName;
            string generatorClassSuffixName = string.Empty;
            string fileName = null;
            var widgetPrefix = GetUIWidgetName();
            var rootName = $"{root.name}{windowComSufName}";

            if (root.name.StartsWith(widgetPrefix))
            {
                rootName = $"{root.name.Replace(GetUIWidgetName(), string.Empty)}{widgetComSufName}";
                generatorClassSuffixName = string.Empty;
            }

            if (!isAutoDiff)
            {
                if (m_isWidget)
                {
                    rootName = $"{root.name}{widgetComSufName}";
                    generatorClassSuffixName = widgetComSufName;
                }
                else
                {
                    rootName = $"{root.name}{windowComSufName}";
                    generatorClassSuffixName = windowComSufName;
                }
            }
            fileName = $"{rootName}.cs";

            StringBuilder strVar = new StringBuilder();
            ErgodicUIComponent(root, root, ref strVar);
            StringBuilder strFile = new StringBuilder();
            strFile.AppendLine("//----------------------------------------------------------");
            strFile.AppendLine("// <auto-generated>");
            strFile.AppendLine("// -This code was generated.");
            strFile.AppendLine("// -Changes to this file may cause incorrect behavior.");
            strFile.AppendLine("// -will be lost if the code is regenerated.");
            strFile.AppendLine("// <auto-generated/>");
            strFile.AppendLine("//----------------------------------------------------------");

#if ENABLE_TEXTMESHPRO
            strFile.AppendLine("using TMPro;");
#endif
            strFile.AppendLine("using UnityEngine;");
            strFile.AppendLine("using UnityEngine.UI;");
            strFile.AppendLine("using TEngine;");
            strFile.AppendLine();
            strFile.AppendLine($"namespace {ScriptGeneratorSetting.GetUINameSpace()}");
            strFile.AppendLine("{");
            {
                strFile.AppendLine($"\t[DisallowMultipleComponent]");
                if (root.name.StartsWith(widgetPrefix))
                {
                    strFile.AppendLine($"\tpublic class {rootName} : MonoBehaviour");
                }
                else
                {
                    strFile.AppendLine($"\tpublic class {rootName} : MonoBehaviour");
                }
                strFile.AppendLine("\t{");
            }

            // 脚本工具生成的代码
            strFile.Append(strVar.ToString());
            strFile.AppendLine("\t}");
            strFile.AppendLine("}");

            string path = savePath.Replace("\\", "/");

            bool isOk = EditorUtility.DisplayDialog("生成脚本确认", $"将在目录: {path} 生成脚本文件: {fileName}", "确认", "取消");
            if (!isOk)
            {
                return false;
            }

            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            var filePath = Path.Combine(path, fileName).Replace("\\", "/");
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                AssetDatabase.Refresh();
            }
            File.WriteAllText(filePath, strFile.ToString(), Encoding.UTF8);
            EditorPrefs.SetString("GeneratorClassName", fileName.Replace(".cs", string.Empty));
            EditorPrefs.SetString("GeneratorClassSuffixName", generatorClassSuffixName);
            AssetDatabase.Refresh();
            return true;
            // Debug.Log($"<color=#1E90FF>脚本已生成到剪贴板，请自行Ctl+V粘贴</color>");
        }

        public static void ErgodicUIComponent(Transform root, Transform transform, ref StringBuilder strVar)
        {
            for (int i = 0; i < transform.childCount; i++)
            {
                Transform child = transform.GetChild(i);
                WriteScriptUIComponent(root, child, ref strVar);

                // 跳过 "m_item"
                if (child.name.StartsWith(GetUIWidgetName()))
                {
                    continue;
                }
                ErgodicUIComponent(root, child, ref strVar);
            }
        }

        private static void WriteScriptUIComponent(Transform root, Transform child, ref StringBuilder strVar)
        {
            string varName = child.name;
            // 查找相关的规则定义
            var rule = ScriptGeneratorSetting.GetScriptGenerateRule()
                .Find(r => varName.StartsWith(r.uiElementRegex));

            if (rule == null)
            {
                return;
            }
            var componentName = rule.componentName.ToString();
            if (string.IsNullOrEmpty(componentName))
            {
                return;
            }
            varName = GetVariableName(varName);
            if (string.IsNullOrEmpty(varName))
            {
                return;
            }
            strVar.AppendLine($"\t\tpublic {componentName} {varName};");
        }

        /// <summary>
        /// 编译完成系统自动调用
        /// </summary>
        [UnityEditor.Callbacks.DidReloadScripts]
        private static void AddComponent2Window()
        {
            var generatorClassName = EditorPrefs.GetString("GeneratorClassName");
            var generatorClassSuffixName = EditorPrefs.GetString("GeneratorClassSuffixName");
            if (string.IsNullOrEmpty(generatorClassName))
            {
                EditorPrefs.DeleteKey("GeneratorClassName");
                EditorPrefs.DeleteKey("GeneratorClassSuffixName");
                return;
            }

            //1.通过反射的方式，从程序集中找到这个脚本，把它挂在到当前的物体上
            //获取所有的程序集
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            //找到Csharp程序集
            var cSharpAssembly = assemblies.First(assembly => assembly.GetName().Name == ScriptGeneratorSetting.GetUINameSpace());
            //获取类所在的程序集路径
            string relClassName = ScriptGeneratorSetting.GetUINameSpace() + "." + generatorClassName;
            Type type = cSharpAssembly.GetType(relClassName);

            if (type == null)
            {
                EditorPrefs.DeleteKey("GeneratorClassName");
                EditorPrefs.DeleteKey("GeneratorClassSuffixName");
                return;
            }

            var windowComSufName = ScriptGeneratorSetting.Instance.WindowComponentSuffixName;
            var widgetComSufName = ScriptGeneratorSetting.Instance.WidgetComponentSuffixName;
            string windowObjName;
            //获取要挂载的那个物体
            if (!string.IsNullOrEmpty(generatorClassSuffixName))
            {
                windowObjName = generatorClassName.Replace(generatorClassSuffixName, "");
            }
            else
            {
                windowObjName = generatorClassName.Replace(windowComSufName, "");
                if (generatorClassName.EndsWith(widgetComSufName))
                {
                    windowObjName = generatorClassName.Replace(widgetComSufName, "");
                    windowObjName = GetPrefixName() + ScriptGeneratorSetting.GetWidgetName() + windowObjName;
                }
            }

            GameObject windowObj = FindGameObjectInAllContexts(windowObjName);

            if (windowObj == null)
            {
                Debug.LogWarning($"找不到GameObject: {windowObjName}");
                EditorPrefs.DeleteKey("GeneratorClassName");
                EditorPrefs.DeleteKey("GeneratorClassSuffixName");
                return;
            }

            //先获取现窗口上有没有挂载该数据组件，如果没挂载在进行挂载
            Component compt = windowObj.GetComponent(type);

            if (compt != null)
            {
                GameObject.DestroyImmediate(compt);
                compt = null;
            }

            if (compt == null)
            {
                compt = windowObj.AddComponent(type);
            }

            //2.通过反射的方式，遍历数据列表 找到对应的字段，赋值
            //获取对象数据列表
            //获取脚本所有字段
            FieldInfo[] fieldInfoList = type.GetFields();
            foreach (var item in fieldInfoList)
            {
                GameObject go = FindDeepChild(windowObj.transform, item.Name)?.gameObject;

                if (go == null)
                {
                    EditorPrefs.DeleteKey("GeneratorClassName");
                    EditorPrefs.DeleteKey("GeneratorClassSuffixName");
                    return;
                }
                if (string.Equals(item.FieldType.Name, "GameObject"))
                {
                    item.SetValue(compt, go);
                }
                else
                {
                    item.SetValue(compt, go.GetComponent(item.FieldType));
                }
            }

            PrefabInstanceStatus status = PrefabUtility.GetPrefabInstanceStatus(windowObj);

            if (status == PrefabInstanceStatus.Connected)
            {
                PrefabUtility.ApplyPrefabInstance(windowObj, InteractionMode.UserAction);
            }

            EditorPrefs.DeleteKey("GeneratorClassName");
            EditorPrefs.DeleteKey("GeneratorClassSuffixName");
        }

        /// <summary>
        /// 在各种上下文中查找GameObject（支持预制体模式）
        /// </summary>
        private static GameObject FindGameObjectInAllContexts(string objectName)
        {
            GameObject result = null;

            // 首先尝试在当前场景中查找
            result = GameObject.Find(objectName);
            if (result != null) return result;

            // 尝试在UIRoot/UICanvas路径下查找
            result = GameObject.Find("UIRoot/UICanvas/" + objectName);
            if (result != null) return result;

            // 处理预制体模式
            if (IsInPrefabMode())
            {
                // 在预制体模式下查找
                result = FindGameObjectInPrefabMode(objectName);
                if (result != null) return result;
            }
            return null;
        }

        /// <summary>
        /// 在预制体模式下查找GameObject
        /// </summary>
        private static GameObject FindGameObjectInPrefabMode(string objectName)
        {
            // 获取当前预制体阶段
            var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
            if (prefabStage != null)
            {
                // 在预制体根物体下查找
                GameObject prefabRoot = prefabStage.prefabContentsRoot;
                if (prefabRoot.name == objectName)
                    return prefabRoot;

                // 在预制体子物体中查找
                Transform found = FindDeepChild(prefabRoot.transform, objectName);
                if (found != null)
                    return found.gameObject;
            }

            // 检查选中的预制体实例
            var selected = Selection.activeGameObject;
            if (selected != null)
            {
                if (selected.name == objectName)
                    return selected;

                Transform found = FindDeepChild(selected.transform, objectName);
                if (found != null)
                    return found.gameObject;
            }

            return null;
        }

        /// <summary>
        /// 检查是否在预制体编辑模式
        /// </summary>
        private static bool IsInPrefabMode()
        {
            // 方法1: 检查当前选中的预制体
            var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
            if (prefabStage != null) return true;

            // 方法2: 检查选中的对象是否是预制体实例
            var selected = Selection.activeGameObject;
            if (selected != null)
            {
                PrefabInstanceStatus status = PrefabUtility.GetPrefabInstanceStatus(selected);
                PrefabAssetType assetType = PrefabUtility.GetPrefabAssetType(selected);
                return assetType != PrefabAssetType.NotAPrefab;
            }

            return false;
        }

        private static Transform FindDeepChild(Transform parent, string childName)
        {
            // 先在直接子级中查找
            Transform result = parent.Find(childName);
            if (result != null)
                return result;

            // 递归在子级的子级中查找
            foreach (Transform child in parent)
            {
                result = FindDeepChild(child, childName);
                if (result != null)
                    return result;
            }

            return null;
        }

        private static object GetButtonFuncName(string varName)
        {
            if (string.IsNullOrEmpty(varName))
            {
                return varName;
            }
            for (int i = 0; i < VARIABLE_NAME_REGEX.Length; i++)
            {
                var prefix = VARIABLE_NAME_REGEX[i];
                if (varName.StartsWith(prefix))
                {
                    return $"OnClick{varName.Replace(prefix + ScriptGeneratorSetting.GetUIComponentWithoutPrefixName(UIComponentName.Button), string.Empty)}Btn";
                }
            }
            return varName;
        }

        private static object GetToggleFuncName(string varName)
        {
            if (string.IsNullOrEmpty(varName))
            {
                return varName;
            }
            for (int i = 0; i < VARIABLE_NAME_REGEX.Length; i++)
            {
                var prefix = VARIABLE_NAME_REGEX[i];
                if (varName.StartsWith(prefix))
                {
                    return
                        $"OnToggle{varName.Replace(prefix + ScriptGeneratorSetting.GetUIComponentWithoutPrefixName(UIComponentName.Toggle), string.Empty)}Change";
                }
            }
            return varName;
        }

        private static object GetSliderFuncName(string varName)
        {
            if (string.IsNullOrEmpty(varName))
            {
                return varName;
            }
            for (int i = 0; i < VARIABLE_NAME_REGEX.Length; i++)
            {
                var prefix = VARIABLE_NAME_REGEX[i];
                if (varName.StartsWith(prefix))
                {
                    return
                        $"OnSlider{varName.Replace(prefix + ScriptGeneratorSetting.GetUIComponentWithoutPrefixName(UIComponentName.Slider), string.Empty)}Change";
                }
            }
            return varName;
        }

        private static string GetPrefixNameByCodeStyle(UIFieldCodeStyle style)
        {
            return ScriptGeneratorSetting.GetPrefixNameByCodeStyle(style);
        }

        private static string GetComponentName(string componentName)
        {
            return GetPrefixName() + componentName;
        }

        private static string GetPrefixName()
        {
            return ScriptGeneratorSetting.GetPrefixNameByCodeStyle(ScriptGeneratorSetting.Instance.CodeStyle);
        }

        private static string GetUIWidgetName()
        {
            return GetComponentName(ScriptGeneratorSetting.GetWidgetName());
        }

        private static string GetVariableName(string varName)
        {
            if (string.IsNullOrEmpty(varName))
            {
                return varName;
            }

            for (int i = 0; i < VARIABLE_NAME_REGEX.Length; i++)
            {
                var prefix = VARIABLE_NAME_REGEX[i];
                if (varName.StartsWith(prefix))
                {
                    varName = varName.Replace(prefix, string.Empty);
                    varName = GetComponentName(varName);
                    break;
                }
            }
            return varName;
        }
    }
}