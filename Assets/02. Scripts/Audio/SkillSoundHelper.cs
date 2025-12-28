using System.Collections.Generic;

namespace NovelianMagicLibraryDefense.Audio
{
    /// <summary>
    /// 스킬 ID에 따른 사운드 키 매핑 헬퍼
    /// CSV 데이터 기반 하드코딩 (스킬 사운드 정리1.csv)
    /// </summary>
    public static class SkillSoundHelper
    {
        /// <summary>
        /// 스킬 발동 시 재생할 사운드 키 (skill_id → sound_key)
        /// MainSkillTable.csv의 ID 기준 (스킬명 매칭)
        /// null = 자체 사운드 사용 또는 사운드 없음
        /// </summary>
        private static readonly Dictionary<int, string> SkillSoundMap = new Dictionary<int, string>
        {
            { 39001, "SFX_Vefects_Anime_Stylized_Dash" },              // 기본공격
            { 39002, "SFX_Vefects_Anime_Stylized_FireBall_Cast" },     // 파이어볼
            { 39003, "SFX_Vefects_Flipbook_Lightning_01" },            // 전기 충격
            { 39004, "SFX_Vefects_Anime_Stylized_Arrow_Shot_Cast" },   // 화살 공격
            { 39005, "SFX_Vefects_Water_Ground_Splash_Small" },        // 물폭탄
            { 39006, "SFX_Vefects_Shots_Squib_Concrete" },             // 결정화
            { 39007, "SFX_Vefects_Stylized_Slash_Fire" },              // 불꽃 파편
            { 39008, "SFX_Vefects_Anime_Stylized_Pick_Up" },           // 하트 블룸
            { 39009, "SFX_Vefects_Stylized_Magic_Attack_Ice_Hit" },    // 슈팅 스타
            { 39010, "Flash 13" },                                      // 팝팝
            { 39011, "SFX_Vefects_Space_Laser_Loop" },                 // 레이저 빔
            { 39012, "SFX_Vefects_Vignettes_Healing_Loop" },           // 프리즘 레이저
            { 39013, "SFX_Vefects_Vignettes_Charmed_Loop" },           // 소울
            { 39014, "SFX_Vefects_Blood_Slashes" },                    // 크림슨 볼트
            { 39015, "SFX_Vefects_Anime_Stylized_Dash" },              // 윈드 블레이드
            { 39016, "SFX_Vefects_Anime_Stylized_Lightning_Cast" },    // 솔라 코어
            { 39017, "Hit 13" },                                        // 트윙클 샷
            { 39018, "Flash 1" },                                       // 블랙 애로우
            { 39019, "Flash 8" },                                       // 녹빛 탄환
            { 39020, "Flash 4" },                                       // 앰버 샷
            { 39021, "SFX_Vefects_Flipbook_Explosion_Magic_03" },      // 블러드 오브
            { 39030, "SFX_Vefects_Space_Missile_Impact" },             // 운석 강림
            // AOE 스킬들 (x = 사운드 없음): 39022~39029
        };

        /// <summary>
        /// 피격 시 재생할 히트 사운드 키 (skill_id → hit_sound_key)
        /// MainSkillTable.csv의 ID 기준 (스킬명 매칭)
        /// null = 히트 사운드 없음
        /// </summary>
        private static readonly Dictionary<int, string> HitSoundMap = new Dictionary<int, string>
        {
            { 39001, "Hit 9" },                                        // 기본공격
            { 39002, "Hit 14" },                                       // 파이어볼
            { 39003, "Hit 8" },                                        // 전기 충격
            { 39004, "Hit 12" },                                       // 화살 공격
            { 39005, "Hit 5" },                                        // 물폭탄
            { 39006, "SFX_Vefects_Hit_05_One_Shot" },                  // 결정화
            { 39007, "SFX_Vefects_Stylized_Magic_Attack_Fire_Hit" },   // 불꽃 파편
            { 39008, "SFX_Vefects_Stylized_Magic_Attack_Sound_Hit" },  // 하트 블룸
            { 39009, "SFX_Vefects_Critical_Attack_01" },               // 슈팅 스타
            { 39010, "Hit 13" },                                        // 팝팝
            { 39011, "SFX_Vefects_Hit_07_Loop_01" },                   // 레이저 빔
            { 39012, "Hit 9" },                                         // 프리즘 레이저
            { 39013, "SFX_Vefects_Radial_Spilky_Hit_One_Shot_01" },    // 소울
            { 39014, "Hit 14" },                                        // 크림슨 볼트
            { 39015, "Hit 11" },                                        // 윈드 블레이드
            { 39016, "Hit 6" },                                         // 솔라 코어
            { 39017, "SFX_Vefects_Critical_Attack_01" },               // 트윙클 샷
            { 39018, "Hit 8" },                                         // 블랙 애로우
            { 39019, "Hit 15" },                                        // 녹빛 탄환
            { 39020, "Hit 4" },                                         // 앰버 샷
            { 39021, "SFX_Vefects_Flipbook_Impact_Blood_01" },         // 블러드 오브
            // AOE 스킬들 (x = 히트 사운드 없음): 39022~39030
        };

        /// <summary>
        /// 스킬 발동 사운드 키 반환
        /// </summary>
        /// <param name="skillId">스킬 ID</param>
        /// <returns>사운드 키 (없으면 null)</returns>
        public static string GetSkillSoundKey(int skillId)
        {
            return SkillSoundMap.TryGetValue(skillId, out string key) ? key : null;
        }

        /// <summary>
        /// 히트 사운드 키 반환
        /// </summary>
        /// <param name="skillId">스킬 ID</param>
        /// <returns>히트 사운드 키 (없으면 null)</returns>
        public static string GetHitSoundKey(int skillId)
        {
            return HitSoundMap.TryGetValue(skillId, out string key) ? key : null;
        }

        /// <summary>
        /// 스킬 발동 사운드가 있는지 확인
        /// </summary>
        public static bool HasSkillSound(int skillId)
        {
            return SkillSoundMap.ContainsKey(skillId);
        }

        /// <summary>
        /// 히트 사운드가 있는지 확인
        /// </summary>
        public static bool HasHitSound(int skillId)
        {
            return HitSoundMap.ContainsKey(skillId);
        }
    }
}
