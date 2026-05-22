using System;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    private const string MusicKey = "Audio.Music";
    private const string SfxKey = "Audio.Sfx";

    [SerializeField] private AudioSource _musicSource;
    [SerializeField] private AudioClip _musicClip;

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
