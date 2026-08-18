/*
 * ============================================================================
 * 文件：WeaponSceneSetup.cs
 * 用途：编辑器一键工具（Editor 层）。
 *       在【当前活动场景】中创建一把"独立测试手枪"，用于查看
 *       枪械系统代码是如何挂载到枪模型上的：
 *       1. 实例化 Assets/Low Poly Weapon Bundle Pack 3/.../Pistol_L.prefab
 *       2. 挂上武器系统组件（AmmoInventory / WeaponInventory / WeaponController）
 *       3. 装备 Wingman 左轮数据（武器_Wingman.asset）作为主武器
 *       4. 添加弹药、配置枪口与相机引用
 *       5. 挂 AutoFireDemo（自动开火演示，不依赖玩家输入）
 *       6. 在枪口前方放一个测试靶子（带 Health，可观察伤害）
 *       7. 保存场景
 *       注意：在哪个场景运行菜单，手枪就创建在哪个场景（不切换场景）。
 * 所属：CharacterController.FPS.Weapon.Editor（Editor 层）
 * ============================================================================
 */

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using CharacterController.FPS.Weapon;
using CharacterController.FPS.Weapon.Ballistic;
using CharacterController.FPS.Weapon.Damage;

namespace CharacterController.FPS.Weapon.Editor
{
    /// <summary>
    /// 场景搭建工具：把独立测试手枪放进【当前打开的场景】。
    /// 菜单：FPS/Weapon/在场景中创建测试手枪 (Pistol_L + Wingman)
    /// </summary>
    public static class WeaponSceneSetup
    {
        // ==================== 路径常量 ====================

        /// <summary>手枪 prefab 路径</summary>
        private const string PistolPrefabPath =
            "Assets/Low Poly Weapon Bundle Pack 3/Prefabs/Weapons/Weapons_Pistol/Pistol_L.prefab";

        /// <summary>Wingman 左轮武器数据路径</summary>
        private const string WingmanDataPath = "Assets/WeaponContent/Weapons/武器_Wingman.asset";

        /// <summary>手枪在场景中的摆放位置（相机前方地面）</summary>
        private static readonly Vector3 PistolPosition = new Vector3(0f, 0.25f, 2f);

        /// <summary>手枪朝向（枪口朝 +Z 前方，正对靶子）</summary>
        private static readonly Quaternion PistolRotation = Quaternion.identity;

        /// <summary>测试靶子位置（枪口正前方）</summary>
        private static readonly Vector3 TargetPosition = new Vector3(0f, 1f, 6f);

        // ==================== 菜单入口（在当前场景创建） ====================

        [MenuItem("FPS/Weapon/在场景中创建测试手枪 (Pistol_L + Wingman)")]
        public static void CreateTestPistolFromMenu()
        {
            // 菜单模式：直接在当前活动场景中创建，绝不切换场景
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid())
            {
                ShowError("当前没有打开的场景，请先打开一个场景（如 Assets/Test.unity）再执行。");
                return;
            }

            CreateTestPistolInScene(scene, showDialog: true);
        }

        // ==================== 批处理入口（供命令行自动执行） ====================

        /// <summary>
        /// 批处理入口：
        /// Unity 命令行 -executeMethod CharacterController.FPS.Weapon.Editor.WeaponSceneSetup.CreateTestPistol
        /// 会打开 Assets/Test.unity 创建手枪并保存。
        /// </summary>
        public static void CreateTestPistol()
        {
            const string scenePath = "Assets/Test.unity";

            Scene scene = default;
            if (System.IO.File.Exists(scenePath))
            {
                scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            }

            if (!scene.IsValid())
            {
                Debug.LogError("[WeaponSceneSetup] 无法打开场景: " + scenePath);
                return;
            }

            CreateTestPistolInScene(scene, showDialog: false);
        }

        // ==================== 一键修复子弹引用（防止手写 fileID 出错） ====================

