using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using ModCore.Database;
using ModCore.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModCore.Commands
{
    [Group("star"), Aliases("s"), Description("Star commands.")]
    public class Star
    {
        public SharedData Shared { get; }
        public InteractivityExtension Interactivity { get; }
        public DatabaseContextBuilder Database { get; }

        public Star(DatabaseContextBuilder db, SharedData shared, InteractivityExtension interactive)
        {
            this.Shared = shared;
            this.Interactivity = interactive;
            this.Database = db;
        }

        [Command("debug"), Aliases("d"), Description("Returns amount of stars in database."), Hidden]
        public async Task DebugAsync(CommandContext ctx)
        {
            using (var db = Database.CreateContext())
            {
                await ctx.RespondAsync($"Stars: {db.StarDatas.Count()}");
            }
        }

        [Command("info"), Aliases("i", "data", "information"), Description("Returns stardata for a specified user.")]
        public async Task ListGivenAsync(CommandContext ctx, [Description("User to show stardata information about.")] DiscordMember member)
        {
            var embed = new DiscordEmbedBuilder()
                .WithColor(DiscordColor.MidnightBlue)
                .WithTitle($"{member.DisplayName} - {member.Username}#{member.Discriminator}");

            using (var db = Database.CreateContext())
            {
                var guildStars = db.StarDatas.Where(x => (ulong)x.GuildId == ctx.Guild.Id);
                var givenStars = guildStars.Where(x => (ulong)x.StargazerId == member.Id);
                var gotStars = guildStars.Where(x => (ulong)x.AuthorId == member.Id);

                embed.Description =
                    $"You have given **{givenStars.Count()}** stars to other users.\n\n" +
                    $"You have been given **{gotStars.Count()}** stars by **{gotStars.Select(x => x.StargazerId).Distinct().Count()}** different users, over **{gotStars.Select(x => x.MessageId).Distinct().Count()}** different messages.";

                var allMembers = await ctx.Guild.GetAllMembersAsync();

                var givenMemberNames = new Dictionary<string, int>();                
                foreach (DatabaseStarData star in givenStars)
                {
                    string memberName = "Removed User";
                    if (allMembers.Any(x => x.Id == (ulong)star.AuthorId))
                    {
                        memberName = allMembers.First(x => x.Id == (ulong)star.AuthorId).Mention;
                    }
                    if (givenMemberNames.ContainsKey(memberName))
                    {
                        givenMemberNames[memberName] += 1;
                    }
                    else
                    {
                        givenMemberNames.Add(memberName, 1);
                    }
                }

                var orderGivenmemberNames = givenMemberNames.OrderByDescending(x => x.Value).Select(x => x.Key + " - " + x.Value);
                embed.AddField("Users who have been given stars by you", string.Join("\n", orderGivenmemberNames.Take(10)), false);
                
                if (orderGivenmemberNames.Count() > 10)
                    embed.Fields.Last().Value += $"\nAnd {orderGivenmemberNames.Count() - 10} more...";
                
                var gotMemberNames = new Dictionary<string, int>();
                foreach (DatabaseStarData star in gotStars)
                {
                    string memberName = "Removed User";
                    if (allMembers.Any(x => x.Id == (ulong)star.StargazerId))
                    {
                        memberName = allMembers.First(x => x.Id == (ulong)star.StargazerId).Mention;
                    }
                    if (gotMemberNames.ContainsKey(memberName))
                    {
                        gotMemberNames[memberName] += 1;
                    }
                    else
                    {
                        gotMemberNames.Add(memberName, 1);
                    }
                }
                var orderedGotMemberNames = gotMemberNames.OrderByDescending(x => x.Value).Select(x => x.Key + " - " + x.Value);
                embed.AddField("Users who have given you stars", string.Join("\n", orderedGotMemberNames.Take(10)), false);
                
                if (orderedGotMemberNames.Count() > 10)
                    embed.Fields.Last().Value += $"\nAnd {orderedGotMemberNames.Count() - 10} more...";

                await ctx.RespondAsync(embed: embed);
            }
        }
    }
}
