using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using IFRTrainer.Core;
using IFRTrainer.Mission;

namespace IFRTrainer.UI
{
    /// <summary>
    /// Top-down radar scope display showing aircraft, VOR stations, and radials.
    /// </summary>
    public class RadarScope : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private RectTransform scopeArea;
        [SerializeField] private RectTransform aircraftIcon;
        [SerializeField] private Image aircraftImage;
        [SerializeField] private RectTransform headingVector;
        [SerializeField] private Image selectedRadialLine;
        [SerializeField] private GameObject vorMarkerPrefab;
        [SerializeField] private Text scaleText;

        [Header("Sprites")]
        [SerializeField] private Sprite playerPlaneSprite;
        [SerializeField] private Sprite vorMarkerSprite;

        [Header("Display Settings")]
        [SerializeField] private float scopeRadiusNM = 20f;
        [SerializeField] private float minScopeRadius = 5f;
        [SerializeField] private float maxScopeRadius = 50f;
        [SerializeField] private float headingVectorLength = 50f;
        [SerializeField] private Color aircraftColor = Color.green;
        [SerializeField] private Color vorColor = Color.cyan;
        [SerializeField] private Color radialColor = Color.yellow;

        [Header("Animation")]
        [SerializeField] private float smoothTime = 0.1f;

        private Dictionary<string, RectTransform> vorMarkers = new Dictionary<string, RectTransform>();
        private MissionManager missionManager;
        private float scopePixelRadius;
        private Vector2 scopeCenter;
        private Vector2 aircraftVelocity;

        private void Awake()
        {
            missionManager = FindFirstObjectByType<MissionManager>();
        }

        private void Start()
        {
            if (scopeArea != null)
            {
                scopePixelRadius = Mathf.Min(scopeArea.rect.width, scopeArea.rect.height) / 2f;
                scopeCenter = scopeArea.rect.center;
            }

            UpdateScaleText();
            ApplySprites();
        }

        private void ApplySprites()
        {
            // Apply player plane sprite if available
            if (aircraftImage != null && SpriteManager.Instance != null)
            {
                var sprite = SpriteManager.Instance.GetPlayerPlane();
                if (sprite != null)
                {
                    aircraftImage.sprite = sprite;
                    aircraftImage.color = Color.white; // Use sprite colors
                    aircraftImage.preserveAspect = true;
                }
            }
            else if (aircraftImage != null && playerPlaneSprite != null)
            {
                aircraftImage.sprite = playerPlaneSprite;
                aircraftImage.color = Color.white;
                aircraftImage.preserveAspect = true;
            }
        }

        private void OnEnable()
        {
            if (missionManager != null)
            {
                missionManager.OnMissionStarted += OnMissionStarted;
            }
        }

        private void OnDisable()
        {
            if (missionManager != null)
            {
                missionManager.OnMissionStarted -= OnMissionStarted;
            }
        }

        private void Update()
        {
            if (!GameManager.Instance || !GameManager.Instance.IsInitialized)
                return;

            UpdateAircraftIcon();
            UpdateHeadingVector();
            UpdateVORMarkers();
            UpdateSelectedRadial();
        }

        private void OnMissionStarted(MissionData mission)
        {
            // Clear old markers
            foreach (var marker in vorMarkers.Values)
            {
                if (marker != null)
                    Destroy(marker.gameObject);
            }
            vorMarkers.Clear();

            // Create markers for mission VORs
            if (mission.vor_stations != null)
            {
                foreach (var vor in mission.vor_stations)
                {
                    CreateVORMarker(vor);
                }
            }
        }

        private void CreateVORMarker(VORStationData vor)
        {
            if (scopeArea == null)
                return;

            GameObject markerObj;

            if (vorMarkerPrefab != null)
            {
                markerObj = Instantiate(vorMarkerPrefab, scopeArea);
            }
            else
            {
                // Create marker dynamically
                markerObj = new GameObject($"VOR_{vor.identifier}");
                markerObj.transform.SetParent(scopeArea, false);

                var rect = markerObj.AddComponent<RectTransform>();
                rect.sizeDelta = new Vector2(32, 32);

                var image = markerObj.AddComponent<Image>();

                // Try to use sprite from SpriteManager
                if (SpriteManager.Instance != null)
                {
                    var sprite = SpriteManager.Instance.GetVORMarker();
                    if (sprite != null)
                    {
                        image.sprite = sprite;
                        image.color = Color.white;
                        image.preserveAspect = true;
                    }
                    else
                    {
                        image.color = vorColor;
                    }
                }
                else if (vorMarkerSprite != null)
                {
                    image.sprite = vorMarkerSprite;
                    image.color = Color.white;
                }
                else
                {
                    image.color = vorColor;
                }

                // Add label
                var labelObj = new GameObject("Label");
                labelObj.transform.SetParent(markerObj.transform, false);

                var labelRect = labelObj.AddComponent<RectTransform>();
                labelRect.anchoredPosition = new Vector2(0, -20);
                labelRect.sizeDelta = new Vector2(50, 20);

                var tmp = labelObj.AddComponent<Text>();
                tmp.text = vor.identifier;
                tmp.fontSize = 12;
                tmp.color = Color.cyan;
                tmp.alignment = TextAnchor.MiddleCenter;
            }

            RectTransform marker = markerObj.GetComponent<RectTransform>();

            // Set label if using prefab
            if (vorMarkerPrefab != null)
            {
                var label = markerObj.GetComponentInChildren<Text>();
                if (label != null)
                {
                    label.text = vor.identifier;
                }

                var image = markerObj.GetComponent<Image>();
                if (image != null)
                {
                    image.color = vorColor;
                }
            }

            vorMarkers[vor.identifier] = marker;
        }

