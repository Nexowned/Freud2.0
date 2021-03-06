﻿#region USING_DIRECTIVES

using Freud.Common.Configuration;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

#endregion USING_DIRECTIVES

namespace Freud.Database.Db.Entities
{
    [Table("rss_subscriptions")]
    public class DatabaseRssSubscription
    {
        [ForeignKey("DbRssFeed")]
        [Column("id")]
        public int Id { get; set; }

        [ForeignKey("DbGuildConfiguration")]
        [Column("gid")]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public long GuildIdDb { get; set; }

        [NotMapped]
        public ulong GuildId { get => (ulong)this.GuildIdDb; set => this.GuildIdDb = (long)value; }

        [Column("cid")]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public long ChannelIdDb { get; set; }

        [NotMapped]
        public ulong ChannelId { get => (ulong)this.ChannelIdDb; set => this.ChannelIdDb = (long)value; }

        [Column("name"), Required, MaxLength(64)]
        public string Name { get; set; }

        public virtual DatabaseGuildConfiguration DbGuildConfiguration { get; set; }
        public virtual DatabaseRssFeed DbRssFeed { get; set; }
    }
}
