#nullable enable
using Newtonsoft.Json;
using UnityEngine;

namespace RuniEngine.Resource.Sounds
{
    public sealed class AudioMetaData : SoundMetaDataBase
    {
        public AudioMetaData(string path, float pitch, float tempo, bool stream, float loopStartTime, AudioClip audioClip) : base(path, pitch, tempo, stream, loopStartTime) => this.audioClip = audioClip;

        [JsonIgnore] public AudioClip audioClip { get; }
    }
}
