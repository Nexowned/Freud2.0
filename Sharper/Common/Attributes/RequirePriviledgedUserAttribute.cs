﻿#region USING_DIRECTIVES
using System;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using Microsoft.Extensions.DependencyInjection;
using Sharper.Database;
#endregion

namespace Sharper.Common.Attributes
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public sealed class RequirePriviledgedUserAttribute : CheckBaseAttribute
    {
        public override Task<bool> ExecuteCheckAsync(CommandContext ctx, bool help)
        {
            if (ctx.User.Id == ctx.Client.CurrentApplication.Owner.Id)
                return Task.FromResult(true);

            using (DatabaseContext db = ctx.Services.GetService<DatabaseContextBuilder>().CreateContext())
                return Task.FromResult(db.PriviledgedUsers.Any(u => u.UserId == ctx.User.Id));
        }
    }
}
