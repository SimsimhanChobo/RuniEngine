#nullable enable
using Newtonsoft.Json;
using RuniEngine.NBS;

namespace RuniEngine.Resource.Sounds
{
    public sealed class NBSMetaData : SoundMetaDataBase
    {
        public NBSMetaData(string path, double pitch, double tempo, NBSFile? nbsFile) : base(path, pitch, tempo) => this.nbsFile = nbsFile;

        public int loopStartTick => nbsFile?.loopStartTick ?? 0;

        [JsonIgnore] public NBSFile? nbsFile { get; }
    }
}
