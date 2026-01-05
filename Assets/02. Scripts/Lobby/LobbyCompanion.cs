using UnityEngine;
using Cysharp.Threading.Tasks;
using NovelianMagicLibraryDefense.Managers;
using NovelianMagicLibraryDefense.Audio;

namespace NovelianMagicLibraryDefense.Lobby
{
    /// <summary>
    /// 로비 동반자 캐릭터 동작 관리
    /// - 랜덤 워크 AI (Idle/Walk 전환)
    /// - 클릭 시 음성 재생 (_4 로비 대사)
    /// - 콜라이더 벽 충돌 시 방향 전환
    /// </summary>
    public class LobbyCompanion : MonoBehaviour
    {
        [Header("캐릭터 설정")]
        [SerializeField] private Animator animator;

        [Header("이동 설정")]
        [SerializeField] private float moveSpeed = 1.5f;
        [SerializeField] private float minIdleTime = 2f;
        [SerializeField] private float maxIdleTime = 5f;
        [SerializeField] private float minWalkTime = 2f;
        [SerializeField] private float maxWalkTime = 4f;

        [Header("상호작용 설정")]
        [SerializeField] private float voiceCooldown = 2f;

        // 내부 상태
        private int characterId;
        private bool isWalking;
        private Vector3 targetDirection;
        private float lastVoiceTime;
        private bool isInitialized;
        private Rigidbody rb;
        private Collider myCollider;

        // 이동 범위 제한 (LobbyCompanionManager에서 설정)
        private float minX, maxX, minZ, maxZ;

        private const string ANIM_IS_WALKING = "IsWalking";

        /// <summary>
        /// 캐릭터 초기화
        /// </summary>
        public void Initialize(int charId)
        {
            characterId = charId;

            // Rigidbody 추가 (물리 기반 이동)
            rb = GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = gameObject.AddComponent<Rigidbody>();
            }
            rb.isKinematic = false;
            rb.useGravity = true;
            rb.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionY; // 회전 고정 + Y 위치 고정

            // Collider 추가 (충돌 감지 + 터치 감지)
            myCollider = GetComponent<CapsuleCollider>();
            if (myCollider == null)
            {
                var capsule = gameObject.AddComponent<CapsuleCollider>();
                capsule.radius = 0.5f;
                capsule.height = 2f;
                capsule.center = new Vector3(0f, 1f, 0f);
                myCollider = capsule;
                Debug.Log($"<color=cyan>[LobbyCompanion]</color> CapsuleCollider 추가: CharacterID {charId}");
            }

            // Animator가 없으면 자동으로 찾기
            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>();
            }

            // 이동 범위 설정 (LobbyCompanionManager에서 가져오기)
            if (LobbyCompanionManager.Instance != null)
            {
                Vector4 bounds = LobbyCompanionManager.Instance.GetMovementBounds();
                minX = bounds.x;
                maxX = bounds.y;
                minZ = bounds.z;
                maxZ = bounds.w;
            }

            isInitialized = true;
            lastVoiceTime = -voiceCooldown; // 즉시 재생 가능하도록

            // InputManager 터치 이벤트 구독
            InputManager.OnShortPress += OnScreenTouched;

            // 랜덤 워크 시작
            StartRandomWalkLoop().Forget();

