using System.Collections.Generic;
using UnityEngine;

namespace PocketReef
{
    // Self-starting prototype: no scene wiring or imported art required.
    public sealed class AquariumGame : MonoBehaviour
    {
        const float Width = 360, Height = 640;
        const int FishPrice = 20, HunterPrice = 70, SharkPrice = 200, MedicinePrice = 15;
        readonly Rect tank = new Rect(16, 148, 328, 410);
        readonly List<Fish> fishes = new List<Fish>();
        readonly List<Drop> food = new List<Drop>();
        readonly List<Drop> coins = new List<Drop>();
        readonly List<Corpse> corpses = new List<Corpse>();
        readonly List<Missile> missiles = new List<Missile>();
        readonly int[] goals = { 100, 1000, 3000, 10000 };
        Texture2D gold, hunter, shark, sick, coinSheet, pixel;
        GUIStyle small, body, title, button, tag;
        int money, level, selectedTool, feedCapacity, feedUpgradePrice;
        float epidemicTimer, messageTime, elapsed;
        bool paused, won, lost, help, medicineReady;
        float raidTimer;
        Raider raider;
        string message;
        float scale;
        Vector2 offset;

        sealed class Fish
        {
            public Vector2 position, target;
            public bool hunter;
            public int tier, meals;
            public float hunger = 70, disease, coinTimer = 8, huntTimer = 15, immunity;
            public bool Adult => !hunter && meals >= 3;
        }
        sealed class Drop { public Vector2 position; public float life; public int value; public bool medicine; }
        sealed class Corpse { public Vector2 position; public float life, size; public int tier; }
        sealed class Missile { public Vector2 position, target; public float life = 3.5f; }
        sealed class Raider { public Vector2 position; public bool right; public float fireTimer; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (FindAnyObjectByType<AquariumGame>() == null)
                new GameObject("Pocket Reef Game").AddComponent<AquariumGame>();
        }

        void Awake()
        {
            Screen.orientation = ScreenOrientation.Portrait;
            Application.targetFrameRate = 60;
            pixel = Texture2D.whiteTexture;
            gold = MakeFish(new Color32(255, 187, 59, 255), false);
            hunter = MakeFish(new Color32(179, 154, 242, 255), true);
            shark = MakeFish(new Color32(86, 163, 199, 255), true);
            sick = MakeFish(new Color32(127, 192, 92, 255), false);
            coinSheet = Resources.Load<Texture2D>("coin-animation");
            if (coinSheet != null) coinSheet.filterMode = FilterMode.Point;
            Restart();
        }

        void Restart()
        {
            fishes.Clear(); food.Clear(); coins.Clear(); corpses.Clear(); missiles.Clear();
            money = 40; level = 0; selectedTool = 0; elapsed = 0;
            paused = won = lost = help = false; epidemicTimer = 45;
            feedCapacity = 1; medicineReady = false; feedUpgradePrice = 10;
            raider = null; raidTimer = Random.Range(18f, 30f);
            AddFish(0); AddFish(0);
            Say("Welcome! Tap the water to feed your fish.");
        }
        void AddFish(int tier)
        {
            fishes.Add(new Fish { position = Point(), target = Point(), hunter = tier > 0, tier = tier });
        }
        Vector2 Point() => new Vector2(Random.Range(42, 318), Random.Range(182, 510));
        void Say(string text) { message = text; messageTime = 5; }
        bool Spend(int price)
        {
            if (money < price) { Say("Not enough money. Collect more coins!"); return false; }
            money -= price; return true;
        }

