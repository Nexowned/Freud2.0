﻿#region USING_DIRECTIVES

using DSharpPlus;
using DSharpPlus.Entities;
using Freud.Common;
using Freud.Common.Collections;
using Freud.Common.Configuration;
using Freud.Common.Tasks;
using Freud.Modules.Administration.Common;
using Freud.Modules.Reactions;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;

#endregion USING_DIRECTIVES

namespace Freud
{
    public sealed class SharedData : IDisposable
    {
        public AsyncExecutor AsyncExecutor { get; }
        public ConcurrentHashSet<ulong> BlockedChannels { get; internal set; }
        public ConcurrentHashSet<ulong> BlockedUsers { get; internal set; }
        public BotConfiguration BotConfiguration { get; internal set; }
        public ConcurrentDictionary<ulong, ConcurrentHashSet<EmojiReaction>> EmojiReactions { get; internal set; }
        public ConcurrentDictionary<ulong, ConcurrentHashSet<Filter>> Filters { get; internal set; }
        public ConcurrentDictionary<ulong, CachedGuildConfiguration> GuildConfigurations { get; internal set; }
        public Logger LogProvider { get; internal set; }
        public bool ListeningStatus { get; internal set; }
        public CancellationTokenSource MainLoopCts { get; internal set; }
        public ConcurrentDictionary<ulong, int> MessageCount { get; internal set; }
        public bool StatusRotationEnabled { get; internal set; }
        public ConcurrentDictionary<ulong, ConcurrentDictionary<int, SavedTaskExecutor>> RemindExecuters { get; internal set; }
        public ConcurrentDictionary<int, SavedTaskExecutor> TaskExecuters { get; internal set; }
        public ConcurrentDictionary<ulong, ConcurrentHashSet<TextReaction>> TextReactions { get; internal set; }
        public UptimeInformation UptimeInformation { get; internal set; }

        private ConcurrentDictionary<ulong, ChannelEvent> ChannelEvents { get; }
        private ConcurrentDictionary<ulong, ConcurrentHashSet<ulong>> PendingResponses { get; }

        public SharedData()
        {
            this.AsyncExecutor = new AsyncExecutor();
            this.BlockedChannels = new ConcurrentHashSet<ulong>();
            this.BlockedUsers = new ConcurrentHashSet<ulong>();
            this.BotConfiguration = BotConfiguration.Default;
            this.ChannelEvents = new ConcurrentDictionary<ulong, ChannelEvent>();
            this.EmojiReactions = new ConcurrentDictionary<ulong, ConcurrentHashSet<EmojiReaction>>();
            this.Filters = new ConcurrentDictionary<ulong, ConcurrentHashSet<Filter>>();
            this.GuildConfigurations = new ConcurrentDictionary<ulong, CachedGuildConfiguration>();
            this.ListeningStatus = true;
            this.MainLoopCts = new CancellationTokenSource();
            this.MessageCount = new ConcurrentDictionary<ulong, int>();
            this.PendingResponses = new ConcurrentDictionary<ulong, ConcurrentHashSet<ulong>>();
            this.RemindExecuters = new ConcurrentDictionary<ulong, ConcurrentDictionary<int, SavedTaskExecutor>>();
            this.StatusRotationEnabled = true;
            this.TaskExecuters = new ConcurrentDictionary<int, SavedTaskExecutor>();
            this.TextReactions = new ConcurrentDictionary<ulong, ConcurrentHashSet<TextReaction>>();
        }

        public void Dispose()
        {
            this.MainLoopCts.Dispose();
            foreach ((int tid, var texec) in this.TaskExecuters)
                texec.Dispose();
        }

        #region CHANNEL_EVENT_HELPERS

        public ChannelEvent GetEventInChannel(ulong cid)
            => this.ChannelEvents.TryGetValue(cid, out ChannelEvent e) ? e : null;

        public bool IsEventRunningInChannel(ulong cid)
           => !(this.GetEventInChannel(cid) is null);

