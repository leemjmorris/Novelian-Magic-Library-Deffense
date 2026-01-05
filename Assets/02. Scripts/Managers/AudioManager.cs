using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Audio;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace NovelianMagicLibraryDefense.Managers
{
    /// <summary>
    /// AudioManager: Manages game BGM and SFX with volume control and Addressable loading
    /// Implements Singleton pattern for global access
    /// Features: BGM/SFX playback, volume control via Audio Mixer, settings persistence, Fade In/Out
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        private static AudioManager instance;
        public static AudioManager Instance => instance;

        [Header("Audio Mixer")]
        [SerializeField] private AudioMixer audioMixer;

        [Header("Audio Mixer Groups")]
        [SerializeField] private AudioMixerGroup bgmGroup;
        [SerializeField] private AudioMixerGroup sfxGroup;
        [SerializeField] private AudioMixerGroup voiceGroup;
        [SerializeField] private AudioMixerGroup skillGroup;

        [Header("Audio Sources")]
        [SerializeField] private AudioSource bgmSource;
        [SerializeField] private int sfxPoolSize = 10;

        private List<AudioSource> sfxPool = new List<AudioSource>();
        private List<AudioSource> skillPool = new List<AudioSource>(); // 스킬 효과음 전용 풀
        private AudioSource voiceSource; // 음성 전용 AudioSource (덱 장착 등 - 이전 음성 정지 후 재생)
        private Dictionary<string, AudioClip> loadedClips = new Dictionary<string, AudioClip>();
        private Dictionary<string, AsyncOperationHandle<AudioClip>> loadedHandles = new Dictionary<string, AsyncOperationHandle<AudioClip>>();
        private Dictionary<string, AsyncOperationHandle<AudioClip>> loadingHandles = new Dictionary<string, AsyncOperationHandle<AudioClip>>();

        // Volume settings (default to 1.0 = 100%)
        private float masterVolume = 1.0f;
        private float bgmVolume = 1.0f;
        private float sfxVolume = 1.0f;
        private float voiceVolume = 1.0f;
        private float skillVolume = 1.0f;

        // PlayerPrefs keys
        private const string MASTER_VOLUME_KEY = "MasterVolume";
        private const string BGM_VOLUME_KEY = "BGMVolume";
        private const string SFX_VOLUME_KEY = "SFXVolume";
        private const string VOICE_VOLUME_KEY = "VoiceVolume";
        private const string SKILL_VOLUME_KEY = "SkillVolume";

        // Audio Mixer parameter names
        private const string MASTER_VOLUME_PARAM = "MasterVolume";
        private const string BGM_VOLUME_PARAM = "BGMVolume";
        private const string SFX_VOLUME_PARAM = "SFXVolume";
        private const string VOICE_VOLUME_PARAM = "VoiceVolume";
        private const string SKILL_VOLUME_PARAM = "SkillVolume";

        // Current BGM tracking
        private string currentBGMName = "";
        public string CurrentBGMName => currentBGMName;
        public bool IsBGMPlaying => bgmSource != null && bgmSource.isPlaying;

        // Pause/Resume support
        private string pausedBGMName = "";
        private float pausedBGMTime = 0f;
        private AudioClip pausedBGMClip = null;
        public bool HasPausedBGM => !string.IsNullOrEmpty(pausedBGMName);

        // Fade cancellation
        private CancellationTokenSource bgmFadeCts;

        // Voice Queue System (캐릭터 대사 순차 재생)
        private Queue<string> voiceQueue = new Queue<string>();
        private bool isVoicePlaying = false;
        private CancellationTokenSource voiceQueueCts;
        private const float VOICE_DELAY = 0.5f; // 음성 간 딜레이 (초)

        private void Awake()
        {
            // Singleton pattern
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;

            // Move to root before DontDestroyOnLoad (required for DontDestroyOnLoad to work)
            transform.SetParent(null);
            DontDestroyOnLoad(gameObject);

            InitializeAudio();
        }

        private void Start()
        {
            // AudioMixer needs to be fully initialized before setting values
            // Awake timing can cause SetFloat to succeed but not actually apply
            ApplyAudioSettingsDelayed().Forget();
        }

        private async UniTaskVoid ApplyAudioSettingsDelayed()
        {
            // Wait one frame for AudioMixer to be fully ready
            await UniTask.Yield();

            // Re-apply the loaded settings
            SetMasterVolume(masterVolume);
            SetBGMVolume(bgmVolume);
            SetSFXVolume(sfxVolume);
            SetVoiceVolume(voiceVolume);
            SetSkillVolume(skillVolume);

            Debug.Log("[AudioManager] Audio settings re-applied after initialization");
        }

        /// <summary>
        /// Initialize audio system: create BGM source and SFX pool
        /// </summary>
        private void InitializeAudio()
        {
            // Create BGM AudioSource if not assigned
            if (bgmSource == null)
            {
                bgmSource = gameObject.AddComponent<AudioSource>();
                bgmSource.loop = true;
                bgmSource.playOnAwake = false;

                if (bgmGroup != null)
                {
                    bgmSource.outputAudioMixerGroup = bgmGroup;
                }
            }

            // Create SFX AudioSource pool
            CreateSFXPool();

            // Create Skill AudioSource pool (스킬 효과음 전용)
            CreateSkillPool();

            // Create Voice AudioSource (for exclusive voice playback)
            CreateVoiceSource();

            // Load saved volume settings
            LoadAudioSettings();

            Debug.Log("[AudioManager] Initialized successfully");
        }

        /// <summary>
        /// Create pool of AudioSources for simultaneous SFX playback
        /// </summary>
        private void CreateSFXPool()
        {
            for (int i = 0; i < sfxPoolSize; i++)
            {
                AudioSource sfxSource = gameObject.AddComponent<AudioSource>();
                sfxSource.loop = false;
                sfxSource.playOnAwake = false;

                if (sfxGroup != null)
                {
                    sfxSource.outputAudioMixerGroup = sfxGroup;
                }

                sfxPool.Add(sfxSource);
            }

            Debug.Log($"[AudioManager] Created SFX pool with {sfxPoolSize} AudioSources (Group: {(sfxGroup != null ? sfxGroup.name : "None")})");
        }

        /// <summary>
        /// Create pool of AudioSources for skill sound effects
        /// </summary>
        private void CreateSkillPool()
        {
            // 스킬 풀은 SFX 풀과 동일한 크기로 생성
            for (int i = 0; i < sfxPoolSize; i++)
            {
                AudioSource skillSource = gameObject.AddComponent<AudioSource>();
                skillSource.loop = false;
                skillSource.playOnAwake = false;

                if (skillGroup != null)
                {
                    skillSource.outputAudioMixerGroup = skillGroup;
                }

                skillPool.Add(skillSource);
            }

            Debug.Log($"[AudioManager] Created Skill pool with {sfxPoolSize} AudioSources (Group: {(skillGroup != null ? skillGroup.name : "None")})");
        }

        /// <summary>
        /// Create dedicated AudioSource for exclusive voice playback
        /// Stops previous voice before playing new one (used for deck equip, etc.)
        /// </summary>
        private void CreateVoiceSource()
        {
            voiceSource = gameObject.AddComponent<AudioSource>();
            voiceSource.loop = false;
            voiceSource.playOnAwake = false;

            if (voiceGroup != null)
            {
                voiceSource.outputAudioMixerGroup = voiceGroup;
            }

            Debug.Log($"[AudioManager] Created Voice AudioSource (Group: {(voiceGroup != null ? voiceGroup.name : "None")})");
        }

        #region BGM Control

        /// <summary>
        /// Play BGM by addressable key
        /// </summary>
        /// <param name="clipName">Addressable key for the audio clip</param>
        /// <param name="forceRestart">If true, restart even if same BGM is playing</param>
        public async void PlayBGM(string clipName, bool forceRestart = false)
        {
            // Skip if same BGM is already playing or transitioning to
            // Issue #605: bgmSource.isPlaying 체크 제거 - 페이드 중에도 중복 방지
            if (!forceRestart && currentBGMName == clipName)
            {
                Debug.Log($"[AudioManager] BGM already playing: {clipName}");
                return;
            }

            CancelBGMFade();

            AudioClip clip = await LoadAudioClipAsync(clipName);

            if (clip != null)
            {
                currentBGMName = clipName;
                bgmSource.clip = clip;
                bgmSource.volume = 1f;
                bgmSource.Play();
                Debug.Log($"[AudioManager] Playing BGM: {clipName}");
            }
        }

        /// <summary>
        /// Play BGM with fade in effect
        /// </summary>
        /// <param name="clipName">Addressable key for the audio clip</param>
        /// <param name="fadeDuration">Fade duration in seconds</param>
        /// <param name="forceRestart">If true, restart even if same BGM is playing</param>
        public async void PlayBGMWithFade(string clipName, float fadeDuration = 1f, bool forceRestart = false)
        {
            // Skip if same BGM is already playing or transitioning to
            // Issue #605: bgmSource.isPlaying 체크 제거 - 페이드 중에도 중복 방지
            if (!forceRestart && currentBGMName == clipName)
            {
                Debug.Log($"[AudioManager] BGM already playing: {clipName}");
                return;
            }

            CancelBGMFade();

            AudioClip clip = await LoadAudioClipAsync(clipName);

            if (clip != null)
            {
                currentBGMName = clipName;
                bgmSource.clip = clip;
                bgmSource.volume = 0f;
                bgmSource.Play();

                FadeInAsync(fadeDuration).Forget();
                Debug.Log($"[AudioManager] Playing BGM with fade in: {clipName}");
            }
        }

        /// <summary>
        /// Stop BGM immediately
        /// </summary>
        public void StopBGM()
        {
            CancelBGMFade();

            bgmSource.Stop();
            bgmSource.volume = 1f;
            currentBGMName = "";
            Debug.Log("[AudioManager] BGM stopped");
        }

        /// <summary>
        /// Stop BGM with fade out effect
        /// </summary>
        public void StopBGMWithFade(float fadeDuration = 1f)
        {
            CancelBGMFade();
            FadeOutAndStopAsync(fadeDuration).Forget();
            Debug.Log("[AudioManager] Stopping BGM with fade out");
        }

        /// <summary>
        /// Issue #605: 모든 사운드 정지 (BGM + SFX + Voice)
        /// 씬 전환 시 사용
        /// </summary>
        public void StopAllSounds()
        {
            // BGM 정지
            StopBGM();

            // SFX 정지
            StopAllSFX();

            // Voice 정지
            StopVoice();

            // Voice Queue 초기화
            ClearVoiceQueue();

            Debug.Log("[AudioManager] All sounds stopped");
        }

        /// <summary>
        /// 모든 SFX 정지 (BGM, Voice 제외)
        /// </summary>
        public void StopAllSFX()
        {
            // SFX 정지
            foreach (var sfxSource in sfxPool)
            {
                if (sfxSource != null && sfxSource.isPlaying)
                {
                    sfxSource.Stop();
                }
            }

            // Skill SFX 정지
            foreach (var skillSource in skillPool)
            {
                if (skillSource != null && skillSource.isPlaying)
                {
                    skillSource.Stop();
                }
            }

            Debug.Log("[AudioManager] All SFX stopped");
        }

        /// <summary>
        /// Crossfade from current BGM to new BGM
        /// </summary>
        /// <param name="clipName">Addressable key for the new audio clip</param>
        /// <param name="fadeDuration">Total crossfade duration in seconds</param>
        /// <param name="forceRestart">If true, crossfade even if same BGM is playing</param>
        public async void CrossfadeBGM(string clipName, float fadeDuration = 1f, bool forceRestart = false)
        {
            // Skip if same BGM is already playing or transitioning to
            // Issue #605: bgmSource.isPlaying 체크 제거 - 페이드 중에도 중복 방지
            if (!forceRestart && currentBGMName == clipName)
            {
                Debug.Log($"[AudioManager] BGM already playing: {clipName}");
                return;
            }

            CancelBGMFade();

            AudioClip clip = await LoadAudioClipAsync(clipName);

            if (clip != null)
            {
                CrossfadeAsync(clip, clipName, fadeDuration).Forget();
                Debug.Log($"[AudioManager] Crossfading to BGM: {clipName}");
            }
        }

        /// <summary>
        /// Cancel any ongoing BGM fade operation
        /// </summary>
        private void CancelBGMFade()
        {
            if (bgmFadeCts != null)
            {
                bgmFadeCts.Cancel();
                bgmFadeCts.Dispose();
                bgmFadeCts = null;
            }
        }

        /// <summary>
        /// Pause current BGM with fade out and play new BGM with fade in
        /// Used for Shop panel - pauses Lobby BGM and plays Shop BGM
        /// </summary>
        /// <param name="newClipName">Addressable key for the new BGM to play</param>
        /// <param name="fadeDuration">Fade duration in seconds</param>
        public async void PauseBGMAndPlay(string newClipName, float fadeDuration = 1f)
        {
            CancelBGMFade();

            // Save current BGM state before pausing
            if (bgmSource.isPlaying && bgmSource.clip != null)
            {
                pausedBGMName = currentBGMName;
                pausedBGMTime = bgmSource.time;
                pausedBGMClip = bgmSource.clip;
                Debug.Log($"[AudioManager] Saving BGM state: {pausedBGMName} at {pausedBGMTime:F2}s");
            }

            // Load new clip
            AudioClip newClip = await LoadAudioClipAsync(newClipName);
            if (newClip == null)
            {
                Debug.LogError($"[AudioManager] Failed to load new BGM: {newClipName}");
                return;
            }

            // Crossfade: current BGM fade out -> pause -> new BGM fade in
            PauseBGMAndPlayAsync(newClip, newClipName, fadeDuration).Forget();
        }

        /// <summary>
        /// Stop current BGM with fade out and resume paused BGM with fade in
        /// Used for Shop panel - stops Shop BGM and resumes Lobby BGM
        /// </summary>
        /// <param name="fadeDuration">Fade duration in seconds</param>
        public void StopAndResumePausedBGM(float fadeDuration = 1f)
        {
            if (!HasPausedBGM)
            {
                Debug.LogWarning("[AudioManager] No paused BGM to resume");
                return;
            }

            CancelBGMFade();
            StopAndResumePausedBGMAsync(fadeDuration).Forget();
        }

        #endregion

        #region SFX Control

        /// <summary>
        /// Play sound effect by addressable key
        /// Supports simultaneous playback using pooled AudioSources
        /// </summary>
        public async void PlaySFX(string clipName)
        {
            AudioClip clip = await LoadAudioClipAsync(clipName);

            if (clip != null)
            {
                AudioSource availableSource = GetAvailableSFXSource();

                if (availableSource != null)
                {
                    availableSource.PlayOneShot(clip);
                    Debug.Log($"[AudioManager] Playing SFX: {clipName}");
                }
                else
                {
                    Debug.LogWarning($"[AudioManager] No available SFX AudioSource for: {clipName}");
                }
            }
        }

        /// <summary>
        /// Play sound effect with volume adjustment
        /// </summary>
        public async void PlaySFX(string clipName, float volumeScale)
        {
            AudioClip clip = await LoadAudioClipAsync(clipName);

            if (clip != null)
            {
                AudioSource availableSource = GetAvailableSFXSource();

                if (availableSource != null)
                {
                    availableSource.PlayOneShot(clip, volumeScale);
                    Debug.Log($"[AudioManager] Playing SFX: {clipName} with volume: {volumeScale}");
                }
            }
        }

        /// <summary>
        /// Get available AudioSource from pool (not currently playing)
        /// </summary>
        private AudioSource GetAvailableSFXSource()
        {
            foreach (AudioSource source in sfxPool)
            {
                if (!source.isPlaying)
                {
                    return source;
                }
            }

            // If all sources are playing, return the first one (it will overlap)
            return sfxPool.Count > 0 ? sfxPool[0] : null;
        }

        #endregion

        #region Skill SFX Control

        /// <summary>
        /// Play skill sound effect by addressable key
        /// Uses dedicated Skill AudioMixer group for separate volume control
        /// </summary>
        public async void PlaySkillSFX(string clipName)
        {
            AudioClip clip = await LoadAudioClipAsync(clipName);

            if (clip != null)
            {
                AudioSource availableSource = GetAvailableSkillSource();

                if (availableSource != null)
                {
                    availableSource.PlayOneShot(clip);
                    Debug.Log($"[AudioManager] Playing Skill SFX: {clipName}");
                }
                else
                {
                    Debug.LogWarning($"[AudioManager] No available Skill AudioSource for: {clipName}");
                }
            }
        }

        /// <summary>
        /// Play skill sound effect with volume adjustment
        /// </summary>
        public async void PlaySkillSFX(string clipName, float volumeScale)
        {
            AudioClip clip = await LoadAudioClipAsync(clipName);

            if (clip != null)
            {
                AudioSource availableSource = GetAvailableSkillSource();

                if (availableSource != null)
                {
                    availableSource.PlayOneShot(clip, volumeScale);
                    Debug.Log($"[AudioManager] Playing Skill SFX: {clipName} with volume: {volumeScale}");
                }
            }
        }

        /// <summary>
        /// Get available AudioSource from skill pool (not currently playing)
        /// </summary>
        private AudioSource GetAvailableSkillSource()
        {
            foreach (AudioSource source in skillPool)
            {
                if (!source.isPlaying)
                {
                    return source;
                }
            }

            // If all sources are playing, return the first one (it will overlap)
            return skillPool.Count > 0 ? skillPool[0] : null;
        }

        #endregion

        #region Voice Control (Exclusive - 덱 장착 등)

        /// <summary>
        /// 음성 재생 (이전 음성 정지 후 새 음성 재생)
        /// 덱 장착 시 캐릭터 음성 등 겹치면 안 되는 경우 사용
        /// </summary>
        /// <param name="clipName">Addressable 키</param>
        public async void PlayVoice(string clipName)
        {
            if (string.IsNullOrEmpty(clipName)) return;

            AudioClip clip = await LoadAudioClipAsync(clipName);

            if (clip != null && voiceSource != null)
            {
                // 이전 음성 정지
                if (voiceSource.isPlaying)
                {
                    voiceSource.Stop();
                }

                // 새 음성 재생
                voiceSource.clip = clip;
                voiceSource.Play();
                Debug.Log($"[AudioManager] Playing voice (exclusive): {clipName}");
            }
        }

        /// <summary>
        /// 현재 음성 정지
        /// </summary>
        public void StopVoice()
        {
            if (voiceSource != null && voiceSource.isPlaying)
            {
                voiceSource.Stop();
                Debug.Log("[AudioManager] Voice stopped");
            }
        }

        #endregion

        #region Voice Queue (캐릭터 대사 순차 재생)

        /// <summary>
        /// 캐릭터 음성을 큐에 추가하고 순차 재생
        /// 동시 소환 시 겹치지 않고 순서대로 재생됨
        /// </summary>
        /// <param name="voiceKey">음성 Addressable 키 (예: "Greum_1")</param>
        public void EnqueueVoice(string voiceKey)
        {
            if (string.IsNullOrEmpty(voiceKey)) return;

            voiceQueue.Enqueue(voiceKey);
            Debug.Log($"[AudioManager] Voice enqueued: {voiceKey} (Queue size: {voiceQueue.Count})");

            // 큐 처리가 실행 중이 아니면 시작
            if (!isVoicePlaying)
            {
                ProcessVoiceQueueAsync().Forget();
            }
        }

        /// <summary>
        /// 음성 큐 순차 처리
        /// Issue #646: Voice 그룹으로 재생하도록 수정
        /// </summary>
        private async UniTaskVoid ProcessVoiceQueueAsync()
        {
            if (isVoicePlaying) return;

            isVoicePlaying = true;
            voiceQueueCts = new CancellationTokenSource();
            var token = voiceQueueCts.Token;

            try
            {
                while (voiceQueue.Count > 0)
                {
                    token.ThrowIfCancellationRequested();

                    string voiceKey = voiceQueue.Dequeue();
                    AudioClip clip = await LoadAudioClipAsync(voiceKey);

                    if (clip != null && voiceSource != null)
                    {
                        // Issue #646: Voice 그룹을 사용하는 voiceSource로 재생
                        voiceSource.PlayOneShot(clip);
                        Debug.Log($"[AudioManager] Playing voice (queue): {voiceKey}");

                        // 클립 재생 시간 + 딜레이만큼 대기
                        float waitTime = clip.length + VOICE_DELAY;
                        await UniTask.Delay((int)(waitTime * 1000), cancellationToken: token);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                Debug.Log("[AudioManager] Voice queue cancelled");
            }
            finally
            {
                isVoicePlaying = false;
                voiceQueueCts?.Dispose();
                voiceQueueCts = null;
            }
        }

        /// <summary>
        /// 음성 큐 초기화 (씬 전환 시 호출)
        /// </summary>
        public void ClearVoiceQueue()
        {
            voiceQueueCts?.Cancel();
            voiceQueue.Clear();
            isVoicePlaying = false;
            Debug.Log("[AudioManager] Voice queue cleared");
        }

        #endregion

        #region Volume Control

        /// <summary>
        /// Set master volume (0.0 to 1.0)
        /// </summary>
        public void SetMasterVolume(float volume)
        {
            masterVolume = Mathf.Clamp01(volume);

            if (audioMixer != null)
            {
                // Convert 0-1 to decibels (-80db to 0db)
                float db = masterVolume > 0 ? 20f * Mathf.Log10(masterVolume) : -80f;
                bool success = audioMixer.SetFloat(MASTER_VOLUME_PARAM, db);
                Debug.Log($"[AudioManager] SetMasterVolume: input={volume}, clamped={masterVolume}, db={db}, success={success}");
            }
            else
            {
                Debug.LogWarning("[AudioManager] audioMixer is null! Cannot set master volume.");
            }
        }

        /// <summary>
        /// Set BGM volume (0.0 to 1.0)
        /// </summary>
        public void SetBGMVolume(float volume)
        {
            bgmVolume = Mathf.Clamp01(volume);

            if (audioMixer != null)
            {
                float db = bgmVolume > 0 ? 20f * Mathf.Log10(bgmVolume) : -80f;
                bool success = audioMixer.SetFloat(BGM_VOLUME_PARAM, db);
                Debug.Log($"[AudioManager] SetBGMVolume: input={volume}, clamped={bgmVolume}, db={db}, success={success}");
            }
            else
            {
                Debug.LogWarning("[AudioManager] audioMixer is null! Cannot set BGM volume.");
            }
        }

        /// <summary>
        /// Set SFX volume (0.0 to 1.0)
        /// </summary>
        public void SetSFXVolume(float volume)
        {
            sfxVolume = Mathf.Clamp01(volume);

            if (audioMixer != null)
            {
                float db = sfxVolume > 0 ? 20f * Mathf.Log10(sfxVolume) : -80f;
                bool success = audioMixer.SetFloat(SFX_VOLUME_PARAM, db);
                Debug.Log($"[AudioManager] SetSFXVolume: input={volume}, clamped={sfxVolume}, db={db}, success={success}");
            }
            else
            {
                Debug.LogWarning("[AudioManager] audioMixer is null! Cannot set SFX volume.");
            }
        }

        /// <summary>
        /// Get current master volume
        /// </summary>
        public float GetMasterVolume() => masterVolume;

        /// <summary>
        /// Get current BGM volume
        /// </summary>
        public float GetBGMVolume() => bgmVolume;

        /// <summary>
        /// Get current SFX volume
        /// </summary>
        public float GetSFXVolume() => sfxVolume;

        /// <summary>
        /// Set Voice volume (0.0 to 1.0) - 캐릭터 대사 볼륨
        /// </summary>
        public void SetVoiceVolume(float volume)
        {
            voiceVolume = Mathf.Clamp01(volume);

            if (audioMixer != null)
            {
                float db = voiceVolume > 0 ? 20f * Mathf.Log10(voiceVolume) : -80f;
                bool success = audioMixer.SetFloat(VOICE_VOLUME_PARAM, db);
                Debug.Log($"[AudioManager] SetVoiceVolume: input={volume}, clamped={voiceVolume}, db={db}, success={success}");
            }
            else
            {
                Debug.LogWarning("[AudioManager] audioMixer is null! Cannot set voice volume.");
            }
        }

        /// <summary>
        /// Get current Voice volume
        /// </summary>
        public float GetVoiceVolume() => voiceVolume;

        /// <summary>
        /// Set Skill volume (0.0 to 1.0) - 스킬 효과음 볼륨
        /// </summary>
        public void SetSkillVolume(float volume)
        {
            skillVolume = Mathf.Clamp01(volume);

            if (audioMixer != null)
            {
                float db = skillVolume > 0 ? 20f * Mathf.Log10(skillVolume) : -80f;
                bool success = audioMixer.SetFloat(SKILL_VOLUME_PARAM, db);
                Debug.Log($"[AudioManager] SetSkillVolume: input={volume}, clamped={skillVolume}, db={db}, success={success}");
            }
            else
            {
                Debug.LogWarning("[AudioManager] audioMixer is null! Cannot set skill volume.");
            }
        }

        /// <summary>
        /// Get current Skill volume
        /// </summary>
        public float GetSkillVolume() => skillVolume;

        #endregion

        #region Settings Persistence

        /// <summary>
        /// Save current volume settings to PlayerPrefs
        /// </summary>
        public void SaveAudioSettings()
        {
            PlayerPrefs.SetFloat(MASTER_VOLUME_KEY, masterVolume);
            PlayerPrefs.SetFloat(BGM_VOLUME_KEY, bgmVolume);
            PlayerPrefs.SetFloat(SFX_VOLUME_KEY, sfxVolume);
            PlayerPrefs.SetFloat(VOICE_VOLUME_KEY, voiceVolume);
            PlayerPrefs.SetFloat(SKILL_VOLUME_KEY, skillVolume);
            PlayerPrefs.Save();

            Debug.Log("[AudioManager] Audio settings saved");
        }

        /// <summary>
        /// Load volume settings from PlayerPrefs
        /// If no saved settings exist, use Audio Mixer's current values as defaults
        /// </summary>
        public void LoadAudioSettings()
        {
            // Audio Mixer에서 현재 값을 읽어와서 기본값으로 사용
            float defaultMaster = GetMixerVolumeAsLinear(MASTER_VOLUME_PARAM);
            float defaultBGM = GetMixerVolumeAsLinear(BGM_VOLUME_PARAM);
            float defaultSFX = GetMixerVolumeAsLinear(SFX_VOLUME_PARAM);
            float defaultVoice = GetMixerVolumeAsLinear(VOICE_VOLUME_PARAM);
            float defaultSkill = GetMixerVolumeAsLinear(SKILL_VOLUME_PARAM);

            // PlayerPrefs에 저장된 값이 있으면 사용, 없으면 Mixer 기본값 사용
            // HasKey로 명시적으로 저장된 값이 있는지 확인
            masterVolume = PlayerPrefs.HasKey(MASTER_VOLUME_KEY)
                ? PlayerPrefs.GetFloat(MASTER_VOLUME_KEY)
                : defaultMaster;
            bgmVolume = PlayerPrefs.HasKey(BGM_VOLUME_KEY)
                ? PlayerPrefs.GetFloat(BGM_VOLUME_KEY)
                : defaultBGM;
            sfxVolume = PlayerPrefs.HasKey(SFX_VOLUME_KEY)
                ? PlayerPrefs.GetFloat(SFX_VOLUME_KEY)
                : defaultSFX;
            voiceVolume = PlayerPrefs.HasKey(VOICE_VOLUME_KEY)
                ? PlayerPrefs.GetFloat(VOICE_VOLUME_KEY)
                : defaultVoice;
            skillVolume = PlayerPrefs.HasKey(SKILL_VOLUME_KEY)
                ? PlayerPrefs.GetFloat(SKILL_VOLUME_KEY)
                : defaultSkill;

            // Apply loaded settings
            SetMasterVolume(masterVolume);
            SetBGMVolume(bgmVolume);
            SetSFXVolume(sfxVolume);
            SetVoiceVolume(voiceVolume);
            SetSkillVolume(skillVolume);

            Debug.Log($"[AudioManager] Audio settings loaded - Master: {masterVolume:F2}, BGM: {bgmVolume:F2}, SFX: {sfxVolume:F2}, Voice: {voiceVolume:F2}, Skill: {skillVolume:F2}");
        }

        /// <summary>
        /// Audio Mixer에서 현재 dB 값을 읽어 0~1 범위의 linear 값으로 변환
        /// </summary>
        private float GetMixerVolumeAsLinear(string paramName)
        {
            if (audioMixer == null) return 1.0f;

            if (audioMixer.GetFloat(paramName, out float dbValue))
            {
                // dB를 linear로 변환: 10^(dB/20)
                // -80dB = 0, 0dB = 1
                if (dbValue <= -80f) return 0f;
                return Mathf.Pow(10f, dbValue / 20f);
            }

            return 1.0f; // 읽기 실패 시 기본값
        }

        #endregion

        #region Addressable Loading

        /// <summary>
        /// Load AudioClip from Addressables asynchronously
        /// Caches loaded clips to avoid duplicate loading
        /// </summary>
        private async UniTask<AudioClip> LoadAudioClipAsync(string clipName)
        {
            // Check if already loaded
            if (loadedClips.TryGetValue(clipName, out AudioClip cachedClip))
            {
                return cachedClip;
            }

            // Check if currently loading
            if (loadingHandles.TryGetValue(clipName, out var loadingHandle))
            {
                await loadingHandle.Task;
                return loadedClips.TryGetValue(clipName, out var clip) ? clip : null;
            }

            try
            {
                AsyncOperationHandle<AudioClip> handle = Addressables.LoadAssetAsync<AudioClip>(clipName);
                loadingHandles[clipName] = handle;

                await handle.Task;

                if (handle.Status == AsyncOperationStatus.Succeeded)
                {
                    loadedClips[clipName] = handle.Result;
                    loadedHandles[clipName] = handle; // Store handle for proper release
                    Debug.Log($"[AudioManager] Loaded audio clip: {clipName}");
                    return handle.Result;
                }
                else
                {
                    Debug.LogError($"[AudioManager] Failed to load audio clip: {clipName}");
                    Addressables.Release(handle);
                    return null;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[AudioManager] Error loading audio clip '{clipName}': {e.Message}");
                return null;
            }
            finally
            {
                loadingHandles.Remove(clipName);
            }
        }

        /// <summary>
        /// Unload a specific audio clip from memory
        /// </summary>
        public void UnloadAudioClip(string clipName)
        {
            if (loadedHandles.TryGetValue(clipName, out var handle))
            {
                if (handle.IsValid())
                {
                    Addressables.Release(handle);
                }
                loadedHandles.Remove(clipName);
                loadedClips.Remove(clipName);
                Debug.Log($"[AudioManager] Unloaded audio clip: {clipName}");
            }
        }

        /// <summary>
        /// Unload all cached audio clips from memory
        /// </summary>
        public void UnloadAllAudioClips()
        {
            foreach (var kvp in loadedHandles)
            {
                if (kvp.Value.IsValid())
                {
                    Addressables.Release(kvp.Value);
                }
            }
            loadedHandles.Clear();
            loadedClips.Clear();
            Debug.Log("[AudioManager] Unloaded all audio clips");
        }

        #endregion

        #region Fade Effects (UniTask)

        /// <summary>
        /// Fade in BGM volume using UniTask
        /// </summary>
        private async UniTaskVoid FadeInAsync(float duration)
        {
            bgmFadeCts = new CancellationTokenSource();
            var token = bgmFadeCts.Token;

            float elapsed = 0f;
            bgmSource.volume = 0f;

            try
            {
                while (elapsed < duration)
                {
                    token.ThrowIfCancellationRequested();
                    elapsed += Time.deltaTime;
                    bgmSource.volume = Mathf.Lerp(0f, 1f, elapsed / duration);
                    await UniTask.Yield(PlayerLoopTiming.Update, token);
                }
                bgmSource.volume = 1f;
            }
            catch (OperationCanceledException)
            {
                // Fade was cancelled, do nothing
            }
        }

        /// <summary>
        /// Fade out BGM volume and stop using UniTask
        /// </summary>
        private async UniTaskVoid FadeOutAndStopAsync(float duration)
        {
            bgmFadeCts = new CancellationTokenSource();
            var token = bgmFadeCts.Token;

            float startVolume = bgmSource.volume;
            float elapsed = 0f;

            try
            {
                while (elapsed < duration)
                {
                    token.ThrowIfCancellationRequested();
                    elapsed += Time.deltaTime;
                    bgmSource.volume = Mathf.Lerp(startVolume, 0f, elapsed / duration);
                    await UniTask.Yield(PlayerLoopTiming.Update, token);
                }

                bgmSource.volume = 0f;
                bgmSource.Stop();
                bgmSource.volume = 1f;
                currentBGMName = "";
            }
            catch (OperationCanceledException)
            {
                // Fade was cancelled, do nothing
            }
        }

        /// <summary>
        /// Crossfade from current BGM to new BGM using UniTask
        /// </summary>
        private async UniTaskVoid CrossfadeAsync(AudioClip newClip, string newClipName, float duration)
        {
            bgmFadeCts = new CancellationTokenSource();
            var token = bgmFadeCts.Token;

            float halfDuration = duration / 2f;
            float startVolume = bgmSource.volume;
            float elapsed = 0f;

            try
            {
                // Fade out current BGM
                while (elapsed < halfDuration)
                {
                    token.ThrowIfCancellationRequested();
                    elapsed += Time.deltaTime;
                    bgmSource.volume = Mathf.Lerp(startVolume, 0f, elapsed / halfDuration);
                    await UniTask.Yield(PlayerLoopTiming.Update, token);
                }

                bgmSource.Stop();
                bgmSource.volume = 0f;

                // Set new clip and fade in
                currentBGMName = newClipName;
                bgmSource.clip = newClip;
                bgmSource.Play();

                elapsed = 0f;
                while (elapsed < halfDuration)
                {
                    token.ThrowIfCancellationRequested();
                    elapsed += Time.deltaTime;
                    bgmSource.volume = Mathf.Lerp(0f, 1f, elapsed / halfDuration);
                    await UniTask.Yield(PlayerLoopTiming.Update, token);
                }

                bgmSource.volume = 1f;
            }
            catch (OperationCanceledException)
            {
                // Fade was cancelled, do nothing
            }
        }

        /// <summary>
        /// Pause current BGM with fade out and play new BGM with fade in
        /// Saves the current playback position for later resumption
        /// </summary>
        private async UniTaskVoid PauseBGMAndPlayAsync(AudioClip newClip, string newClipName, float duration)
        {
            bgmFadeCts = new CancellationTokenSource();
            var token = bgmFadeCts.Token;

            float halfDuration = duration / 2f;
            float startVolume = bgmSource.volume;
            float elapsed = 0f;

            try
            {
                // Fade out current BGM
                while (elapsed < halfDuration)
                {
                    token.ThrowIfCancellationRequested();
                    elapsed += Time.deltaTime;
                    bgmSource.volume = Mathf.Lerp(startVolume, 0f, elapsed / halfDuration);
                    await UniTask.Yield(PlayerLoopTiming.Update, token);
                }

                // Pause (not stop) - save exact position
                pausedBGMTime = bgmSource.time;
                bgmSource.Pause();
                bgmSource.volume = 0f;
                Debug.Log($"[AudioManager] BGM paused at {pausedBGMTime:F2}s");

                // Set new clip and fade in
                currentBGMName = newClipName;
                bgmSource.clip = newClip;
                bgmSource.time = 0f;
                bgmSource.Play();

                elapsed = 0f;
                while (elapsed < halfDuration)
                {
                    token.ThrowIfCancellationRequested();
                    elapsed += Time.deltaTime;
                    bgmSource.volume = Mathf.Lerp(0f, 1f, elapsed / halfDuration);
                    await UniTask.Yield(PlayerLoopTiming.Update, token);
                }

                bgmSource.volume = 1f;
                Debug.Log($"[AudioManager] Now playing: {newClipName}");
            }
            catch (OperationCanceledException)
            {
                // Fade was cancelled, do nothing
            }
        }

        /// <summary>
        /// Stop current BGM with fade out and resume paused BGM with fade in
        /// Resumes from the saved playback position
        /// </summary>
        private async UniTaskVoid StopAndResumePausedBGMAsync(float duration)
        {
            bgmFadeCts = new CancellationTokenSource();
            var token = bgmFadeCts.Token;

            float halfDuration = duration / 2f;
            float startVolume = bgmSource.volume;
            float elapsed = 0f;

            try
            {
                // Fade out current BGM (Shop BGM)
                while (elapsed < halfDuration)
                {
                    token.ThrowIfCancellationRequested();
                    elapsed += Time.deltaTime;
                    bgmSource.volume = Mathf.Lerp(startVolume, 0f, elapsed / halfDuration);
                    await UniTask.Yield(PlayerLoopTiming.Update, token);
                }

                bgmSource.Stop();
                bgmSource.volume = 0f;

                // Restore paused BGM
                currentBGMName = pausedBGMName;
                bgmSource.clip = pausedBGMClip;
                bgmSource.time = pausedBGMTime;
                bgmSource.Play();
                Debug.Log($"[AudioManager] Resuming BGM: {pausedBGMName} from {pausedBGMTime:F2}s");

                // Clear paused state
                pausedBGMName = "";
                pausedBGMTime = 0f;
                pausedBGMClip = null;

                // Fade in resumed BGM
                elapsed = 0f;
                while (elapsed < halfDuration)
                {
                    token.ThrowIfCancellationRequested();
                    elapsed += Time.deltaTime;
                    bgmSource.volume = Mathf.Lerp(0f, 1f, elapsed / halfDuration);
                    await UniTask.Yield(PlayerLoopTiming.Update, token);
                }

                bgmSource.volume = 1f;
            }
            catch (OperationCanceledException)
            {
                // Fade was cancelled, do nothing
            }
        }

        #endregion

        #region Cleanup

        private void OnDestroy()
        {
            if (instance != this) return;

            // Cancel any ongoing fade
            CancelBGMFade();

            // Release all loaded AudioClips
            foreach (var handle in loadedHandles.Values)
            {
                if (handle.IsValid())
                {
                    Addressables.Release(handle);
                }
            }

            // Release any still-loading handles
            foreach (var handle in loadingHandles.Values)
            {
                if (handle.IsValid())
                {
                    Addressables.Release(handle);
                }
            }

            loadedClips.Clear();
            loadedHandles.Clear();
            loadingHandles.Clear();

            Debug.Log("[AudioManager] Cleaned up and released resources");
        }

        #endregion
    }
}
