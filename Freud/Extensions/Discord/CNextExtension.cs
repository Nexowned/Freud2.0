﻿#region USING_DIRECTIVES

using DSharpPlus.CommandsNext;

using System.Collections.Generic;
using System.Linq;

#endregion USING_DIRECTIVES

namespace Freud.Extensions.Discord
{
    internal static class CNextExtension
    {
        public static IReadOnlyList<Command> GetAllRegisteredCommands(this CommandsNextExtension cnext)
        {
            return cnext.RegisteredCommands.SelectMany(cnext.CommandSelector).Distinct().ToList().AsReadOnly();
        }

        public static IEnumerable<Command> CommandSelector(this CommandsNextExtension cnext, KeyValuePair<string, Command> c)
            => cnext.CommandSelector(c.Value);

        public static IEnumerable<Command> CommandSelector(this CommandsNextExtension cnext, Command c)
        {
            Command[] arr = new[] { c };

            if (c is CommandGroup group)
                return arr.Concat(group.Children.SelectMany(cnext.CommandSelector));

            return arr;
        }
    }
}
