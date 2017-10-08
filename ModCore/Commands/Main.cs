﻿using System;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using ModCore.Database;
using ModCore.Entities;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using DSharpPlus.Interactivity;

namespace ModCore.Commands
{
    public class Main
    {
        private DatabaseContextBuilder Database { get; }

        public Main(DatabaseContextBuilder db)
        {
            this.Database = db;
        }

        [Command("ping")]
        public async Task PingAsync(CommandContext ctx)
        {
            await ctx.RespondAsync($"Pong: ({ctx.Client.Ping}) ms.");
        }

        [Command("uptime"), Aliases("u")]
        public async Task UptimeAsync(CommandContext ctx)
        {
            var st = ctx.Dependencies.GetDependency<StartTimes>();
            var bup = DateTimeOffset.Now.Subtract(st.ProcessStartTime);
            var sup = DateTimeOffset.Now.Subtract(st.SocketStartTime);

            // Needs improvement
            await ctx.RespondAsync($"Program uptime: {string.Format("{0} days, {1}", bup.ToString("dd"), bup.ToString(@"hh\:mm\:ss"))}\n" +
                $"Socket uptime: {string.Format("{0} days, {1}", sup.ToString("dd"), sup.ToString(@"hh\:mm\:ss"))}");
        }

        [Command("purgeuser"), Aliases("pu"), RequirePermissions(Permissions.ManageMessages)]
        public async Task PurgeUserAsync(CommandContext ctx, DiscordUser user, int limit, int skip = 0)
        {
            var i = 0;
            var ms = await ctx.Channel.GetMessagesAsync(limit, ctx.Message.Id);
            var delet_this = new List<DiscordMessage>();
            foreach (var m in ms)
            {
                if (user != null && m.Author.Id != user.Id) continue;
                if (i < skip)
                    i++;
                else
                    delet_this.Add(m);
            }
            if (delet_this.Any())
                await ctx.Channel.DeleteMessagesAsync(delet_this, $"Purged messages by {user.Username}#{user.Discriminator} (ID:{user.Id})");
            var resp = await ctx.RespondAsync($"Latest messages by {user.Mention} (ID:{user.Id}) deleted.");
            await Task.Delay(2000);
            await resp.DeleteAsync("Purge command executed.");
            await ctx.Message.DeleteAsync("Purge command executed.");

            await ctx.LogAction($"Purged messages.\nUser: {user.Username}#{user.Discriminator} (ID:{user.Id})\nChannel: #{ctx.Channel.Name} ({ctx.Channel.Id})");
        }

        [Command("purge"), Aliases("p"), RequirePermissions(Permissions.ManageMessages)]
        public async Task PurgeUserAsync(CommandContext ctx, int limit, int skip = 0)
        {
            var i = 0;
            var ms = await ctx.Channel.GetMessagesAsync(limit, ctx.Message.Id);
            var delet_this = new List<DiscordMessage>();
            foreach (var m in ms)
            {
                if (i < skip)
                    i++;
                else
                    delet_this.Add(m);
            }
            if (delet_this.Any())
                await ctx.Channel.DeleteMessagesAsync(delet_this, "Purged messages.");
            var resp = await ctx.RespondAsync($"Latest messages deleted.");
            await Task.Delay(2000);
            await resp.DeleteAsync("Purge command executed.");
            await ctx.Message.DeleteAsync("Purge command executed.");

            await ctx.LogAction($"Purged messages.\nChannel: #{ctx.Channel.Name} ({ctx.Channel.Id})");
        }

        [Command("clean"), Aliases("c"), RequirePermissions(Permissions.ManageMessages)]
        public async Task CleanAsync(CommandContext ctx)
        {
            var prefix = ctx.GetGuildSettings().Prefix;
            var ms = await ctx.Channel.GetMessagesAsync(100, ctx.Message.Id);
            var delet_this = new List<DiscordMessage>();
            foreach (var m in ms)
            {
                if (m.Author.Id == ctx.Client.CurrentUser.Id || m.Content.StartsWith(prefix))
                    delet_this.Add(m);
            }
            if(delet_this.Any())
                await ctx.Channel.DeleteMessagesAsync(delet_this, "Cleaned up commands");
            var resp = await ctx.RespondAsync($"Latest messages deleted.");
            await Task.Delay(2000);
            await resp.DeleteAsync("Clean command executed.");
            await ctx.Message.DeleteAsync("Clean command executed.");

            await ctx.LogAction();
        }

