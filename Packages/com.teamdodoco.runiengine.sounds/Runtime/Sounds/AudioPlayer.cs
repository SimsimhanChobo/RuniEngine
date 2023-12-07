#nullable enable
using RuniEngine.Booting;
using RuniEngine.Pooling;
using RuniEngine.Resource;
using RuniEngine.Resource.Sounds;
using RuniEngine.Threading;
using System;
using UnityEngine;

using Random = UnityEngine.Random;

namespace RuniEngine.Sounds
{
    [RequireComponent(typeof(AudioSource))]
    public sealed class AudioPlayer : SoundPlayerBase
    {
        public override string objectKey => "audio_player.prefab";



        AudioSource? _audioSource;
        public AudioSource? audioSource => _audioSource = this.GetComponentFieldSave(_audioSource);

        AudioLowPassFilter? _audioLowPassFilter;
        public AudioLowPassFilter? audioLowPassFilter => _audioLowPassFilter = this.GetComponentFieldSave(_audioLowPassFilter);



        public override SoundData? soundData => audioData;
        public override SoundMetaDataBase? soundMetaData => audioMetaData;

        public AudioData? audioData { get; private set; }
        public AudioMetaData? audioMetaData { get; private set; }



        public int frequency => audioMetaData != null ? audioMetaData.frequency : 0;

        public int channels => audioMetaData != null ? audioMetaData.channels : 0;


        public override double time
        {
            get => _time;
            set
            {
                if (_time != value)
                {
                    _time = value;
                    if (audioSource != null)
                        audioSource.time = (float)value.Clamp(0, length - 0.01f);
                    
                    TimeChangedEventInvoke();
                }
            }
        }
        [NonSerialized] double _time = 0;

        public int timeSamples
        {
            get => audioSource != null ? audioSource.timeSamples : 0;
            set
            {
                if (timeSamples != value)
                {
                    _time = value / frequency;
                    if (audioSource != null)
                        audioSource.timeSamples = value;

                    TimeChangedEventInvoke();
                }
            }
        }

        public override double length => audioMetaData != null ? audioMetaData.length : 0;



        bool isDisposable = false;



        double tempoAdjustmentTime = 0;
        void Update()
        {
            if (audioSource == null || audioMetaData == null)
                return;

            VarRefresh();
            
            //시간 보정
            if (isPlaying && !isPaused)
            {
                double value = Kernel.deltaTime;
                _time += value * tempo;

                //템포
                if (audioSource.isPlaying && time >= 0 && time <= length - 0.01f)
                {
                    const double condition = 2048d / 48000d;
                    double pitchDivideTempo = tempo / (pitch != 0 ? pitch : 1);
                    
                    tempoAdjustmentTime += value;
                    while (tempoAdjustmentTime >= condition)
                    {
                        float result = (float)((1 - pitchDivideTempo.Abs()) * pitch * condition * tempo.Sign());
                        
                        audioSource.time = (audioSource.time - result).Clamp(0, (float)length - 0.01f);
                        _time = (double)audioSource.timeSamples / frequency;

                        tempoAdjustmentTime -= condition;
                    }
                }
                else
                    tempoAdjustmentTime = 0;
            }
            else
                tempoAdjustmentTime = 0;

            //루프
            if (loop)
            {
                float loopStartTime = (float)audioMetaData.loopStartIndex / frequency;
                bool isLooped = false;

                while (tempo >= 0 && time >= length)
                {
                    _time -= length - loopStartTime;
                    isLooped = true;
                }

                while (tempo < 0 && time < loopStartTime)
                {
                    _time += length - loopStartTime;
                    isLooped = true;
                }

                if (isLooped)
                {
                    audioSource.time = (float)time;
                    LoopedEventInvoke();
                }
            }

            if (isDisposable && !loop && (time < 0 || time > length))
                Remove();
        }

