using System;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
using Color = System.Drawing.Color;



namespace VodkaSona
{
    class Program
    {
        private static String championName = "Sona";
        public static Obj_AI_Hero Player;
        private static Menu _menu;
        private static Orbwalking.Orbwalker _orbwalker;
        private static Spell Q, W, E, R;
        static Items.Item HealthPot;
        static Items.Item ManaPot;
        static SpellSlot IgniteSlot;
        

        private static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += Game_OnLoad;
        }

        static void Game_OnLoad(EventArgs args)
        {
            {
                Player = ObjectManager.Player;
                if (Player.ChampionName != "Sona") return;

                #region Spells
                Q = new Spell(SpellSlot.Q, 850, TargetSelector.DamageType.Magical);
                W = new Spell(SpellSlot.W, 1000);
                E = new Spell(SpellSlot.E, 350);
                R = new Spell(SpellSlot.R, 1000, TargetSelector.DamageType.Magical);

                R.SetSkillshot(0.5f, 125, 3000f, false, SkillshotType.SkillshotLine);
                #endregion

                #region Items
                IgniteSlot = Player.GetSpellSlot("summonerdot");
                HealthPot = new Items.Item(2003, 0);
                ManaPot = new Items.Item(2004, 0);
                #endregion
            }
            #region Menu

            _menu = new Menu("Vodka Sona", "vodka.sona", true);
            var orbwalkerMenu = new Menu(("Orbwalker"), "vodka.sona.orbwalker");
            _orbwalker = new Orbwalking.Orbwalker(orbwalkerMenu);

            _menu.AddSubMenu(orbwalkerMenu);

            TargetSelector.AddToMenu(_menu);

            var comboMenu = new Menu("Combo", "vodka.sona.combo");
            {
                comboMenu.AddItem(new MenuItem("vodka.sona.combo.useq", "Use Q").SetValue(true));
                comboMenu.AddItem(new MenuItem("vodka.sona.combo.usew", "Use W").SetValue(true));
                comboMenu.AddItem(new MenuItem("vodka.sona.combo.usee", "Use E").SetValue(true));
                comboMenu.AddItem(new MenuItem("vodka.sona.combo.user", "Use R").SetValue(true));
            }
            _menu.AddSubMenu(comboMenu);

            var harrassMenu = new Menu("Harass", "vodka.sona.harrass");
            {
                harrassMenu.AddItem(new MenuItem("vodka.sona.harrassuseq", "Use Q").SetValue(true));
                harrassMenu.AddItem(new MenuItem("vodka.sona.harrassusew", "Use W").SetValue(true));
                harrassMenu.AddItem(new MenuItem("vodka.sona.harrassusee", "Use E").SetValue(true));

            }
            _menu.AddSubMenu(harrassMenu);

            var fleeMenu = new Menu("Flee", "vodka.sona.flee");
            {
                fleeMenu.AddItem(new MenuItem("vodka.sona.combo.usew", "Use W").SetValue(true));
                fleeMenu.AddItem(new MenuItem("vodka.sona.combo.usee", "Use E").SetValue(true));

            }
            _menu.AddSubMenu(fleeMenu);

            var drawingMenu = new Menu("Drawing", "vodka.sona.drawing");
            drawingMenu.AddItem(new MenuItem("DrawQ", "Draw Q range").SetValue(new Circle(true, Color.Aqua, Q.Range)));
            drawingMenu.AddItem(new MenuItem("DrawW", "Draw W range").SetValue(new Circle(true, Color.SpringGreen, W.Range)));
            drawingMenu.AddItem(new MenuItem("DrawE", "Draw E range").SetValue(new Circle(true, Color.SlateBlue, E.Range)));
            drawingMenu.AddItem(new MenuItem("DrawR", "Draw R range").SetValue(new Circle(true, Color.Red, R.Range)));
            _menu.AddToMainMenu();

            #endregion

            Interrupter2. += Interrupter_OnPossibleToInterrupt;
            Drawing.OnDraw += Drawing_OnDraw;
            Game.OnUpdate += Game_OnUpdate;
            ShowNotification("Vodka Sona - Loaded", 3000);

        }

        static void Interrupter_OnPossibleToInterrupt(Obj_AI_Base unit, InterruptableSpell spell)
        {
            if (!unit.IsValid || unit.IsDead || !unit.IsTargetable || unit.IsStunned) return;
            if (R.IsReady() && R.IsInRange(unit.Position) && spell.DangerLevel >= InterruptableDangerLevel.High)
            {
                R.Cast(unit.Position, true);
                return;
            }
            else
            {
                if (!_menu.Item("exhaust").GetValue<bool>()) return;
                if (unit.Distance(Player.Position) > 600) return;
                if (Player.GetSpellSlot("SummonerExhaust") != SpellSlot.Unknown && Player.Spellbook.CanUseSpell(Player.GetSpellSlot("SummonerExhaust")) == SpellState.Ready)
                    Player.Spellbook.CastSpell(Player.GetSpellSlot("SummonerExhaust"), unit);
                if ((W.IsReady() && GetPassiveCount() == 2) || (Utility.HasBuff(Player, "sonapassiveattack") && Player.LastCastedSpellName() == "SonaW") || (Utility.HasBuff(Player, "sonapassiveattack") && W.IsReady()))
                {
                    if (W.IsReady()) W.Cast();
                    Player.IssueOrder(GameObjectOrder.AttackUnit, unit);
                }
            }
        }

