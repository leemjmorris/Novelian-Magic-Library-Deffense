using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 개별 플레이어 슬롯을 관리
/// 선택된 캐릭터의 스프라이트를 표시
/// </summary>
public class PlayerSlot : MonoBehaviour
{
    [Header("슬롯 정보")]
    public int slotIndex;             // 슬롯 번호 (0-9)
    public bool isOccupied = false;   // 슬롯이 차있는지 여부
    
    [Header("UI 요소")]
    public Image characterImage;      // 캐릭터 이미지를 표시할 Image (자식 Image)
    public Image slotBackgroundImage; // 슬롯 배경 이미지 (PlayerSlot 자체의 Image)
    public GameObject emptySlotVisual; // 빈 슬롯 표시 (선택사항)
    
    [Header("현재 캐릭터")]
    private GenreType currentGenreType; // 현재 슬롯의 장르 타입

    void Start()
    {
        // slotBackgroundImage 자동 설정 - PlayerSlot 자체의 Image
        if (slotBackgroundImage == null)
        {
            slotBackgroundImage = GetComponent<Image>();
        }

        // characterImage가 설정되지 않았으면 찾기 - 자식 Image만
        if (characterImage == null)
        {
            // 자식들 중에서 "Image"라는 이름을 가진 GameObject 찾기
            Transform imageTransform = transform.Find("Image");
            if (imageTransform != null)
            {
                characterImage = imageTransform.GetComponent<Image>();
            }

            // 못 찾았으면 자식에서 찾기 (자신은 제외)
            if (characterImage == null)
            {
                Image[] images = GetComponentsInChildren<Image>();
                foreach (Image img in images)
                {
                    if (img.gameObject != gameObject)
                    {
                        characterImage = img;
                        break;
                    }
                }
            }

            if (characterImage != null)
            {
                Debug.Log($"슬롯 {slotIndex}: Character Image 자동 연결됨 - {characterImage.gameObject.name}");
            }
            else
            {
                Debug.LogError($"슬롯 {slotIndex}: Character Image 컴포넌트를 찾을 수 없습니다!");
            }
        }

        // 슬롯 배경은 항상 활성화 상태 유지
        if (slotBackgroundImage != null)
        {
            slotBackgroundImage.enabled = true;
            slotBackgroundImage.gameObject.SetActive(true);
        }

        // 자식 Image GameObject는 런타임 시작 시 비활성화
        if (characterImage != null && characterImage.gameObject != gameObject)
        {
            characterImage.gameObject.SetActive(false);
        }

        UpdateSlotVisual();
    }

    /// <summary>
    /// 슬롯에 캐릭터 스프라이트 배치 (간소화 버전)
    /// </summary>
    public void AssignCharacterSprite(Sprite characterSprite, GenreType genreType)
    {
        if (characterSprite == null)
        {
            Debug.LogWarning("캐릭터 스프라이트가 null입니다!");
            return;
        }

        currentGenreType = genreType;
        isOccupied = true;

        // 슬롯 배경은 항상 활성화 및 보이는 상태 유지
        if (slotBackgroundImage != null)
        {
            slotBackgroundImage.enabled = true;
            slotBackgroundImage.gameObject.SetActive(true);
        }

        // 캐릭터 스프라이트 설정
        if (characterImage != null)
        {
            characterImage.sprite = characterSprite;
            characterImage.enabled = true;
            characterImage.color = new Color(1, 1, 1, 1); // 완전 불투명

            // GameObject 활성화
            characterImage.gameObject.SetActive(true);

            // Raycast Target 비활성화 - 클릭이 슬롯 배경까지 전달되도록
            characterImage.raycastTarget = false;

            // RectTransform 확인
            RectTransform rect = characterImage.GetComponent<RectTransform>();
            if (rect != null)
            {
                Debug.Log($"슬롯 {slotIndex} Image 크기: {rect.rect.width}x{rect.rect.height}");
            }

            Debug.Log($"슬롯 {slotIndex}에 장르 {genreType} 캐릭터 배치됨 - Image active: {characterImage.gameObject.activeSelf}, enabled: {characterImage.enabled}");
        }

        // PlayerSlot GameObject 자체는 항상 활성화 상태 유지
        gameObject.SetActive(true);

        UpdateSlotVisual();
    }
    
    /// <summary>
    /// 슬롯 비우기
    /// </summary>
    public void ClearSlot()
    {
        isOccupied = false;

        if (characterImage != null)
        {
            characterImage.sprite = null;
            characterImage.enabled = false;
        }

        UpdateSlotVisual();
    }
    
    /// <summary>
    /// 슬롯 비주얼 업데이트
    /// </summary>
    void UpdateSlotVisual()
    {
        // 슬롯 배경(slotBackgroundImage)은 항상 보이도록 유지
        if (slotBackgroundImage != null)
        {
            slotBackgroundImage.enabled = true;
            slotBackgroundImage.gameObject.SetActive(true);
        }

        // emptySlotVisual은 항상 활성화 상태 유지 (반투명 배경)
        // 제거: emptySlotVisual.SetActive(!isOccupied);

        // 자식 Image GameObject만 활성화/비활성화 (PlayerSlot 자체는 항상 활성화)
        if (characterImage != null && characterImage.gameObject != gameObject)
        {
            characterImage.gameObject.SetActive(isOccupied);
        }
    }
    
    /// <summary>
    /// 슬롯이 비어있는지 확인
    /// </summary>
    public bool IsEmpty()
    {
        return !isOccupied;
    }
    
    /// <summary>
    /// 현재 장르 타입 반환
    /// </summary>
    public GenreType GetGenreType()
    {
        return currentGenreType;
    }
}