// JML: 캐릭터 ID별 비주얼 파츠 매핑 (임시 에셋용)
// 에셋 교체 시 이 파일만 수정하면 됨
namespace Novelian.Combat
{
    using System.Collections.Generic;

    /// <summary>
    /// 캐릭터 ID별 활성화할 비주얼 파츠 정보
    /// </summary>
    public class CharacterVisualData
    {
        public string bodyPart;       // "Body01" ~ "Body20"
        public string hairPart;       // "Hair01" ~ "Hair13"
        public string cloakPart;      // "Cloak01" ~ "Cloak03" 또는 null (없음)
        public string weaponRight;    // weapon_r 자식 중 활성화할 무기
        public string weaponLeft;     // weapon_l 자식 중 활성화할 방패/무기 (또는 null)
    }

    /// <summary>
    /// 캐릭터 ID → 비주얼 파츠 매핑 테이블
    /// </summary>
    public static class CharacterVisualConfig
    {
        // 캐릭터 ID → 비주얼 데이터 매핑
        private static readonly Dictionary<int, CharacterVisualData> visualMapping = new Dictionary<int, CharacterVisualData>
        {
            // Horror (21001 ~ 21004)
            { 21001, new CharacterVisualData { bodyPart = "Body01", hairPart = "Hair01", cloakPart = "Cloak01", weaponRight = "THS01_Sword", weaponLeft = null } },
            { 21002, new CharacterVisualData { bodyPart = "Body02", hairPart = "Hair02", cloakPart = null, weaponRight = "OHS05_Sword", weaponLeft = "Shield01" } },
            { 21003, new CharacterVisualData { bodyPart = "Body03", hairPart = "Hair03", cloakPart = "Cloak02", weaponRight = "THS02_Sword", weaponLeft = null } },
            { 21004, new CharacterVisualData { bodyPart = "Body04", hairPart = "Hair04", cloakPart = null, weaponRight = "OHS07_Sword", weaponLeft = "Shield08" } },

            // Romance (22005 ~ 22008)
            { 22005, new CharacterVisualData { bodyPart = "Body05", hairPart = "Hair05", cloakPart = "Cloak03", weaponRight = "THS04_Sword", weaponLeft = null } },
            { 22006, new CharacterVisualData { bodyPart = "Body06", hairPart = "Hair06", cloakPart = null, weaponRight = "OHS08_Sword", weaponLeft = "Shield12" } },
            { 22007, new CharacterVisualData { bodyPart = "Body07", hairPart = "Hair07", cloakPart = "Cloak01", weaponRight = "THS05_Sword", weaponLeft = null } },
            { 22008, new CharacterVisualData { bodyPart = "Body08", hairPart = "Hair08", cloakPart = null, weaponRight = "OHS09_Sword", weaponLeft = "Shield14" } },

            // Adventure (23009 ~ 23012)
            { 23009, new CharacterVisualData { bodyPart = "Body09", hairPart = "Hair09", cloakPart = "Cloak02", weaponRight = "THS06_Sword", weaponLeft = null } },
            { 23010, new CharacterVisualData { bodyPart = "Body10", hairPart = "Hair10", cloakPart = null, weaponRight = "OHS16_Sword", weaponLeft = "Shield16" } },
            { 23011, new CharacterVisualData { bodyPart = "Body11", hairPart = "Hair11", cloakPart = "Cloak03", weaponRight = "THS07_Sword", weaponLeft = null } },
            { 23012, new CharacterVisualData { bodyPart = "Body12", hairPart = "Hair12", cloakPart = null, weaponRight = "OHS05_Sword", weaponLeft = "Shield18" } },

            // Comedy (24013 ~ 24016)
            { 24013, new CharacterVisualData { bodyPart = "Body13", hairPart = "Hair13", cloakPart = "Cloak01", weaponRight = "THS01_Sword", weaponLeft = null } },
            { 24014, new CharacterVisualData { bodyPart = "Body14", hairPart = "Hair01", cloakPart = null, weaponRight = "OHS07_Sword", weaponLeft = "Shield20" } },
            { 24015, new CharacterVisualData { bodyPart = "Body15", hairPart = "Hair02", cloakPart = "Cloak02", weaponRight = "THS02_Sword", weaponLeft = null } },
            { 24016, new CharacterVisualData { bodyPart = "Body16", hairPart = "Hair03", cloakPart = null, weaponRight = "OHS08_Sword", weaponLeft = "Shield01" } },

            // Mystery (25017 ~ 25020)
            { 25017, new CharacterVisualData { bodyPart = "Body17", hairPart = "Hair04", cloakPart = "Cloak03", weaponRight = "THS04_Sword", weaponLeft = null } },
            { 25018, new CharacterVisualData { bodyPart = "Body18", hairPart = "Hair05", cloakPart = null, weaponRight = "OHS09_Sword", weaponLeft = "Shield08" } },
            { 25019, new CharacterVisualData { bodyPart = "Body19", hairPart = "Hair06", cloakPart = "Cloak01", weaponRight = "THS05_Sword", weaponLeft = null } },
            { 25020, new CharacterVisualData { bodyPart = "Body20", hairPart = "Hair07", cloakPart = "Cloak02", weaponRight = "OHS16_Sword", weaponLeft = "Shield12" } },
        };

        /// <summary>
        /// 캐릭터 ID로 비주얼 데이터 조회
        /// </summary>
        public static CharacterVisualData GetVisualData(int characterId)
        {
            if (visualMapping.TryGetValue(characterId, out var data))
            {
                return data;
            }
            return null;
        }

        /// <summary>
        /// 모든 Body 파츠 이름 목록 (비활성화용)
        /// </summary>
        public static readonly string[] AllBodyParts = new string[]
        {
            "Body01", "Body02", "Body03", "Body04", "Body05",
            "Body06", "Body07", "Body08", "Body09", "Body10",
            "Body11", "Body12", "Body13", "Body14", "Body15",
            "Body16", "Body17", "Body18", "Body19", "Body20"
        };

        /// <summary>
        /// 모든 Hair 파츠 이름 목록 (비활성화용)
        /// </summary>
        public static readonly string[] AllHairParts = new string[]
        {
            "Hair01", "Hair02", "Hair03", "Hair04", "Hair05",
            "Hair06", "Hair07", "Hair08", "Hair09", "Hair10",
            "Hair11", "Hair12", "Hair13"
        };

        /// <summary>
        /// 모든 Cloak 파츠 이름 목록 (비활성화용)
        /// </summary>
        public static readonly string[] AllCloakParts = new string[]
        {
            "Cloak01", "Cloak02", "Cloak03"
        };

        /// <summary>
        /// 모든 weapon_r 자식 무기 이름 목록 (비활성화용)
        /// </summary>
        public static readonly string[] AllWeaponRightParts = new string[]
        {
            "OHS05_Sword", "OHS07_Sword", "OHS08_Sword", "OHS09_Sword", "OHS16_Sword",
            "THS01_Sword", "THS02_Sword", "THS04_Sword", "THS05_Sword", "THS06_Sword", "THS07_Sword"
        };

        /// <summary>
        /// 모든 weapon_l 자식 방패 이름 목록 (비활성화용)
        /// </summary>
        public static readonly string[] AllWeaponLeftParts = new string[]
        {
            "Shield01", "Shield08", "Shield12", "Shield14", "Shield16", "Shield18", "Shield20"
        };
    }
}