        void Update()
        {
            if (paused || won || lost || help) return;
            float dt = Mathf.Min(Time.deltaTime, .1f);
            elapsed += dt; messageTime -= dt; epidemicTimer -= dt;
            if (epidemicTimer <= 0)
            {
                var healthy = fishes.FindAll(f => f.disease == 0 && f.immunity <= 0);
                if (healthy.Count > 0)
                {
                    healthy[Random.Range(0, healthy.Count)].disease = .01f;
                    Say("EPIDEMIC! Load medicine and drop it for a sick fish.");
                }
                epidemicTimer = Random.Range(40f, 58f);
            }
            for (int i = food.Count - 1; i >= 0; i--)
            {
                food[i].position.y += dt * 27; food[i].life -= dt;
                if (food[i].life <= 0 || food[i].position.y > 536) food.RemoveAt(i);
            }
            for (int i = coins.Count - 1; i >= 0; i--)
            {
                coins[i].position.y = Mathf.Min(536, coins[i].position.y + dt * 18);
                coins[i].life -= dt;
                if (coins[i].life <= 0) coins.RemoveAt(i);
            }
            for (int i = 0; i < corpses.Count; i++)
            {
                var c = corpses[i];
                c.position.y = Mathf.Min(524, c.position.y + dt * 10);
                c.position.x = Mathf.Clamp(c.position.x + Mathf.Sin(elapsed * 1.3f + i) * dt * 3, 40, 320);
                c.life -= dt;
                if (c.life <= 0) corpses.RemoveAt(i--);
            }
            raidTimer -= dt;
            if (raider == null && raidTimer <= 0 && fishes.Count > 0)
            {
                bool fromLeft = Random.value < .5f;
                raider = new Raider
                {
                    position = new Vector2(fromLeft ? -30 : 390, 517),
                    right = fromLeft,
                    fireTimer = 2.5f
                };
                Say("A tank is attacking! Tap missiles to shoot them down.");
            }
            if (raider != null)
            {
                raider.position.x += dt * 38 * (raider.right ? 1 : -1);
                raider.fireTimer -= dt;
                if (raider.fireTimer <= 0 && fishes.Count > 0)
                {
                    raider.fireTimer = Random.Range(3.2f, 5f);
                    var target = fishes[Random.Range(0, fishes.Count)];
                    missiles.Add(new Missile
                    {
                        position = new Vector2(raider.position.x, raider.position.y - 16),
                        target = target.position
                    });
                }
                if (raider.position.x < -45 || raider.position.x > 405)
                {
                    raider = null;
                    raidTimer = Random.Range(20f, 34f);
                }
            }
            for (int i = missiles.Count - 1; i >= 0; i--)
            {
                var m = missiles[i];
                m.position = Vector2.MoveTowards(m.position, m.target, dt * 90);
                m.life -= dt;
                Fish hit = null;
                foreach (var f in fishes)
                    if (Vector2.Distance(f.position, m.position) < 14) { hit = f; break; }
                if (hit != null)
                {
                    Kill(hit); missiles.RemoveAt(i);
                    Say("A missile hit a fish! Tap missiles to stop them.");
                    continue;
                }
                if (m.life <= 0) missiles.RemoveAt(i);
            }
            var snapshot = fishes.ToArray();
            foreach (var f in snapshot)
            {
                if (!fishes.Contains(f)) continue;
                f.hunger -= dt * (f.tier == 2 ? .4f : f.tier == 1 ? .55f : .8f);
                f.immunity -= dt;
                if (f.disease > 0)
                {
                    f.disease += dt;
                    // Transmission is a probability per second, independent of frame rate.
                    foreach (var other in fishes)
                        if (other != f && other.disease == 0 && other.immunity <= 0 &&
                            Vector2.Distance(f.position, other.position) < 48 && Random.value < 1 - Mathf.Exp(-dt * .12f))
                            other.disease = .01f;
                }
                if (f.disease >= 30 || f.hunger <= 0)
                {
                    Kill(f); Say(f.disease >= 30 ? "A fish died from disease." : "A fish starved. Keep feeding!"); continue;
                }
                Vector2 destination = f.target;
                if (!f.hunter && food.Count > 0)
                {
                    // Full fish ignore ordinary pellets, but medicine always looks appetising.
                    Drop nearest = null;
                    foreach (var pellet in food)
                        if ((f.hunger < 75 || pellet.medicine) &&
                            (nearest == null || (pellet.position - f.position).sqrMagnitude < (nearest.position - f.position).sqrMagnitude))
                            nearest = pellet;
                    if (nearest != null)
                    {
                        destination = nearest.position;
                        if (Vector2.Distance(f.position, destination) < 13)
                        {
                            food.Remove(nearest);
                            if (nearest.medicine)
                            {
                                if (f.disease > 0)
                                {
                                    f.disease = 0; f.immunity = 20;
                                    Say("The sick fish ate the medicine and recovered!");
                                }
                                else
                                {
                                    Kill(f); Say("A healthy fish ate the medicine and died!"); continue;
                                }
                            }
                            else
                            {
                                f.hunger = Mathf.Min(100, f.hunger + 28); f.meals++;
                                if (f.meals == 3) Say("Your goldfish reached level 2. It now makes coins!");
                            }
                        }
                    }
                }
                if (f.hunter)
                {
                    f.huntTimer -= dt;
                    Fish prey = null;
                    foreach (var candidate in fishes)
                        if (candidate.Adult && (prey == null || Vector2.Distance(f.position, candidate.position) < Vector2.Distance(f.position, prey.position))) prey = candidate;
                    if (prey != null && (f.huntTimer <= 0 || f.hunger < 45))
                    {
                        destination = prey.position;
                        if (Vector2.Distance(f.position, prey.position) < 15)
                        {
                            fishes.Remove(prey); f.hunger = 100; f.huntTimer = f.tier == 2 ? 20 : 24;
                            Say("Hunter ate a level 2 fish. Keep your nursery stocked.");
                        }
                    }
                }
                f.position = Vector2.MoveTowards(f.position, destination, dt * (f.disease > 0 ? 13 : f.tier == 2 ? 40 : f.hunter ? 36 : 25));
                if (Vector2.Distance(f.position, f.target) < 5) f.target = Point();
                if ((f.Adult || f.hunter) && f.disease == 0)
                {
                    f.coinTimer -= dt;
                    if (f.coinTimer <= 0)
                    {
                        coins.Add(new Drop { position = f.position, life = 22, value = f.tier == 2 ? 40 : f.tier == 1 ? 15 : 5 });
                        f.coinTimer = 8;
                    }
                }
            }
            if (money >= goals[level]) won = true;
            if (fishes.Count == 0 && money < FishPrice && coins.Count == 0) lost = true;
        }

