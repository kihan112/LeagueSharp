using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
using Color = System.Drawing.Color;

namespace Himeragi_Series.Champions
{
    class Leblanc : Loader
    {
        private static Spell Q;
        private static Spell W;
        private static Spell E;
        private static Obj_AI_Hero Target;
        private static Obj_AI_Base Clone;
        private static SpellDataInst Ignite;

        private int QLastCastedTime = 0;
        //private int WLastCastedTime = 0;
        //private int ELastCastedTime = 0;
        private int RQLastCastedTime = 0;
        //private int RWLastCastedTime = 0;
        //private int RELastCastedTime = 0;

        public Leblanc()
        {
            Q = new Spell(SpellSlot.Q, 700);
            Q.SetTargetted(.25f, 1600);

            W = new Spell(SpellSlot.W, 600);
            W.SetSkillshot(.5f, 200, 2000, false, SkillshotType.SkillshotCircle);

            E = new Spell(SpellSlot.E, 970);
            E.SetSkillshot(.366f, 70, 1600, true, SkillshotType.SkillshotLine);

            Ignite = Player.Spellbook.GetSpell(Player.GetSpellSlot("summonerdot"));
        }

        public override void Game_OnGameUpdate(EventArgs args)
        {
            KSIgnite();
            CloneLogic();

            if (Orbwalker.ActiveMode.ToString() == "Combo")
            {
                Combo();
            }
            else if (Orbwalker.ActiveMode.ToString() == "Mixed")
            {
                Harass();
            }
        }

        private static SpellSlot UltType()
        {
            if (Player.Spellbook.GetSpell(SpellSlot.R).Name == null)
            {
                return SpellSlot.R;
            }
            switch (Player.Spellbook.GetSpell(SpellSlot.R).Name)
            {
                case "LeblancChaosOrbM":
                    return SpellSlot.Q;
                case "LeblancSlideM":
                    return SpellSlot.W;
                case "LeblancSoulShackleM":
                    return SpellSlot.E;
                default:
                    return SpellSlot.R;
            }
        }

        private static int Wstates()
        {
            if (Player.Spellbook.GetSpell(SpellSlot.W).ToggleState == 1)
                return 1;
            else
                return 2;
        }

        private static int WRstates()
        {
            if (Player.Spellbook.GetSpell(SpellSlot.R).ToggleState == 1)
                return 1;
            else
                return 2;
        }

