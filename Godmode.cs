using System;
using System.Linq;
using System.Timers;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;

namespace GodMode
{
    [ApiVersion(2, 1)]
    public class GodMode : TerrariaPlugin
    {
        private static System.Timers.Timer? update;

        public override string Name => "GodMode";
        public override string Author => "一维";
        public override string Description => "GodMode For players";
        public override Version Version => new Version(1, 6, 2, 0);

        public GodMode(Main game) : base(game) { }

        public override void Initialize()
        {
            ServerApi.Hooks.GameInitialize.Register(this, OnInitialize);

            // 初始化定时器
            update = new System.Timers.Timer { Interval = 1000, AutoReset = true, Enabled = true };
            update.Elapsed += OnElapsed!;
            update.Start(); // 确保定时器启动
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                ServerApi.Hooks.GameInitialize.Deregister(this, OnInitialize);
                update?.Stop();
                update?.Dispose();
            }
            base.Dispose(disposing);
        }

        private void OnInitialize(EventArgs e)
        {
            Commands.ChatCommands.Add(new Command("gm", Cmd, "gm")
            {
                HelpText = "/gm [player] - 切换GodMode模式"
            });

            Commands.ChatCommands.Add(new Command("gm", CmdHelp, "gm help")
            {
                HelpText = "/gm help - 显示GodMode插件的帮助信息。"
            });
        }

        private void Cmd(CommandArgs args)
        {
            if (args.Parameters.Count > 0 && args.Parameters[0].ToLower() == "help")
            {
                CmdHelp(args);
                return;
            }

            TSPlayer targetPlayer = args.Player;

            // 检查是否指定了目标玩家
            if (args.Parameters.Count > 0)
            {
                if (!args.Player.HasPermission("gm"))
                {
                    args.Player.SendErrorMessage("你没有权限切换其他玩家的GodMode。");
                    return;
                }

                string playerName = string.Join(" ", args.Parameters);
                var players = TShock.Players.Where(p => p != null && p.Name.IndexOf(playerName, StringComparison.OrdinalIgnoreCase) >= 0).ToList();

                if (players.Count == 0)
                {
                    args.Player.SendErrorMessage($"未找到玩家 \"{playerName}\"。");
                    return;
                }
                if (players.Count > 1)
                {
                    args.Player.SendErrorMessage($"找到多个玩家: {string.Join(", ", players.Select(p => p.Name))}");
                    return;
                }

                targetPlayer = players[0];
            }

            // 切换 GodMode 状态
            if (targetPlayer.TPlayer.immune)
            {
                DisableGodMode(targetPlayer);
                args.Player.SendSuccessMessage($"已禁用 {targetPlayer.Name} 的 GodMode。");
                if (targetPlayer != args.Player)
                {
                    targetPlayer.SendSuccessMessage($"{args.Player.Name} 已为你禁用 GodMode。");
                }
            }
            else
            {
                EnableGodMode(targetPlayer);
                args.Player.SendSuccessMessage($"已启用 {targetPlayer.Name} 的 GodMode。");
                if (targetPlayer != args.Player)
                {
                    targetPlayer.SendSuccessMessage($"{args.Player.Name} 已为你启用 GodMode。");
                }
            }
        }

        private void CmdHelp(CommandArgs args)
        {
            args.Player.SendSuccessMessage("GodMode 插件帮助：");
            args.Player.SendInfoMessage("/gm - 切换自己的 GodMode 状态。");
            args.Player.SendInfoMessage("/gm <player> - 切换指定玩家的 GodMode 状态（需要权限）。");
            args.Player.SendInfoMessage("/gm help - 显示此帮助信息。");
        }

        private void EnableGodMode(TSPlayer player)
        {
            player.TPlayer.immune = true; // 设置无敌
            player.TPlayer.immuneTime = int.MaxValue; // 刷新无敌时间

            const int invisibilityBuffId = 10; // Buff ID 10：隐身
            const int duration = int.MaxValue; // 极长时间

            player.SetBuff(invisibilityBuffId, duration, false); // 添加隐身Buff
        }

        private void DisableGodMode(TSPlayer player)
        {
            player.TPlayer.immune = false; // 取消无敌
            player.TPlayer.immuneTime = 0;

            const int invisibilityBuffId = 10; // Buff ID 10：隐身
            player.TPlayer.ClearBuff(invisibilityBuffId); // 移除隐身Buff
        }

        private void OnElapsed(object? sender, ElapsedEventArgs e)
        {
            foreach (var player in TShock.Players)
            {
                if (player?.Active == true)
                {
                    // 管理员不会吸引仇恨
                    if (player.HasPermission("gm") && player.TPlayer.immune)
                    {
                        player.TPlayer.aggro = 0; // 设置仇恨值为 0
                        player.TPlayer.immuneTime = int.MaxValue; // 刷新无敌时间
                    }

                    // 确保隐身Buff持续存在
                    const int invisibilityBuffId = 10; // Buff ID 10：隐身
                    if (player.TPlayer.immune && !Array.Exists(player.TPlayer.buffType, buff => buff == invisibilityBuffId))
                    {
                        player.SetBuff(invisibilityBuffId, int.MaxValue, false);
                    }
                }
            }
        }
    }
}
