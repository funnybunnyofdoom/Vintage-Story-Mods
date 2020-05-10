using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;


public class SpawnMod : ModSystem
{
	


	public override bool ShouldLoad(EnumAppSide side)
	{
		return side == EnumAppSide.Server;
	}

	public override void StartServerSide(ICoreServerAPI api)
	{
		base.StartServerSide(api);
		api.RegisterCommand("spawn", "Teleports the player to spawn", "",
			(IServerPlayer player, int groupId, CmdArgs args) =>
			{
				EntityPlayer byEntity = player.Entity; //Get the player


				player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "You are teleporting to spawn", Vintagestory.API.Common.EnumChatType.Notification);
				EntityPos spawnpoint = byEntity.World.DefaultSpawnPosition;
				byEntity.TeleportTo(spawnpoint);


			}, Privilege.chat);
	}
}