        void TankTap(Vector2 position)
        {
            if (paused || won || lost || help) return;
            for (int i = missiles.Count - 1; i >= 0; i--)
                if (Vector2.Distance(position, missiles[i].position) < 18)
                { missiles.RemoveAt(i); Say("Missile destroyed."); return; }
            for (int i = coins.Count - 1; i >= 0; i--)
                if (Vector2.Distance(position, coins[i].position) < 23)
                { money += coins[i].value; coins.RemoveAt(i); return; }
            if (selectedTool == 2)
            {
                Fish closest = null;
                foreach (var f in fishes)
                    if (Vector2.Distance(position, f.position) < 30 && (closest == null || Vector2.Distance(position, f.position) < Vector2.Distance(position, closest.position))) closest = f;
                if (closest == null) { Say("Tap a sick fish to remove it."); return; }
                if (closest.disease == 0) { Say("That fish is healthy."); return; }
                fishes.Remove(closest); Say("Sick fish removed to stop the spread.");
                return;
            }
            if (medicineReady) { DropMedicine(position); return; }
            if (food.Count >= feedCapacity) { Say("The tank is full. The FEED button adds more room."); return; }
            if (Spend(1)) food.Add(new Drop { position = position, life = 15 });
        }

        void Feed()
        {
            if (Blocked()) return;
            if (medicineReady)
            {
                DropMedicine(new Vector2(Random.Range(42, 318), Random.Range(172, 220)));
                return;
            }
            if (!Spend(feedUpgradePrice)) return;
            feedUpgradePrice = Mathf.RoundToInt(feedUpgradePrice * 1.75f);
            feedCapacity++;
            food.Add(new Drop { position = new Vector2(Random.Range(42, 318), Random.Range(172, 220)), life = 15 });
            selectedTool = 0;
            Say("Feed room is now " + feedCapacity + ". Next upgrade $" + feedUpgradePrice + ".");
        }
        void LoadMedicine()
        {
            if (Blocked() || medicineReady) return;
            if (!Spend(MedicinePrice)) return;
            medicineReady = true; selectedTool = 1;
            Say("Medicine loaded. Feed it to a sick fish.");
        }
        void DropMedicine(Vector2 position)
        {
            food.Add(new Drop { position = position, life = 15, medicine = true });
            medicineReady = false; selectedTool = 0;
            Say("Medicine dropped! Healthy fish die if they eat it.");
        }
        void Kill(Fish f)
        {
            corpses.Add(new Corpse { position = f.position, tier = f.tier, life = 28, size = f.tier == 2 ? 58 : f.hunter ? 47 : f.Adult ? 38 : 27 });
            fishes.Remove(f);
        }
        void DrawRaider()
        {
            float x = raider.position.x, y = raider.position.y;
            int dir = raider.right ? 1 : -1;
            Box(new Rect(x - 24, y + 13, 48, 7), "2f3325");
            for (int i = -2; i <= 2; i++) Box(new Rect(x + i * 9 - 2, y + 15, 4, 4), "1e2119");
            Box(new Rect(x - 22, y + 2, 44, 12), "7c8b4a");
            Box(new Rect(x - 22, y + 2, 44, 3), "98a75f");
            Box(new Rect(x - 8, y - 5, 16, 8), "6d7a3f");
            Box(new Rect(x - 2, y - 17, 4, 13), "a7b06a");
            Box(new Rect(x + dir * 14 - 3, y + 4, 6, 6), "c96a4a");
        }

