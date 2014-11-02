using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
using Color = System.Drawing.Color;

namespace Pentakill_Syndra
{
    class Program
    {
        public const string ChampionName = "Syndra";

        //Orbwalker instance
        public static Orbwalking.Orbwalker Orbwalker;

        //Spells
        public static List<Spell> SpellList = new List<Spell>();

        public static Spell Q;
        public static Spell W;
        public static Spell E;
        public static Spell QE;
        public static Spell R;

        public static SpellSlot IgniteSlot;

        public static Items.Item DFG;

        private static int UseRTime = 0;
        private static int DontUseRTime = 0;

        //Menu
        public static Menu Config;

        private static Obj_AI_Hero Player;
        static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }

        private static void Game_OnGameLoad(EventArgs args)
        {
            Player = ObjectManager.Player;

            if (Player.BaseSkinName != ChampionName) return;

            //Create the spells
            Q = new Spell(SpellSlot.Q, 790);
            W = new Spell(SpellSlot.W, 925);
            E = new Spell(SpellSlot.E, 700);
            R = new Spell(SpellSlot.R, 675);
            QE = new Spell(SpellSlot.Q, 1280);

            IgniteSlot = Player.GetSpellSlot("SummonerDot");

            DFG = Utility.Map.GetMap()._MapType == Utility.Map.MapType.TwistedTreeline ? new Items.Item(3188, 750) : new Items.Item(3128, 750);

            Q.SetSkillshot(0.6f, 125f, float.MaxValue, false, SkillshotType.SkillshotCircle);
            W.SetSkillshot(0.25f, 140f, 1600f, false, SkillshotType.SkillshotCircle);
            E.SetSkillshot(0.25f, (float)(45 * 0.5), 2500f, false, SkillshotType.SkillshotCircle);
            QE.SetSkillshot(0f, 60f, 1600f, false, SkillshotType.SkillshotLine);

            SpellList.Add(Q);
            SpellList.Add(W);
            SpellList.Add(E);
            SpellList.Add(R);

            //Create the menu
            Config = new Menu(ChampionName, ChampionName, true);

            //Orbwalker submenu
            Config.AddSubMenu(new Menu("Orbwalking", "Orbwalking"));

            //Add the target selector to the menu as submenu.
            var targetSelectorMenu = new Menu("Target Selector", "Target Selector");
            SimpleTs.AddToMenu(targetSelectorMenu);
            Config.AddSubMenu(targetSelectorMenu);

            //Load the orbwalker and add it to the menu as submenu.
            Orbwalker = new Orbwalking.Orbwalker(Config.SubMenu("Orbwalking"));


            //Combo menu:
            Config.AddSubMenu(new Menu("Combo", "Combo"));
            Config.SubMenu("Combo").AddItem(new MenuItem("UseQCombo", "Use Q").SetValue(true));
            Config.SubMenu("Combo").AddItem(new MenuItem("UseWCombo", "Use W").SetValue(true));
            Config.SubMenu("Combo").AddItem(new MenuItem("UseECombo", "Use E").SetValue(true));
            Config.SubMenu("Combo").AddItem(new MenuItem("UseQECombo", "Use QE").SetValue(true));
            Config.SubMenu("Combo").AddItem(new MenuItem("UseRCombo", "Use R").SetValue(true));     
            Config.SubMenu("Combo").AddItem(new MenuItem("AntiOverkillCombo", "Save R if enemy is killable with Q").SetValue(true));
            Config.SubMenu("Combo").AddItem(new MenuItem("ComboActive", "Combo!").SetValue(new KeyBind(Config.Item("Orbwalk").GetValue<KeyBind>().Key, KeyBindType.Press)));

            //Harass menu:
            Config.AddSubMenu(new Menu("Harass", "Harass"));
            Config.SubMenu("Harass").AddItem(new MenuItem("UseQHarass", "Use Q").SetValue(true));
            Config.SubMenu("Harass").AddItem(new MenuItem("UseWHarass", "Use W").SetValue(false));
            Config.SubMenu("Harass").AddItem(new MenuItem("UseEHarass", "Use E").SetValue(false));
            Config.SubMenu("Harass").AddItem(new MenuItem("UseQEHarass", "Use QE").SetValue(false));
            Config.SubMenu("Harass").AddItem(new MenuItem("AutoHarass", "Auto harass when enemy do AA").SetValue(true));
            Config.SubMenu("Harass").AddItem(new MenuItem("HarassActive", "Harass!").SetValue(new KeyBind("C".ToCharArray()[0], KeyBindType.Press)));
            Config.SubMenu("Harass").AddItem(new MenuItem("HarassActiveT", "Harass (toggle)!").SetValue(new KeyBind("Y".ToCharArray()[0],KeyBindType.Toggle)));

            Config.AddSubMenu(new Menu("Ultimate", "Ultimate"));
            Config.SubMenu("Ultimate").AddSubMenu(new Menu("Dont use R on", "DontUlt"));

            foreach (var enemy in ObjectManager.Get<Obj_AI_Hero>().Where(enemy => enemy.Team != Player.Team))
                Config.SubMenu("Ultimate").SubMenu("DontUlt").AddItem(new MenuItem("DontUlt" + enemy.BaseSkinName, enemy.BaseSkinName).SetValue(false));

            Config.SubMenu("Ultimate").AddItem(new MenuItem("Force ultimate cast", "CastR").SetValue(new KeyBind("J".ToCharArray()[0], KeyBindType.Press)));
            Config.SubMenu("Ultimate").AddItem(new MenuItem("Don't use R in the next 10 seconds", "DontUseR").SetValue(new KeyBind("G".ToCharArray()[0], KeyBindType.Press)));

            //Misc
            Config.AddSubMenu(new Menu("Misc", "Misc"));
            Config.SubMenu("Misc").AddItem(new MenuItem("AutoInterruptSpells", "Interrupt spells").SetValue(true));
            Config.SubMenu("Misc").AddItem(new MenuItem("AntiGapclosers", "Use QE(or E) for GapCloser").SetValue(true));
            Config.SubMenu("Misc").AddItem(new MenuItem("CastQE", "QE closest to cursor").SetValue(new KeyBind("T".ToCharArray()[0],KeyBindType.Press)));


            //Damage after combo:
            var dmgAfterComboItem = new MenuItem("DamageAfterCombo", "Draw damage after combo").SetValue(true);
            Utility.HpBarDamageIndicator.DamageToUnit = GetComboDamage;
            Utility.HpBarDamageIndicator.Enabled = dmgAfterComboItem.GetValue<bool>();
            dmgAfterComboItem.ValueChanged += delegate(object sender, OnValueChangeEventArgs eventArgs)
            {
                Utility.HpBarDamageIndicator.Enabled = eventArgs.GetNewValue<bool>();
            };

            //Drawings menu:
            Config.AddSubMenu(new Menu("Drawings", "Drawings"));
            Config.SubMenu("Drawings").AddItem(new MenuItem("QRange", "Q range").SetValue(new Circle(true, Color.FromArgb(100, 255, 0, 255))));
            Config.SubMenu("Drawings").AddItem(new MenuItem("WRange", "W range").SetValue(new Circle(false, Color.FromArgb(100, 255, 0, 255))));
            Config.SubMenu("Drawings").AddItem(new MenuItem("ERange", "E range").SetValue(new Circle(false, Color.FromArgb(100, 255, 0, 255))));
            Config.SubMenu("Drawings").AddItem(new MenuItem("RRange", "R range").SetValue(new Circle(false, Color.FromArgb(100, 255, 0, 255))));
            Config.SubMenu("Drawings").AddItem(new MenuItem("QERange", "QE range").SetValue(new Circle(true, Color.FromArgb(100, 255, 0, 255))));
            Config.SubMenu("Drawings").AddItem(new MenuItem("QEIndicator", "QE Indicator").SetValue(new Circle(true, Color.FromArgb(100, 255, 0, 255))));
            Config.SubMenu("Drawings").AddItem(dmgAfterComboItem);
            Config.AddToMainMenu();

            //Add the events we are going to use:
            Game.OnGameUpdate += Game_OnGameUpdate;
            Obj_AI_Base.OnProcessSpellCast += Obj_AI_Hero_OnProcessSpellCast;
            Drawing.OnDraw += Drawing_OnDraw;
            Interrupter.OnPossibleToInterrupt += Interrupter_OnPossibleToInterrupt;
            AntiGapcloser.OnEnemyGapcloser += AntiGapcloser_OnEnemyGapcloser;
            Orbwalking.BeforeAttack += Orbwalking_BeforeAttack;
            Game.PrintChat("If it fails to use W, Please go to Appdata -> Roaming -> Leaguesharp folder and clear it and test again before reporting!");
            Game.PrintChat(ChampionName + " Loaded! --- By esk0r, Modified and tweaked by xSalice");
        }

