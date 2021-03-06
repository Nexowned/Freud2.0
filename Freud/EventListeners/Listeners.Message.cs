﻿#region USING_DIRECTIVES

using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Freud.Common;
using Freud.Common.Attributes;
using Freud.Common.Configuration;
using Freud.Database.Db.Entities;
using Freud.Discord.Extensions;
using Freud.EventListeners.Extensions;
using Freud.Extensions;
using Freud.Extensions.Discord;
using Freud.Modules.Administration.Services;
using Humanizer;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;
using System.Threading.Tasks;

#endregion USING_DIRECTIVES

namespace Freud.EventListeners
{
    internal static partial class Listeners
    {
        [AsyncEventListener(DiscordEventType.MessagesBulkDeleted)]
        public static async Task BulkDeleteEventHandlerAsync(FreudShard shard, MessageBulkDeleteEventArgs e)
        {
            if (e.Channel.IsPrivate)
                return;

            var logchn = shard.SharedData.GetLogChannelForGuild(shard.Client, e.Channel.Guild);
            if (logchn is null || e.Channel.IsExempted(shard))
                return;

            var emb = FormEmbedBuilder(EventOrigin.Message, $"Bulk message deletiong occured ({e.Messages.Count} total)", $"In channel {e.Channel.Mention}");
            await logchn.SendMessageAsync(embed: emb.Build());
        }

        [AsyncEventListener(DiscordEventType.MessageCreated)]
        public static async Task MessageCreateEventHandlerAsync(FreudShard shard, MessageCreateEventArgs e)
        {
            if (e.Author.IsBot || e.Channel.IsPrivate)
                return;
            if (shard.SharedData.BlockedChannels.Contains(e.Channel.Id) || shard.SharedData.BlockedUsers.Contains(e.Author.Id))
                return;
            if (!e.Channel.PermissionsFor(e.Guild.CurrentMember).HasFlag(Permissions.SendMessages))
                return;
            if (!string.IsNullOrWhiteSpace(e.Message?.Content) && !e.Message.Content.StartsWith(shard.SharedData.GetGuildPrefix(e.Guild.Id)))
            {
                short rank = shard.SharedData.IncrementMessageCountForUser(e.Author.Id);
                if (rank != 0)
                {
                    DatabaseGuildRank rankInfo;
                    using (var dc = shard.Database.CreateContext())
                        rankInfo = dc.GuildRanks.SingleOrDefault(r => r.GuildId == e.Guild.Id && r.Rank == rank);

                    await e.Channel.EmbedAsync($"GG {e.Author.Mention}! You have advanced to level {Formatter.Bold(rank.ToString())} {(rankInfo is null ? "" : $": {Formatter.Italic(rankInfo.Name)}")} !", StaticDiscordEmoji.Medal);
                }
            }
        }

        [AsyncEventListener(DiscordEventType.MessageCreated)]
        public static async Task MessageCreateProtectionHandlerAsync(FreudShard shard, MessageCreateEventArgs e)
        {
            if (e.Author.IsBot || e.Channel.IsPrivate || string.IsNullOrWhiteSpace(e.Message?.Content))
                return;
            if (shard.SharedData.BlockedChannels.Contains(e.Channel.Id))
                return;

            var gcfg = shard.SharedData.GetGuildConfiguration(e.Guild.Id);

            if (gcfg.RatelimitSettings.Enabled)
                await shard.CNext.Services.GetService<RatelimitService>().HandleNewMessageAsync(e, gcfg.RatelimitSettings);
            if (gcfg.AntispamSettings.Enabled)
                await shard.CNext.Services.GetService<AntispamService>().HandleNewMessageAsync(e, gcfg.AntispamSettings);
        }

        [AsyncEventListener(DiscordEventType.MessageCreated)]
        public static async Task MessageFilterEventHandlerAsync(FreudShard shard, MessageCreateEventArgs e)
        {
            if (e.Author.IsBot || e.Channel.IsPrivate || string.IsNullOrWhiteSpace(e.Message?.Content))
                return;
            if (shard.SharedData.BlockedChannels.Contains(e.Channel.Id))
                return;

            var gcfg = shard.SharedData.GetGuildConfiguration(e.Guild.Id);

            if (gcfg.LinkfilterSettings.Enabled)
            {
                if (await shard.CNext.Services.GetService<LinkfilterService>().HandleNewMessageAsync(e, gcfg.LinkfilterSettings))
                    return;
            }

            if (!shard.SharedData.MessageContainsFilter(e.Guild.Id, e.Message.Content))
                return;
            if (!e.Channel.PermissionsFor(e.Guild.CurrentMember).HasFlag(Permissions.ManageMessages))
                return;

            await e.Message.DeleteAsync("bot: Filter hit");
            await e.Channel.SendMessageAsync($"{e.Author.Mention} said: {FormatterExtensions.Spoiler(Formatter.BlockCode(FormatterExtensions.StripMarkdown(e.Message.Content)))}");
        }

