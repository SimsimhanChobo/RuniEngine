#nullable enable
using Cysharp.Threading.Tasks;
using RuniEngine.Booting;
using RuniEngine.Threading;
using System.Collections.Generic;
using System.IO;
using UnityEngine.Networking;
using UnityEngine;
using RuniEngine.Jsons;
using System.Linq;
using System.Threading;
using OggVorbis;
using System;

using Object = UnityEngine.Object;
using NVorbis;
using NAudio.Wave;
using NVorbis.Contracts;
using NAudio.Vorbis;
using static UnityEngine.Analytics.IAnalytic;
using System.Diagnostics;

namespace RuniEngine.Resource.Sounds
{
    public sealed class AudioLoader : IResourceElement
    {
        public const string soundsNameSpace = "runi-sounds";

        public static int systemFrequency { get => Interlocked.Add(ref _systemFrequency, 0); }
        static int _systemFrequency = 48000;

        public static int systemChannels { get => Interlocked.Add(ref _systemChannels, 0); }
        static int _systemChannels = 2;

        public static AudioListener? audioListener
        {
            get
            {
                if (_audioListener == null || !_audioListener.isActiveAndEnabled)
                    _audioListener = Object.FindFirstObjectByType<AudioListener>();

                return _audioListener;
            }
        }

        static AudioListener? _audioListener;



        public bool isLoaded { get; private set; } = false;
        public ResourcePack? resourcePack { get; set; } = null;



        /// <summary>
        /// AudioData = allAudios[nameSpace][key];
        /// </summary>
        Dictionary<string, Dictionary<string, AudioData>> allAudios = new();



        public const string name = "audios";
        string IResourceElement.name => name;



#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
        static void InitializeOnLoadMethod() => OnAudioConfigurationChanged(false);
#endif



        [Awaken]
        static void Awaken()
        {
            OnAudioConfigurationChanged(false);

            AudioSettings.OnAudioConfigurationChanged += OnAudioConfigurationChanged;
            Kernel.quitting += () => AudioSettings.OnAudioConfigurationChanged -= OnAudioConfigurationChanged;
        }

        static void OnAudioConfigurationChanged(bool deviceWasChanged)
        {
            Interlocked.Exchange(ref _systemFrequency, AudioSettings.outputSampleRate);

            int driverChannels = 1;
            switch (AudioSettings.driverCapabilities)
            {
                case AudioSpeakerMode.Mono:
                    driverChannels = 1;
                    break;
                case AudioSpeakerMode.Stereo:
                    driverChannels = 2;
                    break;
                case AudioSpeakerMode.Quad:
                    driverChannels = 4;
                    break;
                case AudioSpeakerMode.Surround:
                    driverChannels = 5;
                    break;
                case AudioSpeakerMode.Mode5point1:
                    driverChannels = 6;
                    break;
                case AudioSpeakerMode.Mode7point1:
                    driverChannels = 8;
                    break;
                case AudioSpeakerMode.Prologic:
                    driverChannels = 2;
                    break;
            }

            switch (AudioSettings.speakerMode)
            {
                case AudioSpeakerMode.Mono:
                    Interlocked.Exchange(ref _systemChannels, 1.Min(driverChannels));
                    break;
                case AudioSpeakerMode.Stereo:
                    Interlocked.Exchange(ref _systemChannels, 2.Min(driverChannels));
                    break;
                case AudioSpeakerMode.Quad:
                    Interlocked.Exchange(ref _systemChannels, 4.Min(driverChannels));
                    break;
                case AudioSpeakerMode.Surround:
                    Interlocked.Exchange(ref _systemChannels, 5.Min(driverChannels));
                    break;
                case AudioSpeakerMode.Mode5point1:
                    Interlocked.Exchange(ref _systemChannels, 6.Min(driverChannels));
                    break;
                case AudioSpeakerMode.Mode7point1:
                    Interlocked.Exchange(ref _systemChannels, 8.Min(driverChannels));
                    break;
                case AudioSpeakerMode.Prologic:
                    Interlocked.Exchange(ref _systemChannels, 2.Min(driverChannels));
                    break;
            }
        }

        public static AudioData? SearchAudioData(string path, string nameSpace = "")
        {
            ResourceManager.SetDefaultNameSpace(ref nameSpace);

            AudioData? result = null;
            ResourceManager.ResourceElementLoop<AudioLoader>(x =>
            {
                if (x.allAudios.TryGetValue(nameSpace, out var value) && value.TryGetValue(path, out AudioData value2))
                {
                    result = value2;
                    return true;
                }

                return false;
            });

            return result;
        }