        static void Orbwalking_BeforeAttack(Orbwalking.BeforeAttackEventArgs args)
        {
            if (Config.Item("ComboActive").GetValue<KeyBind>().Active)
                args.Process = !(Q.IsReady() || W.IsReady());
        }

        public static void AntiGapcloser_OnEnemyGapcloser(ActiveGapcloser gapcloser)
        {
            if (!Config.Item("AntiGapclosers").GetValue<bool>()) return;

            if (E.IsReady() && gapcloser.Sender.IsValidTarget(E.Range))
            {
                if (Q.IsReady())
                {
                    StartQECombo(gapcloser.Sender);           
                }
                else
                    E.Cast(gapcloser.Sender);
            }
                
        }

        private static void Interrupter_OnPossibleToInterrupt(Obj_AI_Base unit, InterruptableSpell spell)
        {
            if (!Config.Item("AutoInterruptSpells").GetValue<bool>()) return;

            if (E.IsReady() && unit.IsValidTarget(E.Range))
            {
                if (Q.IsReady())
                    StartQECombo(unit);
                else
                    E.Cast(unit);
            }
            else if (Q.IsReady() && E.IsReady() && unit.IsValidTarget(QE.Range))
                StartQECombo(unit);
            else if (Player.Spellbook.GetSpell(SpellSlot.W).ToggleState != 1 && E.IsReady() && W.IsReady() && unit.IsValidTarget(QE.Range))
                StartWECombo(unit);
        }