        void Styles()
        {
            if (body != null) return;
            body = new GUIStyle(GUI.skin.label) { fontSize = 13, alignment = TextAnchor.MiddleLeft, wordWrap = true };
            body.normal.textColor = Hex("e2f5ec");
            small = new GUIStyle(body) { fontSize = 10 };
            tag = new GUIStyle(small) { fontSize = 9, alignment = TextAnchor.MiddleCenter };
            tag.normal.textColor = Hex("cfe6dc");
            title = new GUIStyle(body) { fontSize = 25, fontStyle = FontStyle.Bold };
            button = new GUIStyle(GUI.skin.button) { fontSize = 12, fontStyle = FontStyle.Bold, wordWrap = true };
            button.normal.textColor = Hex("e2f5ec");
        }
        static Color Hex(string value) { ColorUtility.TryParseHtmlString("#" + value, out var color); return color; }
        void Box(Rect rect, string color)
        { GUI.color = Hex(color); GUI.DrawTexture(rect, pixel); GUI.color = Color.white; }
        void Label(Rect rect, string text, GUIStyle style = null) => GUI.Label(rect, text, style ?? body);
        bool Button(Rect rect, string text) => GUI.Button(rect, text, button);
        bool IconButton(Rect rect, string caption, bool selected, System.Action<Rect> icon)
        {
            GUI.backgroundColor = selected ? Hex("79c6a2") : Color.white;
            bool clicked = Button(rect, "");
            GUI.backgroundColor = Color.white;
            icon(new Rect(rect.center.x - 17, rect.y + 4, 34, 21));
            tag.normal.textColor = Hex("cfe6dc");
            GUI.Label(new Rect(rect.x, rect.y + 29, rect.width, 15), caption, tag);
            if (selected) Box(new Rect(rect.x + 4, rect.y + 44, rect.width - 8, 3), "79c6a2");
            return clicked;
        }
        void FishIcon(Rect rect, Texture2D texture) => GUI.DrawTextureWithTexCoords(rect, texture, new Rect(0, 0, 1, 1));
        void PauseIcon(Rect rect)
        {
            Box(new Rect(rect.center.x - 9, rect.y + 2, 7, 17), "e2f5ec");
            Box(new Rect(rect.center.x + 2, rect.y + 2, 7, 17), "e2f5ec");
        }
        void PlayIcon(Rect rect)
        {
            for (int i = 0; i < 9; i++) Box(new Rect(rect.center.x - 8, rect.y + 2 + i * 2, 3 + i * 1.7f, 2), "e2f5ec");
        }
        void FeedIcon(Rect rect)
        {
            Box(new Rect(rect.center.x - 3, rect.y + 2, 5, 5), "e0a071");
            Box(new Rect(rect.center.x - 10, rect.y + 11, 5, 5), "e0a071");
            Box(new Rect(rect.center.x + 4, rect.y + 11, 5, 5), "e0a071");
        }
        void CureIcon(Rect rect)
        {
            Box(new Rect(rect.center.x - 9, rect.y + 8, 18, 6), "8fe0b0");
            Box(new Rect(rect.center.x - 3, rect.y + 2, 6, 18), "8fe0b0");
        }
        void RemoveIcon(Rect rect)
        {
            for (int i = 0; i < 5; i++)
            {
                Box(new Rect(rect.center.x - 8 + i * 3, rect.y + 2 + i * 3, 4, 4), "ef8b78");
                Box(new Rect(rect.center.x + 4 - i * 3, rect.y + 2 + i * 3, 4, 4), "ef8b78");
            }
        }