        [Command("ban"), Aliases("b"), RequirePermissions(Permissions.BanMembers)]
        public async Task BanAsync(CommandContext ctx, DiscordMember m, [RemainingText]string reason = "")
        {
            if (ctx.Member.Id == m.Id)
            {
                await ctx.RespondAsync("You can't do that to yourself! You have so much to live for!");
                return;
            }

            var ustr = $"{ctx.User.Username}#{ctx.User.Discriminator} ({ctx.User.Id})";
            var rstr = string.IsNullOrWhiteSpace(reason) ? "" : $": {reason}";
            await ctx.Guild.BanMemberAsync(m, 7, $"{ustr}{rstr}");
            await ctx.RespondAsync($"Banned user {m.DisplayName} (ID:{m.Id})");

            await ctx.LogAction($"Banned user {m.DisplayName} (ID:{m.Id})\n{rstr}");
        }

        [Command("hackban"), Aliases("hb"), RequirePermissions(Permissions.BanMembers)]
        public async Task HackBanAsync(CommandContext ctx, ulong id, [RemainingText]string reason = "")
        {
            if (ctx.Member.Id == id)
            {
                await ctx.RespondAsync("You can't do that to yourself! You have so much to live for!");
                return;
            }

            var ustr = $"{ctx.User.Username}#{ctx.User.Discriminator} ({ctx.User.Id})";
            var rstr = string.IsNullOrWhiteSpace(reason) ? "" : $": {reason}";
            await ctx.Guild.BanMemberAsync(id, 7, $"{ustr}{rstr}");
            await ctx.RespondAsync($"User hackbanned successfully.");

            await ctx.LogAction($"Hackbanned ID: {id}\n{rstr}");
        }

        [Command("kick"), Aliases("k"), RequirePermissions(Permissions.KickMembers)]
        public async Task KickAsync(CommandContext ctx, DiscordMember m, [RemainingText]string reason = "")
        {
            if (ctx.Member.Id == m.Id)
            {
                await ctx.RespondAsync("You can't do that to yourself! You have so much to live for!");
                return;
            }

            var ustr = $"{ctx.User.Username}#{ctx.User.Discriminator} ({ctx.User.Id})";
            var rstr = string.IsNullOrWhiteSpace(reason) ? "" : $": {reason}";
            await m.RemoveAsync($"{ustr}{rstr}");
            await ctx.RespondAsync($"Kicked user {m.DisplayName} (ID:{m.Id})");

            await ctx.LogAction($"Kicked user {m.DisplayName} (ID:{m.Id})\n{rstr}");
        }

        [Command("softban"), Aliases("s"), RequireUserPermissions(Permissions.KickMembers)]
        public async Task SoftbanAsync(CommandContext ctx, DiscordMember m, [RemainingText]string reason = "")
        {
            if (ctx.Member.Id == m.Id)
            {
                await ctx.RespondAsync("You can't do that to yourself! You have so much to live for!");
                return;
            }

            var ustr = $"{ctx.User.Username}#{ctx.User.Discriminator} ({ctx.User.Id})";
            var rstr = string.IsNullOrWhiteSpace(reason) ? "" : $": {reason}";
            await m.BanAsync(7, $"{ustr}{rstr} (softban)");
            await m.UnbanAsync(ctx.Guild, $"{ustr}{rstr}");
            await ctx.RespondAsync($"Softbanned user {m.DisplayName} (ID:{m.Id})");

            await ctx.LogAction($"Softbanned user {m.DisplayName} (ID:{m.Id})\n{rstr}");
        }

        [Command("mute"), Aliases("m"), RequirePermissions(Permissions.MuteMembers)]
        public async Task MuteAsync(CommandContext ctx, DiscordMember m, [RemainingText]string reason = "")
        {
            if (ctx.Member.Id == m.Id)
            {
                await ctx.RespondAsync("You can't do that to yourself! You have so much to live for!");
                return;
            }

            var gcfg = ctx.GetGuildSettings();
            if (gcfg == null)
            {
                await ctx.RespondAsync("Guild is not configured. Adjust this guild's configuration and re-run this command.");
                return;
            }

            var b = gcfg.MuteRoleId;
            var mute = ctx.Guild.GetRole(b);
            if (b == 0 || mute == null)
            {
                await ctx.RespondAsync("Mute role is not configured or missing. Set a correct role and re-run this command.");
                return;
            }

            var ustr = $"{ctx.User.Username}#{ctx.User.Discriminator} ({ctx.User.Id})";
            var rstr = string.IsNullOrWhiteSpace(reason) ? "" : $": {reason}";
            await m.GrantRoleAsync(mute, $"{ustr}{rstr} (mute)");
            await ctx.RespondAsync($"Muted user {m.DisplayName} (ID:{m.Id}) { (reason != "" ? "With reason: " + reason : "")}");

            await ctx.LogAction($"Muted user {m.DisplayName} (ID:{m.Id}) { (reason != "" ? "With reason: " + reason : "")}");
        }

