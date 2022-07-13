using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace simpleservermessage.src
{
    class simpleservermessage : ModSystem
    {
        ICoreServerAPI myAPI;
        int messageplace = 0;
        long BCL; //Event listener

        public override bool ShouldLoad(EnumAppSide side)
        {
            return side == EnumAppSide.Server;
        }

        public override void StartServerSide(ICoreServerAPI api)
        {
            base.StartServerSide(api);
            myAPI = api;
            IPermissionManager ipm = api.Permissions;
            ipm.RegisterPrivilege("ssm","Simple Server Messages");
            ipm.RemovePrivilegeFromGroup("suplayer", BPrivilege.ssm);
            ipm.AddPrivilegeToGroup("admin", BPrivilege.ssm);
            api.RegisterCommand("ssm", "Simple Server Message Management", "[add|remove|list|frequency|now|help|version]", cmd_ssm, BPrivilege.ssm);
            System.Console.WriteLine("Simple Server Message loaded - [Author: FunnyBunnyofDOOM]");
            try
            {
                var Config = api.LoadModConfig<ssmConfig>("ssmconfig.json");
                if (Config != null)
                {
                    api.Logger.Notification("Mod Config successfully loaded.");
                    ssmConfig.Current = Config;
                }
                else
                {
                    api.Logger.Notification("No Mod Config specified. Falling back to default settings");
                    ssmConfig.Current = ssmConfig.getDefault();
                }
            }
            catch
            {
                ssmConfig.Current = ssmConfig.getDefault();
                api.Logger.Error("Failed to load custom mod configuration. Falling back to default settings!");
            }
            finally
            {
                if (ssmConfig.Current.messages == null)
                    ssmConfig.Current.messages = ssmConfig.getDefault().messages;
                if (ssmConfig.Current.frequency == null)
                    ssmConfig.Current.frequency = ssmConfig.getDefault().frequency;

                api.StoreModConfig(ssmConfig.Current, "ssmconfig.json");
            }
            int broadcastFrequency = (int) ssmConfig.Current.frequency;
            BCL = myAPI.Event.RegisterGameTickListener(broadcast, (broadcastFrequency * 100000)); //This has to be after the config try statement so that all the values are filled
        }

        private void cmd_ssm(IServerPlayer player, int groupId, CmdArgs args)
        {
            string cmd = args.PopWord();
            switch (cmd)
            {
                case "add":       
                    string text = args.PopAll();
                    ssmConfig.Current.messages.Add(text);
                    myAPI.StoreModConfig(ssmConfig.Current,"ssmconfig.json");
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "Added message.", Vintagestory.API.Common.EnumChatType.Notification);
                    break;
                case "remove":
                    int? listindex = args.PopInt();
                    if (listindex != null)
                    {
                        int lindex = (int)listindex;

                        List<string> msglist = ssmConfig.Current.messages;
                        if (msglist.Count >= lindex)
                        {
                            string removemsg = msglist.ElementAt(lindex);
                            ssmConfig.Current.messages.Remove(removemsg);
                            myAPI.StoreModConfig(ssmConfig.Current, "ssmconfig.json");
                            player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "Removed message", Vintagestory.API.Common.EnumChatType.Notification);
                        }
                        else
                        {
                            player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "Please use /ssm list and then use /ssm remove number to delete the message", Vintagestory.API.Common.EnumChatType.Notification);
                        }
                    }
                    else
                    {
                        player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "Please use /ssm remove aNumber to remove a message", Vintagestory.API.Common.EnumChatType.Notification);
                    }
                    break;
                case "list":
                    List<string> listofmessages = ssmConfig.Current.messages;
                    int lastindex = listofmessages.Count;
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "List of current server messages:", Vintagestory.API.Common.EnumChatType.Notification);
                    for (int i = 0; i < lastindex; i++)
                    {
                        player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, i+" : "+listofmessages[i], Vintagestory.API.Common.EnumChatType.Notification);
                    }
                    break;
                case "frequency":
                    int? frqnum = args.PopInt();
                    if (frqnum != null & frqnum >=1)
                    {
                        ssmConfig.Current.frequency = frqnum;
                        myAPI.StoreModConfig(ssmConfig.Current, "ssmconfig.json");
                        player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "Broadcast Message Frequency set to " + frqnum + " Minutes.", Vintagestory.API.Common.EnumChatType.Notification);
                        myAPI.Event.UnregisterGameTickListener(BCL);
                        int bcFrequency = (int)ssmConfig.Current.frequency;
                        BCL = myAPI.Event.RegisterGameTickListener(broadcast,(bcFrequency*100000));
                    }
                    else
                    {
                        player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "Please enter a number in minutes", Vintagestory.API.Common.EnumChatType.Notification);
                    }
                    break;
                case "now":
                    if (ssmConfig.Current.messages.Count > 0)
                    {
                        List<String> messagelist = ssmConfig.Current.messages;


                        if (messageplace < messagelist.Count)
                        {
                            myAPI.BroadcastMessageToAllGroups(messagelist[messageplace], Vintagestory.API.Common.EnumChatType.AllGroups);
                            messageplace++;
                        }
                        else
                        {
                            messageplace = 0;
                            myAPI.BroadcastMessageToAllGroups(messagelist[messageplace], Vintagestory.API.Common.EnumChatType.AllGroups);
                        }
                    }
                    break;
                case "help":
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "/ssm add <i>Server Message</i> - adds a message to the list of server messages", Vintagestory.API.Common.EnumChatType.Notification);
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "/ssm list - lists the existing server messages ", Vintagestory.API.Common.EnumChatType.Notification);
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "/ssm remove <i>number from /ssm list</i> - remove the message from the number in the list", Vintagestory.API.Common.EnumChatType.Notification);
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "/ssm frequency <i>number in minutes</i> - changes the duration of minutes between messages", Vintagestory.API.Common.EnumChatType.Notification);
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "/ssm now - broadcasts the next server message in line", Vintagestory.API.Common.EnumChatType.Notification);
                    break;
                case "version":
                    var modinfo = Mod.Info;
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "Mod Name: " + modinfo.Name + " | Author: FunnyBunnyofDOOM | Version: " + modinfo.Version, Vintagestory.API.Common.EnumChatType.Notification);
                    break;
                case null:
                    player.SendMessage(Vintagestory.API.Config.GlobalConstants.GeneralChatGroup, "use /ssm help|add|remove|list|frequency|now", Vintagestory.API.Common.EnumChatType.Notification);
                    break;

            }
        }

        private void broadcast(float obj)
        {
            if (ssmConfig.Current.messages.Count > 0)
            {
                List<String> messagelist = ssmConfig.Current.messages;
                
                
                if (messageplace < messagelist.Count)
                {
                    myAPI.BroadcastMessageToAllGroups(messagelist[messageplace], Vintagestory.API.Common.EnumChatType.AllGroups);
                    messageplace++;
                }
                else
                {
                    messageplace = 0;
                    myAPI.BroadcastMessageToAllGroups(messagelist[messageplace], Vintagestory.API.Common.EnumChatType.AllGroups);
                }
            }
        }

        public class ssmConfig
        {
            public static ssmConfig Current { get; set; }

            public List<String> messages { get; set; }
            public int? frequency;

            public static ssmConfig getDefault()
            {
                var config = new ssmConfig();
                List<String> dmessages = new List<string>
                {
                    "Welcome to the server!"
                };
                int frq = 10;

                config.messages = dmessages;
                config.frequency = frq;
                return config;
            }
        }
        public class BPrivilege : Privilege
        {
            /// <summary>
            /// Ability to use Simple Server Message commands
            /// </summary>
            public static string ssm = "ssm";
        }

    }
}
