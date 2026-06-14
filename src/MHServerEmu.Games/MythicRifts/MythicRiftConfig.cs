using MHServerEmu.Core.Config;

namespace MHServerEmu.Games.MythicRifts
{
    public sealed class MythicRiftConfig : ConfigContainer
    {
        public bool EnableThirtyWaveMode { get; private set; } = true;
    }
}
