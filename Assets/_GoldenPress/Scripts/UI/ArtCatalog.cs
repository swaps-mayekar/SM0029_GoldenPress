using System.Collections.Generic;
using UnityEngine;

namespace GoldenPress.UI
{
    /// <summary>
    /// Loads final art from Resources/Art. Falls back gracefully if an asset is missing.
    /// </summary>
    public static class ArtCatalog
    {
        private static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();
        private static bool _warmed;

        public static Sprite SplashHero => Get("splash_hero");
        public static Sprite HubBackground => Get("hub_bg");
        public static Sprite ProductionBackground => Get("production_bg");
        public static Sprite PanelFrame => Get("panel_frame");
        public static Sprite WoodenPress => Get("wooden_press");
        public static Sprite OilTank => Get("oil_tank");
        public static Sprite FamilyShop => Get("family_shop");
        public static Sprite OilBottle => Get("oil_bottle");
        public static Sprite Groundnuts => Get("groundnuts");
        public static Sprite Debris => Get("debris");
        public static Sprite LogoMark => Get("logo_mark");
        public static Sprite Basket => Get("basket");
        public static Sprite Coin => Get("coin");

        public static void Warm()
        {
            if (_warmed)
            {
                return;
            }

            _warmed = true;
            Get("splash_hero");
            Get("hub_bg");
            Get("production_bg");
            Get("panel_frame");
            Get("wooden_press");
            Get("oil_tank");
            Get("family_shop");
            Get("oil_bottle");
            Get("groundnuts");
            Get("sunflower");
            Get("mustard");
            Get("sesame");
            Get("coconut");
            Get("soybean");
            Get("debris");
            Get("logo_mark");
            Get("basket");
            Get("coin");
        }

        public static Sprite Get(string resourceName)
        {
            if (Cache.TryGetValue(resourceName, out var cached) && cached != null)
            {
                return cached;
            }

            var sprite = Resources.Load<Sprite>("Art/" + resourceName);
            if (sprite == null)
            {
                // Texture may exist but not yet marked as Sprite; try Texture2D wrap.
                var tex = Resources.Load<Texture2D>("Art/" + resourceName);
                if (tex != null)
                {
                    sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
                }
            }

            Cache[resourceName] = sprite;
            return sprite;
        }

        public static Sprite SeedForOil(string oilId)
        {
            // Oil ids match Resources/Art filenames except groundnut → groundnuts.
            var resourceName = oilId == "groundnut" ? "groundnuts" : oilId;
            if (!string.IsNullOrEmpty(resourceName))
            {
                var seed = Get(resourceName);
                if (seed != null)
                {
                    return seed;
                }
            }

            return Groundnuts != null ? Groundnuts : GameTheme.WhiteSprite;
        }
    }
}
