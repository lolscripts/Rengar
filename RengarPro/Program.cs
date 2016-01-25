﻿using System;
using System.Linq;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Menu;
using EloBuddy.SDK.Menu.Values;
using EloBuddy.SDK.Enumerations;
using EloBuddy.SDK.Events;
using Color = System.Drawing.Color;

namespace RengarPro
{
    class Program
    {
        public static AIHeroClient Rengar
        {
            get
            {
                return Player.Instance;
            }
        }
        private static readonly int[] BlueSmite = { 3706, 1400, 1401, 1402, 1403 };
        private static readonly int[] RedSmite = { 3715, 1415, 1414, 1413, 1412 };
        protected static SpellSlot Smite;
        public static Obj_AI_Base SelectedEnemy;
        public static Spell.Active Q;
        public static Spell.Active W;
        public static Spell.Skillshot E;
        public static Spell.Active R;
        public static Menu Menu, AllMenu;

        public static bool RengarHasPassive
        {
            get
            {
                return Rengar.HasBuff("rengarpassivebuff");
            }
        }

        public static bool RengarUltiActive
        {
            get
            {
                return Rengar.HasBuff("RengarR");
            }
        }
        protected static void SmiteCombo()
        {
            if (BlueSmite.Any(id => Item.HasItem(id)))
            {
                Smite = Rengar.GetSpellSlotFromName("s5_summonersmiteplayerganker");
                return;
            }

            if (RedSmite.Any(id => Item.HasItem(id)))
            {
                Smite = Rengar.GetSpellSlotFromName("s5_summonersmiteduel");
                return;
            }

            Smite = Rengar.GetSpellSlotFromName("summonersmite");
        }
        static void Main()
        {
            Loading.OnLoadingComplete += Loading_OnLoadingComplete;
        }

        private static void Loading_OnLoadingComplete(EventArgs args)
        {
            if (Rengar.Hero != Champion.Rengar)
            {
                return;
            }

            Q = new Spell.Active(SpellSlot.Q, (uint)(Rengar.GetAutoAttackRange() + 100));
            W = new Spell.Active(SpellSlot.W, 500);
            E = new Spell.Skillshot(SpellSlot.E, 1000, SkillShotType.Linear,250,1500,70);
            R = new Spell.Active(SpellSlot.R, 2500);
            Drawing.OnDraw += Drawing_OnDraw;
            Game.OnUpdate += Game_OnUpdate;
            Game.OnWndProc += Game_OnWndProc;
            MenuInit();
            Dash.OnDash += Dash_OnDash;
            Magnet.Initialize();
            Targetting.Initialize();
        }