        public static RawAudioClip? GetRawAudio(string path)
        {
            if (!File.Exists(path))
                return null;

            int channels;
            int frequency;
            long samples;

            float[] buffer;
            float[] datas;

            WaveStream byteReader;
            if (path.EndsWith(".ogg", StringComparison.OrdinalIgnoreCase))
                byteReader = new VorbisWaveReader(path);
            else
                byteReader = new AudioFileReader(path);

            ISampleProvider reader = byteReader.ToSampleProvider();

            try
            {
                channels = byteReader.WaveFormat.Channels;
                frequency = byteReader.WaveFormat.SampleRate;
                samples = byteReader.Length / 4;

                buffer = new float[channels * frequency];
                datas = new float[samples];
                
                int position = 0;
                int readSampleLength;

                int datasLength = datas.Length;
                int bufferLength = buffer.Length;

                while ((readSampleLength = reader.Read(buffer, 0, bufferLength)) > 0)
                {
                    for (int i = 0; i < readSampleLength; i++)
                    {
                        if (position + i >= datasLength)
                            break;

                        datas[position + i] = buffer[i];
                    }

                    position += readSampleLength;
                }

                return new RawAudioClip(datas, frequency, channels);
            }
            catch (Exception e)
            {
                Debug.Log(path);
                Debug.LogException(e);
            }
            finally
            {
                byteReader.Dispose();
            }

            /*if (path.EndsWith(".ogg", StringComparison.OrdinalIgnoreCase))
            {
                using VorbisReader reader = new VorbisReader(path);

                try
                {
                    int channels = reader.Channels;
                    int frequency = reader.SampleRate;
                    long samples = reader.TotalSamples;

                    float[] buffer = new float[frequency * channels];
                    float[] datas = new float[samples * channels];

                    Stopwatch stopwatch = Stopwatch.StartNew();
                    Debug.Log("start : " + Path.GetFileName(path));

                    int readSampleLength;
                    while ((readSampleLength = reader.ReadSamples(buffer, 0, buffer.Length)) > 0)
                    {
                        long curPosition = (reader.SamplePosition * channels) - readSampleLength;
                        for (int i = 0; i < readSampleLength; i++)
                            datas[curPosition + i] = buffer[i];
                    }

                    Debug.Log("end : " + Path.GetFileName(path));
                    Debug.Log(stopwatch.Elapsed);
                    stopwatch.Stop();

                    return new RawAudioClip(datas, frequency, channels);
                }
                catch (Exception e)
                {
                    Debug.Log(path);
                    Debug.LogException(e);
                }
            }
            else
            {
                int channels;
                int frequency;
                long samples;

                float[] buffer;
                float[] datas;

                WaveStream byteReader = new AudioFileReader(path);
                ISampleProvider reader = byteReader.ToSampleProvider();

                try
                {
                    channels = byteReader.WaveFormat.Channels;
                    frequency = byteReader.WaveFormat.SampleRate;
                    samples = byteReader.Length;

                    buffer = new float[channels * frequency];
                    List<float> dataLists = new List<float>((int)samples.Clamp(0, int.MaxValue));

                    int position = 0;
                    int readSampleLength;

                    Stopwatch stopwatch = Stopwatch.StartNew();
                    while ((readSampleLength = reader.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        dataLists.AddRange(buffer);
                        position += readSampleLength;
                    }

                    samples = position;
                    datas = dataLists.ToArray();

                    stopwatch.Stop();

                    return new RawAudioClip(datas, frequency, channels);
                }
                catch (Exception e)
                {
                    Debug.Log(path);
                    Debug.LogException(e);
                }
                finally
                {
                    byteReader.Dispose();
                }
            }*/

            return null;
        }



        public static async UniTask<AudioClip?> GetAudio(string path, AudioType type, bool stream = false, HideFlags hideFlags = HideFlags.DontSave)
        {
#if !((UNITY_STANDALONE_LINUX && !UNITY_EDITOR) || UNITY_EDITOR_LINUX)
            if (File.Exists(path))
            {
                if (type == AudioType.OGGVORBIS && !stream)
                    return await VorbisPlugin.ToAudioClipAsync(await File.ReadAllBytesAsync(path), Path.GetFileNameWithoutExtension(path));

                NotMainThreadException.Exception();

                using UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(path.UrlPathPrefix(), type);
                DownloadHandlerAudioClip downloadHandlerAudioClip = (DownloadHandlerAudioClip)www.downloadHandler;
                downloadHandlerAudioClip.streamAudio = stream;

                await www.SendWebRequest();

                if (www.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError(www.error);
                    return null;
                }

                AudioClip audioClip = DownloadHandlerAudioClip.GetContent(www);
                audioClip.name = Path.GetFileNameWithoutExtension(path);
                audioClip.hideFlags = hideFlags;

                ResourceManager.allLoadedResources.Add(audioClip);
                return audioClip;
            }
#endif
            return null;
        }



        public static string[] GetSoundDataKeys(string nameSpace = "")
        {
            ResourceManager.SetDefaultNameSpace(ref nameSpace);

            string[] result = Array.Empty<string>();
            if (!ResourceManager.ResourceElementLoop<AudioLoader>(x =>
            {
                if (!x.isLoaded)
                    return false;

                if (x.allAudios.ContainsKey(nameSpace))
                {
                    result = x.allAudios[nameSpace].Keys.ToArray();
                    return true;
                }
                else
                    return false;
            }))
            {
                string path = Path.Combine(Kernel.streamingAssetsPath, ResourceManager.rootName, nameSpace, name);
                Dictionary<string, AudioData>? audioDatas = JsonManager.JsonRead<Dictionary<string, AudioData>>(path + ".json");
                if (audioDatas != null)
                    result = audioDatas.Keys.ToArray();
            }

            return result;
        }