            Debug.Log($"<color=cyan>[LobbyCompanion]</color> 초기화 완료: CharacterID {characterId}");
        }

        /// <summary>
        /// 랜덤 워크 루프 (Idle ↔ Walk 반복)
        /// </summary>
        private async UniTaskVoid StartRandomWalkLoop()
        {
            while (isInitialized && this != null)
            {
                // Idle 상태
                await IdleState();

                // Walk 상태
                await WalkState();
            }
        }

        /// <summary>
        /// Idle 상태 (대기)
        /// </summary>
        private async UniTask IdleState()
        {
            isWalking = false;
            SetAnimatorWalking(false);

            float idleDuration = Random.Range(minIdleTime, maxIdleTime);
            await UniTask.Delay((int)(idleDuration * 1000), cancellationToken: this.GetCancellationTokenOnDestroy());
        }

        /// <summary>
        /// Walk 상태 (이동)
        /// </summary>
        private async UniTask WalkState()
        {
            isWalking = true;
            SetAnimatorWalking(true);

            // 랜덤 방향 설정
            targetDirection = new Vector3(
                Random.Range(-1f, 1f),
                0f,
                Random.Range(-1f, 1f)
            ).normalized;

            float walkDuration = Random.Range(minWalkTime, maxWalkTime);
            float elapsed = 0f;

            while (elapsed < walkDuration && isWalking)
            {
                // 이동 방향으로 캐릭터 회전 (매 프레임마다 부드럽게)
                if (targetDirection != Vector3.zero)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(targetDirection);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 0.2f);
                }

                // 이동
                Vector3 movement = targetDirection * moveSpeed * Time.deltaTime;
                Vector3 newPosition = transform.position + movement;

                // 이동 범위 체크 및 방향 전환
                bool outOfBounds = false;
                if (newPosition.x < minX || newPosition.x > maxX)
                {
                    targetDirection.x *= -1; // X축 반전
                    outOfBounds = true;
                }
                if (newPosition.z < minZ || newPosition.z > maxZ)
                {
                    targetDirection.z *= -1; // Z축 반전
                    outOfBounds = true;
                }

                if (!outOfBounds)
                {
                    rb.MovePosition(newPosition);
                }

                elapsed += Time.deltaTime;
                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken: this.GetCancellationTokenOnDestroy());
            }

            isWalking = false;
            SetAnimatorWalking(false);
        }

        /// <summary>
        /// 벽 충돌 시 방향 전환
        /// </summary>
        private void OnCollisionEnter(Collision collision)
        {
            // 벽에 부딪히면 반대 방향으로 전환
            if (isWalking)
            {
                targetDirection = Vector3.Reflect(targetDirection, collision.contacts[0].normal);
                targetDirection.y = 0f; // Y축 고정
                targetDirection.Normalize();

                // 즉시 회전
                if (targetDirection != Vector3.zero)
                {
                    transform.rotation = Quaternion.LookRotation(targetDirection);
                }
            }
        }

        /// <summary>
        /// 화면 터치 시 Raycast로 자신이 클릭됐는지 확인
        /// </summary>
        private void OnScreenTouched(Vector2 screenPosition)
        {
            if (!isInitialized) return;

            // 메인 카메라로 Raycast
            Camera mainCamera = Camera.main;
            if (mainCamera == null) return;

            Ray ray = mainCamera.ScreenPointToRay(screenPosition);
            RaycastHit[] hits = Physics.RaycastAll(ray, 100f);

            foreach (var hit in hits)
            {
                if (hit.collider == myCollider)
                {
                    OnCharacterTouched();
                    break;
                }
            }
        }

        /// <summary>
        /// 캐릭터가 터치됐을 때 처리
        /// </summary>
        private void OnCharacterTouched()
        {
            // 쿨타임 체크
            if (Time.time - lastVoiceTime < voiceCooldown)
            {
                Debug.Log($"<color=yellow>[LobbyCompanion]</color> 음성 쿨타임 중... 남은 시간: {voiceCooldown - (Time.time - lastVoiceTime):F1}초");
                return;
            }

            PlayLobbyVoice();
            lastVoiceTime = Time.time;
        }

        /// <summary>
        /// 로비 터치 음성 재생 (_4)
        /// </summary>
        private void PlayLobbyVoice()
        {
            if (AudioManager.Instance == null)
            {
                Debug.LogWarning("[LobbyCompanion] AudioManager not available");
                return;
            }

            string voiceKey = CharacterVoiceHelper.GetLobbyVoiceKey(characterId);

            if (string.IsNullOrEmpty(voiceKey))
            {
                Debug.LogWarning($"[LobbyCompanion] 음성 키를 찾을 수 없습니다. CharacterID: {characterId}");
                return;
            }

            Debug.Log($"<color=cyan>[LobbyCompanion]</color> 음성 재생: {voiceKey}");
            AudioManager.Instance.PlayVoice(voiceKey);
        }

        /// <summary>
        /// Animator Walk 상태 설정
        /// </summary>
        private void SetAnimatorWalking(bool walking)
        {
            if (animator != null)
            {
                animator.SetBool(ANIM_IS_WALKING, walking);
            }
        }

        private void OnDestroy()
        {
            isInitialized = false;

            // 이벤트 구독 해제
            InputManager.OnShortPress -= OnScreenTouched;
        }
    }
}
