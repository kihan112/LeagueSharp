using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeagueSharp;
using LeagueSharp.Common;

namespace Ryze
{
    class Program
    {
        static Spell Q;
        static Spell Qnc;
        static Spell W;
        static Spell E;
        static Spell R;

        static int PassiveCount;
        static int LastCasting;

        static Menu menu;
        static Orbwalking.Orbwalker orbwalker;

        static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += OnGameLoad;
        }

        static void OnGameLoad(EventArgs args)
        {
            if (ObjectManager.Player.ChampionName != "Ryze")
                return;

            Q = new Spell(SpellSlot.Q, 900f);
            Q.SetSkillshot(0.25f, 50f, 1700, true, SkillshotType.SkillshotLine);
            Qnc = new Spell(SpellSlot.Q, 900f);
            Qnc.SetSkillshot(0.25f, 50f, 1700, false, SkillshotType.SkillshotLine);
            W = new Spell(SpellSlot.W, 600f);
            E = new Spell(SpellSlot.E, 600f);
            R = new Spell(SpellSlot.R);

            menu = new Menu("Ryze", "Ryze", true);

            Menu OrbMenu = new Menu("OrbWalker", "OrbWalker");
            orbwalker = new Orbwalking.Orbwalker(OrbMenu);
            menu.AddSubMenu(OrbMenu);

            Menu TSMenu = new Menu("TargetSelector", "TargetSelector");
            menu.AddSubMenu(TSMenu);

            var Combo = new Menu("Combo", "Combo");
            {
                Combo.AddItem(new MenuItem("BlockAA", "Block AA while combo").SetValue(true));
            }
            menu.AddSubMenu(Combo);

            var Harass = new Menu("Harass", "Harass");
            {
                Harass.AddItem(new MenuItem("HarassUseQ", "Use Q").SetValue(true));
                Harass.AddItem(new MenuItem("HarassUseE", "Use E").SetValue(false));
            }
            menu.AddSubMenu(Harass);

            var Draw = new Menu("Draw", "Draw");
            {
                Draw.AddItem(new MenuItem("QRange", "Q Range").SetValue(true));
                Draw.AddItem(new MenuItem("WERange", "W,E Range").SetValue(true));
                //Draw.AddItem(new MenuItem("ComboInform", "Combo Informations").SetValue(true));
            }
            menu.AddSubMenu(Draw);

            menu.AddToMainMenu();

            Drawing.OnDraw += OnDraw;
            Game.OnUpdate += OnUpdate;
            Spellbook.OnCastSpell += OnCast;
        }      
        static void OnCast(Spellbook sender, SpellbookCastSpellEventArgs args)
        {
            {
                if ((args.Slot == SpellSlot.Q) || (args.Slot == SpellSlot.W) || (args.Slot == SpellSlot.E))
                    LastCasting = Environment.TickCount;
            }
        }

        static void OnUpdate(EventArgs args)
        {
            if (ObjectManager.Player.Buffs.Any(Buff => Buff.Name == "ryzepassivecharged" && Buff.IsValidBuff()))
                PassiveCount = 5;
            else
            {
                BuffInstance buffinstance = ObjectManager.Player.Buffs.Find(Buff => Buff.Name == "ryzepassivestack" && Buff.IsValidBuff());
                if (buffinstance != null)
                    PassiveCount = buffinstance.Count;
                else
                    PassiveCount = 0;
            }

            orbwalker.SetAttack(true);

            if ((orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo) && (menu.Item("BlockAA").GetValue<bool>()))
                orbwalker.SetAttack(false);

            if (LastCasting + 250 <= Environment.TickCount)
            {
                if (orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo)
                    DoCombo();

                else if (orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Mixed)
                    DoHarass();
            }

        }

        static int GetEnabledSkillsCount()
        {
            int enabledcount = 0;

            if (Q.IsReady())
                enabledcount++;

            if (W.IsReady())
                enabledcount++;

            if (E.IsReady())
                enabledcount++;

            if (R.IsReady())
                enabledcount++;

            return enabledcount;
        }

