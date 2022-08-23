using chronojigs.chunks;
using chronojigs.chronometer;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace chronojigs.Core
{
    class chronojigs : ModSystem
    {
        public override void Start(ICoreAPI api)
        {
            base.Start(api);
            api.World.Logger.Event("'Extra Juice' mod started");
            api.RegisterItemClass("chronometer", typeof(chronometer.chronometer));
        }
        public override void StartServerSide(ICoreServerAPI api)
        {
            base.StartServerSide(api);

        }
    }
}