        static int GetPassiveCount()
        {
            foreach (BuffInstance buff in Player.Buffs)
                if (buff.Name == "sonapassivecount") return buff.Count;
            return 0;
        }

        public static void ShowNotification(string message, int duration = -1, bool dispose = true)
        {
            Notifications.AddNotification(new Notification(message, duration, dispose));
        }

        private static void Drawing_OnDraw(EventArgs args)
        {
            var menuItem1 = _menu.Item("DrawQ").GetValue<Circle>();
            var menuItem2 = _menu.Item("DrawE").GetValue<Circle>();
            var menuItem3 = _menu.Item("DrawW").GetValue<Circle>();
            var menuItem4 = _menu.Item("DrawR").GetValue<Circle>();

            if (menuItem1.Active && Q.IsReady()) Render.Circle.DrawCircle(Player.Position, Q.Range, Color.SpringGreen);
            if (menuItem2.Active && E.IsReady()) Render.Circle.DrawCircle(Player.Position, E.Range, Color.Crimson);
            if (menuItem3.Active && W.IsReady()) Render.Circle.DrawCircle(Player.Position, W.Range, Color.Aqua);
            if (menuItem4.Active && R.IsReady()) Render.Circle.DrawCircle(Player.Position, R.Range, Color.Firebrick);
        }

        private static void Game_OnUpdate(EventArgs args)
        {
            switch (_orbwalker.ActiveMode)
            {
                case Orbwalking.OrbwalkingMode.Combo:
                    Combo();
                    break;
                case Orbwalking.OrbwalkingMode.Mixed:
                    Laneclear();
                    break;
            }
        }

        private static void Laneclear()
        {
            if (E.IsReady() && _menu.Item("vodka.sona.combo.usee").GetValue<bool>())
            {
                
            }
        }

        private static void Combo()
        {
            bool vQ = Q.IsReady() && _menu.Item("vodka.sona.combo.useq").GetValue<bool>();
            bool vW = W.IsReady() && _menu.Item("vodka.sona.combo.useW").GetValue<bool>();
            bool vE = E.IsReady() && _menu.Item("vodka.sona.combo.usee").GetValue<bool>();
            bool vR = R.IsReady() && _menu.Item("vodka.sona.combo.user").GetValue<bool>();

            Obj_AI_Hero tsQ = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Magical);
            Obj_AI_Hero tsR = TargetSelector.GetTarget(R.Range, TargetSelector.DamageType.Magical);

            if (vE)
            {
                UseESmart(TargetSelector.GetTarget(1700, TargetSelector.DamageType.Magical));
            }

            if (Q.IsReady() && _menu.Item("vodka.sona.combo.useq").GetValue<bool>() && Vector3.Distance(Player.Position, tsQ.Position) < Q.Range)
            {
                    Q.Cast();
            }

            if (vW)
            {
                UseWSmart(_menu.Item("healC").GetValue<Slider>().Value, _menu.Item("healN").GetValue<Slider>().Value);
            }

            if (vR && AlliesInRange(W.Range) >= 1 && tsQ.)
            {
                
            }
        }

        //Ty DEKTUS, copypasted as fuck :P
        public static void UseESmart(Obj_AI_Base target)
        {
            try
            {

                if (target.Path.Length == 0 || !target.IsMoving)
                    return;
                Vector2 nextEnemPath = target.Path[0].To2D();
                var dist = Player.Position.To2D().Distance(target.Position.To2D());
                var distToNext = nextEnemPath.Distance(Player.Position.To2D());
                if (distToNext <= dist)
                    return;
                var msDif = Player.MoveSpeed - target.MoveSpeed;
                if (msDif <= 0 && !Orbwalking.InAutoAttackRange(target))
                    E.Cast();

                var reachIn = dist/msDif;
                if (reachIn > 3)
                    E.Cast();
            }
            catch
            {
                
            }

        }

        static void UseWSmart(int percent, int count)
        {
            Obj_AI_Hero ally = MostWoundedAllyInRange(W.Range);
            double wHeal = (10 + 20 * W.Level + .2 * Player.FlatMagicDamageMod) * (1 + (Player.Health / Player.MaxHealth) / 2);
            int allies = AlliesInRange(W.Range);

            if (allies >= count && (ally.Health / ally.MaxHealth) * 100 <= percent)
                W.Cast();
            if (allies < 2 && _menu.Item("vodka.sona.combo.usew").GetValue<bool>())
                if (_menu.Item("vodka.sona.combo.usew").GetValue<bool>() && Player.MaxHealth - Player.Health > wHeal)
                    W.Cast();
                else if ((Player.Health / Player.MaxHealth) * 100 <= percent) W.Cast(); ;
        }

        static Obj_AI_Hero MostWoundedAllyInRange(float range)
        {
            float lastHealth = 9000f;
            Obj_AI_Hero temp = new Obj_AI_Hero();
            foreach (Obj_AI_Hero hero in ObjectManager.Get<Obj_AI_Hero>())
                if (hero.IsAlly && !hero.IsMe && !hero.IsDead && Vector3.Distance(Player.Position, hero.Position) <= range && hero.Health < lastHealth)
                {
                    lastHealth = hero.Health;
                    temp = hero;
                }
            return temp;
        }

        static int AlliesInRange(float range)
        {
            int count = 0;
            foreach (Obj_AI_Hero hero in ObjectManager.Get<Obj_AI_Hero>())
                if (hero.IsAlly && !hero.IsMe && Vector3.Distance(Player.Position, hero.Position) <= range) count++;
            return count;
        }
    }
}