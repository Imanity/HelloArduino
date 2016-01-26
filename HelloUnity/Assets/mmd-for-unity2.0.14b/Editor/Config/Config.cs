using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Linq;
using MMD.PMD;

namespace MMD
{
    /// <summary>
    /// MFU嗦必须的配置管理
    /// </summary>
    [Serializable]
    public class Config : ScriptableObject
    {
        public InspectorConfig inspector_config;
        public DefaultPMDImportConfig pmd_config;
        public DefaultVMDImportConfig vmd_config;

        private List<ConfigBase> update_list;
        public void OnEnable()
        {
            // 禁止使用Inspector编辑
            hideFlags = HideFlags.NotEditable;
            if (pmd_config == null)
            {
                // 初始化处理
                pmd_config = new DefaultPMDImportConfig();
                vmd_config = new DefaultVMDImportConfig();
                inspector_config = new InspectorConfig();
            }
            if (update_list == null)
            {
                update_list = new List<ConfigBase>();
                update_list.Add(inspector_config);
                update_list.Add(pmd_config);
                update_list.Add(vmd_config);
            }
        }

        /// <summary>
        /// GUI绘制
        /// </summary>
        public void OnGUI()
        {
            if (update_list == null) return;
            update_list.ForEach((item) =>
            {
                item.OnGUI();
            });
        }

        /// <summary>
        /// 保存配置
        /// </summary>
        /// <returns>路径</returns>
        public static string GetConfigPath()
        {
            var path = AssetDatabase.GetAllAssetPaths().Where(item => item.Contains("Config.cs")).First();
            path = path.Substring(0, path.LastIndexOf('/') + 1) + "Config.asset";
            return path;
        }

        /// <summary>
        /// 读入Config.asset 如无则生成
        /// </summary>
        /// <returns>读入生成的object</returns>
        public static Config LoadAndCreate()
        {
            var path = Config.GetConfigPath();
            var config = (Config)AssetDatabase.LoadAssetAtPath(path, typeof(Config));

            //// 没有的话创建一个
            if (config == null)
            {
                config = CreateInstance<Config>();
                AssetDatabase.CreateAsset(config, path);
                EditorUtility.SetDirty(config);
            }
			Debug.Log(config);
            return config;
        }
    }

    /// <summary>
    ///InspectorConfig
    /// </summary>
    [Serializable]
    public class InspectorConfig : ConfigBase
    {
        public bool use_pmd_preload = false;
        public bool use_vmd_preload = false;

        public InspectorConfig()
        {
            this.title = "Inspector Config";
        }

        public override void OnGUI()
        {
            base.OnGUI(() =>
                {
                    use_pmd_preload = EditorGUILayout.Toggle("Use PMD Preload", use_pmd_preload);
                    use_vmd_preload = EditorGUILayout.Toggle("Use VMD Preload", use_vmd_preload);
                }
            );
        }
    }

    /// <summary>
    /// PMD导入的默认设置
    /// </summary>
    [Serializable]
    public class DefaultPMDImportConfig : ConfigBase
    {
        public PMDConverter.ShaderType shader_type = PMDConverter.ShaderType.MMDShader;
        public bool use_mecanim = false;
        public bool rigidFlag = true;
        public bool use_ik = true;
        public float scale = 0.085f;
        public bool is_pmx_base_import = false;

        public DefaultPMDImportConfig()
        {
            this.title = "Default PMD Import Config";
        }

        public override void OnGUI()
        {
            base.OnGUI(() =>
                {
                    shader_type = (PMDConverter.ShaderType)EditorGUILayout.EnumPopup("Shader Type", shader_type);
                    rigidFlag = EditorGUILayout.Toggle("Rigidbody", rigidFlag);
                    use_mecanim = false;
                    use_ik = EditorGUILayout.Toggle("Use IK", use_ik);
                    is_pmx_base_import = EditorGUILayout.Toggle("Use PMX Base Import", is_pmx_base_import);
                }
            );
        }
    }

    /// <summary>
    /// VMD导入的默认设置
    /// </summary>
    [Serializable]
    public class DefaultVMDImportConfig : ConfigBase
    {
        public bool createAnimationFile;
        public int interpolationQuality;

        public DefaultVMDImportConfig()
        {
            this.title = "Default VMD Import Config";
        }

        public override void OnGUI()
        {
            base.OnGUI(() =>
                {
                    createAnimationFile = EditorGUILayout.Toggle("Create Asset", createAnimationFile);
                    interpolationQuality = EditorGUILayout.IntSlider("Interpolation Quality", interpolationQuality, 1, 10);
                }
            );
        }
    }

    /// <summary>
    /// 配置基类
    /// </summary>
    public class ConfigBase
    {
        /// <summary>
        /// 指定标题
        /// </summary>
        protected string title = "";

        /// <summary>
        /// 开闭的状态
        /// </summary>
        private bool fold = true;

        /// <summary>
        /// 进行GUI绘制
        /// </summary>
        /// <param name="OnGUIFunction">没有返回值的lamda表达式</param>
        public void OnGUI(Action OnGUIFunction)
        {
            fold = EditorGUILayout.Foldout(fold, title);
            if (fold)
                OnGUIFunction();
            EditorGUILayout.Space();
        }

        /// <summary>
        /// 进行GUI绘制
        /// </summary>
        public virtual void OnGUI()
        {
        }
    }
}
