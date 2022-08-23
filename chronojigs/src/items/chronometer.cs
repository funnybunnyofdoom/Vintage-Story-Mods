using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using chronojigs.chunks;
using chronojigs.Core;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.ServerMods;


namespace chronojigs.chronometer
{
    class chronometer : Item
    {
        private Dictionary<IServerChunk, int> temporalChunks;
        private ICoreAPI capi;
        ICoreServerAPI sapi;
        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);
            capi = api;
            chronojigs.Core.chronojigs coreClass = new Core.chronojigs();
            coreClass.StartServerSide(sapi);


        }
        public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handling)
        {


            

            IWorldChunk chunk = capi.World.BlockAccessor.GetChunk(blockSel.Position.X, blockSel.Position.Y, blockSel.Position.Z);

            System.Diagnostics.Debug.WriteLine("Chronometer USED");
            sapi.BroadcastMessageToAllGroups("YO EVERYONE", Vintagestory.API.Common.EnumChatType.Notification);
            /*if (!temporalChunks.ContainsKey(chunk))
            {
                AddChunkToDictionary(chunk);
            }

            int temporalEnergy = temporalChunks[chunk];
            IPlayer splayer = sapi.World.NearestPlayer(byEntity.Pos.X, byEntity.Pos.Y, byEntity.Pos.Z);
            splayer.
            sapi.SendMessage(splayer, Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "Selected chunk  has " + temporalEnergy + " Temporal Energy.", Vintagestory.API.Common.EnumChatType.Notification);
            */
        }

        private void OnSaveGameLoading()
        {
            temporalChunks = new Dictionary<IServerChunk, int>();
        }

        private void OnSaveGameSaving()
        {
            foreach (KeyValuePair<IServerChunk, int> chunk in temporalChunks)
            {
                if (chunk.Value == 0) continue;
                chunk.Key.SetServerModdata("temporal energy", SerializerUtil.Serialize(chunk.Value));
            }
        }

        private void AddChunkToDictionary(IServerChunk chunk)
        {
            byte[] data = chunk.GetServerModdata("temporal energy");
            int tEnergy = data == null ? 0 : SerializerUtil.Deserialize<int>(data);

            temporalChunks.Add(chunk, tEnergy);
        }
    }
}
