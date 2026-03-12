using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace IFRTrainer.UI
{
    /// <summary>
    /// Manages loading and applying pixel plane sprites from the asset pack.
    /// </summary>
    public class SpriteManager : MonoBehaviour
    {
        public static SpriteManager Instance { get; private set; }

        [Header("Sprite Sheet")]
        [SerializeField] private Texture2D spriteSheet;

        [Header("Aircraft Sprites")]
        public Sprite planeWhite;
        public Sprite planeBlack;
        public Sprite planeGray;
        public Sprite planeRed;
        public Sprite planeOrange;
        public Sprite planeYellow;
        public Sprite planeGreen;
        public Sprite planeTeal;
        public Sprite planeBlue;
        public Sprite planePurple;
        public Sprite planePink;
        public Sprite planeBrown;
        public Sprite jetGray;
        public Sprite jetGreen;
        public Sprite jetBlue;

        [Header("Other Sprites")]
        public Sprite vorStation;
        public Sprite bullet1;
        public Sprite bullet2;
        public Sprite[] explosionFrames;

        private Dictionary<string, Sprite> spriteCache = new Dictionary<string, Sprite>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            LoadSpritesFromSheet();
        }

        private void LoadSpritesFromSheet()
        {
            if (spriteSheet == null)
            {
                // Try to load from Resources
                spriteSheet = Resources.Load<Texture2D>("Sprites/PixelPlanesAssetPack");
            }

            if (spriteSheet == null)
            {
                Debug.LogWarning("Sprite sheet not found. Using generated sprites.");
                GeneratePlaceholderSprites();
                return;
            }

            // Load sprites from the sprite sheet
            // The sprite sheet is 128x128, with 24x24 planes and 16x16 other items
            LoadAircraftSprites();
            LoadOtherSprites();
        }

        private void LoadAircraftSprites()
        {
            // Row 1 (top): white, black, gray (y=112)
            planeWhite = CreateSprite(0, 112, 24, 24, "plane_white");
            planeBlack = CreateSprite(24, 112, 24, 24, "plane_black");
            planeGray = CreateSprite(48, 112, 24, 24, "plane_gray");

            // Row 2: red, orange, yellow (y=88)
            planeRed = CreateSprite(0, 88, 24, 24, "plane_red");
            planeOrange = CreateSprite(24, 88, 24, 24, "plane_orange");
            planeYellow = CreateSprite(48, 88, 24, 24, "plane_yellow");

            // Row 3: green, teal, blue (y=64)
            planeGreen = CreateSprite(0, 64, 24, 24, "plane_green");
            planeTeal = CreateSprite(24, 64, 24, 24, "plane_teal");
            planeBlue = CreateSprite(48, 64, 24, 24, "plane_blue");

            // Row 4: purple, pink, brown (y=40)
            planePurple = CreateSprite(0, 40, 24, 24, "plane_purple");
            planePink = CreateSprite(24, 40, 24, 24, "plane_pink");
            planeBrown = CreateSprite(48, 40, 24, 24, "plane_brown");

            // Row 5: jets (y=16)
            jetGray = CreateSprite(0, 16, 24, 24, "jet_gray");
            jetGreen = CreateSprite(24, 16, 24, 24, "jet_green");
            jetBlue = CreateSprite(48, 16, 24, 24, "jet_blue");
        }

        private void LoadOtherSprites()
        {
            // VOR station marker (we'll use one of the small sprites or create one)
            vorStation = CreateSprite(0, 0, 16, 16, "vor_station");

            // Bullets
            bullet1 = CreateSprite(80, 120, 8, 8, "bullet_1");
            bullet2 = CreateSprite(88, 120, 8, 8, "bullet_2");

            // Explosion frames (for potential future use)
            explosionFrames = new Sprite[6];
            explosionFrames[0] = CreateSprite(80, 88, 16, 16, "explosion_1");
            explosionFrames[1] = CreateSprite(96, 88, 16, 16, "explosion_2");
            explosionFrames[2] = CreateSprite(112, 88, 16, 16, "explosion_3");
            explosionFrames[3] = CreateSprite(80, 72, 16, 16, "explosion_4");
            explosionFrames[4] = CreateSprite(96, 72, 16, 16, "explosion_5");
            explosionFrames[5] = CreateSprite(112, 72, 16, 16, "explosion_6");
        }

        private Sprite CreateSprite(int x, int y, int width, int height, string name)
        {
            if (spriteSheet == null) return null;

            // Unity texture coordinates start from bottom-left
            Rect rect = new Rect(x, y, width, height);
            Vector2 pivot = new Vector2(0.5f, 0.5f);

            Sprite sprite = Sprite.Create(spriteSheet, rect, pivot, 16f); // 16 pixels per unit
            sprite.name = name;
            spriteCache[name] = sprite;

            return sprite;
        }

        private void GeneratePlaceholderSprites()
        {
            // Generate simple colored placeholder sprites
            planeWhite = CreateColoredSprite(Color.white, "plane_white");
            planeBlack = CreateColoredSprite(Color.black, "plane_black");
            planeGray = CreateColoredSprite(Color.gray, "plane_gray");
            planeRed = CreateColoredSprite(Color.red, "plane_red");
            planeOrange = CreateColoredSprite(new Color(1f, 0.5f, 0f), "plane_orange");
            planeYellow = CreateColoredSprite(Color.yellow, "plane_yellow");
            planeGreen = CreateColoredSprite(Color.green, "plane_green");
            planeTeal = CreateColoredSprite(Color.cyan, "plane_teal");
            planeBlue = CreateColoredSprite(Color.blue, "plane_blue");
            planePurple = CreateColoredSprite(new Color(0.5f, 0f, 0.5f), "plane_purple");
            planePink = CreateColoredSprite(new Color(1f, 0.5f, 0.8f), "plane_pink");
            planeBrown = CreateColoredSprite(new Color(0.5f, 0.25f, 0f), "plane_brown");
            jetGray = CreateColoredSprite(Color.gray, "jet_gray");
            jetGreen = CreateColoredSprite(Color.green, "jet_green");
            jetBlue = CreateColoredSprite(Color.blue, "jet_blue");
            vorStation = CreateColoredSprite(Color.cyan, "vor_station");
        }

        private Sprite CreateColoredSprite(Color color, string name)
        {
            Texture2D tex = new Texture2D(24, 24);
            Color[] pixels = new Color[24 * 24];

            // Create a simple triangle shape for the plane
            for (int y = 0; y < 24; y++)
            {
                for (int x = 0; x < 24; x++)
                {
                    // Triangle pointing up
                    int centerX = 12;
                    int distFromCenter = Mathf.Abs(x - centerX);
                    int maxWidth = (24 - y) / 2;

                    if (distFromCenter <= maxWidth && y < 22)
                    {
                        pixels[y * 24 + x] = color;
                    }
                    else
                    {
                        pixels[y * 24 + x] = Color.clear;
                    }
                }
            }

            tex.SetPixels(pixels);
            tex.filterMode = FilterMode.Point;
            tex.Apply();

            Sprite sprite = Sprite.Create(tex, new Rect(0, 0, 24, 24), new Vector2(0.5f, 0.5f), 16f);
            sprite.name = name;
            spriteCache[name] = sprite;

            return sprite;
        }

        public Sprite GetSprite(string name)
        {
            if (spriteCache.TryGetValue(name, out Sprite sprite))
            {
                return sprite;
            }
            return null;
        }

        public Sprite GetPlayerPlane()
        {
            return planeGreen ?? planeWhite;
        }

        public Sprite GetVORMarker()
        {
            return vorStation ?? planeTeal;
        }

        public Sprite[] GetAllPlanes()
        {
            return new Sprite[]
            {
                planeWhite, planeBlack, planeGray,
                planeRed, planeOrange, planeYellow,
                planeGreen, planeTeal, planeBlue,
                planePurple, planePink, planeBrown,
                jetGray, jetGreen, jetBlue
            };
        }
    }
}