        [AsyncEventListener(DiscordEventType.MessageCreated)]
        public static async Task MessageEmojiReactionEventHandlerAsync(FreudShard shard, MessageCreateEventArgs e)
        {
            if (e.Author.IsBot || e.Channel.IsPrivate || string.IsNullOrWhiteSpace(e.Message?.Content))
                return;
            if (shard.SharedData.BlockedChannels.Contains(e.Channel.Id) || shard.SharedData.BlockedUsers.Contains(e.Author.Id))
                return;
            if (!e.Channel.PermissionsFor(e.Guild.CurrentMember).HasFlag(Permissions.AddReactions))
                return;
            if (!shard.SharedData.EmojiReactions.TryGetValue(e.Guild.Id, out var ereactions))
                return;

            var ereaction = ereactions?.Where(er => er.IsMatch(e.Message?.Content ?? "")).Shuffle().FirstOrDefault();

            if (!(ereaction is null))
            {
                try
                {
                    var emoji = DiscordEmoji.FromName(shard.Client, ereaction.Response);

                    await e.Message.CreateReactionAsync(emoji);
                } catch
                {
                    using (var dc = shard.Database.CreateContext())
                    {
                        dc.EmojiReactions.RemoveRange(dc.EmojiReactions.Where(er => er.GuildId == e.Guild.Id && er.Reaction == ereaction.Response));

                        await dc.SaveChangesAsync();
                    }
                }
            }
        }

        [AsyncEventListener(DiscordEventType.MessageCreated)]
        public static async Task MessageTextReactionEventHandlerAsync(FreudShard shard, MessageCreateEventArgs e)
        {
            if (e.Author.IsBot || e.Channel.IsPrivate || string.IsNullOrWhiteSpace(e.Message?.Content))
                return;
            if (e.Message.Content.StartsWith(shard.SharedData.GetGuildPrefix(e.Guild.Id)))
                return;
            if (shard.SharedData.BlockedChannels.Contains(e.Channel.Id) || shard.SharedData.BlockedUsers.Contains(e.Author.Id))
                return;
            if (!e.Channel.PermissionsFor(e.Guild.CurrentMember).HasFlag(Permissions.SendMessages))
                return;
            if (!shard.SharedData.TextReactions.TryGetValue(e.Guild.Id, out var treactions))
                return;

            var tr = treactions?.FirstOrDefault(r => r.IsMatch(e.Message.Content));

            if (!tr?.IsCooldownActive() ?? false)

                await e.Channel.SendMessageAsync(tr.Response.Replace("%user%", e.Author.Mention));
        }

        [AsyncEventListener(DiscordEventType.MessageDeleted)]
        public static async Task MessageDeleteEventHandlerAsync(FreudShard shard, MessageDeleteEventArgs e)
        {
            if (e.Channel.IsPrivate || e.Message is null)
                return;

            var logchn = shard.SharedData.GetLogChannelForGuild(shard.Client, e.Guild);
            if (logchn is null || e.Channel.IsExempted(shard))
                return;
            if (e.Message.Author == e.Client.CurrentUser && shard.SharedData.IsEventRunningInChannel(e.Channel.Id))
                return;

            var emb = FormEmbedBuilder(EventOrigin.Message, "Message deleted");
            emb.AddField("Location", e.Channel.Mention, inline: true);
            emb.AddField("Author", e.Message.Author?.Mention ?? _unknown, inline: true);

            var entry = await e.Guild.GetLatestAuditLogEntryAsync(AuditLogActionType.MessageDelete);
            if (!(entry is null) && entry is DiscordAuditLogMessageEntry mentry)
            {
                var member = await e.Guild.GetMemberAsync(mentry.UserResponsible.Id);
                if (member.IsExempted(shard))
                    return;

                emb.AddField("User responsible", mentry.UserResponsible.Mention, inline: true);

                if (!string.IsNullOrWhiteSpace(mentry.Reason))
                    emb.AddField("Reason", mentry.Reason);
                emb.WithFooter(mentry.CreationTimestamp.ToUtcTimestamp(), mentry.UserResponsible.AvatarUrl);
            }

            if (!string.IsNullOrWhiteSpace(e.Message.Content))
            {
                emb.AddField("Content", $"{Formatter.BlockCode(string.IsNullOrWhiteSpace(e.Message.Content) ? "<empty content>" : FormatterExtensions.StripMarkdown(e.Message.Content.Truncate(1000)))}");

                if (shard.SharedData.MessageContainsFilter(e.Guild.Id, e.Message.Content))
                    emb.WithDescription(Formatter.Italic("Message contained a filter."));
            }

            if (e.Message.Embeds.Any())
                emb.AddField("Embeds", e.Message.Embeds.Count.ToString(), inline: true);
            if (e.Message.Reactions.Any())
                emb.AddField("Reactions", string.Join(" ", e.Message.Reactions.Select(r => r.Emoji.GetDiscordName())), inline: true);
            if (e.Message.Attachments.Any())
                emb.AddField("Attachments", string.Join("\n", e.Message.Attachments.Select(a => a.FileName)), inline: true);
            if (e.Message.CreationTimestamp != null)
                emb.AddField("Message creation time", e.Message.CreationTimestamp.ToUtcTimestamp(), inline: true);

            await logchn.SendMessageAsync(embed: emb.Build());
        }

