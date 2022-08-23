using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.GameContent;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using chronojigs.chunks;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;

namespace chronojigs.chunks
{
    internal class ChunkDataStorage : ModSystem
    {
        private ICoreServerAPI sapi;
        private Dictionary<IServerChunk, int> temporalChunks;

        public override void StartServerSide(ICoreServerAPI api)
        {
            sapi = api;

            api.Event.SaveGameLoaded += OnSaveGameLoading;
            api.Event.GameWorldSave += OnSaveGameSaving;

            api.Event.OnEntityDeath += EntityDeath;
            //api.Event.DidUseBlock += OnUseBlock;
    }

        public static SimpleParticleProperties myParticles = new SimpleParticleProperties(1, 1, ColorUtil.ColorFromRgba(50, 220, 220, 220), new Vec3d(), new Vec3d(), new Vec3f(), new Vec3f());

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

        private void EntityDeath(Entity entity, DamageSource damageSource)
        {
            IServerChunk chunk = sapi.WorldManager.GetChunk(entity.Pos.AsBlockPos);

            int temporalEnergy = 0;
            if (!temporalChunks.ContainsKey(chunk))
            {
                AddChunkToDictionary(chunk);
            }

            if (entity.FirstCodePart() == "drifter")
            {
                var drifterType = entity.LastCodePart();
                switch (drifterType)
                {
                    case "normal":
                        temporalEnergy = 1;
                        temporalChunks[chunk] = temporalChunks[chunk] + temporalEnergy;
                        myParticles.MinQuantity = temporalEnergy*3;
                        break;
                    case "deep":
                        temporalEnergy = 3;
                        temporalChunks[chunk] = temporalChunks[chunk] + temporalEnergy;
                        myParticles.MinQuantity = temporalEnergy * 3;
                        break;
                    case "tainted":
                        temporalEnergy = 7;
                        temporalChunks[chunk] = temporalChunks[chunk] + temporalEnergy;
                        myParticles.MinQuantity = temporalEnergy * 3;
                        break;
                    case "corrupt":
                        temporalEnergy = 16;
                        temporalChunks[chunk] = temporalChunks[chunk] + temporalEnergy;
                        myParticles.MinQuantity = temporalEnergy * 3;
                        break;
                    case "nightmare":
                        temporalEnergy = 35;
                        temporalChunks[chunk] = temporalChunks[chunk] + temporalEnergy;
                        myParticles.MinQuantity = temporalEnergy * 3;
                        break;
                    case "headed":
                        temporalEnergy = 75;
                        temporalChunks[chunk] = temporalChunks[chunk] + temporalEnergy;
                        myParticles.MinQuantity = temporalEnergy * 3;
                        break;
                }
                

            }
            else if (entity.FirstCodePart() == "bell")
            {
                temporalChunks[chunk] = temporalChunks[chunk] + 160;
            }
            else if (entity.FirstCodePart() == "locust")
            {
                var locustType = entity.LastCodePart();
                switch (locustType)
                {
                    case "Bronze":
                        temporalEnergy = 1;
                        temporalChunks[chunk] = temporalChunks[chunk] + temporalEnergy;
                        myParticles.MinQuantity = temporalEnergy * 3;
                        break;
                    case "Corrupt":
                        temporalEnergy = 3;
                        temporalChunks[chunk] = temporalChunks[chunk] + temporalEnergy;
                        myParticles.MinQuantity = temporalEnergy * 3;
                        break;
                    case "sawblade":
                        temporalEnergy = 35;
                        temporalChunks[chunk] = temporalChunks[chunk] + temporalEnergy;
                        myParticles.MinQuantity = temporalEnergy * 3;
                        break;
                }
            }
                ParticleTimer(temporalEnergy, entity);
        }


        private void ParticleTimer(int temporalEnergy,Entity entity)
        {
            Random rand = new Random();
            myParticles.Color = ColorUtil.ToRgba(150 + (temporalEnergy * 2), rand.Next(temporalEnergy, 255), rand.Next(temporalEnergy, 255), rand.Next(temporalEnergy));
            myParticles.GravityEffect = 0.1F;
            myParticles.MinVelocity = new Vec3f((float)rand.NextDouble(),5, (float)rand.NextDouble());
            myParticles.MinSize = 0.1F;
            myParticles.MaxSize = 1.0F;
            myParticles.ParticleModel = EnumParticleModel.Quad;
            myParticles.MinPos = entity.Pos.AsBlockPos.ToVec3d();
            myParticles.AddPos = new Vec3d(0.5, 4, 0.5);
            entity.World.SpawnParticles(myParticles);
        }

        private void AddChunkToDictionary(IServerChunk chunk)
        {
            byte[] data = chunk.GetServerModdata("temporal energy");
            int tEnergy = data == null ? 0 : SerializerUtil.Deserialize<int>(data);

            temporalChunks.Add(chunk, tEnergy);
        }
    }
}
