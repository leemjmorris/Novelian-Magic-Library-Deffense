using System.Collections.Generic;
using UnityEngine;

namespace NovelianMagicLibraryDefense.Audio
{
    /// <summary>
    /// 캐릭터 ID → 음성 Addressable 키 매핑 헬퍼
    /// Path_ID 기반으로 캐릭터별 음성 키를 반환
    /// </summary>
    public static class CharacterVoiceHelper
    {
        // Path_ID → 음성 Addressable 키 베이스 이름
        private static readonly Dictionary<int, string> VoiceKeyMap = new Dictionary<int, string>
        {
            { 850001, "Greum" },       // 그믐
            { 850002, "Tenebra" },     // 테네브라
            { 850003, "Whisper" },     // 위스퍼
            { 850004, "Nyemryeong" },  // 념령
            { 850005, "Mir" },         // 미르
            { 850006, "Cor" },         // 코르
            { 850007, "Serin" },       // 세린
            { 850008, "Libra" },       // 리브라
            { 850009, "Nuri" },        // 누리
            { 850010, "Iter" },        // 이테르
            { 850011, "Trail" },       // 트레일
            { 850012, "Rumen" },       // 루멘
            { 850013, "Dodam" },       // 도담
            { 850014, "Rudo" },        // 루도
            { 850015, "Punch" },       // 펀치
            { 850016, "Tobi" },        // 토비
            { 850017, "Io" },          // 이오
            { 850018, "Ratio" },       // 라티오
            { 850019, "Ki" },          // 키
            { 850020, "Verita" },      // 베리타
        };

        /// <summary>
        /// Character_ID로 소환 음성 Addressable 키 반환 (항상 _1)
        /// </summary>
        /// <param name="characterId">CharacterTable의 Character_ID</param>
        /// <returns>음성 Addressable 키 (예: "Greum_1"), 실패 시 null</returns>
        public static string GetSummonVoiceKey(int characterId)
        {
            string baseKey = GetVoiceKeyBaseByCharacterId(characterId);
            return baseKey != null ? $"{baseKey}_1" : null;
        }

        /// <summary>
        /// Character_ID로 덱 장착 음성 Addressable 키 반환 (항상 _2)
        /// </summary>
        /// <param name="characterId">CharacterTable의 Character_ID</param>
        /// <returns>음성 Addressable 키 (예: "Greum_2"), 실패 시 null</returns>
        public static string GetEquipVoiceKey(int characterId)
        {
            string baseKey = GetVoiceKeyBaseByCharacterId(characterId);
            return baseKey != null ? $"{baseKey}_2" : null;
        }

        /// <summary>
        /// Character_ID로 3성 달성 음성 Addressable 키 반환 (항상 _3)
        /// </summary>
        /// <param name="characterId">CharacterTable의 Character_ID</param>
        /// <returns>음성 Addressable 키 (예: "Greum_3"), 실패 시 null</returns>
        public static string GetThreeStarVoiceKey(int characterId)
        {
            string baseKey = GetVoiceKeyBaseByCharacterId(characterId);
            return baseKey != null ? $"{baseKey}_3" : null;
        }

        /// <summary>
        /// Character_ID로 로비 터치 음성 Addressable 키 반환 (항상 _4)
        /// </summary>
        /// <param name="characterId">CharacterTable의 Character_ID</param>
        /// <returns>음성 Addressable 키 (예: "Greum_4"), 실패 시 null</returns>
        public static string GetLobbyVoiceKey(int characterId)
        {
            string baseKey = GetVoiceKeyBaseByCharacterId(characterId);
            return baseKey != null ? $"{baseKey}_4" : null;
        }

        /// <summary>
        /// Character_ID로 음성 키 베이스 조회 (내부 헬퍼)
        /// </summary>
        private static string GetVoiceKeyBaseByCharacterId(int characterId)
        {
            if (CSVLoader.Instance == null || !CSVLoader.Instance.IsInit)
            {
                Debug.LogWarning("[CharacterVoiceHelper] CSVLoader not initialized");
                return null;
            }

            var characterData = CSVLoader.Instance.GetData<CharacterData>(characterId);
            if (characterData == null)
            {
                Debug.LogWarning($"[CharacterVoiceHelper] CharacterData not found for ID: {characterId}");
                return null;
            }

            if (!VoiceKeyMap.TryGetValue(characterData.Path_ID, out string baseKey))
            {
                Debug.LogWarning($"[CharacterVoiceHelper] Voice key not found for Path_ID: {characterData.Path_ID}");
                return null;
            }

            return baseKey;
        }

        /// <summary>
        /// [Deprecated] 랜덤 음성 키 반환 - GetSummonVoiceKey 또는 GetEquipVoiceKey 사용 권장
        /// </summary>
        public static string GetRandomVoiceKey(int characterId)
        {
            return GetSummonVoiceKey(characterId);
        }

        /// <summary>
        /// Path_ID로 직접 음성 키 베이스 조회
        /// </summary>
        public static string GetVoiceKeyBase(int pathId)
        {
            return VoiceKeyMap.TryGetValue(pathId, out string baseKey) ? baseKey : null;
        }
    }
}