        private static PredictionOutput CanHit(Spell spell, Obj_AI_Base target, HitChance hitchance = HitChance.High)
        {
            PredictionOutput output;
            output = spell.GetPrediction(target);
            if (output.Hitchance >= hitchance)
            {
                if (spell == E)
                {
                    if (E.IsInRange(target, 800))
                    {
                        return output;
                    }
                    else
                    {
                        return null;
                    }
                }
                else
                {
                    return output;
                }
            }
            else
                return null;
        }
        private void Combo()
        {
            Target = TargetSelector.GetTarget(800f, TargetSelector.DamageType.Magical);
            if (Target == null)
            {
                /*Target = TargetSelector.GetTarget(1900f, TargetSelector.DamageType.Magical);
                double QEdamage = (60 + 50 * Q.Level + Player.FlatMagicDamageMod * 0.8) + (15 + 25 * E.Level + Player.FlatMagicDamageMod * 0.5);  //Q+E damage without stun
                if ((ObjectManager.Get<Obj_AI_Hero>().LongCount(hero => ((hero.Team == Target.Team) && (Target != hero) && (hero.Position.Distance(Target.Position) <= 1500))) == 0) && (Player.HealthPercentage() >= 30f))
                {
                    QEdamage = QEdamage + (15 + 25 * E.Level + Player.FlatMagicDamageMod * 0.5);
                }

                //[TODO]: 적의 위치 근처의 적 타워, 적 명수에 따라 E추가데미지도 계산+주도록 추가할것
                if ((Target == null) || (Player.CalcDamage(Target, Damage.DamageType.Magical, QEdamage) <= Target.Health))
                {*/               
                return;
                //}
            }

            if (Player.Spellbook.GetSpell(SpellSlot.R).IsReady())
            {
                SpellSlot ulttype = UltType(); //[Todo] 데미지계산을 통해 WQR할지 WRQ할지 정하기
                if (ulttype == SpellSlot.Q && !Q.IsReady() && Q.IsInRange(Target, (int)GetRealQRange(Target)))
                {
                    PredictionOutput canhit;
                    if (W.IsReady())
                    {
                        W.Delay = W.Delay + Q.Delay;
                        canhit = CanHit(W, Target);
                        W.Delay = W.Delay - Q.Delay;
                        if (canhit != null)
                        {
                            if (Player.Spellbook.CastSpell(SpellSlot.R, Target))                            
                                return;
                        }
                    }
                    else
                    {
                        if (Player.Spellbook.CastSpell(SpellSlot.R, Target))
                            return;
                    }

                }
                else if (ulttype == SpellSlot.W && WRstates() == 1 && !(W.IsReady() && Wstates() == 1) && Q.IsReady())
                {                      
                    E.Delay = E.Delay + Q.Delay * 2;
                    PredictionOutput canhit = CanHit(E, Target);                      
                    E.Delay = E.Delay - Q.Delay * 2;
                    if (Prediction.GetPrediction(new PredictionInput { Unit = Target, Delay = 0.25f }).UnitPosition.Distance(Player.Position) >= (float)Q.Range || canhit == null)
                    {
                        canhit = CanHit(W, Target);
                        if (canhit != null)
                        {                                               
                            Player.Spellbook.CastSpell(SpellSlot.R, canhit.CastPosition);                                                        
                            return;
                        }
                        else
                        {                
                            //[TODO] 피계산 할까 말까
                            Player.Spellbook.CastSpell(SpellSlot.R, Player.Position.To2D().Extend(Target.Position.To2D(), W.Range).To3D());                  
                            return;
                        }
                    }
                }
                else if (ulttype == SpellSlot.W && WRstates() == 1 && !(W.IsReady() && Wstates() == 1))
                {
                    PredictionOutput canhit = CanHit(W, Target);
                    if (canhit != null)
                    {
                        Player.Spellbook.CastSpell(SpellSlot.R, canhit.CastPosition);
                        return;
                    }
                    else
                    {
                        //[TODO] 피계산 할까 말까
                        Player.Spellbook.CastSpell(SpellSlot.R, Player.Position.To2D().Extend(Target.Position.To2D(), W.Range).To3D());
                        return;
                    }
                }
                else if (ulttype == SpellSlot.E && !Q.IsReady() && !(W.IsReady() && Wstates() == 1)) //이부분 추후 Q가 Ready상태라도 복사E 쓰는상황 판단하게 개선하기
                {
                    PredictionOutput canhitoutput = CanHit(E, Target);
                    if (canhitoutput != null)
                    {
                        Player.Spellbook.CastSpell(SpellSlot.R, canhitoutput.CastPosition);
                        return;
                    }
                }
            }

            if (Q.IsReady() && Q.IsInRange(Target, (int)GetRealQRange(Target)))
            {
                if (W.IsReady() && Wstates() == 1)
                {                    
                    if (Player.Spellbook.CastSpell(SpellSlot.Q, Target))                  
                        return;
                }
                else
                {
                    if (Player.Spellbook.GetSpell(SpellSlot.R).IsReady() && UltType() == SpellSlot.W)
                    {
                            
                        E.Delay = E.Delay + Q.Delay * 2;
                        PredictionOutput canhit = CanHit(E, Target);            
                        E.Delay = E.Delay - Q.Delay * 2;
                        if (Prediction.GetPrediction(new PredictionInput { Unit = Target, Delay = 0.25f }).UnitPosition.Distance(Player.Position) < (float)Q.Range && canhit != null)
                        {
                            if (Player.Spellbook.CastSpell(SpellSlot.Q, Target))
                                return;
                        }
                    }
                    else
                    {
                        if (Player.Spellbook.CastSpell(SpellSlot.Q, Target))
                            return;
                    }
                }
            }

            if (W.IsReady() && Wstates() == 1)
            {
                PredictionOutput canhitoutput = CanHit(W, Target);
                if (canhitoutput == null)
                {
                    W.Cast(Player.Position.To2D().Extend(Target.Position.To2D(), W.Range).To3D());                                        
                    return;
                }
                else
                {
                    if (W.Cast(Target) == Spell.CastStates.SuccessfullyCasted)
                        return;
                }
            }

            if (E.IsReady() && !(Player.Spellbook.GetSpell(SpellSlot.R).IsReady() && (UltType() == SpellSlot.W || (UltType() == SpellSlot.Q && Q.IsInRange(Target, (int)GetRealQRange(Target))))))
            {
                /*PredictionOutput canhitoutput = CanHit(E, Target);
                if (canhitoutput != null)
                {
                    E.Cast(canhitoutput.CastPosition);
                    return;
                }*/
                if (E.IsInRange(Target, 800))
                {
                    if (E.Cast(Target) == Spell.CastStates.SuccessfullyCasted)                  
                        return;
                }
                    
            }
        }
        
