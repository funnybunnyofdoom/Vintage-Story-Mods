using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;


public class SpawnMod : ModSystem
{
	ICoreServerAPI sapi;
	Dictionary<string, BlockPos> backSave;

	public override bool ShouldLoad(EnumAppSide side)
	{
		return side == EnumAppSide.Server;
	}

	public override void StartServerSide(ICoreServerAPI api)
	{
		base.StartServerSide(api);
		sapi = api;
		api.Event.SaveGameLoaded += OnSaveGameLoading;
		api.Event.GameWorldSave += OnSaveGameSaving;
		api.RegisterCommand("south", "Teleports the player to the south spawn", "",cmd_spawn, Privilege.chat);
	}

    private void cmd_spawn(IServerPlayer player, int groupId, CmdArgs args)
	{

		EntityPlayer byEntity = player.Entity; //Get the player
		if (backSave.ContainsKey(player.PlayerUID))
		{
			backSave.Remove(player.PlayerUID);
		}
		backSave.Add(player.PlayerUID, player.Entity.Pos.AsBlockPos);

		player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "You are teleporting to southern spawn", Vintagestory.API.Common.EnumChatType.Notification);
		EntityPos spawnpoint = byEntity.World.DefaultSpawnPosition;
		byEntity.TeleportTo((sapi.WorldManager.MapSizeX/2)+1,114,(sapi.WorldManager.MapSizeZ/2)+30000);

	}

	private void OnSaveGameSaving()
	{
		sapi.WorldManager.SaveGame.StoreData("back", SerializerUtil.Serialize(backSave));
	}

	private void OnSaveGameLoading()
	{
		byte[] data = sapi.WorldManager.SaveGame.GetData("back");

		backSave = data == null ? new Dictionary<string, BlockPos>() : SerializerUtil.Deserialize<Dictionary<string, BlockPos>>(data);
	}
}