        public void RegisterEventInChannel(ChannelEvent cevent, ulong cid)
            => this.ChannelEvents.AddOrUpdate(cid, cevent, (c, e) => cevent);

        public void UnregisterEventInChannel(ulong cid)
        {
            if (!this.ChannelEvents.TryRemove(cid, out _))
                this.ChannelEvents[cid] = null;
        }

        #endregion CHANNEL_EVENT_HELPERS

        #region RANK_HELPERS

        public short CalculateRankForMessageCount(int msgcount)
            => (short)Math.Floor(Math.Sqrt(msgcount / 10));

        public short CalculateRankForUser(ulong uid)
            => this.MessageCount.TryGetValue(uid, out int count) ? this.CalculateRankForMessageCount(count) : (short)0;

        public int CalculateXpNeededForRank(short index)
            => index * index * 10;

        public int GetMessageCountForUser(ulong uid)
            => this.MessageCount.TryGetValue(uid, out int count) ? count : 0;

        public short IncrementMessageCountForUser(ulong uid)
        {
            this.MessageCount.AddOrUpdate(uid, 1, (k, v) => v + 1);

            short previous = this.CalculateRankForMessageCount(this.MessageCount[uid] - 1);
            short current = this.CalculateRankForMessageCount(this.MessageCount[uid]);

            return current != previous ? current : (short)0;
        }

        #endregion RANK_HELPERS

        #region GUILD_DATA_HELPERS

        public CachedGuildConfiguration GetGuildConfiguration(ulong gid)
        => this.GuildConfigurations.GetOrAdd(gid, CachedGuildConfiguration.Default);

        public string GetGuildPrefix(ulong gid)
        {
            if (this.GuildConfigurations.TryGetValue(gid, out var gcfg) && !string.IsNullOrWhiteSpace(gcfg.Prefix))
                return this.GuildConfigurations[gid].Prefix;
            else
                return this.BotConfiguration.DefaultPrefix;
        }

        public DiscordChannel GetLogChannelForGuild(DiscordClient client, DiscordGuild guild)
        {
            var gcfg = this.GetGuildConfiguration(guild.Id);

            return gcfg.LoggingEnabled ? guild.GetChannel(gcfg.LogChannelId) : null;
        }

        public bool GuildHasTextReaction(ulong gid, string trigger)
            => this.TextReactions.TryGetValue(gid, out var trs) && (trs?.Any(tr => tr.ContainsTriggerPattern(trigger)) ?? false);

        public bool MessageContainsFilter(ulong gid, string message)
        {
            if (!this.Filters.TryGetValue(gid, out var filters) || filters is null)
                return false;
            message = message.ToLowerInvariant();

            return filters.Any(f => f.Trigger.IsMatch(message));
        }

        public void UpdateGuildConfiguration(ulong gid, Func<CachedGuildConfiguration, CachedGuildConfiguration> modifier)
            => this.GuildConfigurations[gid] = modifier(this.GuildConfigurations[gid]);

        #endregion GUILD_DATA_HELPERS

        #region PENDING_RESPONSES_HELPERS

        public void AddPendingResponse(ulong cid, ulong uid)
        {
            this.PendingResponses.AddOrUpdate(cid, new ConcurrentHashSet<ulong> { uid },
                (k, v) => { v.Add(uid); return v; });
        }

        public bool PendingResponseExists(ulong cid, ulong uid)
            => this.PendingResponses.TryGetValue(cid, out var pending) && pending.Contains(uid);

        public bool TryRemovePendingResponse(ulong cid, ulong uid)
        {
            if (!this.PendingResponses.TryGetValue(cid, out var pending))
                return true;

            bool success = pending.TryRemove(uid);
            if (!this.PendingResponses[cid].Any())
                this.PendingResponses.TryRemove(cid, out _);

            return success;
        }

        #endregion PENDING_RESPONSES_HELPERS
    }
}
