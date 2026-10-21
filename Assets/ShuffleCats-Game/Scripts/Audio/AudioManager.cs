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
            PlayerPrefs.SetInt(SaveKeys.Music, value ? 1 : 0);
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
            PlayerPrefs.SetInt(SaveKeys.Sfx, value ? 1 : 0);
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
            // Loop is handled manually so we can advance through the playlist;
            // a looping source would repeat a single track forever.
            _musicSource.loop = false;
        }

        ApplyMusic();
        ApplySfx();
    }

    private void Update()
    {
        // Advance to the next track once the current one finishes. Guarded by
        // _wasPlaying so a muted/stopped source is not mistaken for a finished track.
        if (_isMuted == false && _musicSource != null && MusicOn && _wasPlaying && _musicSource.isPlaying == false)
        {
            _currentTrackIndex = (_currentTrackIndex + 1) % _musicClips.Length;
            PlayCurrentTrack();
        }
    }

    private void Start()
    {
        // Calling Play() in Awake() is unreliable — the audio engine isn't always
        // ready yet and the first Play() gets silently dropped. Re-apply here so the
        // music actually starts. ApplyMusic() is idempotent (guards on isPlaying).
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

    // Full silence on top of the player's own music/SFX settings, for the time
    // an ad is on screen or the browser tab is hidden (Yandex requirements 4.7
    // and 1.3). The settings are untouched, so unmuting restores exactly what
    // the player had.
    public void SetMuted(bool muted)
    {
        if (_isMuted == muted)
        {
            return;
        }

        _isMuted = muted;
        // YandexSdk owns AudioListener.pause, including pauses before Awake.

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

    public static void PlayWinStatic() => _instance?.PlayWin();

    public static void PlayCoinBonusStatic() => _instance?.PlayCoinBonus();

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
        // WebGL can start a queued Play after Stop when its audio context resumes.
        // Keep the source silent too, without changing the player's saved volume.
        _musicSource.mute = _isMuted || !musicOn;
        _musicSource.volume = musicOn ? MusicVolume : 0f;

        if (musicOn && HasMusicClips())
        {
            if (!_isMuted && !_musicSource.isPlaying)
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
        AudioListener.volume = SfxOn ? SfxVolume : 0f;
    }
}