        public UniTask Load() => Load(null);
        public async UniTask Load(IProgress<float>? progress)
        {
            if (resourcePack == null)
                return;

            await UniTask.SwitchToThreadPool();

            Dictionary<string, Dictionary<string, AudioData>> tempAllAudios = new();

            for (int i = 0; i < resourcePack.nameSpaces.Count; i++)
            {
                string nameSpace = resourcePack.nameSpaces[i];
                string folderPath = Path.Combine(resourcePack.path, ResourceManager.rootName, nameSpace, name);

                Dictionary<string, AudioData>? audioDatas = JsonManager.JsonRead<Dictionary<string, AudioData>>(folderPath + ".json");
                if (audioDatas == null)
                {
                    ReportProgress();
                    continue;
                }

                List<UniTask> tasks = new List<UniTask>();
                foreach (var audioData in audioDatas)
                {
                    tasks.Add(Task());

                    //병렬 로드
                    async UniTask Task()
                    {
                        if (audioData.Value.audios == null)
                            return;

                        List<UniTask> tasks2 = new List<UniTask>();
                        List<AudioMetaData> audioMetaDatas = new List<AudioMetaData>();

                        for (int i = 0; i < audioData.Value.audios.Length; i++)
                        {
                            tasks2.Add(Task2());

                            //병렬 로드 2
                            async UniTask Task2()
                            {
                                AudioMetaData? audioMetaData = audioData.Value.audios[i];
                                string audioPath = Path.Combine(folderPath, audioMetaData.path);

                                if (!ResourceManager.FileExtensionExists(audioPath, out audioPath, ExtensionFilter.musicFileFilter))
                                    return;

                                AudioType audioType = Path.GetExtension(audioPath).ToLower() switch
                                {
                                    ".ogg" => AudioType.OGGVORBIS,
                                    ".mp3" => AudioType.MPEG,
                                    ".mp2" => AudioType.MPEG,
                                    ".wav" => AudioType.WAV,
                                    ".aiff" => AudioType.AIFF,
                                    ".xm" => AudioType.XM,
                                    ".mod" => AudioType.MOD,
                                    ".it" => AudioType.IT,
                                    ".vag" => AudioType.VAG,
                                    ".xma" => AudioType.XMA,
                                    ".s3m" => AudioType.S3M,
                                    _ => AudioType.UNKNOWN,
                                };

                                RawAudioClip? rawAudioClip = null;
                                /*if (audioType == AudioType.OGGVORBIS)
                                    rawAudioClip = await UniTask.RunOnThreadPool(() => GetRawAudio(audioPath));
                                else*/
                                {
                                    AudioClip? audioClip = await await ThreadDispatcher.Execute(() => GetAudio(audioPath, audioType));
                                    if (audioClip != null)
                                    {
                                        rawAudioClip = new RawAudioClip(audioClip);
                                        Object.DestroyImmediate(audioClip);
                                    }
                                }

                                if (rawAudioClip != null)
                                {
#if ENABLE_RUNI_ENGINE_RHYTHMS
                                    audioMetaData = new AudioMetaData(audioMetaData.path, audioMetaData.pitch, audioMetaData.tempo, audioMetaData.loopStartIndex, audioMetaData.loopOffsetIndex, audioMetaData.bpms, audioMetaData.rhythmOffsetIndex, rawAudioClip);
#else
                                    audioMetaData = new AudioMetaData(audioMetaData.path, audioMetaData.pitch, audioMetaData.tempo, audioMetaData.loopStartIndex, audioMetaData.loopOffsetIndex, rawAudioClip);
#endif
                                }

                                if (audioMetaData != null)
                                    audioMetaDatas.Add(audioMetaData);
                            }
                        }

                        await UniTask.WhenAll(tasks2);

                        tempAllAudios.TryAdd(nameSpace, new Dictionary<string, AudioData>());
                        tempAllAudios[nameSpace].TryAdd(audioData.Key, new AudioData(audioData.Value.subtitle, audioData.Value.isBGM, audioMetaDatas.ToArray()));
                    }
                }

                await UniTask.WhenAll(tasks);

                ReportProgress();

                void ReportProgress() => progress?.Report((float)(i + 1) / resourcePack.nameSpaces.Count);
            }
            Debug.Log("Audio Loading End");
            await UniTask.SwitchToMainThread(PlayerLoopTiming.Initialization);

            allAudios = tempAllAudios;
            isLoaded = true;
        }

        public async UniTask Unload()
        {
            allAudios = new();
            isLoaded = false;

            await UniTask.CompletedTask;
        }
    }
}
