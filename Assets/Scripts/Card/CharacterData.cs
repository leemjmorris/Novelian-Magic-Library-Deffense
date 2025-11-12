using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 캐릭터 정보를 담는 ScriptableObject
/// adventurer, Horror, jester, Romance, snoop 프리팹과 연동
/// </summary>
[CreateAssetMenu(fileName = "CharacterData", menuName = "Game/Character Data")]
public class CharacterData : ScriptableObject
{
    [Header("캐릭터 기본 정보")]
    public string characterName;      // 캐릭터 이름
    public GenreType genreType;       // 장르 타입 (1-5)

    [Header("캐릭터 스프라이트")]
    public Sprite characterSprite;    // 슬롯에 표시될 캐릭터 이미지

    [Header("프리팹")]
    public GameObject characterPrefab; // 캐릭터 프리팹 (Player 폴더의 프리팹)

    [Header("카드 정보")]
    public string cardDescription;    // 카드 설명 (선택사항)
}