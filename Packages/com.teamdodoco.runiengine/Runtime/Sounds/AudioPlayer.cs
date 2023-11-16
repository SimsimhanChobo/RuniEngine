#nullable enable
using RuniEngine.Resource.Sounds;
using RuniEngine.Threading;
using System;
using System.Threading;
using UnityEngine;

using Random = UnityEngine.Random;

namespace RuniEngine.Sounds
{
    [RequireComponent(typeof(AudioSource))]
    public sealed class AudioPlayer : SoundPlayerBase
    {
        readonly AudioSource? _audioSource;
        public AudioSource? audioSource => this.GetComponentFieldSave(_audioSource);



        public override SoundData? soundData => audioData;
        public override SoundMetaDataBase? soundMetaData => audioMetaData;

        public AudioData? audioData { get; private set; }
        public AudioMetaData? audioMetaData { get; private set; }



        public float[] samples => _samples;
        [NonSerialized] float[] _samples = new float[0];



        public int frequency => _frequency;
        int _frequency = 0;

        public int channels => _channels;
        int _channels = 0;


        public override double time
        {
            get => currentSampleIndex / channels.Clamp(1, 2) / (frequency != 0 ? frequency : AudioLoader.systemFrequency);
            set => currentSampleIndex = value * channels.Clamp(1, 2) * (frequency != 0 ? frequency : AudioLoader.systemFrequency);
        }

        public double currentSampleIndex
        {
            get => Interlocked.CompareExchange(ref _currentSampleIndex, 0, 0);
            set
            {
                if (currentSampleIndex != value)
                {
                    Interlocked.Exchange(ref _currentSampleIndex, value);
                    TimeChangedEventInvoke();
                }
            }
        }
        double _currentSampleIndex;

        public override double length => _length;
        double _length = 0;



        private void OnEnable()
        {
            Play();
        }



        void Update()
        {
            if (audioSource == null)
                return;
            
            {
                float pitch = (float)realPitch * ((float)frequency / AudioLoader.systemFrequency) * ((float)channels / AudioLoader.systemChannels);
                if (pitch != 0)
                    audioSource.pitch = pitch;
                else
                    audioSource.pitch = 1;
            }

            audioSource.volume = 1;
            //audioSource.panStereo = panStereo;
            audioSource.spatialBlend = spatial ? 1 : 0;

            audioSource.minDistance = minDistance;
            audioSource.maxDistance = maxDistance;

            if (isPlaying && (!audioSource.enabled || !audioSource.isPlaying || audioSource.clip != null))
            {
                audioSource.enabled = true;
                audioSource.Stop();

                audioSource.clip = null;
                audioSource.Play();
            }
        }



        public override void Refresh()
        {
            try
            {
                ThreadManager.Lock(ref onAudioFilterReadLock);

                audioData = AudioLoader.SearchAudioData(key, nameSpace);
                if (audioData == null || audioData.audios.Length <= 0)
                    return;

                audioMetaData = audioData.audios[Random.Range(0, audioData.audios.Length)];

                AudioClip audioClip = audioMetaData.audioClip;
                if (audioClip.loadType != AudioClipLoadType.DecompressOnLoad)
                    return;

                _samples = new float[audioClip.samples * audioClip.channels];
                audioClip.GetData(samples, 0);

                _frequency = audioClip.frequency;
                _channels = audioClip.channels;

                _length = audioClip.length;
            }
            finally
            {
                ThreadManager.Unlock(ref onAudioFilterReadLock);
            }
        }

        public override void Play()
        {
            Stop();
            Refresh();

            if (audioSource != null)
            {
                audioSource.clip = null;
                audioSource.Play();
            }

            base.Play();
        }

        public override void Stop()
        {
            base.Stop();

            try
            {
                ThreadManager.Lock(ref onAudioFilterReadLock);

                if (audioSource != null)
                    audioSource.Stop();

                audioData = null;
                audioMetaData = null;

                _frequency = 0;
                _channels = 0;

                _length = 0;

                Interlocked.Exchange(ref _currentSampleIndex, 0);
            }
            finally
            {
                ThreadManager.Unlock(ref onAudioFilterReadLock);
            }
        }