        void OnGUI()
        {
            Styles();
            scale = Mathf.Min(Screen.width / Width, Screen.height / Height);
            offset = new Vector2((Screen.width - Width * scale) / 2, (Screen.height - Height * scale) / 2);
            GUI.matrix = Matrix4x4.TRS(offset, Quaternion.identity, Vector3.one * scale);
            Box(new Rect(0, 0, Width, Height), "091f2c");
            Label(new Rect(20, 12, 260, 18), "POCKET REEF  /  AQUARIUM KEEPER", small);
            if (Button(new Rect(296, 6, 48, 24), "?")) help = !help;
            Box(new Rect(16, 34, 328, 46), "163b48");
            Label(new Rect(28, 38, 132, 30), "$" + money, title);
            Label(new Rect(174, 40, 156, 16), "STAGE " + (level + 1) + "   /   GOAL $" + goals[level], small);
            Box(new Rect(174, 62, 153, 5), "244e59");
            Box(new Rect(174, 62, 153 * Mathf.Clamp01((float)money / goals[level]), 5), "f3c65c");
            // Shop and tool buttons live at the top so the tank can use the lower screen.
            if (IconButton(new Rect(14, 86, 44, 48), "$" + FishPrice, false, r => FishIcon(r, gold)) && !Blocked() && fishes.Count < 30 && Spend(FishPrice)) AddFish(0);
            if (IconButton(new Rect(62, 86, 44, 48), "$" + HunterPrice, false, r => FishIcon(r, hunter)) && !Blocked() && fishes.Count < 30 && Spend(HunterPrice)) AddFish(1);
            if (IconButton(new Rect(110, 86, 44, 48), "$" + SharkPrice, false, r => FishIcon(r, shark)) && !Blocked() && fishes.Count < 30 && Spend(SharkPrice)) AddFish(2);
            if (IconButton(new Rect(158, 86, 44, 48), "$" + feedUpgradePrice + " " + food.Count + "/" + feedCapacity, selectedTool == 0, FeedIcon) && !Blocked()) Feed();
            if (IconButton(new Rect(206, 86, 44, 48), "$" + MedicinePrice, medicineReady, CureIcon) && !Blocked()) LoadMedicine();
            if (IconButton(new Rect(254, 86, 44, 48), "FREE", selectedTool == 2, RemoveIcon) && !Blocked()) selectedTool = 2;
            if (IconButton(new Rect(302, 86, 44, 48), paused ? "RESUME" : "PAUSE", false, paused ? (System.Action<Rect>)PlayIcon : PauseIcon) && !won && !lost && !help) paused = !paused;
            Box(tank, "165567");
            for (int row = 0; row < 14; row++) Box(new Rect(18, 150 + row * 28, 324, 13), row % 2 == 0 ? "17586a" : "185a6b");
            Box(new Rect(18, 148, 324, 6), "2a7e8b");
            for (int i = 0; i < 27; i++)
            {
                float wave = Mathf.Sin(elapsed * 2.6f + i * .72f) * 1.5f;
                Box(new Rect(20 + i * 12, 148 + wave, 10, 3), "a9e2dc");
            }
            for (int i = 0; i < 13; i++)
            {
                float y = 545 - (elapsed * (9 + i % 4) + i * 29) % 395;
                Box(new Rect(28 + (i * 53) % 300, y, 3, 3), "438594");
            }
            Box(new Rect(18, 533, 324, 25), "bcaa7f");
            for (int i = 0; i < 24; i++) Box(new Rect(20 + i * 14, 539 + i % 3 * 4, 4, 3), "8c906b");
            for (int i = 0; i < 6; i++)
            {
                float x = i < 3 ? 30 + i * 9 : 300 + (i - 3) * 9;
                Box(new Rect(x, 494 - i % 3 * 12, 5, 42 + i % 3 * 12), "428c76");
                Box(new Rect(x - 5, 501 - i % 3 * 12, 6, 5), "69ad83");
            }
            if (raider != null) DrawRaider();
            foreach (var p in food)
            {
                if (p.medicine)
                {
                    Box(new Rect(p.position.x - 5, p.position.y - 5, 10, 10), "8fe0b0");
                    Box(new Rect(p.position.x - 3, p.position.y - 1, 6, 2), "0d2a22");
                    Box(new Rect(p.position.x - 1, p.position.y - 3, 2, 6), "0d2a22");
                }
                else Box(new Rect(p.position.x - 3, p.position.y - 3, 6, 6), "dd9471");
            }
            foreach (var f in fishes)
            {
                float size = f.tier == 2 ? 58 : f.hunter ? 47 : f.Adult ? 38 : 27;
                var rect = new Rect(f.position.x - size / 2, f.position.y - size / 3, size, size * .65f);
                bool flip = f.target.x < f.position.x;
                GUI.DrawTextureWithTexCoords(rect, f.disease > 0 ? sick : f.tier == 2 ? shark : f.hunter ? hunter : gold, new Rect(flip ? 1 : 0, 0, flip ? -1 : 1, 1));
                Label(new Rect(f.position.x - 20, f.position.y + 14, 70, 17), f.disease > 0 ? "SICK " + (1 + Mathf.Min(2, (int)(f.disease / 10))) + "/3" : f.tier == 2 ? "SHARK" : f.hunter ? "HUNTER" : f.Adult ? "LV.2" : "LV.1", small);
                if (f.hunger < 35) Label(new Rect(f.position.x - 18, f.position.y - 30, 60, 18), "HUNGRY", small);
                if (f.disease > 0) { Box(new Rect(f.position.x - 15, f.position.y - 19, 30, 3), "713b53"); Box(new Rect(f.position.x - 15, f.position.y - 19, 30 * (1 - f.disease / 30), 3), "bbdf83"); }
            }
            foreach (var m in missiles)
            {
                Box(new Rect(m.position.x - 3, m.position.y - 7, 6, 12), "d9d5c5");
                Box(new Rect(m.position.x - 2, m.position.y - 10, 4, 4), "ff6b57");
                Box(new Rect(m.position.x - 5, m.position.y + 3, 10, 3), "6a6f7a");
                Box(new Rect(m.position.x - 2, m.position.y + 6, 2, 3), "ffb27a");
            }
            foreach (var c in corpses)
            {
                var rect = new Rect(c.position.x - c.size / 2, c.position.y - c.size / 3, c.size, c.size * .65f);
                GUI.color = new Color(.65f, .65f, .65f, Mathf.Clamp01(c.life / 3));
                GUI.DrawTextureWithTexCoords(rect, c.tier == 2 ? shark : c.tier == 1 ? hunter : gold, new Rect(0, 1, 1, -1));
                GUI.color = Color.white;
            }
            foreach (var c in coins)
            {
                if (coinSheet != null)
                {
                    int frame = (int)(elapsed * 12) % 4;
                    float x = frame == 0 ? 0 : frame == 1 ? 71 : frame == 2 ? 149 : 199;
                    float w = frame == 0 ? 65 : frame == 1 ? 51 : frame == 2 ? 23 : 50;
                    float width = w * 24f / 64f;
                    GUI.DrawTextureWithTexCoords(new Rect(c.position.x - width / 2, c.position.y - 12, width, 24), coinSheet, new Rect(x / 249f, 0, w / 249f, 1));
                }
                else
                {
                    Box(new Rect(c.position.x - 9, c.position.y - 9, 18, 18), "c88b38");
                    Box(new Rect(c.position.x - 6, c.position.y - 8, 12, 14), "ffda71");
                    GUI.color = Hex("62452d"); Label(new Rect(c.position.x - 6, c.position.y - 10, 24, 20), "$", small); GUI.color = Color.white;
                }
            }
            if (Event.current.type == EventType.MouseDown && tank.Contains(Event.current.mousePosition))
            { TankTap(Event.current.mousePosition); Event.current.Use(); }
            Label(new Rect(20, 566, 320, 40), messageTime > 0 ? message : "Tap water to feed • FEED adds room • Tap coins to collect", small);
            if (help) Modal("KEEP YOUR REEF ALIVE", "Tap water: $1 food. The tank starts with room for 1. FEED adds room and food: $10 first, then 1.75x each time. Three meals grow a level 2 goldfish.\n\nLevel 2 fish drop $5 coins. Hunters ($70) drop $15 and sharks ($200) drop $40; both eat level 2 fish. Tap coins to collect.\n\nDisease kills in 30 seconds. CURE ($15) loads the next pellet as medicine: it cures a sick fish but kills a healthy one. REMOVE taps out a sick fish for free.\n\nA tank sometimes drives past and fires missiles. Tap a missile to destroy it.\n\nReach the money goal to advance.", "LET'S PLAY", () => help = false);
            else if (won) Modal("GOAL REACHED!", "You saved $" + goals[level] + ".\n\n" + (level == goals.Length - 1 ? "Your reef is thriving. All stages complete!" : "Keep your fish and savings as you take on the next goal."), level == goals.Length - 1 ? "PLAY AGAIN" : "NEXT STAGE", () => { if (level == goals.Length - 1) Restart(); else { level++; won = false; Say("New goal: $" + goals[level]); } });
            else if (lost) Modal("A QUIET REEF", "No fish or savings remain.\n\nStart again and keep a little money aside for food and medicine.", "TRY AGAIN", Restart);
            else if (paused) Modal("TAKE A BREATHER", "Your reef is paused.", "RESUME", () => paused = false);
        }
        bool Blocked() => paused || won || lost || help;
        void Modal(string heading, string text, string action, System.Action clicked)
        {
            GUI.color = new Color(0, 0, 0, .75f); GUI.DrawTexture(new Rect(0, 0, Width, Height), pixel); GUI.color = Color.white;
            Box(new Rect(28, 116, 304, 396), "143744");
            Label(new Rect(46, 130, 268, 40), heading, new GUIStyle(title) { fontSize = 20 });
            Label(new Rect(46, 172, 268, 294), text);
            if (Button(new Rect(46, 470, 268, 34), action)) clicked();
        }
        static Texture2D MakeFish(Color color, bool predator)
        {
            string[] map = { "................", ".......##.......", ".....######.....", "##..#########...", ".#############..", "..#########.###.", ".#############..", "##..#########...", ".....######.....", ".......##......." };
            var texture = new Texture2D(16, 10) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < 10; y++) for (int x = 0; x < 16; x++)
            {
                Color c = map[y][x] == '#' ? color : Color.clear;
                if (map[y][x] == '#' && y > 6) c *= new Color(.8f, .7f, .65f, 1);
                if (x == 12 && y == 4) c = Hex("102a3b");
                if (x == 11 && y == 3) c = Color.white;
                if (predator && x == 14 && y == 6) c = Color.white;
                texture.SetPixel(x, 9 - y, c);
            }
            texture.Apply(); return texture;
        }
        void OnDestroy()
        { Destroy(gold); Destroy(hunter); Destroy(shark); Destroy(sick); }
    }
}
