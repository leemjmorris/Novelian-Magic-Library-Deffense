using System;
using CsvHelper.Configuration.Attributes;

/// <summary>
/// CharacterVisualConfigTable.csv 데이터 클래스
/// 캐릭터별 비주얼 파츠 설정 (Issue #356)
/// </summary>
[Serializable]
public class CharacterVisualConfigData
{
    [Name("character_id")]
    public int character_id { get; set; }

    [Name("body_prefab")]
    [Optional]
    public string BodyPrefabPath { get; set; }

    [Name("head_prefab")]
    [Optional]
    public string HeadPrefabPath { get; set; }

    [Name("hair_prefab")]
    [Optional]
    public string HairPrefabPath { get; set; }

    [Name("face_prefab")]
    [Optional]
    public string FacePrefabPath { get; set; }

    [Name("weapon_r_prefab")]
    [Optional]
    public string WeaponRightPrefabPath { get; set; }

    [Name("weapon_l_prefab")]
    [Optional]
    public string WeaponLeftPrefabPath { get; set; }

    [Name("accessory_prefab")]
    [Optional]
    public string AccessoryPrefabPath { get; set; }

    // CSV의 //로 시작하는 주석 컬럼 (선택적)
    [Name("//character_name")]
    [Optional]
    public string character_name { get; set; }

    [Name("//description")]
    [Optional]
    public string description { get; set; }
}