        static void StartQECombo(Obj_AI_Base target)
        {
            if (!(Q.IsReady() && E.IsReady())) return;
            if (target.IsValidTarget(Q.Range))
            {
                PredictionOutput prediction = Q.GetPrediction(target);
                if (prediction.Hitchance >= HitChance.Medium)
                    Q.Cast(Player.ServerPosition.To2D().Extend(prediction.CastPosition.To2D(), Player.Distance(prediction.CastPosition.To2D()) - 90f));
            }
            else if (target.IsValidTarget(QE.Range))
            {
                QE.Delay = Q.Delay - (E.Range / QE.Speed);
                PredictionOutput prediction = QE.GetPrediction(target);
                if (prediction.Hitchance >= HitChance.Medium)
                {

                    Q.Cast(Player.ServerPosition.To2D().Extend(prediction.CastPosition.To2D(), E.Range));
                }
            }
        }

        private static void Obj_AI_Hero_OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (sender.IsMe && (args.SData.Name == "SyndraQ"))
                Q.LastCastAttemptT = Environment.TickCount;
            if (sender.IsMe && (args.SData.Name == "SyndraW" || args.SData.Name == "syndrawcast"))
                W.LastCastAttemptT = Environment.TickCount;
            if (sender.IsMe && (args.SData.Name == "SyndraE" || args.SData.Name == "syndrae5"))
                E.LastCastAttemptT = Environment.TickCount;