        [NonSerialized] int onAudioFilterReadLock = 0;
        protected override void OnAudioFilterRead(float[] data, int channels)
        {
            try
            {
                ThreadManager.Lock(ref onAudioFilterReadLock);
                
                if (isPlaying && !isPaused && realTempo != 0 && soundMetaData != null)
                {
                    double currentIndex = currentSampleIndex;
                    float volume = (float)this.volume;
                    bool loop = this.loop;

                    int loopStartIndex = (int)(soundMetaData.loopStartTime / channels / AudioLoader.systemFrequency);
                    int loopOffsetIndex = (int)(soundMetaData.loopOffsetTime / channels / AudioLoader.systemFrequency);

                    if (realPitch > 0)
                    {
                        for (int i = 0; i < data.Length; i += channels)
                        {
                            //현재 재생중인 오디오 채널이 2 보다 크다면 변환 없이 재생
                            if (this.channels > 2)
                            {
                                //정상적으로 재생되지 않을 확률이 높음
                                for (int j = 0; j < channels; j++)
                                    data[i + j] = GetSample(i, channels) * volume;
                            }
                            else if (this.channels == 2) //현재 재생중인 오디오 채널이 2일때
                            {
                                //현재 시스템 채널이 1 보다 크다면 스테레오로 재생
                                if (channels > 1)
                                {
                                    if (!spatial)
                                    {
                                        float left = GetSample(i, 0);
                                        float right = GetSample(i, 1);
                                        float leftStereo = -panStereo.Clamp(-1, 0);
                                        float rightStereo = panStereo.Clamp(0, 1);

                                        data[i] = (left + 0f.Lerp(right, leftStereo)) * (1 - rightStereo) * volume * 1f.Lerp(0.5f, panStereo.Abs());
                                        data[i + 1] = (right + 0f.Lerp(left, rightStereo)) * (1 - leftStereo) * volume * 1f.Lerp(0.5f, panStereo.Abs());
                                    }
                                    else
                                    {
                                        float left = GetSample(i, 0);
                                        float right = GetSample(i, 1);

                                        data[i] = left * volume;
                                        data[i + 1] = right * volume;
                                    }
                                }
                                else //현재 시스템 채널이 1 이하라면 모노로 재생
                                {
                                    /* 
                                     * 오디오 품질이 구려진다
                                     * 왜 그런지는 알 것 같은데 해결 방법을 ㅁ?ㄹ
                                     * 애초에 지금 템포도 구현을 잘못해놔서 이상해지지만 구현을 제대로 하기에는 너무 어렵다
                                     * 나중에 오디오 재생 다시 설계해봐야할 듯?
                                     * 그 나중에가 언제가 될진 모르겠지만...
                                     */
                                    float left = GetSample(i, 0);
                                    float right = GetSample(i, 1);

                                    data[i] = (left + right) * 0.5f * volume;
                                }
                            }
                            else if (this.channels < 2) //현재 재생중인 오디오의 채널이 2 보다 작다면 변환 없이 재생
                            {
                                for (int j = 0; j < channels; j++)
                                    data[i + j] = GetSample(i, 0) * volume / channels;
                            }

                            float GetSample(int i, int j)
                            {
                                int sampleIndex;
                                if (tempo > 0)
                                    sampleIndex = (int)currentIndex + i + j;
                                else
                                    sampleIndex = (int)currentIndex - i + j;

                                if (loop)
                                    sampleIndex = loopStartIndex + sampleIndex.Repeat(samples.Length - 1 - loopStartIndex);

                                if (sampleIndex >= 0 && sampleIndex < samples.Length)
                                {
                                    float sample = samples[sampleIndex];

                                    //루프
                                    if (loop)
                                    {
                                        int rawLoopOffsetSampleIndex = sampleIndex - (samples.Length - 1 - loopOffsetIndex);
                                        int loopOffsetSampleIndex = loopStartIndex + rawLoopOffsetSampleIndex.Repeat(samples.Length - 1 - loopStartIndex); ;

                                        if (rawLoopOffsetSampleIndex >= 0 && loopOffsetSampleIndex >= 0 && loopOffsetSampleIndex < samples.Length)
                                            sample += samples[loopOffsetSampleIndex];
                                    }

                                    return sample;
                                }

                                return 0;
                            }
                        }
                    }

                    {
                        double value = data.Length * (realTempo / (realPitch != 0 ? realPitch : 1));
                        currentIndex += value;

                        if (value == value.Floor())
                            currentIndex = currentIndex.Round();

                        if (loop)
                        {
                            if (currentIndex >= samples.Length)
                            {
                                currentIndex = loopStartIndex + loopOffsetIndex;
                                LoopedEventInvoke();
                            }
                            else if (currentIndex < loopStartIndex + loopOffsetIndex)
                            {
                                currentIndex = samples.Length - 1;
                                LoopedEventInvoke();
                            }
                        }
                    }

                    currentSampleIndex = currentIndex;
                }
            }
            finally
            {
                ThreadManager.Unlock(ref onAudioFilterReadLock);
            }

            base.OnAudioFilterRead(data, channels);
        }
    }
}