﻿#region USING_DIRECTIVES

using Freud.Common.Configuration;
using Freud.Database.Db.Entities;
using Freud.Modules.Administration.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System;
using static Freud.Database.Db.DatabaseContextBuilder;

#endregion USING_DIRECTIVES

namespace Freud.Database.Db
{
    public class DatabaseContext : DbContext
    {
        public virtual DbSet<DatabaseExemptAntispam> AntispamExempts { get; set; }
        public virtual DbSet<DatabaseAutoRole> AutoAssignableRoles { get; set; }
        public virtual DbSet<DatabaseBankAccount> BankAccounts { get; set; }
        public virtual DbSet<DatabaseBirthday> Birthdays { get; set; }
        public virtual DbSet<DatabaseBlockedChannel> BlockedChannels { get; set; }
        public virtual DbSet<DatabaseBlockedUser> BlockedUsers { get; set; }
        public virtual DbSet<DatabaseBotStatus> BotStatuses { get; set; }
        public virtual DbSet<DatabaseCommandRule> CommandRules { get; set; }
        public virtual DbSet<DatabaseEmojiReaction> EmojiReactions { get; set; }
        public virtual DbSet<DatabaseFilter> Filters { get; set; }
        public virtual DbSet<DatabaseForbiddenName> ForbiddenNames { get; set; }
        public virtual DbSet<DatabaseGuildConfiguration> GuildConfiguration { get; set; }
        public virtual DbSet<DatabaseGuildRank> GuildRanks { get; set; }
        public virtual DbSet<DatabaseInsult> Insults { get; set; }
        public virtual DbSet<DatabaseExemptLogging> LoggingExempts { get; set; }
        public virtual DbSet<DatabaseMeme> Memes { get; set; }
        public virtual DbSet<DatabaseMessageCount> MessageCount { get; set; }
        public virtual DbSet<DatabasePrivilegedUser> PrivilegedUsers { get; set; }
        public virtual DbSet<DatabaseExemptRatelimit> RatelimitExempts { get; set; }
        public virtual DbSet<DatabaseReminder> Reminders { get; set; }
        public virtual DbSet<DatabaseRssFeed> RssFeeds { get; set; }
        public virtual DbSet<DatabaseRssSubscription> RssSubscriptions { get; set; }
        public virtual DbSet<DatabaseSavedTask> SavedTasks { get; set; }
        public virtual DbSet<DatabaseSelfRole> SelfAssignableRoles { get; set; }
        public virtual DbSet<DatabaseTextReaction> TextReactions { get; set; }

        private string ConnectionString { get; }
        private DatabaseProvider Provider { get; }

        public DatabaseContext(DatabaseProvider provider, string connectionString)
        {
            this.Provider = provider;
            this.ConnectionString = connectionString;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (optionsBuilder.IsConfigured)
                return;

            optionsBuilder.ConfigureWarnings(warnings => warnings.Throw(CoreEventId.IncludeIgnoredWarning));

            switch (this.Provider)
            {
                case DatabaseProvider.PostgreSQL:
                    optionsBuilder.UseNpgsql(this.ConnectionString);
                    break;

                case DatabaseProvider.SQLite:
                    optionsBuilder.UseSqlite(this.ConnectionString);
                    break;

                case DatabaseProvider.SQLServer:
                    optionsBuilder.UseSqlServer(this.ConnectionString);
                    break;

                default:
                    throw new NotSupportedException("Provider not supported!");
            }
        }