        [Command("unmute"), Aliases("um"), RequirePermissions(Permissions.MuteMembers)]
        public async Task UnmuteAsync(CommandContext ctx, DiscordMember m, [RemainingText]string reason = "")
        {
            if (ctx.Member.Id == m.Id)
            {
                await ctx.RespondAsync("You can't do that to yourself! You have so much to live for!");
                return;
            }

            var gcfg = ctx.GetGuildSettings();
            if (gcfg == null)
            {
                await ctx.RespondAsync("Guild is not configured. Adjust this guild's configuration and re-run this command.");
                return;
            }

            var b = gcfg.MuteRoleId;
            var mute = ctx.Guild.GetRole(b);
            if (b == 0 || mute == null)
            {
                await ctx.RespondAsync("Mute role is not configured or missing. Set a correct role and re-run this command.");
                return;
            }

            var ustr = $"{ctx.User.Username}#{ctx.User.Discriminator} ({ctx.User.Id})";
            var rstr = string.IsNullOrWhiteSpace(reason) ? "" : $": {reason}";
            await m.RevokeRoleAsync(mute, $"{ustr}{rstr} (unmute)");
            await ctx.RespondAsync($"Unmuted user {m.DisplayName} (ID:{m.Id}) { (reason != "" ? "With reason: " + reason : "")}");

            await ctx.LogAction($"Unmuted user {m.DisplayName} (ID:{m.Id}) { (reason != "" ? "With reason: " + reason : "")}");
        }

        [Command("userinfo"), Aliases("ui")]
        public async Task UserInfoAsync(CommandContext ctx, DiscordMember usr)
        {
            var embed = new DiscordEmbedBuilder()
                .WithColor(DiscordColor.MidnightBlue)
                .WithTitle($"@{usr.Username}#{usr.Discriminator} - ID: {usr.Id}");

            if (usr.IsBot) embed.Title += " __[BOT]__ ";
            if (usr.IsOwner) embed.Title += " __[OWNER]__ ";

            embed.Description =
                $"Registered on     : {usr.CreationTimestamp.DateTime}\n" +
                $"Joined Guild on  : {usr.JoinedAt.DateTime}";

            var roles = new StringBuilder();
            foreach (var r in usr.Roles) roles.Append($"[{r.Name}] ");
            if (roles.Length == 0) roles.Append("*None*");
            embed.AddField("Roles", roles.ToString());

            var permsobj = usr.PermissionsIn(ctx.Channel);
            var perms = permsobj.ToPermissionString();
            if (((permsobj & Permissions.Administrator) | (permsobj & Permissions.AccessChannels)) == 0)
                perms = "**[!] User can't see this channel!**\n" + perms;
            if (perms == String.Empty) perms = "*None*";
            embed.AddField("Permissions", perms);

            embed.WithFooter($"{ctx.Guild.Name} / #{ctx.Channel.Name} / {DateTime.Now}");

            await ctx.RespondAsync("", false, embed: embed);
        }

        [Command("leave"), Description("Makes this bot leave the current server."), RequireUserPermissions(Permissions.Administrator)]
        public async Task LeaveAsync(CommandContext ctx)
        {
            var interactivity = ctx.Dependencies.GetDependency<InteractivityModule>();
            await ctx.RespondAsync("Are you sure you want to remove modcore from your guild?");
            var m = await interactivity.WaitForMessageAsync(x => x.ChannelId == ctx.Channel.Id && x.Author.Id == ctx.Member.Id, TimeSpan.FromSeconds(30));

            if (m == null)
                await ctx.RespondAsync("Timed out.");
            else if (m.Message.Content == "yes")
            {
                await ctx.RespondAsync("Thanks for using ModCore. Leaving this guild.");
                await ctx.Guild.LeaveAsync();
            }
            else
                await ctx.RespondAsync("Operation canceled by user.");
        }
    }
}