        private void Harass()
        {
            Target = TargetSelector.GetTarget(800f, TargetSelector.DamageType.Magical);
            if (Target == null) return;

            if (Q.IsReady() && Q.IsInRange(Target, (int)GetRealQRange(Target)))
            {
                if (Player.Spellbook.CastSpell(SpellSlot.Q, Target))             
                    return;
            }
            //PredictionOutput output = CanHit(E, Target);
            //if (QLastCastedTime + Q.Delay * 1000 + (Player.Position.To2D().Distance(Target.Position.To2D()) / Q.Speed) < Environment.TickCount + E.Delay * 1000 + (Player.Position.To2D().Distance(Target.Position.To2D()) / E.Speed) && (RQLastCastedTime + Q.Delay * 1000 + (Player.Position.To2D().Distance(Target.Position.To2D()) / Q.Speed) < Environment.TickCount + E.Delay * 1000 + (Player.Position.To2D().Distance(Target.Position.To2D()) / E.Speed) && (output != null))
            if (E.IsReady() && QLastCastedTime + Q.Delay * 1000 + (Player.Position.To2D().Distance(Target.Position.To2D()) / Q.Speed) < Environment.TickCount + E.Delay * 1000 + (Player.Position.To2D().Distance(Target.Position.To2D()) / E.Speed) && RQLastCastedTime + Q.Delay * 1000 + (Player.Position.To2D().Distance(Target.Position.To2D()) / Q.Speed) < Environment.TickCount + E.Delay * 1000 + (Player.Position.To2D().Distance(Target.Position.To2D()) / E.Speed))
            {
                //E.Cast(output.CastPosition);
                if (E.IsInRange(Target, 800))
                {
                    if (E.Cast(Target) == Spell.CastStates.SuccessfullyCasted)                   
                        return;
                }

            }
            PredictionOutput woutput = CanHit(W, Target);
            if (W.IsReady() && Wstates() == 1 && QLastCastedTime + Q.Delay * 1000 + (Player.Position.To2D().Distance(Target.Position.To2D()) / Q.Speed) < Environment.TickCount + W.Delay * 1000 + (Player.Position.To2D().Distance(Target.Position.To2D()) / W.Speed) && RQLastCastedTime + Q.Delay * 1000 + (Player.Position.To2D().Distance(Target.Position.To2D()) / Q.Speed) < Environment.TickCount + W.Delay * 1000 + (Player.Position.To2D().Distance(Target.Position.To2D()) / W.Speed) && woutput != null)
            {
                if (Q.IsReady())
                {
                    if (E.IsReady())
                    {
                        E.From = woutput.CastPosition;
                        E.Delay = E.Delay + W.Delay + Player.Position.Distance(woutput.CastPosition) / W.Speed + Q.Delay + (woutput.CastPosition.Distance(Target.Position) / Q.Speed);
                        PredictionOutput canhit = CanHit(E, Target);
                        E.From = new Vector3();
                        E.Delay = E.Delay + W.Delay + Player.Position.Distance(woutput.CastPosition) / W.Speed + Q.Delay + (woutput.CastPosition.Distance(Target.Position) / Q.Speed);
                        if (canhit != null)
                        {
                            if (W.Cast(Target) == Spell.CastStates.SuccessfullyCasted)                   
                                return;
                        }
                    }
                }
                else
                {
                    if (W.Cast(Target) == Spell.CastStates.SuccessfullyCasted)
                        return;
                }
            }

            if (W.IsReady() && Wstates() == 2 && !(Q.IsReady() && Q.IsInRange(Target, (int)GetRealQRange(Target))))
            {     
                if (CanHit(E, Target) == null)
                    Player.Spellbook.CastSpell(SpellSlot.W);
            }

        }

        public override void Spellbook_OnCastSpell(Spellbook spellbook, SpellbookCastSpellEventArgs args)
        {
            if (spellbook.Owner.IsMe)
            {
                if (args.Slot == SpellSlot.Q)
                {
                    QLastCastedTime = Environment.TickCount;
                }
                /*else if (args.Slot == SpellSlot.W)
                {
                    WLastCastedTime = Environment.TickCount;
                }
                else if (args.Slot == SpellSlot.E)
                {
                    ELastCastedTime = Environment.TickCount;
                }*/
                else if (args.Slot == SpellSlot.R)
                {
                    SpellSlot Ult = UltType();
                    if (Ult == SpellSlot.Q)
                    {
                        RQLastCastedTime = Environment.TickCount;
                    }
                    /*else if (Ult == SpellSlot.W)
                    {
                        RWLastCastedTime = Environment.TickCount;
                    }
                    else if (Ult == SpellSlot.E)
                    {
                        RELastCastedTime = Environment.TickCount;
                    }*/
                }
            }
        }

        public static float GetRealQRange(AttackableUnit target)
        {
            var result = 700 + Player.BoundingRadius;
            if (target.IsValidTarget())
            {
                return result + target.BoundingRadius;
            }
            return result;
        }

        private static void CloneLogic()
        {
            if (Clone == null || !Clone.IsValid)
            {
                return;
            }

            var pos = Player.ServerPosition;
            if (Player.GetWaypoints().Count > 1)
            {
                pos = Player.GetWaypoints()[1].To3D();
            }
            Utility.DelayAction.Add(100, () => { Clone.IssueOrder(GameObjectOrder.MovePet, pos); });

        }

        private static void KSIgnite()
        {
            var unit =
                ObjectManager.Get<Obj_AI_Hero>()
                    .FirstOrDefault(
                        obj =>
                            obj.IsValidTarget(600) &&
                            obj.Health < Player.GetSummonerSpellDamage(obj, Damage.SummonerSpell.Ignite));
            if (unit != null && unit.IsValid)
            {
                Player.Spellbook.CastSpell(Ignite.Slot, unit);
            }
        }

        public override void GameObject_OnCreate(GameObject sender, EventArgs args)
        {
            if (sender.IsValid && sender.Name.Equals(Player.Name))
            {
                Clone = sender as Obj_AI_Base;
            }
        }
    }
}
