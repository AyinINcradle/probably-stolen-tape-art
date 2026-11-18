using System;
using UnityEngine;

namespace TapeArt
{
    public class TapeArtMod : IMod
    {
        private ModManifest manifest;
        private ModLog log;
        private bool upgradesRegistered;

        /// <summary>给没有 ModLog 引用的静态辅助类写日志用。</summary>
        public static ModLog Log { get; private set; }

        public void Init(ModManifest manifest)
        {
            this.manifest = manifest;
            log = new ModLog(manifest);
            Log = log;
            log.Log("init, game version " + ModHelper.GetGameVersion());
        }

        public void OnEnable()
        {
            ModHook.OnModItemDirectoryInit += Items.Register;
            ModHook.OnNewGameStarted += OnNewGameStarted;
            ModHook.OnWakingUpLate += OnWakingUp;
            ModHook.OnHandlingNightlyServicesLate += OnNightlyServices;
            ModHook.OnGameLoadedLate += OnGameLoaded;
            ModHook.OnGameLoadedInit += EnsureUpgrades;
            ModHook.OnCreateTooltipLate += OnTooltip;
            ModHook.OnNetworkUpgradeUnlocked += OnUpgradeUnlocked;

            // 注意：人脉升级不能在这里注册。NetworkUpgrade.RegisterModUpgrade 会访问
            // PlayerStore.Instance，而该属性在菜单阶段会懒加载 new PlayerStore() 并空引用。
            Merchant.Register();

            log.Log("enabled: items and merchant registered");
        }

        public void OnDisable()
        {
#if TAPE_ART_DEBUG
            DebugTools.Unregister();
#endif
            ModHook.OnModItemDirectoryInit -= Items.Register;
            ModHook.OnNewGameStarted -= OnNewGameStarted;
            ModHook.OnWakingUpLate -= OnWakingUp;
            ModHook.OnHandlingNightlyServicesLate -= OnNightlyServices;
            ModHook.OnGameLoadedLate -= OnGameLoaded;
            ModHook.OnGameLoadedInit -= EnsureUpgrades;
            ModHook.OnCreateTooltipLate -= OnTooltip;
            ModHook.OnNetworkUpgradeUnlocked -= OnUpgradeUnlocked;
            log.Log("disabled");
        }

        /// <summary>
        /// 注册人脉升级。必须等一局游戏存在（PlayerStore 被构造出来）之后才能调用，
        /// 否则 RegisterModUpgrade 里的 PlayerStore.Instance 会在主菜单阶段触发空引用。
        /// </summary>
        private void EnsureUpgrades()
        {
            if (upgradesRegistered)
            {
                return;
            }
            try
            {
                Network.Register();
                upgradesRegistered = true;
                log.Log("network upgrades registered");
#if TAPE_ART_DEBUG
                DebugTools.Register();
#endif
            }
            catch (Exception e)
            {
                log.Error("register network upgrades failed: " + e);
            }
        }

        /// <summary>开局沿用原版【磁带迷】词条：额外送一块艺术品灯牌，并解锁「磁带艺术Ⅰ」。</summary>
        private void OnNewGameStarted(string scenarioId)
        {
            Network.BeginNewRun();
            EnsureUpgrades();

            if (!ModHelper.IsStartingPerkActive(Ids.TapeheadPerk))
            {
                return;
            }

            try
            {
                GameItem sign = ModHelper.SpawnItem(Ids.Sign);
                if (sign != null)
                {
                    ModHelper.MoveItemToInventory(sign);
                }

                NetworkUpgrade upgrade = ModHelper.GetNetworkUpgrade(Ids.Upgrade1);
                if (upgrade != null)
                {
                    upgrade.Unlock();
                }

                log.Log("tapehead perk: art sign granted, TAPE_ART_I unlocked");
            }
            catch (Exception e)
            {
                log.Error("tapehead perk handling failed: " + e);
            }
        }

        private void OnWakingUp()
        {
            try
            {
                Network.OnMorning(log);
            }
            catch (Exception e)
            {
                log.Error("morning check failed: " + e);
            }
        }

        private void OnNightlyServices()
        {
            try
            {
                Core.NightlyPower(log);
            }
            catch (Exception e)
            {
                log.Error("nightly services failed: " + e);
            }
        }

        /// <summary>载入存档后做一次轻量自检，只在出错时记录。</summary>
        private void OnGameLoaded()
        {
            try
            {
                Network.OnRunLoaded();
            }
            catch (Exception e)
            {
                log.Error("load check failed: " + e);
            }
        }

        private void OnUpgradeUnlocked(string id)
        {
            if (id == null || !id.StartsWith("TAPE_ART_", StringComparison.Ordinal))
            {
                return;
            }
            log.Log("upgrade purchased: " + id);
            if (id == Ids.Upgrade1)
            {
                // 兜底：若 onUnlock 未能执行（例如注册时机太晚），这里保证赠品和商人到位。
                Network.OnTierOneUnlocked();
            }
        }

        private void OnTooltip(RichTextBuilder builder, GameItem item)        {
            if (builder == null || item == null)
            {
                return;
            }

            try
            {
                if (item.identifier == Ids.Core)
                {
                    int level = Core.Level(item);
                    int usage = Math.Max(Core.MinUsage, Core.BaseUsage - Core.UsagePerLevel * level);

                    // 排版参考自动售货机/售水机核心：说明 → 当前数值 → 空行 → 升级标题 → 每条升级 → 电费
                    builder.AddLine("装进磁带播放机，播放区里的磁带会替店里招徕顾客。", true,
                        RenderHandler.ColorPalette.LightTurquoise);
                    builder.AddLine("吸引力：" + Core.AttractionBonus(), true,
                        RenderHandler.ColorPalette.LightTurquoise);

                    builder.AddLine(" ");
                    builder.AddLine("升级", true, RenderHandler.ColorPalette.LightCyan);
                    builder.AddLine("电子元件（普通）" + level + "/" + Core.MaxLevel
                                    + "：每晚电费减少 " + (Core.UsagePerLevel * level) + " 点", true,
                        RenderHandler.ColorPalette.LightCyan);
                    builder.AddLine("每晚电费：" + usage + " 信用点", true,
                        RenderHandler.ColorPalette.LightCyan);

                    if (!Core.IsPowered())
                    {
                        builder.AddLine("现金不足，当前未通电。", true,
                            RenderHandler.ColorPalette.LightOrange);
                    }
                }
                else if (item.identifier == Ids.Sign)
                {
                    builder.AddLine("放进展示柜：当天有几率吸引磁带迷商人上门。", true,
                        RenderHandler.ColorPalette.LightCyan);
                }
            }
            catch (Exception)
            {
                // 提示框构建失败不该影响游戏
            }
        }
    }
}
