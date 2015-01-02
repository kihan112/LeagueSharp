using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeagueSharp;
using LeagueSharp.Common;
using Microsoft.Win32.SafeHandles;
using SharpDX;

namespace Himeragi_Series
{
    class Program
    {

        static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }
        private static void Game_OnGameLoad(EventArgs args)
        {
            Loader loader = new Loader(true);
        }


    }

    class Loader
    {
        public static Menu Config;
        public static Orbwalking.Orbwalker Orbwalker;
        public static Obj_AI_Hero Player;

        public Loader()
        {
            Game.OnGameUpdate += Game_OnGameUpdate;
            Drawing.OnDraw += Drawing_OnDraw;
            Interrupter.OnPossibleToInterrupt += Interrupter_OnPosibleToInterrupt;
            AntiGapcloser.OnEnemyGapcloser += AntiGapcloser_OnEnemyGapcloser;
            GameObject.OnCreate += GameObject_OnCreate;
            Obj_AI_Base.OnProcessSpellCast += Obj_AI_Base_OnProcessSpellCast;
            Game.OnGameSendPacket += Game_OnSendPacket;
            Game.OnGameProcessPacket += Game_OnGameProcessPacket;
            GameObject.OnDelete += GameObject_OnDelete;
            Obj_AI_Base.OnIssueOrder += ObjAiHeroOnOnIssueOrder;
            Spellbook.OnCastSpell += Spellbook_OnCastSpell;
        }

        public Loader(bool load)
        {
            if (load)
                OnGameLoad();
        }

        public void OnGameLoad()
        {
            Player = ObjectManager.Player;
            Game.PrintChat("Himeragi Series by Himeragi Yukina");
            Game.PrintChat("Feel free to donate via Paypal to: <font color = \"#87CEEC\">kihan112@naver.com</font>");

            Config = new Menu("Himeragi Series : " + Player.ChampionName, "HimeragiSeries." + Player.ChampionName, true);
            Config.AddSubMenu(new Menu("Info", "Info"));
            Config.SubMenu("Info").AddItem(new MenuItem("Author", "By Himeragi Yukina"));
            Config.SubMenu("Info").AddItem(new MenuItem("Paypal", "Donate: kihan112@naver.com"));

            var TSMenu = new Menu("Target Selector", "Target Selector");
            TargetSelector.AddToMenu(TSMenu);
            Config.AddSubMenu(TSMenu);

            Config.AddSubMenu(new Menu("Orbwalking", "Orbwalking"));
            Orbwalker = new Orbwalking.Orbwalker(Config.SubMenu("Orbwalking"));

            Config.AddToMainMenu();

            try
            {
                if (Activator.CreateInstance(null, "Himeragi_Series.Champions." + Player.ChampionName) != null)
                {
                    Game.PrintChat("Himeragi Yukina's " + Player.ChampionName + " Loaded!</font>");
                }
            }
            catch
            {
                Game.PrintChat("Himeragi Series : {0} Not Support !", Player.ChampionName);
            }
        }

        public virtual void Spellbook_OnCastSpell(GameObject unit, SpellbookCastSpellEventArgs args) { }

        public virtual void Drawing_OnDraw(EventArgs args) {}

        public virtual void AntiGapcloser_OnEnemyGapcloser(ActiveGapcloser gapcloser) {}

        public virtual void Interrupter_OnPosibleToInterrupt(Obj_AI_Base unit, InterruptableSpell spell) {}

        public virtual void Game_OnGameUpdate(EventArgs args) {}

        public virtual void GameObject_OnCreate(GameObject sender, EventArgs args) {}

        public virtual void GameObject_OnDelete(GameObject sender, EventArgs args) {}

        public virtual void Obj_AI_Base_OnProcessSpellCast(Obj_AI_Base unit, GameObjectProcessSpellCastEventArgs args) {}

        public virtual void Game_OnSendPacket(GamePacketEventArgs args) {}

        public virtual void Game_OnGameProcessPacket(GamePacketEventArgs args) {}

        public virtual void ObjAiHeroOnOnIssueOrder(Obj_AI_Base sender, GameObjectIssueOrderEventArgs args) {}
    }
}
