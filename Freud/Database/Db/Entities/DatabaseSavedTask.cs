﻿#region USING_DIRECTIVES

using Freud.Common.Configuration;
using Freud.Common.Tasks;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

#endregion USING_DIRECTIVES

namespace Freud.Database.Db.Entities
{
    [Table("saved_tasks")]
    public class DatabaseSavedTask
    {
        public static DatabaseSavedTask FromSavedTaskInfo(SavedTaskInfo tinfo)
        {
            var dbti = new DatabaseSavedTask { ExecutionTime = tinfo.ExecutionTime.UtcDateTime };
            switch (tinfo)
            {
                case UnbanTaskInfo ubti:
                    dbti.GuildId = ubti.GuildId;
                    dbti.UserId = ubti.UnbanId;
                    dbti.Type = SavedTaskType.Unban;
                    break;

                case UnmuteTaskInfo umti:
                    umti.GuildId = umti.GuildId;
                    dbti.UserId = umti.UserId;
                    dbti.RoleId = umti.MuteRoleId;
                    dbti.Type = SavedTaskType.Unmute;
                    break;

                default:
                    return null;
            }

            return dbti;
        }

        [Key]
        [Column("id")]
        public int Id { get; set; }

        [ForeignKey("DbGuildConfiguration")]
        [Column("gid")]
        public long GuildIdDb { get; set; }

        [NotMapped]
        public ulong GuildId { get => (ulong)this.GuildIdDb; set => this.GuildIdDb = (long)value; }

        [Column("uid")]
        public long UserIdDb { get; set; }

        [NotMapped]
        public ulong UserId { get => (ulong)this.UserIdDb; set => this.UserIdDb = (long)value; }

        [Column("rid")]
        public long? RoleIdDb { get; set; }

        [NotMapped]
        public ulong RoleId { get => (ulong)this.RoleIdDb.GetValueOrDefault(); set => this.RoleIdDb = (long)value; }

        [Column("type")]
        public SavedTaskType Type { get; set; }

        [Column("execution_time", TypeName = "timestamptz")]
        public DateTimeOffset ExecutionTime { get; set; }

        public virtual DatabaseGuildConfiguration DbGuildConfiguration { get; set; }
    }
}