        void VarRefresh()
        {
            if (audioSource == null)
                return;

            if (audioSource.bypassEffects)
                audioSource.volume = volume;
            else
                audioSource.volume = 1;

            if (pitch != 0)
            {
                audioSource.mute = false;
                audioSource.pitch = realPitch * tempo.Sign();
            }
            else
            {
                audioSource.mute = true;
                audioSource.pitch = 1;
            }

            audioSource.loop = false;
            audioSource.panStereo = panStereo;

            audioSource.spatialBlend = spatial ? 1 : 0;

            audioSource.minDistance = minDistance;
            audioSource.maxDistance = maxDistance;
            
            if (isPlaying)
            {
                if (!isPaused && tempo != 0)
                {
                    if (!audioSource.enabled)
                    {
                        audioSource.enabled = true;

                        audioSource.Stop();
                        audioSource.Play();

                        audioSource.time = (float)time.Clamp(0, length - 0.01f);
                    }

                    if (time >= 0 && time < length - 0.01f && !audioSource.isPlaying)
                    {
                        audioSource.enabled = true;
                        audioSource.UnPause();

                        if (!audioSource.isPlaying)
                        {
                            audioSource.Stop();
                            audioSource.Play();

                            audioSource.time = (float)time.Clamp(0, length - 0.01f);
                        }
                    }
                }
                else
                    audioSource.Pause();
            }
            else if (audioSource.isPlaying)
                audioSource.Stop();
        }



        public override bool Refresh()
        {
            if (audioSource == null)
                return false;

            AudioData? audioData = AudioLoader.SearchAudioData(key, nameSpace);
            if (audioData == null || audioData.audios.Length <= 0)
                return false;

            AudioMetaData? audioMetaData = audioData.audios[Random.Range(0, audioData.audios.Length)];
            if (audioMetaData == null || audioMetaData.audioClip == null)
                return false;

            this.audioData = audioData;
            this.audioMetaData = audioMetaData;

            audioSource.clip = audioMetaData.audioClip;
            audioSource.enabled = true;

            audioSource.Stop();
            audioSource.Play();

            audioSource.time = (float)time.Clamp(0, length - 0.01f);

            return true;
        }

        public override void Play()
        {
            if (!isActiveAndEnabled || audioSource == null)
                return;

            Stop();
            if (!Refresh())
                return;
            
            VarRefresh();

            audioSource.enabled = true;

            audioSource.Stop();
            audioSource.Play();

            if (tempo < 0 && audioMetaData != null)
                audioSource.time = (float)length - 0.01f;

            base.Play();
        }

        public override void Stop()
        {
            base.Stop();

            if (audioSource != null)
                audioSource.Stop();

            audioData = null;
            audioMetaData = null;

            time = 0;
        }

        protected override void OnAudioFilterRead(float[] data, int channels)
        {
            for (int i = 0; i < data.Length; i++)
                data[i] *= volume;

            base.OnAudioFilterRead(data, channels);
        }

        public static AudioPlayer? PlayAudio(string key, string nameSpace = "", float volume = 1, bool loop = false, float pitch = 1, float tempo = 1, float panStereo = 0, Transform? parent = null) => InternalPlayAudio(key, nameSpace, volume, loop, pitch, tempo, panStereo, parent, false, Vector3.zero, 0, 16);

        public static AudioPlayer? PlayAudio(string key, string nameSpace, float volume, bool loop, float pitch, float tempo, float panStereo, Transform? parent, Vector3 position, float minDistance = 0, float maxDistance = 16) => InternalPlayAudio(key, nameSpace, volume, loop, pitch, tempo, panStereo, parent, true, position, minDistance, maxDistance);

        static AudioPlayer? InternalPlayAudio(string key, string nameSpace, float volume, bool loop, float pitch, float tempo, float panStereo, Transform? parent, bool spatial, Vector3 position, float minDistance, float maxDistance)
        {
            NotMainThreadException.Exception();
            NotPlayModeException.Exception();
            ResourceDataNotLoadedException.Exception();

            ResourceManager.SetDefaultNameSpace(ref nameSpace);

            AudioPlayer? audioPlayer = (AudioPlayer?)ObjectPoolingManager.ObjectCreate("audio_player.prefab", parent).monoBehaviour;
            if (audioPlayer == null)
                return null;

            audioPlayer.key = key;
            audioPlayer.nameSpace = nameSpace;

            audioPlayer.volume = volume;
            audioPlayer.loop = loop;
            audioPlayer.pitch = pitch;
            audioPlayer.tempo = tempo;

            audioPlayer.panStereo = panStereo;
            audioPlayer.spatial = spatial;

            audioPlayer.minDistance = minDistance;
            audioPlayer.maxDistance = maxDistance;

            audioPlayer.transform.localPosition = position;
            
            audioPlayer.isDisposable = true;
            audioPlayer.Play();
            
            return audioPlayer;
        }
    }
}