        protected override void OnModelCreating(ModelBuilder model)
        {
            model.HasDefaultSchema("f");

            model.Entity<DatabaseAutoRole>().HasKey(e => new { e.GuildIdDb, e.RoleIdDb });
            model.Entity<DatabaseBankAccount>().HasKey(e => new { e.GuildIdDb, e.UserIdDb });
            model.Entity<DatabaseBirthday>().HasKey(e => new { e.GuildIdDb, e.ChannelIdDb, e.UserIdDb });
            model.Entity<DatabaseBlockedChannel>().Property(bc => bc.Reason).HasDefaultValue(null);
            model.Entity<DatabaseBlockedUser>().Property(bu => bu.Reason).HasDefaultValue(null);
            model.Entity<DatabaseEmojiReactionTrigger>().HasKey(t => new { t.ReactionId, t.Trigger });
            model.Entity<DatabaseCommandRule>().HasKey(e => new { e.GuildIdDb, e.ChannelIdDb, e.Command });
            model.Entity<DatabaseExemptAntispam>().HasKey(e => new { e.IdDb, e.GuildIdDb, e.Type });
            model.Entity<DatabaseExemptLogging>().HasKey(e => new { e.IdDb, e.GuildIdDb, e.Type });
            model.Entity<DatabaseExemptRatelimit>().HasKey(e => new { e.IdDb, e.GuildIdDb, e.Type });
            model.Entity<DatabaseGuildConfiguration>().Property(gcfg => gcfg.AntifloodAction).HasDefaultValue(PunishmentActionType.PermanentBan);
            model.Entity<DatabaseGuildConfiguration>().Property(gcfg => gcfg.AntifloodCooldown).HasDefaultValue(10);
            model.Entity<DatabaseGuildConfiguration>().Property(gcfg => gcfg.AntifloodEnabled).HasDefaultValue(false);
            model.Entity<DatabaseGuildConfiguration>().Property(gcfg => gcfg.AntifloodSensitivity).HasDefaultValue(5);
            model.Entity<DatabaseGuildConfiguration>().Property(gcfg => gcfg.AntiInstantLeaveCooldown).HasDefaultValue(3);
            model.Entity<DatabaseGuildConfiguration>().Property(gcfg => gcfg.AntiInstantLeaveEnabled).HasDefaultValue(false);
            model.Entity<DatabaseGuildConfiguration>().Property(gcfg => gcfg.AntispamAction).HasDefaultValue(PunishmentActionType.PermanentMute);
            model.Entity<DatabaseGuildConfiguration>().Property(gcfg => gcfg.AntispamEnabled).HasDefaultValue(false);
            model.Entity<DatabaseGuildConfiguration>().Property(gcfg => gcfg.AntispamSensitivity).HasDefaultValue(5);
            model.Entity<DatabaseGuildConfiguration>().Property(gcfg => gcfg.Currency).HasDefaultValue(null);
            model.Entity<DatabaseGuildConfiguration>().Property(gcfg => gcfg.LogChannelIdDb).HasDefaultValue();
            model.Entity<DatabaseGuildConfiguration>().Property(gcfg => gcfg.MuteRoleIdDb).HasDefaultValue(null);
            model.Entity<DatabaseGuildConfiguration>().Property(gcfg => gcfg.LeaveChannelIdDb).HasDefaultValue(null);
            model.Entity<DatabaseGuildConfiguration>().Property(gcfg => gcfg.LeaveMessage).HasDefaultValue(null);
            model.Entity<DatabaseGuildConfiguration>().Property(gcfg => gcfg.LinkfilterBootersEnabled).HasDefaultValue(true);
            model.Entity<DatabaseGuildConfiguration>().Property(gcfg => gcfg.LinkfilterDiscordInvitesEnabled).HasDefaultValue(false);
            model.Entity<DatabaseGuildConfiguration>().Property(gcfg => gcfg.LinkfilterDisturbingWebsitesEnabled).HasDefaultValue(true);
            model.Entity<DatabaseGuildConfiguration>().Property(gcfg => gcfg.LinkfilterEnabled).HasDefaultValue(false);
            model.Entity<DatabaseGuildConfiguration>().Property(gcfg => gcfg.LinkfilterIpLoggersEnabled).HasDefaultValue(true);
            model.Entity<DatabaseGuildConfiguration>().Property(gcfg => gcfg.LinkfilterUrlShortenersEnabled).HasDefaultValue(true);
            model.Entity<DatabaseGuildConfiguration>().Property(gcfg => gcfg.Prefix).HasDefaultValue(null);
            model.Entity<DatabaseGuildConfiguration>().Property(gcfg => gcfg.RatelimitAction).HasDefaultValue(PunishmentActionType.TemporaryMute);
            model.Entity<DatabaseGuildConfiguration>().Property(gcfg => gcfg.RatelimitEnabled).HasDefaultValue(false);
            model.Entity<DatabaseGuildConfiguration>().Property(gcfg => gcfg.RatelimitSensitivity).HasDefaultValue(5);
            model.Entity<DatabaseGuildConfiguration>().Property(gcfg => gcfg.ReactionResponse).HasDefaultValue(false);
            model.Entity<DatabaseGuildConfiguration>().Property(gcfg => gcfg.SuggestionsEnabled).HasDefaultValue(false);
            model.Entity<DatabaseGuildConfiguration>().Property(gcfg => gcfg.WelcomeChannelIdDb).HasDefaultValue(null);
            model.Entity<DatabaseGuildConfiguration>().Property(gcfg => gcfg.WelcomeMessage).HasDefaultValue(null);
            model.Entity<DatabaseGuildRank>().HasKey(e => new { e.GuildIdDb, e.Rank });
            model.Entity<DatabaseMeme>().HasKey(e => new { e.GuildIdDb, e.Name });
            model.Entity<DatabaseMessageCount>().Property(ui => ui.MessageCount).HasDefaultValue(1);
            model.Entity<DatabaseReminder>().Property(r => r.IsRepeating).HasDefaultValue(false);
            model.Entity<DatabaseReminder>().Property(r => r.RepeatIntervalDb).HasDefaultValue(TimeSpan.FromMilliseconds(-1));
            model.Entity<DatabaseRssSubscription>().HasKey(e => new { e.Id, e.GuildIdDb, e.ChannelIdDb });
            model.Entity<DatabaseSelfRole>().HasKey(e => new { e.GuildIdDb, e.RoleIdDb });
            model.Entity<DatabaseTextReactionTrigger>().HasKey(t => new { t.ReactionId, t.Trigger });
        }
    }
}
