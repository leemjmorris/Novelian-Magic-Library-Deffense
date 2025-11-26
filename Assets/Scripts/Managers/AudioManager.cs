using System;
using System.Collections;
using System.Collections.Generic;
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

        [Header("Audio Sources")]
        [SerializeField] private AudioSource bgmSource;
        [SerializeField] private int sfxPoolSize = 10;

        private List<AudioSource> sfxPool = new List<AudioSource>();
        private Dictionary<string, AudioClip> loadedClips = new Dictionary<string, AudioClip>();
        private Dictionary<string, AsyncOperationHandle<AudioClip>> loadingHandles = new Dictionary<string, AsyncOperationHandle<AudioClip>>();

        // Volume settings (default to 1.0 = 100%)
        private float masterVolume = 1.0f;
        private float bgmVolume = 1.0f;
        private float sfxVolume = 1.0f;

        // PlayerPrefs keys
        private const string MASTER_VOLUME_KEY = "MasterVolume";
        private const string BGM_VOLUME_KEY = "BGMVolume";
        private const string SFX_VOLUME_KEY = "SFXVolume";

        // Audio Mixer parameter names
        private const string MASTER_VOLUME_PARAM = "MasterVolume";
        private const string BGM_VOLUME_PARAM = "BGMVolume";
        private const string SFX_VOLUME_PARAM = "SFXVolume";

        // Fade coroutine
        private Coroutine bgmFadeCoroutine;

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

                if (audioMixer != null)
                {
                    AudioMixerGroup[] groups = audioMixer.FindMatchingGroups("BGM");
                    if (groups.Length > 0)
                    {
                        bgmSource.outputAudioMixerGroup = groups[0];
                    }
                }
            }

            // Create SFX AudioSource pool
            CreateSFXPool();

            // Load saved volume settings
            LoadAudioSettings();

            Debug.Log("[AudioManager] Initialized successfully");
        }

        /// <summary>
        /// Create pool of AudioSources for simultaneous SFX playback
        /// </summary>
        private void CreateSFXPool()
        {
            AudioMixerGroup sfxGroup = null;

            if (audioMixer != null)
            {
                AudioMixerGroup[] groups = audioMixer.FindMatchingGroups("SFX");
                if (groups.Length > 0)
                {
                    sfxGroup = groups[0];
                }
            }

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

            Debug.Log($"[AudioManager] Created SFX pool with {sfxPoolSize} AudioSources");
        }

        #region BGM Control

        /// <summary>
        /// Play BGM by addressable key
        /// </summary>
        public async void PlayBGM(string clipName)
        {
            AudioClip clip = await LoadAudioClipAsync(clipName);

            if (clip != null)
            {
                bgmSource.clip = clip;
                bgmSource.Play();
                Debug.Log($"[AudioManager] Playing BGM: {clipName}");
            }
        }

        /// <summary>
        /// Play BGM with fade in effect
        /// </summary>
        public async void PlayBGMWithFade(string clipName, float fadeDuration = 1f)
        {
            AudioClip clip = await LoadAudioClipAsync(clipName);

            if (clip != null)
            {
                // Stop any ongoing fade
                if (bgmFadeCoroutine != null)
                {
                    StopCoroutine(bgmFadeCoroutine);
                }

                bgmSource.clip = clip;
                bgmSource.volume = 0f;
                bgmSource.Play();

                bgmFadeCoroutine = StartCoroutine(FadeIn(bgmSource, fadeDuration));
                Debug.Log($"[AudioManager] Playing BGM with fade in: {clipName}");
            }
        }

        /// <summary>
        /// Stop BGM immediately
        /// </summary>
        public void StopBGM()
        {
            if (bgmFadeCoroutine != null)
            {
                StopCoroutine(bgmFadeCoroutine);
                bgmFadeCoroutine = null;
            }

            bgmSource.Stop();
            Debug.Log("[AudioManager] BGM stopped");
        }

        /// <summary>
        /// Stop BGM with fade out effect
        /// </summary>
        public void StopBGMWithFade(float fadeDuration = 1f)
        {
            if (bgmFadeCoroutine != null)
            {
                StopCoroutine(bgmFadeCoroutine);
            }

            bgmFadeCoroutine = StartCoroutine(FadeOutAndStop(bgmSource, fadeDuration));
            Debug.Log("[AudioManager] Stopping BGM with fade out");
        }

        /// <summary>
        /// Crossfade from current BGM to new BGM
        /// </summary>
        public async void CrossfadeBGM(string clipName, float fadeDuration = 1f)
        {
            AudioClip clip = await LoadAudioClipAsync(clipName);

            if (clip != null)
            {
                if (bgmFadeCoroutine != null)
                {
                    StopCoroutine(bgmFadeCoroutine);
                }

                bgmFadeCoroutine = StartCoroutine(CrossfadeCoroutine(clip, fadeDuration));
                Debug.Log($"[AudioManager] Crossfading to BGM: {clipName}");
            }
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
                audioMixer.SetFloat(MASTER_VOLUME_PARAM, db);
            }

            Debug.Log($"[AudioManager] Master volume set to: {masterVolume}");
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
                audioMixer.SetFloat(BGM_VOLUME_PARAM, db);
            }

            Debug.Log($"[AudioManager] BGM volume set to: {bgmVolume}");
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
                audioMixer.SetFloat(SFX_VOLUME_PARAM, db);
            }

            Debug.Log($"[AudioManager] SFX volume set to: {sfxVolume}");
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
            PlayerPrefs.Save();

            Debug.Log("[AudioManager] Audio settings saved");
        }

        /// <summary>
        /// Load volume settings from PlayerPrefs
        /// </summary>
        public void LoadAudioSettings()
        {
            masterVolume = PlayerPrefs.GetFloat(MASTER_VOLUME_KEY, 1.0f);
            bgmVolume = PlayerPrefs.GetFloat(BGM_VOLUME_KEY, 1.0f);
            sfxVolume = PlayerPrefs.GetFloat(SFX_VOLUME_KEY, 1.0f);

            // Apply loaded settings
            SetMasterVolume(masterVolume);
            SetBGMVolume(bgmVolume);
            SetSFXVolume(sfxVolume);

            Debug.Log("[AudioManager] Audio settings loaded");
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
            if (loadedClips.ContainsKey(clipName))
            {
                return loadedClips[clipName];
            }

            // Check if currently loading
            if (loadingHandles.ContainsKey(clipName))
            {
                await loadingHandles[clipName].Task;
                return loadedClips.ContainsKey(clipName) ? loadedClips[clipName] : null;
            }

            try
            {
                AsyncOperationHandle<AudioClip> handle = Addressables.LoadAssetAsync<AudioClip>(clipName);
                loadingHandles[clipName] = handle;

                await handle.Task;

                if (handle.Status == AsyncOperationStatus.Succeeded)
                {
                    loadedClips[clipName] = handle.Result;
                    Debug.Log($"[AudioManager] Loaded audio clip: {clipName}");
                    return handle.Result;
                }
                else
                {
                    Debug.LogError($"[AudioManager] Failed to load audio clip: {clipName}");
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

        #endregion

        #region Fade Effects

        /// <summary>
        /// Fade in audio source volume
        /// </summary>
        private IEnumerator FadeIn(AudioSource source, float duration)
        {
            float startVolume = 0f;
            float targetVolume = 1f;
            float elapsed = 0f;

            source.volume = startVolume;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                source.volume = Mathf.Lerp(startVolume, targetVolume, elapsed / duration);
                yield return null;
            }

            source.volume = targetVolume;
            bgmFadeCoroutine = null;
        }

        /// <summary>
        /// Fade out audio source volume and stop
        /// </summary>
        private IEnumerator FadeOutAndStop(AudioSource source, float duration)
        {
            float startVolume = source.volume;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                source.volume = Mathf.Lerp(startVolume, 0f, elapsed / duration);
                yield return null;
            }

            source.volume = 0f;
            source.Stop();
            source.volume = 1f; // Reset for next use
            bgmFadeCoroutine = null;
        }

        /// <summary>
        /// Crossfade from current BGM to new BGM
        /// </summary>
        private IEnumerator CrossfadeCoroutine(AudioClip newClip, float duration)
        {
            float halfDuration = duration / 2f;

            // Fade out current BGM
            yield return FadeOutAndStop(bgmSource, halfDuration);

            // Set new clip and fade in
            bgmSource.clip = newClip;
            bgmSource.volume = 0f;
            bgmSource.Play();

            yield return FadeIn(bgmSource, halfDuration);
        }

        #endregion

        #region Cleanup

        private void OnDestroy()
        {
            if (instance != this) return;

            // Release all loaded AudioClips
            foreach (var handle in loadingHandles.Values)
            {
                if (handle.IsValid())
                {
                    Addressables.Release(handle);
                }
            }

            loadedClips.Clear();
            loadingHandles.Clear();

            Debug.Log("[AudioManager] Cleaned up and released resources");
        }

        #endregion
    }
}
