﻿using Dalamud.Configuration;
using Dalamud.Plugin;
using System;
using Dalamud.Game.ClientState.Keys;
using System.Collections.Generic;
using Dalamud.Game.Text;

namespace SimpleMarketBoard
{

    [Serializable]
    public class SimpleMarketBoardConfig : IPluginConfiguration
    {
        public int Version { get; set; } = 0;

        // ----------------- General -----------------
        public int HoverDelayMS { get; set; } = 1000;
        public bool EnableRecentHistory { get; set; } = true;
        public bool TotalIncludeTax { get; set; } = true;
        public bool CleanCacheAsYouGo { get; set; } = true;

        // ----------------- Keybinding -----------------
        public bool KeybindingEnabled { get; set; } = true;
        public bool AllowKeybindingAfterHover { get; set; } = true;
        public VirtualKey[] BindingHotkey { get; set; } = new VirtualKey[] { VirtualKey.CONTROL, VirtualKey.X };

        // ----------------- API & cache -----------------
        public int RequestTimeoutMS { get; set; } = 10000;
        public int MaxCacheItems { get; set; } = 30;
        public string selectedWorld { get; set; } = "";


        // ----------------- Message -----------------
        public bool EnableChatLog { get; set; } = true;
        public bool EnableToastLog { get; set; } = true;
        public XivChatType ChatLogChannel { get; set; } = XivChatType.None;

        // ----------------- Cache -----------------
        public List<ulong> SearchHistoryId { get; set; } = new List<ulong>();
        public bool FilterHQ { get; set; } = false;

        // the below exist just to make saving less cumbersome
        [NonSerialized]
        private DalamudPluginInterface? pluginInterface;

        public void Initialize(DalamudPluginInterface pluginInterface)
        {
            this.pluginInterface = pluginInterface;
        }

        public void Save()
        {
            this.pluginInterface!.SavePluginConfig(this);
        }
    }
}