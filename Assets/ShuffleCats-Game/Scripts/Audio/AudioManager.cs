using System;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    private const string MusicKey = "Audio.Music";
    private const string SfxKey = "Audio.Sfx";

    [SerializeField] private AudioSource _musicSource;
    [SerializeField] private AudioClip _musicClip;

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

    public static AudioManager Instance => _instance;

    public event Action Changed;

    public bool MusicOn
    {
        get => PlayerPrefs.GetInt(MusicKey, 1) == 1;
        set
        {
            PlayerPrefs.SetInt(MusicKey, value ? 1 : 0);
            PlayerPrefs.Save();
            ApplyMusic();
            Changed?.Invoke();
        }
    }

    public bool SfxOn
    {
        get => PlayerPrefs.GetInt(SfxKey, 1) == 1;
        set
        {
            PlayerPrefs.SetInt(SfxKey, value ? 1 : 0);
            PlayerPrefs.Save();
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
        DontDestroyOnLoad(gameObject);

        if (_musicSource != null)
        {
            _musicSource.ignoreListenerVolume = true;
            _musicSource.loop = true;
            if (_musicClip != null)
            {
                _musicSource.clip = _musicClip;
            }
        }

        ApplyMusic();
        ApplySfx();
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
        if (_meowClip == null || _sfxSource == null || SfxOn == false)
        {
            return;
        }

        _sfxSource.Stop();
        _sfxSource.clip = _meowClip;
        _sfxSource.time = Mathf.Clamp(_meowStartTime, 0f, Mathf.Max(0f, _meowClip.length - 0.01f));
        _sfxSource.pitch = UnityEngine.Random.Range(_meowMinPitch, _meowMaxPitch);
        _sfxSource.Play();
    }

    public void PlayUIClick() => PlaySfx(_uiClickClip);

    public void PlayWin() => PlaySfx(_winClip);

    public void PlayCoinBonus() => PlaySfx(_coinBonusClip);

    public static void PlayUIClickStatic() => _instance?.PlayUIClick();

    public static void PlayWinStatic() => _instance?.PlayWin();

    public static void PlayCoinBonusStatic() => _instance?.PlayCoinBonus();

    private void PlaySfx(AudioClip clip)
    {
        if (clip == null || _sfxSource == null || SfxOn == false)
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

        if (MusicOn && _musicSource.clip != null)
        {
            if (!_musicSource.isPlaying)
            {
                _musicSource.Play();
            }
        }
        else
        {
            _musicSource.Stop();
        }
    }

    private void ApplySfx()
    {
        AudioListener.volume = SfxOn ? 1f : 0f;
    }
}