        private static void Dash_OnDash(Obj_AI_Base sender, Dash.DashEventArgs e)
        {
            if (!sender.IsMe)
            {
                return;
            }
            var target = TargetSelector.GetTarget(1500, DamageType.Physical);
            if (!target.IsValidTarget())
            {
                return;
            }
            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo))
            {
                if ((int)Rengar.Mana == 5)
                {
                    switch (AllMenu["combo.mode"].Cast<Slider>().CurrentValue)
                    {
                        case 2:
                            if (E.IsReady() && target.IsValidTarget(E.Range))
                            {
                                E.Cast(target);
                            }
                            break;
                        case 1:
                            if (Q.IsReady() && target.IsValidTarget(Q.Range))
                            {
                                QCastResetAa();
                            }

                            if (target.IsValidTarget(Q.Range))
                            {
                                {
                                    if (target.IsValidTarget(W.Range))
                                    {
                                        W.Cast();
                                    }

                                    E.Cast(target);
                                    FullItem(target);
                                }
                            }

                            break;
                    }
                }
            }
            switch (AllMenu["combo.mode"].Cast<Slider>().CurrentValue)
            {
                case 2:
                    if (E.IsReady() && target.IsValidTarget(E.Range))
                    {
                        E.Cast(target);
                    }
                    break;

                case 1:
                    if (RengarUltiActive)
                    {
                        QCastResetAa();
                    }
                    break;
            }
            if (e.Duration - 100 - Game.Ping / 2 > 0)
            {
              
                Core.DelayAction(() => FullItem(target), (e.Duration - 100 - Game.Ping / 2));
            }
        }

        private static void Game_OnWndProc(WndEventArgs args)
        {
            if (args.Msg != (uint)WindowMessages.LeftButtonDown)
            {
                return;
            }
            var unit2 =
                ObjectManager.Get<Obj_AI_Base>()
                    .FirstOrDefault(
                        a =>
                        (a.IsValidTarget()) && a.IsEnemy && a.Distance(Game.CursorPos) < a.BoundingRadius + 80
                        );
            if (unit2 != null)
            {
                SelectedEnemy = unit2;
            }
        }

        private static void Game_OnUpdate(EventArgs args)
        {
            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo))
            {
                Combo();
            }
            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.LaneClear))
            {
                LaneClear();
            }
            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.JungleClear))
            {
                JungleClear();
            }
            AutoHeal();
            AutoYoumu();
            Skin();
            BetaQ();
            SmiteCombo();
        }

        private static void BetaQ()
        {
            var comboselecctedd = AllMenu["combo.mode"].Cast<Slider>().CurrentValue;
            if (RengarUltiActive && comboselecctedd == 1 && SelectedEnemy.Distance(Rengar.ServerPosition) <= 1000)
            {
                Core.DelayAction(() => QCastResetAa(), 180);
            }
        }

        private static void QCastResetAa()
        {
            Q.Cast();
            Orbwalker.ResetAutoAttack();
        }

        private static void FullItem(AIHeroClient target)
        {
            Items();
            BotrkAndBilgewater(target);
        }

        private static void CastSmite(SpellSlot smiteSlotx, AIHeroClient target)
        {
            if (AllMenu["use.smite"].Cast<CheckBox>().CurrentValue && !RengarUltiActive && Smite != SpellSlot.Unknown
                    && Rengar.Spellbook.CanUseSpell(Smite) == SpellState.Ready && target.IsValidTarget(500))
            {
                Rengar.Spellbook.CastSpell(smiteSlotx, target);
            }
        }

        private static void Skin()
        {
            var skinHackActive = AllMenu["skin.active"].Cast<CheckBox>().CurrentValue;
            var skinHackSelected = AllMenu["skin.value"].Cast<Slider>().CurrentValue;

            if(!skinHackActive)
            {
                Rengar.SetSkinId(0);
                return;
            }

            switch (skinHackSelected)
            {
                case 1: { Rengar.SetSkinId(1); break; }
                case 2: { Rengar.SetSkinId(2); break; }
                case 3: { Rengar.SetSkinId(3); break; }
            }
        }

        private static void Drawing_OnDraw(EventArgs args)
        {
            var comboModeDrawActive = AllMenu["draw.mode"].Cast<CheckBox>().CurrentValue;
            var comboModeSelected = AllMenu["combo.mode"].Cast<Slider>().CurrentValue;
            var selectedEnemyDrawActive = AllMenu["draw.selectedenemy"].Cast<CheckBox>().CurrentValue;

            if (comboModeDrawActive)
            {
                switch (comboModeSelected)
                {
                    case 1:
                        {
                            Drawing.DrawText(Drawing.Width * 0.70f, Drawing.Height * 0.95f, Color.White, "Mode : OneShot");
                            break;
                        }
                    case 2:
                        {
                            Drawing.DrawText(Drawing.Width * 0.70f, Drawing.Height * 0.95f, Color.White, "Mode : Snare");
                            break;
                        }
                }
            }
            if (selectedEnemyDrawActive && SelectedEnemy.IsValidTarget() && SelectedEnemy.IsVisible && !SelectedEnemy.IsDead && !(SelectedEnemy.IsMinion || SelectedEnemy.IsMonster) && !(SelectedEnemy is Obj_AI_Turret))
            {
                Drawing.DrawText(
                Drawing.WorldToScreen(SelectedEnemy.Position).X - 40,
                Drawing.WorldToScreen(SelectedEnemy.Position).Y + 10,
                Color.White,
                "Selected Target");
            }
        }
        private static void JungleClear()
        {
            var useQActive = AllMenu["jungleclear.q"].Cast<CheckBox>().CurrentValue;
            var useWActive = AllMenu["jungleclear.w"].Cast<CheckBox>().CurrentValue;
            var useEActive = AllMenu["jungleclear.e"].Cast<CheckBox>().CurrentValue;
            var jungleClearSaveStacksActive = AllMenu["jungleclear.save"].Cast<CheckBox>().CurrentValue;
            foreach (var jungleMinion in EntityManager.MinionsAndMonsters.Monsters)
            {
                if (Rengar.Mana < 5 || ((int)Rengar.Mana == 5 && !jungleClearSaveStacksActive))
                {

                    if (useQActive && Q.IsReady() && Rengar.Distance(jungleMinion) < Rengar.AttackRange)
                    {
                        QCastResetAa();
                        Items();
                    }
                    if (useWActive && W.IsReady() && Rengar.Distance(jungleMinion) <= W.Range)
                    {
                        W.Cast();
                    }
                    if (useEActive && E.IsReady() && Rengar.Distance(jungleMinion) <= E.Range)
                    {
                        E.Cast(jungleMinion);
                    }
                }
            }
        }

        private static void LaneClear()
        {
            var useQActive = AllMenu["laneclear.q"].Cast<CheckBox>().CurrentValue;
            var useWActive = AllMenu["laneclear.w"].Cast<CheckBox>().CurrentValue;
            var useEActive = AllMenu["laneclear.e"].Cast<CheckBox>().CurrentValue;
            var laneClearSaveStacksActive = AllMenu["laneclear.save"].Cast<CheckBox>().CurrentValue;
            var laneTarget = EntityManager.MinionsAndMonsters.EnemyMinions.FirstOrDefault(x => !x.IsDead && Q.IsInRange(x));

            if (Rengar.Mana < 5 || ((int)Rengar.Mana == 5 && !laneClearSaveStacksActive))
            {
                if (useWActive && W.IsReady() && laneTarget.IsValidTarget())
                {
                    W.Cast();
                }
                if (useQActive && Q.IsReady() && laneTarget.IsValidTarget())
                {
                    QCastResetAa();
                }
                if (laneTarget.IsValidTarget(Rengar.GetAutoAttackRange()))
                {
                    Items();
                }
                if (useEActive && E.IsReady() && laneTarget.IsValidTarget())
                {
                    E.Cast(laneTarget);
                }
            }
        }

        private static void AutoHeal()
        {
            var tickedAutoHp = AllMenu["autohp.active"].Cast<CheckBox>().CurrentValue;
            var valueOfAutoHp = AllMenu["autohp.value"].Cast<Slider>().CurrentValue;

            if(tickedAutoHp && (int)Rengar.Mana == 5 && Rengar.HealthPercent <= valueOfAutoHp) { W.Cast(); }
        }

        private static void Combo()
        {
            var comboModeSelected = AllMenu["combo.mode"].Cast<Slider>().CurrentValue;
            var normalTarget = TargetSelector.SelectedTarget != null
                                ? TargetSelector.SelectedTarget
                                : TargetSelector.GetTarget(E.Range, DamageType.Physical);
            var ePrediction = E.GetPrediction(normalTarget);
            var useEOutQRangeActive = AllMenu["useeoutofq"].Cast<CheckBox>().CurrentValue;
            

            if (SelectedEnemy.IsValidTarget(E.Range))
            {
                TargetSelector.GetPriority(normalTarget);
                if (TargetSelector.SelectedTarget != null)
                {
                    TargetSelector.GetPriority(TargetSelector.SelectedTarget);
                }
            }

            if (RengarUltiActive || normalTarget == null)
            {
                return;
            }
            switch (comboModeSelected)
            {
                case 1://OneShot Mode Active
                    {
                        if (Rengar.Mana <= 4 && !RengarHasPassive) //Normal Lane Target Logic
                        {
                            if (W.IsReady() && normalTarget.IsValidTarget(W.Range)) { W.Cast(); }
                            if (Q.IsReady() && normalTarget.IsValidTarget(Q.Range)) { QCastResetAa(); }
                            FullItem(normalTarget);
                            CastSmite(Smite, normalTarget);
                            if (E.IsReady() && normalTarget.IsValidTarget(E.Range) && ePrediction.HitChance >= HitChance.High && ePrediction.CollisionObjects.Count() == 0) { E.Cast(normalTarget); }
                        }

                        if ((int)Rengar.Mana == 5 && !RengarHasPassive) //When Have 5 Prio Use Q
                        {
                            if (Q.IsReady() && normalTarget.IsValidTarget(Q.Range)) { QCastResetAa(); }
                            FullItem(normalTarget);
                            CastSmite(Smite, normalTarget);
                        }

                        if (RengarHasPassive && Rengar.Mana <= 4) //Passive Logic
                        {
                            if (Q.IsReady() && normalTarget.IsValidTarget(600)) { QCastResetAa(); }
                            FullItem(normalTarget);
                            CastSmite(Smite, normalTarget);
                            if (W.IsReady() && normalTarget.IsValidTarget(W.Range)) { W.Cast(); }
                        }
                        if (RengarHasPassive && (int)Rengar.Mana == 5)
                        {
                            if (Q.IsReady() && normalTarget.IsValidTarget(600)) { QCastResetAa(); }
                            FullItem(normalTarget);
                            CastSmite(Smite, normalTarget);
                        }
                        if (!RengarHasPassive && normalTarget.Distance(Rengar) <= E.Range &&
                            useEOutQRangeActive)//Use E out of Range Q When One Shot Mode Active
                        {
                            if (E.IsReady() && normalTarget.IsValidTarget(E.Range) &&
                                ePrediction.HitChance >= HitChance.High && ePrediction.CollisionObjects.Count() == 0)
                            {
                                E.Cast(normalTarget);
                            }
                        }
                        break;
                    }
                case 2://Snare Combo
                    {
                        if (Rengar.Mana <= 4 && !RengarHasPassive) //Normal Lane Target Logic
                        {
                            if (W.IsReady() && normalTarget.IsValidTarget(W.Range)) { W.Cast(); }
                            if (Q.IsReady() && normalTarget.IsValidTarget(Q.Range)) { QCastResetAa(); }
                            FullItem(normalTarget);
                            CastSmite(Smite, normalTarget);
                            if (E.IsReady() && normalTarget.IsValidTarget(E.Range) && ePrediction.HitChance >= HitChance.High && ePrediction.CollisionObjects.Count() == 0) { E.Cast(normalTarget); }
                        }

                        if ((int)Rengar.Mana == 5 && !RengarHasPassive) //When Have 5 Prio Use E
                        {
                            if (E.IsReady() && normalTarget.IsValidTarget(E.Range) && ePrediction.HitChance >= HitChance.High && ePrediction.CollisionObjects.Count() == 0) { E.Cast(normalTarget); }
                        }

                        if (RengarHasPassive && Rengar.Mana <= 4) //Passive Logic
                        {
                            if (Q.IsReady() && normalTarget.IsValidTarget(600)) { QCastResetAa(); }
                            FullItem(normalTarget);
                            CastSmite(Smite, normalTarget);
                            if (W.IsReady() && normalTarget.IsValidTarget(W.Range)) { W.Cast(); }
                        }
                        if (RengarHasPassive && (int)Rengar.Mana == 5)
                        {
                            if (E.IsReady() && normalTarget.IsValidTarget(E.Range) && ePrediction.HitChance >= HitChance.High && ePrediction.CollisionObjects.Count() == 0) { E.Cast(normalTarget); }
                        }
                        break;
                    }
            }
        }
        private static void MenuInit()
        {
            Menu = MainMenu.AddMenu("Rengar - Lolscripts.net", "Rengar - Lolscripts.net");
            Menu.AddGroupLabel("Rengar - Lolscripts.net CARGADO !!!");
            Menu.AddLabel("Lolscripts.net");
            Menu.AddLabel("Cualquier duda consulte en el post");
            Menu.AddLabel("Lolscripts.net");
            Menu.AddLabel("Lolscripts.net");
            Menu.AddLabel("Diviértete !");

            AllMenu = Menu.AddSubMenu("Config", "Config");
            AllMenu.AddSeparator();
            AllMenu.AddGroupLabel("Combo Modo");
            AllMenu.AddLabel("| 1 -> One Shot || 2 -> Snare |");
            AllMenu.Add("combo.mode", new Slider("Combo Modo", 1, 1, 2));
            var switcher = AllMenu.Add("Switcher", new KeyBind("Combo Modo Switcher", false, KeyBind.BindTypes.HoldActive, (uint)'T'));
            switcher.OnValueChange += delegate (ValueBase<bool> sender, ValueBase<bool>.ValueChangeArgs args)
            {
                if (args.NewValue == true)
                {
                    var cast = AllMenu["combo.mode"].Cast<Slider>();
                    if (cast.CurrentValue == cast.MaxValue)
                    {
                        cast.CurrentValue = 0;
                    }
                    else
                    {
                        cast.CurrentValue++;
                    }
                }
            };
            AllMenu.Add("autoyoumu", new CheckBox("Auto Youmu con la Ulti"));
            AllMenu.Add("draw.mode", new CheckBox("Circulos Modo"));
            AllMenu.Add("draw.selectedenemy", new CheckBox("Circulo en target seleccionado"));
            AllMenu.Add("magnet.enable", new CheckBox("Activar Iman"));
            AllMenu.Add("use.smite", new CheckBox("Usar Smite en Combo"));
            AllMenu.Add("useeoutofq", new CheckBox("Usar E fuera del rango de la Q"));
            AllMenu.AddSeparator();
            AllMenu.AddGroupLabel("LimpiarLinea Modo");
            AllMenu.Add("laneclear.q", new CheckBox("Usar Q"));
            AllMenu.Add("laneclear.w", new CheckBox("Usar W"));
            AllMenu.Add("laneclear.e", new CheckBox("Usar E"));
            AllMenu.Add("laneclear.save", new CheckBox("Guardar Stacks", false));
            AllMenu.AddSeparator();
            AllMenu.AddGroupLabel("LimpiarJungla Modo");
            AllMenu.Add("jungleclear.q", new CheckBox("Usar Q"));
            AllMenu.Add("jungleclear.w", new CheckBox("Usar W"));
            AllMenu.Add("jungleclear.e", new CheckBox("Usar E"));
            AllMenu.Add("jungleclear.save", new CheckBox("Guardar Stacks", false));
            AllMenu.AddSeparator();
            AllMenu.AddGroupLabel("Auto Hp %x when 5 prio");
            AllMenu.Add("autohp.active", new CheckBox("AutoHP Activo"));
            AllMenu.Add("autohp.value", new Slider("AutoHP Valor", 30, 1));
            AllMenu.AddSeparator();
            AllMenu.Add("skin.active", new CheckBox("Skin Hack Activo"));
            AllMenu.AddLabel("| 1 -> HeadHunter || 2 -> NighHunter || 3-> SSW");
            AllMenu.Add("skin.value", new Slider("Seleccionado Skin", 3, 1, 3));
        }

        private static void Items()
        {
            var normalTarget = TargetSelector.GetTarget(R.Range, DamageType.Physical);
            if (Item.CanUseItem(3074) && normalTarget.IsValidTarget(400))
            {
                Item.UseItem(3074);
            }
            if (Item.CanUseItem(3077) && normalTarget.IsValidTarget(400))
            {
                Item.UseItem(3077);
            }
        }

        private static void BotrkAndBilgewater(AIHeroClient targetforuseBotRk)
        {
            if (Item.CanUseItem(3144)) { Item.UseItem(3144, targetforuseBotRk); }
            if (Item.CanUseItem(3153)) { Item.UseItem(3153, targetforuseBotRk); }
        }

        private static void AutoYoumu()
        {
            var autoYoumuActive = AllMenu["autoyoumu"].Cast<CheckBox>().CurrentValue;
            
            if (!autoYoumuActive) { return; }
            if (RengarUltiActive && Item.CanUseItem(3142)) 
			{
            Core.DelayAction((() => Item.UseItem(3142)),600); 
			}
        }
    }
}