        [AsyncEventListener(DiscordEventType.MessageUpdated)]
        public static async Task MessageUpdateEventHandlerAsync(FreudShard shard, MessageUpdateEventArgs e)
        {
            if (e.Author is null || e.Author.IsBot || e.Channel is null || e.Channel.IsPrivate || e.Message is null)
                return;
            if (shard.SharedData.BlockedChannels.Contains(e.Channel.Id))
                return;
            if (e.Message.Author == e.Client.CurrentUser && shard.SharedData.IsEventRunningInChannel(e.Channel.Id))
                return;
            if (!(e.Message.Content is null) && shard.SharedData.MessageContainsFilter(e.Guild.Id, e.Message.Content))
            {
                try
                {
                    await e.Message.DeleteAsync("bot: Filter hit after update");
                    await e.Channel.SendMessageAsync($"{e.Author.Mention} said: {FormatterExtensions.Spoiler(Formatter.BlockCode(FormatterExtensions.StripMarkdown(e.Message.Content)))}");
                } catch
                {
                    // swallow
                }
            }

            var logchn = shard.SharedData.GetLogChannelForGuild(shard.Client, e.Guild);
            if (logchn is null || !e.Message.IsEdited || e.Channel.IsExempted(shard))
                return;

            var member = await e.Guild.GetMemberAsync(e.Author.Id);
            if (member.IsExempted(shard))
                return;

            string pcontent = string.IsNullOrWhiteSpace(e.MessageBefore?.Content) ? "" : e.MessageBefore.Content.Truncate(700);
            string acontent = string.IsNullOrWhiteSpace(e.Message?.Content) ? "" : e.Message.Content.Truncate(700);
            string ctime = e.Message.CreationTimestamp == null ? _unknown : e.Message.CreationTimestamp.ToUtcTimestamp();
            string etime = e.Message.EditedTimestamp is null ? _unknown : e.Message.EditedTimestamp.Value.ToUtcTimestamp();
            string bextra = $"Embeds: {e.MessageBefore?.Embeds?.Count ?? 0}, Reactions: {e.MessageBefore?.Reactions?.Count ?? 0}, Attachments: {e.MessageBefore.Attachments?.Count ?? 0}";
            string aextra = $"Embeds: {e.Message.Embeds.Count}, Reactions: {e.Message.Reactions.Count}, Attachments: {e.Message.Attachments.Count}";

            var emb = FormEmbedBuilder(EventOrigin.Message, "Message updated");
            emb.WithDescription(Formatter.MaskedUrl("Jump to message", e.Message.JumpLink));
            emb.AddField("Location", e.Channel.Mention, inline: true);
            emb.AddField("Author", e.Message.Author?.Mention ?? _unknown, inline: true);
            emb.AddField("Before update", $"Created {ctime}\n{bextra}\nContent:{Formatter.BlockCode(FormatterExtensions.StripMarkdown(pcontent))}");
            emb.AddField("After update", $"Edited {etime}\n{aextra}\nContent:{Formatter.BlockCode(FormatterExtensions.StripMarkdown(acontent))}");

            await logchn.SendMessageAsync(embed: emb.Build());
        }
    }
}