        private void UpdateAircraftIcon()
        {
            if (aircraftIcon == null)
                return;

            // Aircraft is always centered on the scope
            aircraftIcon.anchoredPosition = Vector2.SmoothDamp(
                aircraftIcon.anchoredPosition, Vector2.zero, ref aircraftVelocity, smoothTime);

            // Rotate aircraft icon to match heading
            float heading = GameManager.Instance.CurrentDynamics.Heading;
            aircraftIcon.localEulerAngles = new Vector3(0, 0, -heading);
        }

        private void UpdateHeadingVector()
        {
            if (headingVector == null)
                return;

            float heading = GameManager.Instance.CurrentDynamics.Heading;
            headingVector.localEulerAngles = new Vector3(0, 0, -heading);

            // Scale vector based on speed (optional)
            float speed = GameManager.Instance.CurrentDynamics.Speed;
            float scaledLength = headingVectorLength * (speed / 150f); // Normalized to 150 knots
            headingVector.sizeDelta = new Vector2(headingVector.sizeDelta.x, scaledLength);
        }

        private void UpdateVORMarkers()
        {
            if (missionManager?.CurrentMission?.vor_stations == null)
                return;

            var aircraftPos = GameManager.Instance.CurrentPosition;

            foreach (var vor in missionManager.CurrentMission.vor_stations)
            {
                if (!vorMarkers.TryGetValue(vor.identifier, out RectTransform marker))
                    continue;

                // Calculate relative position
                Vector2 relativePos = CalculateRelativePosition(
                    aircraftPos.Latitude, aircraftPos.Longitude,
                    vor.latitude, vor.longitude);

                // Convert to screen position
                Vector2 screenPos = RelativeToScreenPosition(relativePos);

                // Update marker position
                marker.anchoredPosition = screenPos;

                // Hide if outside scope
                float distance = relativePos.magnitude;
                marker.gameObject.SetActive(distance <= scopeRadiusNM);
            }
        }

        private void UpdateSelectedRadial()
        {
            if (selectedRadialLine == null)
                return;

            var indication = GameManager.Instance.CurrentVORIndication;
            if (!indication.IsValid)
            {
                selectedRadialLine.gameObject.SetActive(false);
                return;
            }

            selectedRadialLine.gameObject.SetActive(true);

            // Get OBS setting
            float obs = SimulationBridge.GetOBS();

            // Rotate line to show selected radial
            // The radial extends FROM the VOR, so we show it relative to the VOR position
            RectTransform lineTransform = selectedRadialLine.rectTransform;
            lineTransform.localEulerAngles = new Vector3(0, 0, -obs);

            // Color based on TO/FROM
            selectedRadialLine.color = indication.ToFromFlag == ToFromFlag.To ?
                Color.Lerp(radialColor, Color.green, 0.3f) : radialColor;
        }

        private Vector2 CalculateRelativePosition(double acLat, double acLon, double targetLat, double targetLon)
        {
            // Calculate bearing and distance
            float bearing = SimulationBridge.CalculateBearing(acLat, acLon, targetLat, targetLon);
            float distance = SimulationBridge.CalculateDistance(acLat, acLon, targetLat, targetLon);

            // Convert to x/y offset (north = +y, east = +x)
            float bearingRad = bearing * Mathf.Deg2Rad;
            float x = distance * Mathf.Sin(bearingRad);
            float y = distance * Mathf.Cos(bearingRad);

            return new Vector2(x, y);
        }

        private Vector2 RelativeToScreenPosition(Vector2 relativeNM)
        {
            // Scale from nautical miles to pixels
            float scale = scopePixelRadius / scopeRadiusNM;
            return relativeNM * scale;
        }

        public void ZoomIn()
        {
            scopeRadiusNM = Mathf.Max(minScopeRadius, scopeRadiusNM * 0.75f);
            UpdateScaleText();
        }

        public void ZoomOut()
        {
            scopeRadiusNM = Mathf.Min(maxScopeRadius, scopeRadiusNM * 1.5f);
            UpdateScaleText();
        }

        public void SetRange(float rangeNM)
        {
            scopeRadiusNM = Mathf.Clamp(rangeNM, minScopeRadius, maxScopeRadius);
            UpdateScaleText();
        }

        private void UpdateScaleText()
        {
            if (scaleText != null)
            {
                scaleText.text = $"{scopeRadiusNM:F0} NM";
            }
        }
    }
}
