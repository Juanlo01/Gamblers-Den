using System;
using System.Collections.Generic;
using FMOD;
using FMOD.Studio;
using FMODUnity;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace SimpleAudioSystem
{
    public class AudioManager : MonoBehaviour
    {
        private const string MasterVolumeKey = "SimpleAudio.Master";
        private const string MusicVolumeKey = "SimpleAudio.Music";
        private const string SFXVolumeKey = "SimpleAudio.SFX";

        [SerializeField]
        private AudioDatabase audioDatabase;

        [SerializeField]
        private string masterBusPath = "bus:/";

        [SerializeField]
        private string musicBusPath = "bus:/Music";

        [SerializeField]
        private string sfxBusPath = "bus:/SFX";

        private EventInstance currentMusicInstance;
        private string currentMusicId;
        private bool hasCurrentMusic;

        private Bus masterBus;
        private Bus musicBus;
        private Bus sfxBus;
        private bool hasMasterBus;
        private bool hasMusicBus;
        private bool hasSFXBus;

        private float masterVolume = 1f;
        private float musicVolume = 1f;
        private float sfxVolume = 1f;

        private readonly HashSet<string> warnedMissingIds =
            new HashSet<string>(StringComparer.Ordinal);

        private bool warnedBlankId;
        private bool warnedNotReady;
        private bool shuttingDown;

        public static AudioManager Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            if (audioDatabase == null)
            {
                Debug.LogError(
                    "AudioManager has no AudioDatabase assigned. Playback is disabled.",
                    this);
            }

            LoadVolumes();
            ResolveBuses();
            ApplyLoadedVolumes();
        }

        public bool PlayOneShot(string id)
        {
            if (!TryGetEntry(id, out AudioEntry entry))
            {
                return false;
            }

            if (entry.EventReference.IsNull)
            {
                WarnMissingIdOnce(id, "has no assigned FMOD event");
                return false;
            }

            try
            {
                RuntimeManager.PlayOneShot(entry.EventReference);
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogWarning(
                    $"AudioManager could not play one-shot '{id}': {exception.Message}",
                    this);
                return false;
            }
        }

        public bool PlayMusic(string id)
        {
            if (hasCurrentMusic && string.Equals(currentMusicId, id, StringComparison.Ordinal))
            {
                if (currentMusicInstance.isValid())
                {
                    return true;
                }

                currentMusicInstance = default;
                currentMusicId = null;
                hasCurrentMusic = false;
            }

            if (!TryGetEntry(id, out AudioEntry entry))
            {
                return false;
            }

            if (entry.EventReference.IsNull)
            {
                WarnMissingIdOnce(id, "has no assigned FMOD event");
                return false;
            }

            if (hasCurrentMusic)
            {
                StopAndReleaseCurrentMusic(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
            }

            EventInstance newInstance = default;

            try
            {
                newInstance = RuntimeManager.CreateInstance(entry.EventReference);
                if (!newInstance.isValid())
                {
                    Debug.LogWarning(
                        $"AudioManager could not create music event '{id}'.",
                        this);
                    return false;
                }

                RESULT startResult = newInstance.start();
                if (startResult != RESULT.OK)
                {
                    Debug.LogWarning(
                        $"AudioManager could not start music '{id}': {startResult}.",
                        this);
                    newInstance.release();
                    return false;
                }

                currentMusicInstance = newInstance;
                currentMusicId = id;
                hasCurrentMusic = true;
                return true;
            }
            catch (Exception exception)
            {
                if (newInstance.isValid())
                {
                    newInstance.release();
                }

                Debug.LogWarning(
                    $"AudioManager could not play music '{id}': {exception.Message}",
                    this);
                return false;
            }
        }

        public bool StopMusic()
        {
            if (!hasCurrentMusic)
            {
                return false;
            }

            if (!currentMusicInstance.isValid())
            {
                currentMusicInstance = default;
                currentMusicId = null;
                hasCurrentMusic = false;
                return false;
            }

            StopAndReleaseCurrentMusic(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
            return true;
        }

        public bool SetMasterVolume(float value)
        {
            masterVolume = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(MasterVolumeKey, masterVolume);
            return ApplyBusVolume(masterBus, hasMasterBus, masterVolume, masterBusPath);
        }

        public bool SetMusicVolume(float value)
        {
            musicVolume = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(MusicVolumeKey, musicVolume);
            return ApplyBusVolume(musicBus, hasMusicBus, musicVolume, musicBusPath);
        }

        public bool SetSFXVolume(float value)
        {
            sfxVolume = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(SFXVolumeKey, sfxVolume);
            return ApplyBusVolume(sfxBus, hasSFXBus, sfxVolume, sfxBusPath);
        }

        // Unity UI Slider wrappers
        public void SetMasterVolumeFromSlider(float value)
        {
            SetMasterVolume(value);
        }

        public void SetMusicVolumeFromSlider(float value)
        {
            SetMusicVolume(value);
        }

        public void SetSFXVolumeFromSlider(float value)
        {
            SetSFXVolume(value);
        }


        // Getters
        public float GetMasterVolume()
        {
            return masterVolume;
        }

        public float GetMusicVolume()
        {
            return musicVolume;
        }

        public float GetSFXVolume()
        {
            return sfxVolume;
        }

        private bool TryGetEntry(string id, out AudioEntry entry)
        {
            entry = null;

            if (audioDatabase == null)
            {
                if (!warnedNotReady)
                {
                    Debug.LogWarning(
                        "AudioManager is not ready because no AudioDatabase is assigned.",
                        this);
                    warnedNotReady = true;
                }

                return false;
            }

            if (string.IsNullOrWhiteSpace(id))
            {
                if (!warnedBlankId)
                {
                    Debug.LogWarning(
                        "AudioManager cannot play a blank audio ID.",
                        this);
                    warnedBlankId = true;
                }

                return false;
            }

            if (!audioDatabase.TryGetEntry(id, out entry))
            {
                WarnMissingIdOnce(id, "was not found in the AudioDatabase");
                return false;
            }

            return true;
        }

        private void WarnMissingIdOnce(string id, string reason)
        {
            if (warnedMissingIds.Add(id))
            {
                Debug.LogWarning($"Audio ID '{id}' {reason}.", this);
            }
        }

        private void LoadVolumes()
        {
            masterVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(MasterVolumeKey, 1f));
            musicVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(MusicVolumeKey, 1f));
            sfxVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(SFXVolumeKey, 1f));
        }

        private void ResolveBuses()
        {
            hasMasterBus = TryResolveBus(masterBusPath, out masterBus);
            hasMusicBus = TryResolveBus(musicBusPath, out musicBus);
            hasSFXBus = TryResolveBus(sfxBusPath, out sfxBus);
        }

        private bool TryResolveBus(string path, out Bus bus)
        {
            bus = default;

            if (string.IsNullOrWhiteSpace(path))
            {
                Debug.LogWarning("AudioManager has a blank FMOD bus path.", this);
                return false;
            }

            try
            {
                bus = RuntimeManager.GetBus(path);
                if (bus.isValid())
                {
                    return true;
                }
            }
            catch (Exception exception)
            {
                Debug.LogWarning(
                    $"AudioManager could not resolve FMOD bus '{path}': " +
                    exception.Message,
                    this);
                return false;
            }

            Debug.LogWarning(
                $"AudioManager could not resolve FMOD bus '{path}'.",
                this);
            return false;
        }

        private void ApplyLoadedVolumes()
        {
            ApplyBusVolume(masterBus, hasMasterBus, masterVolume, masterBusPath);
            ApplyBusVolume(musicBus, hasMusicBus, musicVolume, musicBusPath);
            ApplyBusVolume(sfxBus, hasSFXBus, sfxVolume, sfxBusPath);
        }

        private bool ApplyBusVolume(Bus bus, bool hasBus, float value, string path)
        {
            if (!hasBus || !bus.isValid())
            {
                return false;
            }

            RESULT result = bus.setVolume(value);
            if (result == RESULT.OK)
            {
                return true;
            }

            Debug.LogWarning(
                $"AudioManager could not set volume on FMOD bus '{path}': {result}.",
                this);
            return false;
        }

        private void StopAndReleaseCurrentMusic(FMOD.Studio.STOP_MODE stopMode)
        {
            EventInstance instance = currentMusicInstance;

            if (instance.isValid())
            {
                RESULT stopResult = instance.stop(stopMode);
                if (stopResult != RESULT.OK)
                {
                    Debug.LogWarning(
                        $"AudioManager could not stop the current music: {stopResult}.",
                        this);
                }

                RESULT releaseResult = instance.release();
                if (releaseResult != RESULT.OK)
                {
                    Debug.LogWarning(
                        $"AudioManager could not release the current music: {releaseResult}.",
                        this);
                }
            }

            currentMusicInstance = default;
            currentMusicId = null;
            hasCurrentMusic = false;
        }

        private void OnApplicationQuit()
        {
            Shutdown();
        }

        private void OnDestroy()
        {
            if (Instance != this)
            {
                return;
            }

            Shutdown();
            Instance = null;
        }

        private void Shutdown()
        {
            if (shuttingDown)
            {
                return;
            }

            shuttingDown = true;
            PlayerPrefs.Save();

            if (hasCurrentMusic)
            {
                StopAndReleaseCurrentMusic(FMOD.Studio.STOP_MODE.IMMEDIATE);
            }
        }
    }
}