        /// <summary>
        /// 菜单：FPS/Weapon/修复场景子弹预制体引用 (Test.unity)
        /// 通过资源路径重新加载子弹预制体上的 Projectile 组件并写回场景，
        /// 彻底避免手写 fileID 溢出/导入失败导致的 Missing 引用问题。
        /// </summary>
        [MenuItem("FPS/Weapon/修复场景子弹预制体引用 (Test.unity)")]
        public static void FixSceneProjectilePrefab()
        {
            const string scenePath = "Assets/Test.unity";
            const string bulletPrefabPath =
                "Assets/Low Poly Weapon Bundle Pack 3/Prefabs/Bullets/Bullets_Pistol/Bullet_Pistol_A_Projectile.prefab";

            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.path != scenePath)
            {
                scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            }

            var projectile = AssetDatabase.LoadAssetAtPath<Projectile>(bulletPrefabPath);
            if (projectile == null)
            {
                Debug.LogError("[WeaponSceneSetup] 子弹预制体上未找到 Projectile 组件: " + bulletPrefabPath);
                return;
            }

            bool fixedAny = false;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                var controller = root.GetComponent<WeaponController>();
                if (controller == null)
                {
                    continue;
                }

                var so = new SerializedObject(controller);
                var prop = so.FindProperty("projectilePrefab");
                prop.objectReferenceValue = projectile;
                so.ApplyModifiedProperties();
                fixedAny = true;
                Debug.Log($"[WeaponSceneSetup] 已修复 {root.name} 的 projectilePrefab 引用 -> {projectile.name}");
            }

            if (fixedAny)
            {
                EditorSceneManager.SaveScene(scene);
                Debug.Log("[WeaponSceneSetup] 场景已保存: " + scenePath);
            }
            else
            {
                Debug.LogWarning("[WeaponSceneSetup] 场景中没有找到带 WeaponController 的物体");
            }
        }

        // ==================== 一键设置测试手枪射击模式 ====================

        /// <summary>
        /// 菜单：FPS/Weapon/设置测试手枪射击模式 (自动/连发/单发)
        /// 用 Unity 自己的 API 写 supportedModes，避免手写 List&lt;enum&gt;
        /// 的序列化字节与 Unity 实际解析不一致（导致切换总是全自动）。
        /// 循环顺序：全自动 → 三连发 → 单发（右键依次切换）。
        /// </summary>
        [MenuItem("FPS/Weapon/设置测试手枪射击模式 (自动/连发/单发)")]
        public static void SetupTestWeaponFireModes()
        {
            const string weaponPath = "Assets/WeaponContent/Weapons/武器_测试手枪.asset";

            var weapon = AssetDatabase.LoadAssetAtPath<WeaponData>(weaponPath);
            if (weapon == null)
            {
                Debug.LogError("[WeaponSceneSetup] 找不到测试武器资产: " + weaponPath);
                return;
            }

            var so = new SerializedObject(weapon);
            var list = so.FindProperty("supportedModes");
            if (list == null || !list.isArray)
            {
                Debug.LogError("[WeaponSceneSetup] 找不到 supportedModes 属性");
                return;
            }

            // 清空后用 Unity API 填入三个模式：全自动(2)、三连发(1)、单发(0)
            list.ClearArray();
            list.arraySize = 3;
            list.GetArrayElementAtIndex(0).enumValueIndex = (int)FireModeType.FullAuto;
            list.GetArrayElementAtIndex(1).enumValueIndex = (int)FireModeType.Burst;
            list.GetArrayElementAtIndex(2).enumValueIndex = (int)FireModeType.SemiAuto;

            var defaultProp = so.FindProperty("defaultMode");
            if (defaultProp != null)
            {
                defaultProp.enumValueIndex = (int)FireModeType.SemiAuto; // 初始单发
            }

            so.ApplyModifiedProperties();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // 读回打印，方便确认
            var check = AssetDatabase.LoadAssetAtPath<WeaponData>(weaponPath);
            var sb = new System.Text.StringBuilder();
            foreach (var m in check.supportedModes)
            {
                if (sb.Length > 0)
                {
                    sb.Append(" -> ");
                }
                sb.Append(m);
            }

            Debug.Log($"[WeaponSceneSetup] 测试手枪射击模式已设置为 [{sb}]（共 {check.supportedModes.Count} 个），" +
                      $"默认模式: {check.defaultMode}。右键切换顺序：单发→全自动→三连发→单发");
        }

        // ==================== 核心创建逻辑 ====================

        /// <summary>
        /// 在指定场景中创建独立测试手枪 + 测试靶子，并保存该场景。
        /// </summary>
        /// <param name="scene">目标场景（必须是已打开的有效场景）</param>
        /// <param name="showDialog">是否弹出成功/失败对话框（菜单模式 true，批处理 false）</param>
        public static void CreateTestPistolInScene(Scene scene, bool showDialog)
        {
            // 1. 加载资源
            var pistolPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PistolPrefabPath);
            if (pistolPrefab == null)
            {
                ShowError($"找不到手枪 prefab: {PistolPrefabPath}\n请确认该文件存在于项目内。", showDialog);
                return;
            }

            var wingman = AssetDatabase.LoadAssetAtPath<WeaponData>(WingmanDataPath);
            if (wingman == null)
            {
                ShowError($"找不到 Wingman 武器数据: {WingmanDataPath}\n请先执行菜单 FPS/Weapon/一键生成示例武器内容。", showDialog);
                return;
            }

            // 2. 实例化手枪到场景（PrefabUtility 保证保留 prefab 关联）
            var pistol = (GameObject)PrefabUtility.InstantiatePrefab(pistolPrefab, scene);
            if (pistol == null)
            {
                ShowError("手枪实例化失败。", showDialog);
                return;
            }

            pistol.name = "Test_Pistol_Wingman";
            pistol.transform.SetPositionAndRotation(PistolPosition, PistolRotation);

            // 3. 挂武器系统组件（这就是"枪械代码挂载"的核心展示）
            var ammoInventory = pistol.AddComponent<AmmoInventory>();
            var weaponInventory = pistol.AddComponent<WeaponInventory>();
            var weaponController = pistol.AddComponent<WeaponController>();

            // 4. 用 SerializedObject 配置私有引用字段
            ConfigureReferences(pistol, ammoInventory, weaponInventory, weaponController);

            // 5. 装备 Wingman 左轮 + 补充弹药
            weaponInventory.EquipWeapon(WeaponSlotType.Primary, wingman, wingman.magazineSize);
            if (wingman.ammoType != null)
            {
                ammoInventory.AddAmmo(wingman.ammoType, wingman.ammoType.maxStack);
            }

            // 6. 挂自动开火演示脚本（不依赖玩家输入，直观展示枪械循环）
            pistol.AddComponent<AutoFireDemo>();

            // 7. 在枪口前方创建测试靶子（带 Health 可观察伤害与护甲扣除）
            CreateTestTarget(scene);

            // 8. 标记并保存场景
            EditorSceneManager.MarkSceneDirty(scene);
            bool saved = EditorSceneManager.SaveScene(scene);
            if (!saved)
            {
                ShowError("场景保存失败，请手动保存（Ctrl+S）。", showDialog);
                return;
            }

            // 9. 选中新物体并提示结果
            Selection.activeGameObject = pistol;
            SceneView.FrameLastActiveSceneView();

            string msg = $"测试手枪已创建并保存到场景 [{scene.name}]\n\n" +
                         $"• 模型：Pistol_L（Low Poly Weapon Bundle）\n" +
                         $"• 武器：Wingman 左轮（伤害45 / 弹匣6 / 重型弹药）\n" +
                         $"• 位置：({PistolPosition.x}, {PistolPosition.y}, {PistolPosition.z})\n" +
                         $"• 自动演示：每 0.8 秒开一枪，弹匣打空自动换弹\n" +
                         $"• 测试靶：枪口前方 ({TargetPosition.x}, {TargetPosition.y}, {TargetPosition.z})\n\n" +
                         $"点击 Play 即可观察自动开火与伤害。\n" +
                         $"（本工具不接入玩家输入；查看组件挂载请选中手枪看 Inspector）";

            Debug.Log("[WeaponSceneSetup] " + msg);
            if (showDialog)
            {
                EditorUtility.DisplayDialog("武器系统 - 创建完成", msg, "好的");
            }
        }

        // ==================== 内部方法 ====================

        /// <summary>
        /// 创建测试靶子：立方体 + Health 组件（护甲+血量），
        /// 用于观察自动开火的伤害效果。
        /// </summary>
        private static void CreateTestTarget(Scene scene)
        {
            // 避免重复创建（重复执行菜单时不堆积靶子）
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.name == "Test_Target")
                {
                    Object.DestroyImmediate(root);
                }
            }

            var target = GameObject.CreatePrimitive(PrimitiveType.Cube);
            target.name = "Test_Target";
            target.transform.SetPositionAndRotation(TargetPosition, Quaternion.identity);
            // 拉长成靶子形状
            target.transform.localScale = new Vector3(0.8f, 2f, 0.4f);
            SceneManager.MoveGameObjectToScene(target, scene);

            // 红色材质（临时生成，仅用于区分靶子）
            var renderer = target.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"))
                {
                    color = new Color(0.9f, 0.2f, 0.2f)
                };
            }

            // 挂 Health：护甲 50 + 血量 100（对齐 APEX 护甲先扣的设计）
            var health = target.AddComponent<Health>();
            var healthSo = new SerializedObject(health);
            healthSo.FindProperty("maxShield").floatValue = 50f;
            healthSo.FindProperty("maxHealth").floatValue = 100f;
            healthSo.ApplyModifiedProperties();
        }

        /// <summary>
        /// 配置手枪上的武器系统引用：
        /// WeaponController.inventory / muzzle / viewCamera，
        /// WeaponInventory.ammoInventory。
        /// </summary>
        private static void ConfigureReferences(GameObject pistol,
            AmmoInventory ammoInventory, WeaponInventory weaponInventory,
            WeaponController weaponController)
        {
            // 枪口：优先用 Pistol_L_Barrel 子物体（模型枪管末端）
            Transform muzzle = FindChildByName(pistol.transform, "Pistol_L_Barrel");
            if (muzzle == null)
            {
                // 找不到枪管时退化为根物体
                muzzle = pistol.transform;
            }

            // 相机：场景中的 Main Camera（自动开火射线方向来源）
            Camera viewCamera = Object.FindFirstObjectByType<Camera>();
            if (viewCamera == null)
            {
                Debug.LogWarning("[WeaponSceneSetup] 场景中没有相机，viewCamera 留空");
            }

            // WeaponInventory.ammoInventory
            var invSo = new SerializedObject(weaponInventory);
            invSo.FindProperty("ammoInventory").objectReferenceValue = ammoInventory;
            invSo.ApplyModifiedProperties();

            // WeaponController 引用
            var ctrlSo = new SerializedObject(weaponController);
            ctrlSo.FindProperty("inventory").objectReferenceValue = weaponInventory;
            ctrlSo.FindProperty("muzzle").objectReferenceValue = muzzle;
            if (viewCamera != null)
            {
                ctrlSo.FindProperty("viewCamera").objectReferenceValue = viewCamera;
            }

            ctrlSo.ApplyModifiedProperties();
        }

        /// <summary>
        /// 在层级中按名称查找子物体（深度优先）。
        /// </summary>
        private static Transform FindChildByName(Transform root, string name)
        {
            if (root.name == name)
            {
                return root;
            }

            for (int i = 0; i < root.childCount; i++)
            {
                var result = FindChildByName(root.GetChild(i), name);
                if (result != null)
                {
                    return result;
                }
            }

            return null;
        }

        /// <summary>
        /// 统一错误提示：菜单模式弹窗，批处理模式只打日志。
        /// </summary>
        private static void ShowError(string message, bool showDialog = true)
        {
            Debug.LogError("[WeaponSceneSetup] " + message);
            if (showDialog)
            {
                EditorUtility.DisplayDialog("武器系统 - 创建失败", message, "知道了");
            }
        }
    }
}
#endif