            if (sender.Team != Player.Team && Config.Item("AutoHarass").GetValue<bool>() && (args.SData.Name.Contains("attack")))
                Harass();
        }

        static void StartWECombo(Obj_AI_Base target)
        {
            if (!(W.IsReady() && E.IsReady())) return;
            if (target.IsValidTarget(Q.Range) && OrbManager.WObject(true) != null)
            {                              
                W.From = OrbManager.WObject(true).ServerPosition;
                PredictionOutput prediction = W.GetPrediction(target);
                if (prediction.Hitchance >= HitChance.Medium)
                    W.Cast(Player.ServerPosition.To2D().Extend(prediction.CastPosition.To2D(), Player.Distance(prediction.CastPosition.To2D()) - 90f));
            }
            else if (target.IsValidTarget(QE.Range) && OrbManager.WObject(true) != null)
            {
                W.From = OrbManager.WObject(true).ServerPosition;
                QE.Delay = W.Delay + (W.From.To2D().Distance(Player.ServerPosition.To2D().Extend(Player.ServerPosition.To2D(), E.Range)) / W.Speed) - (E.Range / QE.Speed);
                PredictionOutput prediction = QE.GetPrediction(target);

                if (prediction.Hitchance >= HitChance.Medium)
                {
                    W.Cast(Player.ServerPosition.To2D().Extend(prediction.CastPosition.To2D(), E.Range));
                }
            }
        }

        private static void Drawing_OnDraw(EventArgs args)
        {
            if (Player.IsDead) return;

            //Draw the ranges of the spells.
            var menuItem = Config.Item("QERange").GetValue<Circle>();
            if (menuItem.Active) Utility.DrawCircle(Player.Position, QE.Range, menuItem.Color);

            foreach (var spell in SpellList)
            {
                menuItem = Config.Item(spell.Slot + "Range").GetValue<Circle>();
                if (menuItem.Active)
                    Utility.DrawCircle(Player.Position, spell.Range, menuItem.Color);
            }

            if (Config.Item("CastQE").GetValue<KeyBind>().Active)
            {
                menuItem = Config.Item("QEIndicator").GetValue<Circle>();
                if (menuItem.Active)
                    Utility.DrawCircle(Game.CursorPos, 300f, menuItem.Color);
            }
            
        }

        private static void Game_OnGameUpdate(EventArgs args)
        {
            if (Player.IsDead) return;
            //Update the R range
            R.Range = R.Level == 3 ? 750 : 675;
            if (Config.Item("CastQE").GetValue<KeyBind>().Active && E.IsReady() && Q.IsReady())
                foreach (var enemy in ObjectManager.Get<Obj_AI_Hero>())
                    if (enemy.IsValidTarget(QE.Range) && Game.CursorPos.Distance(enemy.ServerPosition) < 300)
                        StartQECombo(enemy);
            if (Config.Item("ComboActive").GetValue<KeyBind>().Active)
            {
                Combo();
            }
            else
            {
                if (Config.Item("HarassActive").GetValue<KeyBind>().Active || Config.Item("HarassActiveT").GetValue<KeyBind>().Active)
                    Harass();

            }
            if (Config.Item("DontUseR").GetValue<KeyBind>().Active)
            {
                DontUseRTime = Environment.TickCount;
                UseRTime = 0;
            }
            else if (Config.Item("CastR").GetValue<KeyBind>().Active)
            {
                DontUseRTime = 0;
                UseRTime = Environment.TickCount;
            }
        }

        private static void Combo()
        {
            UseSpells(Config.Item("UseQCombo").GetValue<bool>(), Config.Item("UseWCombo").GetValue<bool>(),Config.Item("UseECombo").GetValue<bool>(), Config.Item("UseQECombo").GetValue<bool>(), Config.Item("UseRCombo").GetValue<bool>());
        }

        private static void Harass()
        {
            UseSpells(Config.Item("UseQHarass").GetValue<bool>(), Config.Item("UseWHarass").GetValue<bool>(), Config.Item("UseEHarass").GetValue<bool>(), Config.Item("UseQEHarass").GetValue<bool>(), false);
        }

        static void UseSpells(bool useQ, bool useW, bool useE, bool useQE, bool useR)
        {
            var qTarget = SimpleTs.GetTarget(Q.Range + Q.Width, SimpleTs.DamageType.Magical);
            var wTarget = SimpleTs.GetTarget(W.Range + W.Width, SimpleTs.DamageType.Magical);
            var qeTarget = SimpleTs.GetTarget(QE.Range, SimpleTs.DamageType.Magical);
            var rTarget = SimpleTs.GetTarget(R.Range, SimpleTs.DamageType.Magical);

            if (Environment.TickCount - DontUseRTime < 10000)
                useR = false;
            if (useQE && ((qTarget != null) || (qeTarget != null)) && Q.IsReady() && E.IsReady())
            {
                if (qTarget != null)
                    StartQECombo(qTarget);
                else if (qeTarget != null)
                    StartQECombo(qeTarget);
            }
            
            if (useW && useE && ((wTarget == null) && (qeTarget != null)) && W.IsReady() && Player.Spellbook.GetSpell(SpellSlot.W).ToggleState != 1 && E.IsReady())
            {
                StartWECombo(qeTarget);
            }
            
            if (qTarget != null && useQ && Q.IsReady())
            {
                PredictionOutput prediction = Q.GetPrediction(qTarget, true);
                if (prediction.Hitchance >= HitChance.Medium)
                    Q.Cast(prediction.CastPosition, true);
            }
            if (useE && E.IsReady())
            {
                foreach (var enemy in ObjectManager.Get<Obj_AI_Hero>())
                {
                    if (enemy.IsValidTarget(E.Range) && Player.GetSpellDamage(enemy, SpellSlot.E) > enemy.Health)
                        E.Cast(enemy.ServerPosition);
                }

                if (Environment.TickCount - W.LastCastAttemptT > Game.Ping + 250)
                {
                    foreach (var enemy in ObjectManager.Get<Obj_AI_Hero>())
                    {
                        if (enemy.IsValidTarget(QE.Range))
                            UseE(enemy);
                    }
                }

            }
                
            if (useW && W.IsReady())
            {
                if (Player.Spellbook.GetSpell(SpellSlot.W).ToggleState == 1 && W.IsReady() && qeTarget != null)
                {
                    //WObject
                    var gObjectPos = OrbManager.GetOrbToGrab((int)W.Range, wTarget == null);

                    if (gObjectPos.To2D().IsValid() && Environment.TickCount - W.LastCastAttemptT > Game.Ping + 250 && 
                        Environment.TickCount - E.LastCastAttemptT > Game.Ping + 250 + (E.Range / E.Speed) && Environment.TickCount - Q.LastCastAttemptT > Game.Ping + 250 + (E.Range / E.Speed) && W.IsReady())
                    {
                        W.Cast(gObjectPos);
                        W.LastCastAttemptT = Environment.TickCount;
                    }
                }
                else if (wTarget != null && Player.Spellbook.GetSpell(SpellSlot.W).ToggleState != 1 && W.IsReady() && Environment.TickCount - W.LastCastAttemptT > Game.Ping + 250)
                {
                    if (OrbManager.WObject(false) != null && W.IsReady())
                    {
                        W.From = OrbManager.WObject(false).ServerPosition;
                        PredictionOutput prediction = W.GetPrediction(wTarget, true);
                        if (prediction.Hitchance >= HitChance.Medium)
                            W.Cast(prediction.CastPosition);
                    }
                }
            }
            if (useR)
            {
                if (qTarget.IsValidTarget(R.Range))
                {
                    if (qTarget.IsValidTarget(R.Range) && ((Config.Item("DontUlt" + qTarget.BaseSkinName) != null && Config.Item("DontUlt" + qTarget.BaseSkinName).GetValue<bool>() == false) &&
                        (GetComboDamage(qTarget) > qTarget.Health) && (GetComboDamageWithoutUlt(qTarget) < qTarget.Health) &&
                        ((Config.Item("AntiOverkillCombo").GetValue<bool>() == false) || (Q.Level == 5 ? Player.GetSpellDamage(qTarget, SpellSlot.Q) * 1.15 : Player.GetSpellDamage(qTarget, SpellSlot.Q)) < qTarget.Health))
                        || (Environment.TickCount - UseRTime < 10000))
                    {
                        Player.SummonerSpellbook.CastSpell(IgniteSlot, qTarget); //R.IsReady()
                        DFG.Cast(qTarget);
                        if (R.IsReady())
                        {
                            R.Cast(qTarget);
                        }
                    }
                }
                else if (rTarget.IsValidTarget(R.Range))
                {
                    if (rTarget.IsValidTarget(R.Range) && ((Config.Item("DontUlt" + rTarget.BaseSkinName) != null && Config.Item("DontUlt" + rTarget.BaseSkinName).GetValue<bool>() == false) &&
                        (GetComboDamage(rTarget) > rTarget.Health) && (GetComboDamageWithoutUlt(rTarget) < rTarget.Health) &&
                        ((Config.Item("AntiOverkillCombo").GetValue<bool>() == false) || (Q.Level == 5 ? Player.GetSpellDamage(rTarget, SpellSlot.Q) * 1.15 : Player.GetSpellDamage(rTarget, SpellSlot.Q)) < rTarget.Health))
                        || (Environment.TickCount - UseRTime < 10000))
                    {
                        Player.SummonerSpellbook.CastSpell(IgniteSlot, rTarget); //R.IsReady()
                        DFG.Cast(rTarget);
                        if (R.IsReady())
                        {
                            R.Cast(rTarget);
                        }
                    }
                }
                else
                {
                    foreach (var enemy in ObjectManager.Get<Obj_AI_Hero>())
                    {
                        if (enemy.IsValidTarget(R.Range) && ((Config.Item("DontUlt" + enemy.BaseSkinName) != null && Config.Item("DontUlt" + enemy.BaseSkinName).GetValue<bool>() == false) &&
                            (GetComboDamage(enemy) > enemy.Health) && (GetComboDamageWithoutUlt(enemy) < enemy.Health) &&
                            ((Config.Item("AntiOverkillCombo").GetValue<bool>() == false) || (Q.Level == 5 ? Player.GetSpellDamage(enemy, SpellSlot.Q) * 1.15 : Player.GetSpellDamage(enemy, SpellSlot.Q)) < enemy.Health))
                            || (Environment.TickCount - UseRTime < 10000))
                        {
                            Player.SummonerSpellbook.CastSpell(IgniteSlot, enemy); //R.IsReady()
                            DFG.Cast(enemy);
                            if (R.IsReady())
                            {
                                R.Cast(enemy);
                            }
                        }
                    }
                }
            }
        }

        private static float GetComboDamageWithoutUlt(Obj_AI_Base enemy)
        {
            var damage = 0d;

            if (Q.IsReady() && Q.Level == 5)
                damage += Player.GetSpellDamage(enemy, SpellSlot.Q) * 1.15;
            else if (Q.IsReady())
                damage += Player.GetSpellDamage(enemy, SpellSlot.Q);

            if (W.IsReady())
                damage += Player.GetSpellDamage(enemy, SpellSlot.W);

            if (E.IsReady())
                damage += Player.GetSpellDamage(enemy, SpellSlot.E);

            return (float)damage;
        }
        private static float GetUltDamage(Obj_AI_Base enemy)
        {
            var damage = 0d;

            if (DFG.IsReady())
                damage += Player.GetItemDamage(enemy, Damage.DamageItems.Dfg) / 1.2;

            if (IgniteSlot != SpellSlot.Unknown && Player.SummonerSpellbook.CanUseSpell(IgniteSlot) == SpellState.Ready && enemy.IsValidTarget(600))
                damage += ObjectManager.Player.GetSummonerSpellDamage(enemy, Damage.SummonerSpell.Ignite);

            if (R.IsReady())
                damage += Math.Min(6, Player.Spellbook.GetSpell(SpellSlot.R).Ammo) * Player.GetSpellDamage(enemy, SpellSlot.R, 1);

            return (float)damage * (DFG.IsReady() ? 1.2f : 1);
        }
        private static float GetComboDamage(Obj_AI_Base enemy)
        {
            var damage = 0d;

            if (Q.IsReady() && Q.Level == 5)
                damage += Player.GetSpellDamage(enemy, SpellSlot.Q) * 1.15;
            else if (Q.IsReady())
                damage += Player.GetSpellDamage(enemy, SpellSlot.Q);

            if (DFG.IsReady())
                damage += Player.GetItemDamage(enemy, Damage.DamageItems.Dfg) / 1.2;

            if (W.IsReady())
                damage += Player.GetSpellDamage(enemy, SpellSlot.W);

            if (E.IsReady())
                damage += Player.GetSpellDamage(enemy, SpellSlot.E);

            if (IgniteSlot != SpellSlot.Unknown && Player.SummonerSpellbook.CanUseSpell(IgniteSlot) == SpellState.Ready && enemy.IsValidTarget(600))
                damage += ObjectManager.Player.GetSummonerSpellDamage(enemy, Damage.SummonerSpell.Ignite);

            if (R.IsReady())
                damage += Math.Min(6, Player.Spellbook.GetSpell(SpellSlot.R).Ammo) * Player.GetSpellDamage(enemy, SpellSlot.R, 1);

            return (float)damage * (DFG.IsReady() ? 1.2f : 1);
        }

        static void UseE(Obj_AI_Base enemy)
        {
            foreach (var orb in OrbManager.GetOrbs(true))
            {
                if (Player.Distance(orb) < E.Range)
                {
                    var startPoint = orb.To2D().Extend(Player.ServerPosition.To2D(), 100);
                    var endPoint = Player.ServerPosition.To2D().Extend(orb.To2D(), Player.Distance(orb) > 200 ? 1300 : 1000);
                    QE.Delay = E.Delay + Player.Distance(orb) / QE.Speed;
                    QE.From = orb;
                    PredictionOutput enemyPred = QE.GetPrediction(enemy);
                    if (enemyPred.Hitchance >= HitChance.Medium && E.IsReady() && enemyPred.UnitPosition.To2D().Distance(startPoint, endPoint, false) < QE.Width + enemy.BoundingRadius)
                    {
                        E.Cast(orb);
                        E.LastCastAttemptT = Environment.TickCount;
                        return;
                    }
                }
            }
        }
    }
}
