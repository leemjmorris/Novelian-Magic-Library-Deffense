using UnityEditor;
using UnityEngine;
using System.Reflection;

/// <summary>
/// Custom Editor for SkillConfig
/// Provides enhanced UI with conditional fields and CSV data loading
/// </summary>
[CustomEditor(typeof(SkillConfig))]
public class SkillConfigEditor : Editor
{
    private SkillConfig config;
    private SkillData[] availableSkills;
    private string[] skillDisplayNames;
    private int selectedSkillIndex = 0;

    private void OnEnable()
    {
        config = (SkillConfig)target;
        LoadAvailableSkills();
    }

    private void LoadAvailableSkills()
    {
        // Load skills from CSV file directly in Editor
        string csvPath = "Assets/Resources/CSV/SkillTable.csv";

        if (!System.IO.File.Exists(csvPath))
        {
            Debug.LogError($"[SkillConfigEditor] CSV file not found: {csvPath}");
            availableSkills = new SkillData[0];
            skillDisplayNames = new string[] { "CSV 파일을 찾을 수 없습니다" };
            return;
        }

        try
        {
            // Read CSV file
            string csvText = System.IO.File.ReadAllText(csvPath);

            // Parse CSV
            var skillList = new System.Collections.Generic.List<SkillData>();
            string[] lines = csvText.Split('\n');

            // Skip header lines (first 3 lines: Korean headers, English headers, Type definitions)
            for (int i = 3; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (string.IsNullOrEmpty(line)) continue;

                string[] values = line.Split(',');
                if (values.Length < 7) continue;

                try
                {
                    SkillData skill = new SkillData
                    {
                        Skill_ID = int.Parse(values[0].Trim()),
                        Skill_Name = values[1].Trim(),
                        Skill_Type = (SkillType)int.Parse(values[2].Trim()),
                        Attack_Range = (AttackRange)int.Parse(values[3].Trim()),
                        Cooldown = float.Parse(values[4].Trim()),
                        Cast_Time = float.Parse(values[5].Trim()),
                        Effect_ID = int.Parse(values[6].Trim()),
                        Equipable = values[7].Trim() == "1",
                        Description = values.Length > 8 ? values[8].Trim() : ""
                    };

                    skillList.Add(skill);
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"[SkillConfigEditor] Failed to parse line {i + 1}: {e.Message}");
                }
            }

            availableSkills = skillList.ToArray();

            // Create display names
            skillDisplayNames = new string[availableSkills.Length];
            for (int i = 0; i < availableSkills.Length; i++)
            {
                skillDisplayNames[i] = $"[{availableSkills[i].Skill_ID}] {availableSkills[i].Skill_Name} ({GetSkillTypeIcon(availableSkills[i].Skill_Type)})";

                // Find current selection
                if (availableSkills[i].Skill_ID == config.skillID)
                {
                    selectedSkillIndex = i;
                }
            }

            Debug.Log($"[SkillConfigEditor] Loaded {availableSkills.Length} skills from CSV");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[SkillConfigEditor] Error loading CSV: {e.Message}");
            availableSkills = new SkillData[0];
            skillDisplayNames = new string[] { "CSV 로드 실패" };
        }
    }

    private string GetSkillTypeIcon(SkillType type)
    {
        switch (type)
        {
            case SkillType.Attack: return "⚔️";
            case SkillType.Buff: return "✨";
            case SkillType.Debuff: return "💀";
            default: return "❓";
        }
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawHeader();
        DrawBasicInfo();
        DrawCastModeSettings();
        DrawProjectileSettings();
        DrawAOESettings();
        DrawDashSettings();
        DrawMovingAOESettings();
        DrawEffectSettings();
        DrawCharacterAssignment();

        serializedObject.ApplyModifiedProperties();
    }

    private new void DrawHeader()
    {
        EditorGUILayout.Space(10);
        GUIStyle headerStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 16,
            alignment = TextAnchor.MiddleCenter
        };

        EditorGUILayout.LabelField("⚔️ SKILL CONFIGURATION ⚔️", headerStyle);
        EditorGUILayout.Space(5);

        EditorGUILayout.HelpBox(
            "1. Skill ID를 입력하세요\n" +
            "2. 'Load From CSV' 버튼을 클릭하세요\n" +
            "3. 스킬 타입을 선택하고 프리팹을 할당하세요\n" +
            "4. 캐릭터를 드래그 & 드롭으로 할당하세요",
            MessageType.Info
        );

        EditorGUILayout.Space(10);
    }

    private void DrawBasicInfo()
    {
        EditorGUILayout.LabelField("📋 Basic Info", EditorStyles.boldLabel);

        EditorGUILayout.BeginVertical("box");

        // Reload CSV Button
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("스킬 선택", EditorStyles.boldLabel);
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("🔄 CSV 새로고침", GUILayout.Width(120)))
        {
            LoadAvailableSkills();
            Debug.Log("[SkillConfigEditor] CSV reloaded!");
        }
        EditorGUILayout.EndHorizontal();

        // Skill Selection Dropdown
        EditorGUI.BeginChangeCheck();

        int newIndex = EditorGUILayout.Popup("Select Skill", selectedSkillIndex, skillDisplayNames);

        if (EditorGUI.EndChangeCheck() && availableSkills != null && availableSkills.Length > 0)
        {
            selectedSkillIndex = newIndex;
            SkillData selectedSkill = availableSkills[selectedSkillIndex];

            // Update config
            config.skillID = selectedSkill.Skill_ID;
            config.skillName = selectedSkill.Skill_Name;
            config.skillType = selectedSkill.Skill_Type;
            config.attackRange = selectedSkill.Attack_Range;
            config.cooldown = selectedSkill.Cooldown;
            config.castTime = selectedSkill.Cast_Time;
            config.effectID = selectedSkill.Effect_ID;

            EditorUtility.SetDirty(config);
            Debug.Log($"[SkillConfig] Selected: {selectedSkill.Skill_Name} (ID: {selectedSkill.Skill_ID})");
        }

        EditorGUILayout.Space(10);

        // Display selected skill info (read-only)
        EditorGUILayout.LabelField("선택된 스킬 정보", EditorStyles.boldLabel);

        GUI.enabled = false;
        EditorGUILayout.TextField("Skill Name", config.skillName);
        EditorGUILayout.EnumPopup("Skill Type", config.skillType);
        EditorGUILayout.EnumPopup("Attack Range", config.attackRange);
        EditorGUILayout.FloatField("Cooldown", config.cooldown);
        EditorGUILayout.FloatField("Cast Time", config.castTime);
        EditorGUILayout.IntField("Effect ID", config.effectID);
        GUI.enabled = true;

        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(10);
    }

    private void DrawCastModeSettings()
    {
        EditorGUILayout.LabelField("🎭 Casting Mode", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.PropertyField(serializedObject.FindProperty("castMode"));
        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(10);
    }

    private void DrawProjectileSettings()
    {
        EditorGUILayout.LabelField("🎯 Projectile Settings", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical("box");

        SerializedProperty hasProjectileProp = serializedObject.FindProperty("hasProjectile");
        EditorGUILayout.PropertyField(hasProjectileProp);

        if (hasProjectileProp.boolValue)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(serializedObject.FindProperty("projectilePrefab"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("projectileSpeed"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("isHoming"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("isPiercing"));
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(10);
    }

    private void DrawAOESettings()
    {
        EditorGUILayout.LabelField("💥 AOE Settings", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical("box");

        SerializedProperty aoeTypeProp = serializedObject.FindProperty("aoeType");
        EditorGUILayout.PropertyField(aoeTypeProp);

        AreaOfEffectType aoeType = (AreaOfEffectType)aoeTypeProp.enumValueIndex;

        if (aoeType != AreaOfEffectType.None && aoeType != AreaOfEffectType.Full)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(serializedObject.FindProperty("aoeRadius"));

            if (aoeType == AreaOfEffectType.Cone)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("aoeAngle"));
            }
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(10);
    }

    private void DrawDashSettings()
    {
        EditorGUILayout.LabelField("⚡ Dash Settings (Flicker Strike)", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical("box");

        SerializedProperty isDashProp = serializedObject.FindProperty("isDashSkill");
        EditorGUILayout.PropertyField(isDashProp);

        if (isDashProp.boolValue)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(serializedObject.FindProperty("maxDashTargets"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("dashInterval"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("dashRange"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("returnToOrigin"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("dashTrailEffect"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("slashEffect"));
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(10);
    }

    private void DrawMovingAOESettings()
    {
        EditorGUILayout.LabelField("🌪️ Moving AOE Settings", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical("box");

        SerializedProperty isMovingAOEProp = serializedObject.FindProperty("isMovingAOE");
        EditorGUILayout.PropertyField(isMovingAOEProp);

        if (isMovingAOEProp.boolValue)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(serializedObject.FindProperty("movePattern"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("moveSpeed"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("lifetime"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("tickInterval"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("aoeEffectPrefab"));
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(10);
    }

    private void DrawEffectSettings()
    {
        EditorGUILayout.LabelField("✨ Visual Effects", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical("box");

        EditorGUILayout.HelpBox(
            "이펙트 발동 순서:\n" +
            "1️⃣ Muzzle Flash (발사 섬광) - 발사 위치에서 즉시 재생\n" +
            "2️⃣ Projectile Effect (투사체 비주얼) - 투사체 자체 이펙트\n" +
            "3️⃣ Trail Effects (트레일) - 투사체를 따라다니는 꼬리\n" +
            "4️⃣ On-Hit Effect (피격 시) - 적이 맞는 순간 재생\n" +
            "5️⃣ After-Hit Effect (피격 후) - 데미지 후 적에게 붙어서 지속",
            MessageType.Info
        );

        EditorGUILayout.Space(5);

        EditorGUILayout.LabelField("발사 관련 이펙트", EditorStyles.miniBoldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("muzzleFlashEffectPrefab"),
            new GUIContent("Muzzle Flash", "발사 섬광 (발사 위치에 남음)"));

        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField("투사체 관련 이펙트", EditorStyles.miniBoldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("projectileEffectPrefab"),
            new GUIContent("Projectile Effect", "투사체 자체 이펙트"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("trailEffectPrefabs"),
            new GUIContent("Trail Effects", "투사체를 따라다니는 꼬리 이펙트"), true);

        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField("피격 관련 이펙트", EditorStyles.miniBoldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("onHitEffectPrefab"),
            new GUIContent("On-Hit Effect", "적이 맞는 순간 재생"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("afterHitEffectPrefab"),
            new GUIContent("After-Hit Effect", "데미지 후 적에게 붙어서 지속"));

        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField("기타 (Deprecated)", EditorStyles.miniBoldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("impactEffectPrefab"),
            new GUIContent("Impact Effect (Old)", "더 이상 사용하지 않음 - onHitEffectPrefab 사용"));

        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(10);
    }

    private void DrawCharacterAssignment()
    {
        EditorGUILayout.LabelField("👥 Character Assignment", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical("box");

        EditorGUILayout.HelpBox(
            "캐릭터 Prefab을 드래그 & 드롭으로 추가하세요.\n" +
            "이 스킬을 사용할 모든 캐릭터를 할당할 수 있습니다.",
            MessageType.Info
        );

        EditorGUILayout.PropertyField(serializedObject.FindProperty("assignedCharacters"), true);

        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(10);
    }
}

/// <summary>
/// Property Drawer for ReadOnly attribute
/// </summary>
[CustomPropertyDrawer(typeof(ReadOnlyAttribute))]
public class ReadOnlyDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        GUI.enabled = false;
        EditorGUI.PropertyField(position, property, label, true);
        GUI.enabled = true;
    }
}
