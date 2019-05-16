﻿#pragma warning disable 0649

using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SoundEngine : MonoBehaviour
{
    [SerializeField]
    float _sfxVolume = 1.0f;
    [SerializeField]
    float _musicVolume = 1.0f;

    [SerializeField]
    float _sfxPitchVariance = 0.4f;

    [SerializeField]
    float musicFadeTime = 3f;

    [SerializeField]
    AudioClip[] soundEffects;
    [SerializeField]
    AudioClip[] musicTracks;

    GameObject[] _musicObjects;
    GameObject _currentMusic;

    private static SoundEngine Instance;

    public void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        //DontDestroyOnLoad(gameObject);

        PreloadMusic();
    }

    public void Start()
    {
        if (!musicTracks.Any())
            return;

        string[] tracks =
            SceneManager.GetActiveScene().buildIndex == 0
            ? GameConstants.menuTracks
            : GameConstants.LevelTracks;

        PlayMusic(tracks[Random.Range(0, tracks.Length)]);
    }

    private void PreloadMusic()
    {
        foreach (AudioClip track in musicTracks)
        {
            GameObject musicObj = new GameObject(track.name);
            musicObj.transform.parent = transform;
            musicObj.transform.localPosition = Vector3.zero;

            AudioSource musicSource = musicObj.AddComponent<AudioSource>();
            musicSource.clip = track;
            musicSource.volume = 0f;
            musicSource.loop = true;
        }
    }

    public IEnumerator PlayAndDelete(GameObject sfxObj)
    {
        AudioSource audioSource = sfxObj.GetComponent<AudioSource>();
        audioSource.Play();
        yield return new WaitForSecondsRealtime(audioSource.clip.length * (1f + _sfxPitchVariance));
        Destroy(sfxObj);
    }

    public void PlaySFX(string clipname, bool varyPitch = true)
    {
        AudioClip clip = soundEffects
            .Where((AudioClip a) => a.name == clipname)
            .First();

        if (clip == null)
        {
            Debug.Log("SoundEngine: tried to play sound effect '" + clipname + "', but it doesn't exist.");
            return;
        }

        // the clip we're looking for exists! let's play it!
        GameObject audioObj = new GameObject("audio_" + clipname);
        audioObj.transform.parent = transform;
        audioObj.transform.localPosition = Vector3.zero;

        float pitchMultiplier = varyPitch
            ? Random.Range(
                1f / (1f + _sfxPitchVariance),
                1f * (1f + _sfxPitchVariance))
            : 1f;
        AudioSource audioSource = audioObj.AddComponent<AudioSource>();
        audioSource.clip = clip;
        audioSource.volume = _sfxVolume;
        audioSource.pitch = pitchMultiplier;

        StartCoroutine(PlayAndDelete(audioObj));
    }

    public IEnumerator FadeMusicIn(AudioSource track)
    {
        track.volume = 0f;
        track.Play();

        float timeMultiplier = Mathf.PI / (2 * musicFadeTime);
        float fadePosition = 0f;

        while (fadePosition < musicFadeTime)
        {
            fadePosition += Time.deltaTime;
            track.volume = (1f - Mathf.Cos(fadePosition * timeMultiplier)) * _musicVolume;
            yield return new WaitForEndOfFrame();
        }

        track.volume = _musicVolume;
    }

    public IEnumerator FadeMusicOut(AudioSource track)
    {
        float timeMultiplier = Mathf.PI / (2 * musicFadeTime);
        float fadePosition = 0f;

        while (fadePosition < musicFadeTime)
        {
            fadePosition += Time.deltaTime;
            track.volume = Mathf.Cos(fadePosition * timeMultiplier) * _musicVolume;
            yield return new WaitForEndOfFrame();
        }

        track.volume = 0f;
        yield return new WaitForEndOfFrame();
        track.Stop();
    }

    private IEnumerator _playMusic(string trackname, bool waitForFade)
    {
        bool didstop = StopMusic();
        if (didstop && waitForFade)
        {
            yield return new WaitForSecondsRealtime(musicFadeTime);
        }

        _currentMusic = transform.Find(trackname).gameObject;

        if (_currentMusic != null)
        {
            StartCoroutine(
                FadeMusicIn(
                    _currentMusic.GetComponent<AudioSource>()));
        }
        else
        {
            Debug.Log("SoundEngine: tried to play music track '" + trackname + "', but it doesn't exist!");
        }
    }

    public void PlayMusic(string trackname, bool waitForFade = true)
    {
        StartCoroutine(_playMusic(trackname, waitForFade));
    }

    public void PlayMusicNofade(string trackname)
    {
        StopMusicNofade();

        _currentMusic = transform.Find(trackname).gameObject;

        if (_currentMusic != null)
        {
            AudioSource source = _currentMusic.GetComponent<AudioSource>();
            source.volume = _musicVolume;
            source.Play();
        }
        else
        {
            Debug.Log("SoundEngine: tried to play music track '" + trackname + "', but it doesn't exist!");
        }
    }

    public bool StopMusic()
    {
        bool stopped = _currentMusic != null;

        if (stopped)
        {
            StartCoroutine(
                FadeMusicOut(
                    _currentMusic.GetComponent<AudioSource>()));
        }

        _currentMusic = null;
        return stopped;
    }

    public bool StopMusicNofade()
    {
        bool stopped = _currentMusic != null;

        if (stopped)
        {
            AudioSource source = _currentMusic.GetComponent<AudioSource>();
            source.volume = 0f;
            source.Stop();
        }

        return stopped;
    }

    public static SoundEngine GetEngine()
        => GameObject.FindGameObjectWithTag(GameConstants.SoundEngineTag).GetComponent<SoundEngine>();
}
