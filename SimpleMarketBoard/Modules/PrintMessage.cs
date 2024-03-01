using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text;
using System.Collections.Generic;
using System.Linq;
using System;


namespace SimpleMarketBoard;

public class PrintMessage
{
    private readonly Plugin plugin;
    private readonly string messagePrefix;

    public PrintMessage(Plugin plugin)
    {
        this.plugin = plugin;
        messagePrefix = plugin.NameShort;
    }

    public void Dispose()
    {
    }


    // -------------------------------- SeString methods --------------------------------
    public void PrintMessageChat(List<Payload> payloadList)
    {
        if (this.plugin.Config.ChatLogChannel == XivChatType.None)
        {
            Service.Chat.Print(new XivChatEntry
            {
                Message = BuildSeString(messagePrefix, payloadList),
            });
        }
        else
        {
            Service.Chat.Print(new XivChatEntry
            {
                Message = BuildSeString(messagePrefix, payloadList),
                Type = this.plugin.Config.ChatLogChannel,
            });
        }
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


    // -------------------------------- toast message methods --------------------------------
    public void PrintMessageToast(string message)
    {
        Service.Toasts.ShowNormal(message);
    }

}