        static void DoCombo()
        {
            var QCooltime = 4f + (4f * ObjectManager.Player.PercentCooldownMod);
            var Target = TargetSelector.GetTarget(Program.E.Range, TargetSelector.DamageType.Magical);
            if (Target == null)
                return;

            int EnabledSkillsCount = GetEnabledSkillsCount();
            if (PassiveCount == 5)
            {
                if (W.IsReady() || W.Cooldown < 1.25f)
                {
                    if (W.IsReady())
                    {
                        W.Cast(Target);
                        return;
                    }
                    else
                    {
                        if (R.IsReady())
                        {
                            R.Cast();
                            return;
                        }
                        return;
                    }
                }
                else if (Q.IsReady())
                {
                    Qnc.Cast(Target);
                    return;
                }
                else if (E.IsReady())
                {
                    E.Cast(Target);
                    return;
                }
                else if (R.IsReady())
                {
                    R.Cast();
                    return;
                }
            }
            else if ((5 - PassiveCount) == EnabledSkillsCount)
            {
                if (PassiveCount == 4)
                {
                    if (W.IsReady())
                    {                      
                        W.Cast(Target);
                        return;                        
                    }          
                    else if (E.IsReady() && ((W.Cooldown <= QCooltime * 2) || (R.Cooldown <= QCooltime * 2)))               
                    {                 
                        E.Cast(Target);     
                        return;
                    }
                    else if (Q.IsReady() && ((W.Cooldown <= QCooltime) || (E.Cooldown <= QCooltime) || (R.Cooldown <= QCooltime)))
                    {   
                        Qnc.Cast(Target);   
                        return;
                    }                                           
                    else if (R.IsReady())       
                    {      
                        R.Cast();     
                        return;
                    }                                    
                }
                else if (PassiveCount == 3)
                {
                    if (R.IsReady())
                    {
                        if (Q.IsReady())
                        {
                            Qnc.Cast(Target);
                            return;
                        }
                        else if (W.IsReady())
                        {
                            W.Cast(Target);
                            return;
                        }
                        else if (E.IsReady())
                        {
                            E.Cast(Target);
                            return;
                        }
                    }
                    else
                    {
                        if (Q.IsReady() && W.IsReady())
                        {
                            Qnc.Cast(Target);
                            return;
                        }
                        else if(Q.IsReady() && E.IsReady())
                        {                                             
                            E.Cast(Target);
                            return;
                        }
                        else if(W.IsReady() && E.IsReady())
                        {                                     
                            E.Cast(Target);
                            return;
                        }
                    }
                }
                else if (PassiveCount == 2)
                {
                    if (Q.IsReady())
                    {
                        if (R.IsReady() && W.IsReady())
                        {
                            W.Cast(Target);
                            return;
                        }
                        else
                        {
                            Qnc.Cast(Target);
                            return;
                        }
                    }
                    else
                    {                      
                        W.Cast(Target);                
                        return;
                    }
                }
                else if (PassiveCount == 1)
                {
                    W.Cast(Target);
                    return;
                }
            }
            else if ((5 - PassiveCount) < EnabledSkillsCount)
            {
                if (W.IsReady())
                {
                    W.Cast(Target);
                    return;
                }
                else if (Q.IsReady())
                {
                    Qnc.Cast(Target);
                    return;
                }
                else if (E.IsReady())
                {
                    E.Cast(Target);
                    return;
                }
            }
            else
            {
                if (W.IsReady())
                {
                    W.Cast(Target);
                    return;
                }
                else if (Q.IsReady())
                {
                    Qnc.Cast(Target);
                    return;
                }
                else if (E.IsReady())
                {
                    E.Cast(Target);
                    return;
                }
            }
        }

        static void DoHarass()
        {
            var Target = TargetSelector.GetTarget(Program.E.Range, TargetSelector.DamageType.Magical);
            if (Target == null)
            {
                Target = TargetSelector.GetTarget(Program.Q.Range, TargetSelector.DamageType.Magical);
                if (Target != null)
                {
                    if (menu.Item("HarassUseQ").GetValue<bool>() && Q.IsReady())
                        Q.Cast(Target);
                }
            }
            else
            {
                if (menu.Item("HarassUseE").GetValue<bool>() && E.IsReady())
                    E.Cast(Target);
                if (menu.Item("HarassUseQ").GetValue<bool>() && Q.IsReady())
                    Q.Cast(Target);
            }

        }

        static void OnDraw(EventArgs args)
        {
            if (menu.Item("QRange").GetValue<bool>())
            {
                Render.Circle.DrawCircle(ObjectManager.Player.Position, Program.Q.Range, System.Drawing.Color.White, 2);
            }
            if (menu.Item("WERange").GetValue<bool>())
            {
                Render.Circle.DrawCircle(ObjectManager.Player.Position, Program.W.Range, System.Drawing.Color.White, 2);
            } 

        }
    }
}
