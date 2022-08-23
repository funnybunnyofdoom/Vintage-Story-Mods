using chronojigs.chunks;
using chronojigs.chronometer;
using chronojigs.altars;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using System;

namespace chronojigs.Core
{
    public class chronojigs : ModSystem
    {

        ICoreServerAPI sapi;
        public override void Start(ICoreAPI api)
        {
            base.Start(api);
            api.World.Logger.Event("'Extra Juice' mod started");
            api.RegisterItemClass("chronometer", typeof(chronometer.chronometer));
            api.RegisterBlockEntityClass("energyaltarentity", typeof(altars.energyaltarentity));
        }
        public override void StartServerSide(ICoreServerAPI api)
        {
            base.StartServerSide(api);
            sapi = api;
        }

        public void messageplayers(string puid,int tEnergy)
        {
            System.Diagnostics.Debug.WriteLine("Energy is " + tEnergy);
        }

       
    }
    
}
