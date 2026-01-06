//LMJ : Character partial class - Visual Parts Management (Issue #356)
namespace Novelian.Combat
{
    using UnityEngine;
    using NovelianMagicLibraryDefense.Managers;

    /// <summary>
    /// 캐릭터 비주얼 파츠 관리
    /// - CharacterVisualConfig 적용
    /// - 파츠 슬롯 캐싱
    /// - 무기 장착
    /// </summary>
    public partial class Character
    {
        #region Visual Config Application

        /// <summary>
        /// JML: CSV 기반 비주얼 설정 적용 (Issue #356)
        /// CharacterVisualConfigTable에서 캐릭터별 파츠 프리팹 로드 및 적용
        /// </summary>
        /// <param name="csvCharacterId">캐릭터 ID (CharacterTable)</param>
        private void ApplyVisualConfig(int csvCharacterId)
        {
            // CharacterVisualConfigTable에서 비주얼 설정 로드
            var visualConfig = CSVLoader.Instance?.GetData<CharacterVisualConfigData>(csvCharacterId);
            if (visualConfig == null)
            {
                // 비주얼 설정이 없는 캐릭터는 기본 비주얼 사용 (정상 케이스)
                // CSV에 모든 캐릭터의 비주얼 설정이 있지 않을 수 있음
                return;
            }

            GameLog.Log($"[Character] Applying visual config for character {csvCharacterId}");

            // 파츠 슬롯 캐싱
            CachePartSlots();

            // 각 파츠 적용
            ApplyBodyPart(visualConfig);
            ApplyHeadPart(visualConfig);
            ApplyHairPart(visualConfig);
            ApplyFacePart(visualConfig);
            ApplyWeaponPart(visualConfig);
            ApplyAccessoryPart(visualConfig);

            GameLog.Log($"[Character] Visual config applied successfully");
        }

        /// <summary>
        /// 파츠 슬롯 Transform 캐싱 (성능 최적화)
        /// </summary>
        private void CachePartSlots()
        {
            if (characterObj == null)
            {
                GameLog.LogWarning("[Character] characterObj is null, cannot cache part slots");
                return;
            }

            cachedTransforms.Clear();

            // 일반적인 파츠 슬롯 이름으로 탐색
            string[] slotNames = new string[]
            {
                "Body", "Head", "Hair", "Face",
                "Weapon_R", "Weapon_L", "WeaponSlot_R", "WeaponSlot_L",
                "Accessory", "Hat", "Glasses", "Wings"
            };

            foreach (string slotName in slotNames)
            {
                Transform slot = FindDeepChild(characterObj.transform, slotName);
                if (slot != null)
                {
                    cachedTransforms[slotName] = slot;
                }
            }

            // 무기 슬롯 특별 처리
            weaponRightSlot = cachedTransforms.ContainsKey("Weapon_R") ? cachedTransforms["Weapon_R"]
                           : cachedTransforms.ContainsKey("WeaponSlot_R") ? cachedTransforms["WeaponSlot_R"]
                           : null;

            weaponLeftSlot = cachedTransforms.ContainsKey("Weapon_L") ? cachedTransforms["Weapon_L"]
                          : cachedTransforms.ContainsKey("WeaponSlot_L") ? cachedTransforms["WeaponSlot_L"]
                          : null;

            GameLog.Log($"[Character] Cached {cachedTransforms.Count} part slots");
        }

        /// <summary>
        /// 깊은 자식 탐색 (재귀)
        /// </summary>
        private Transform FindDeepChild(Transform parent, string name)
        {
            if (parent.name == name)
                return parent;

            foreach (Transform child in parent)
            {
                Transform result = FindDeepChild(child, name);
                if (result != null)
                    return result;
            }

            return null;
        }

        #endregion

        #region Part Application

        private void ApplyBodyPart(CharacterVisualConfigData config)
        {
            if (string.IsNullOrEmpty(config.BodyPrefabPath))
                return;

            // Body는 보통 기본 메쉬이므로 Material만 교체하거나 전체 교체
            // 구현: Resources 또는 Addressables에서 로드
            GameLog.Log($"[Character] Body part: {config.BodyPrefabPath}");
        }

        private void ApplyHeadPart(CharacterVisualConfigData config)
        {
            if (string.IsNullOrEmpty(config.HeadPrefabPath))
                return;

            if (cachedTransforms.TryGetValue("Head", out Transform headSlot))
            {
                InstantiatePartAtSlot(config.HeadPrefabPath, headSlot);
            }
        }

        private void ApplyHairPart(CharacterVisualConfigData config)
        {
            if (string.IsNullOrEmpty(config.HairPrefabPath))
                return;

            if (cachedTransforms.TryGetValue("Hair", out Transform hairSlot))
            {
                InstantiatePartAtSlot(config.HairPrefabPath, hairSlot);
            }
        }

        private void ApplyFacePart(CharacterVisualConfigData config)
        {
            if (string.IsNullOrEmpty(config.FacePrefabPath))
                return;

            if (cachedTransforms.TryGetValue("Face", out Transform faceSlot))
            {
                InstantiatePartAtSlot(config.FacePrefabPath, faceSlot);
            }
        }

        private void ApplyWeaponPart(CharacterVisualConfigData config)
        {
            // 오른손 무기
            if (!string.IsNullOrEmpty(config.WeaponRightPrefabPath) && weaponRightSlot != null)
            {
                InstantiatePartAtSlot(config.WeaponRightPrefabPath, weaponRightSlot);
                GameLog.Log($"[Character] Right weapon: {config.WeaponRightPrefabPath}");
            }

            // 왼손 무기
            if (!string.IsNullOrEmpty(config.WeaponLeftPrefabPath) && weaponLeftSlot != null)
            {
                InstantiatePartAtSlot(config.WeaponLeftPrefabPath, weaponLeftSlot);
                GameLog.Log($"[Character] Left weapon: {config.WeaponLeftPrefabPath}");
            }
        }

        private void ApplyAccessoryPart(CharacterVisualConfigData config)
        {
            if (string.IsNullOrEmpty(config.AccessoryPrefabPath))
                return;

            if (cachedTransforms.TryGetValue("Accessory", out Transform accessorySlot))
            {
                InstantiatePartAtSlot(config.AccessoryPrefabPath, accessorySlot);
            }
        }

        /// <summary>
        /// 파츠 프리팹을 슬롯에 인스턴스화
        /// </summary>
        private void InstantiatePartAtSlot(string prefabPath, Transform slot)
        {
            if (string.IsNullOrEmpty(prefabPath) || slot == null)
                return;

            // 기존 파츠 제거
            foreach (Transform child in slot)
            {
                Object.Destroy(child.gameObject);
            }

            // 프리팹 로드 및 인스턴스화
            GameObject prefab = Resources.Load<GameObject>(prefabPath);
            if (prefab != null)
            {
                GameObject instance = Object.Instantiate(prefab, slot);
                instance.transform.localPosition = Vector3.zero;
                instance.transform.localRotation = Quaternion.identity;
                instance.transform.localScale = Vector3.one;
            }
            else
            {
                GameLog.LogWarning($"[Character] Part prefab not found: {prefabPath}");
            }
        }

        #endregion
    }
}
