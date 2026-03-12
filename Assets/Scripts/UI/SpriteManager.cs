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

            Debug.Log($"Loaded sprite sheet: {spriteSheet.width}x{spriteSheet.height}");

            // Use generated sprites - the sprite sheet layout varies
            // This is safer than hardcoding coordinates
            GeneratePlaceholderSprites();
        }

        private Sprite CreateSprite(int x, int y, int width, int height, string name)
        {
            if (spriteSheet == null) return null;

            // Validate bounds
            if (x + width > spriteSheet.width || y + height > spriteSheet.height)
            {
                Debug.LogWarning($"Sprite {name} at ({x},{y},{width},{height}) exceeds texture bounds ({spriteSheet.width}x{spriteSheet.height})");
                return null;
            }

            Rect rect = new Rect(x, y, width, height);
            Vector2 pivot = new Vector2(0.5f, 0.5f);

            Sprite sprite = Sprite.Create(spriteSheet, rect, pivot, 16f);
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
