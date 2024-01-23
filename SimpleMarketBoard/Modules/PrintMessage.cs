using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Dalamud.Configuration;
using Dalamud.ContextMenu;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.IoC;
using Dalamud.Logging;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;



namespace SimpleMarketBoard
{
    public class PrintMessage
    {

        private Plugin plugin;

        public PrintMessage(Plugin plugin)
        {
            this.plugin = plugin;
        }

        private static SeString BuildSeString(string? pluginName, IEnumerable<Payload> payloads)
        {
            var basePayloads = BuildBasePayloads(pluginName);
            return new SeString(basePayloads.Concat(payloads).ToList());
        }

        private static IEnumerable<Payload> BuildBasePayloads(string? pluginName) => new List<Payload>
        {
            new UIForegroundPayload(0), new TextPayload($"[{pluginName}] "), new UIForegroundPayload(548),
        };

        public void PrintMessageChat(List<Payload> payloadList)
        {
            if (this.plugin.Config.ChatLogChannel == XivChatType.None)
            {
                Service.Chat.Print(new XivChatEntry
                {
                    Message = BuildSeString("SMB", payloadList),
                });
            }
            else
            {
                Service.Chat.Print(new XivChatEntry
                {
                    Message = BuildSeString("SMB", payloadList),
                    Type = this.plugin.Config.ChatLogChannel,
                });
            }
        }

        public void PrintMessageToast(string message)
        {
            Service.Toasts.ShowNormal(message);
        }

    }
}
