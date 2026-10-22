using System;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public const float DefaultVolume = 0.35f;

    [SerializeField] private AudioSource _musicSource;
    [SerializeField] private AudioClip[] _musicClips;

    [Header("SFX")]
    [SerializeField] private AudioSource _sfxSource;
    [SerializeField] private AudioClip _meowClip;
    [SerializeField] private float _meowStartTime = 0f;
    [SerializeField] private float _meowMinPitch = 0.92f;
    [SerializeField] private float _meowMaxPitch = 1.08f;
    [SerializeField] private AudioClip _uiClickClip;
    [SerializeField] private AudioClip _winClip;
    [SerializeField] private AudioClip _coinBonusClip;

    private static AudioManager _instance;

    private int _currentTrackIndex;
    private bool _wasPlaying;
    private bool _isMuted;

    public static AudioManager Instance => _instance;

    public event Action Changed;

    public float MusicVolume
    {
        get => Mathf.Clamp01(PlayerPrefs.GetFloat(SaveKeys.MusicVolume, DefaultVolume));
        set
        {
            PlayerPrefs.SetFloat(SaveKeys.MusicVolume, Mathf.Clamp01(value));
            PlayerPrefs.Save();
            CloudSave.Push();
            ApplyMusic();
            Changed?.Invoke();
        }
    }

    public float SfxVolume
    {
        get => Mathf.Clamp01(PlayerPrefs.GetFloat(SaveKeys.SfxVolume, DefaultVolume));
        set
        {
            PlayerPrefs.SetFloat(SaveKeys.SfxVolume, Mathf.Clamp01(value));
            PlayerPrefs.Save();
            CloudSave.Push();
            ApplySfx();
            Changed?.Invoke();
        }
    }

    public bool MusicOn
    {
        get => PlayerPrefs.GetInt(SaveKeys.Music, 1) == 1;
        set
        {
            int enabledValue;

            if (value)
            {
                enabledValue = 1;
            }
            else
            {
                enabledValue = 0;
            }

            PlayerPrefs.SetInt(SaveKeys.Music, enabledValue);
            PlayerPrefs.Save();
            CloudSave.Push();
            ApplyMusic();
            Changed?.Invoke();
        }
    }

    public bool SfxOn
    {
        get => PlayerPrefs.GetInt(SaveKeys.Sfx, 1) == 1;
        set
        {
            int enabledValue;

            if (value)
            {
                enabledValue = 1;
            }
            else
            {
                enabledValue = 0;
            }

            PlayerPrefs.SetInt(SaveKeys.Sfx, enabledValue);
            PlayerPrefs.Save();
            CloudSave.Push();
            ApplySfx();
            Changed?.Invoke();
        }
    }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        _isMuted = YandexSdk.IsAudioPaused;
        DontDestroyOnLoad(gameObject);

        if (_musicSource != null)
        {
            _musicSource.ignoreListenerVolume = true;
            _musicSource.loop = false;
        }

        ApplyMusic();
        ApplySfx();
    }

    private void Update()
    {
        if (_isMuted == false && _musicSource != null && MusicOn && _wasPlaying && _musicSource.isPlaying == false)
        {
            _currentTrackIndex = (_currentTrackIndex + 1) % _musicClips.Length;
            PlayCurrentTrack();
        }
    }

    private void Start()
    {
        // Retry after Awake in case the audio engine was not ready for the first Play.
        ApplyMusic();
    }

    public void PlayMeow()
    {
        if (_isMuted || _meowClip == null || _sfxSource == null || SfxOn == false)
        {
            return;
        }

        _sfxSource.Stop();
        _sfxSource.clip = _meowClip;
        _sfxSource.time = Mathf.Clamp(_meowStartTime, 0f, Mathf.Max(0f, _meowClip.length - 0.01f));
        _sfxSource.pitch = UnityEngine.Random.Range(_meowMinPitch, _meowMaxPitch);
        _sfxSource.Play();
    }

    public void RefreshSettings()
    {
        ApplyMusic();
        ApplySfx();
        Changed?.Invoke();
    }

    public void SetMuted(bool muted)
    {
        if (_isMuted == muted)
        {
            return;
        }

        _isMuted = muted;

        if (_musicSource == null)
        {
            return;
        }

        if (muted)
        {
            _musicSource.Pause();
        }
        else if (MusicOn && HasMusicClips())
        {
            _musicSource.UnPause();
        }

        ApplyMusic();
    }

    public void PlayUIClick() => PlaySfx(_uiClickClip);

    public void PlayWin() => PlaySfx(_winClip);

    public void PlayCoinBonus() => PlaySfx(_coinBonusClip);

    public static void PlayUIClickStatic() => _instance?.PlayUIClick();

    private void PlaySfx(AudioClip clip)
    {
        if (_isMuted || clip == null || _sfxSource == null || SfxOn == false)
        {
            return;
        }

        _sfxSource.pitch = 1f;
        _sfxSource.PlayOneShot(clip);
    }

    private void ApplyMusic()
    {
        if (_musicSource == null)
        {
            return;
        }

        bool musicOn = MusicOn;

        // WebGL can resume a queued Play after Stop; mute also silences that deferred playback.
        if (_isMuted || musicOn == false)
        {
            _musicSource.mute = true;
        }
        else
        {
            _musicSource.mute = false;
        }

        if (musicOn)
        {
            _musicSource.volume = MusicVolume;
        }
        else
        {
            _musicSource.volume = 0f;
        }

        if (musicOn && HasMusicClips())
        {
            if (_isMuted == false && _musicSource.isPlaying == false)
            {
                PlayCurrentTrack();
            }
        }
        else
        {
            _musicSource.Stop();
            _wasPlaying = false;
        }
    }

    private void PlayCurrentTrack()
    {
        if (_musicSource == null || HasMusicClips() == false)
        {
            return;
        }

        _currentTrackIndex = Mathf.Clamp(_currentTrackIndex, 0, _musicClips.Length - 1);
        AudioClip clip = _musicClips[_currentTrackIndex];

        if (clip == null)
        {
            return;
        }

        _musicSource.clip = clip;
        _musicSource.Play();
        _wasPlaying = true;
    }

    private bool HasMusicClips()
    {
        return _musicClips != null && _musicClips.Length > 0;
    }

    private void ApplySfx()
    {
        if (SfxOn)
        {
            AudioListener.volume = SfxVolume;
        }
        else
        {
            AudioListener.volume = 0f;
        }
    }
}
