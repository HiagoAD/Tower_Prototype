using Game.Core;
using Game.Presentation;
using Game.Webhook;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Editor
{
    /// <summary>Level 1's in-game HUD: altitude meter, controls hint, the two event-card columns, and the pause button and lives row.</summary>
    internal static class Level1Hud
    {
        private readonly struct ProgressBarParts
        {
            public readonly RectTransform Fill;
            public readonly RectTransform Marker;
            public readonly Text HeightLabel;
            public readonly Text FinishLabel;

            public ProgressBarParts(RectTransform fill, RectTransform marker, Text heightLabel, Text finishLabel)
            {
                Fill = fill;
                Marker = marker;
                HeightLabel = heightLabel;
                FinishLabel = finishLabel;
            }
        }

        /// <summary>
        /// The in-game HUD panel, hidden until the session starts: altitude meter,
        /// controls hint, both event-card columns, the pause button, the lives row and the HudView that drives them. The panel is
        /// shrunk to Screen.safeArea so every top/bottom-anchored child clears a camera cutout.
        /// </summary>
        public static GameObject Build(UiKit kit, RectTransform canvasRect, GameSession session, GameSettings settings, Sprite gloveSprite)
        {
            GameObject hudPanel = UiKit.BuildPanel(canvasRect, "HudPanel");
            hudPanel.SetActive(false);
            // The test phone has a camera cutout; shrink the whole HUD to Screen.safeArea so every
            // top/bottom-anchored child below clears it instead of drawing under the notch.
            hudPanel.AddComponent<SafeAreaFitter>();

            ProgressBarParts bar = BuildProgressBar(kit, hudPanel.transform);

            Text levelLabel = BuildLevelLabel(kit, hudPanel.transform);

            Button pauseButton = BuildPauseButton(kit, hudPanel.transform, session);
            (GameObject livesRoot, GameObject[] lifeIcons) = BuildLives(hudPanel.transform);

            BuildControlsHint(kit, hudPanel, session, settings);
            BuildEventFeed(hudPanel, session, settings, gloveSprite, BumpPolarity.Positive);
            BuildEventFeed(hudPanel, session, settings, gloveSprite, BumpPolarity.Negative);

            var hudViewGo = new GameObject("HudView");
            hudViewGo.transform.SetParent(hudPanel.transform, false);
            HudView hudView = hudViewGo.AddComponent<HudView>();
            SceneBinding.Bind(hudView, "session", session);
            SceneBinding.Bind(hudView, "settings", settings);
            SceneBinding.Bind(hudView, "progressFill", bar.Fill);
            SceneBinding.Bind(hudView, "progressMarker", bar.Marker);
            SceneBinding.Bind(hudView, "heightLabel", bar.HeightLabel);
            SceneBinding.Bind(hudView, "finishLabel", bar.FinishLabel);
            SceneBinding.Bind(hudView, "levelLabel", levelLabel);
            SceneBinding.Bind(hudView, "livesRoot", livesRoot);
            SceneBinding.BindArray(hudView, "lifeIcons", lifeIcons);
            SceneBinding.Bind(hudView, "pauseButton", pauseButton.gameObject);
            return hudPanel;
        }

        /// <summary>
        /// "LEVEL n/N" over the level's name, top-centre. The HUD panel is already shrunk to the safe
        /// area, so a top-anchored label clears a camera cutout; it sits above the altitude meter's
        /// goal label (left edge, 0.8 of the height) and clear of both event columns (0.46).
        /// </summary>
        private static Text BuildLevelLabel(UiKit kit, Transform hudPanel)
        {
            Text label = UiKit.AddText(hudPanel, "LevelLabel", "LEVEL 1/1\n", 44, TextAnchor.UpperCenter, new Vector2(0f, -16f),
                anchorMin: new Vector2(0.5f, 1f), anchorMax: new Vector2(0.5f, 1f), pivot: new Vector2(0.5f, 1f), sizeDelta: new Vector2(640f, 130f));
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            Level1Ui.StyleHeavyText(kit, label, Level1Palette.HudYellow);
            return label;
        }

        /// <summary>
        /// Top-right, clear of the top-centre level label (which ends 220 units short of the right
        /// edge). Always built: HudView shows it only when the pause-menu feature is on.
        /// </summary>
        private static Button BuildPauseButton(UiKit kit, Transform hudPanel, GameSession session)
        {
            const float margin = 32f;
            const float size = 110f;
            Button button = kit.AddButton(hudPanel, "PauseButton", "II", new Vector2(-margin, -margin), Level1Palette.HudButton, Level1Palette.HudYellow,
                anchorMin: Vector2.one, anchorMax: Vector2.one, pivot: Vector2.one, sizeDelta: new Vector2(size, size));
            Level1Ui.StyleHeavyText(kit, button.GetComponentInChildren<Text>(), Level1Palette.HudYellow);
            UnityEventTools.AddPersistentListener(button.onClick, session.Pause);
            return button;
        }

        /// <summary>
        /// One heart per possible life, right-aligned in a row under the pause button: the top edge
        /// beside the pause button is taken by the level label, and the right edge is free down to
        /// the negative event cards (0.46 of the height). Built from UI primitives because the
        /// Android built-in font has no heart glyph. HudView shows the row only when the lives
        /// feature is on, and as many hearts as there are lives left.
        /// </summary>
        private static (GameObject root, GameObject[] icons) BuildLives(Transform hudPanel)
        {
            const float heartSize = 40f;
            const float spacing = 78f;
            const float top = 32f + 110f + 16f; // HUD margin, pause button, gap.

            var rowGo = new GameObject("Lives", typeof(RectTransform));
            rowGo.transform.SetParent(hudPanel, false);
            var row = rowGo.GetComponent<RectTransform>();
            row.anchorMin = row.anchorMax = row.pivot = Vector2.one;
            row.sizeDelta = new Vector2(GameFeatures.MaxLives * spacing, 60f);
            row.anchoredPosition = new Vector2(-32f, -top);

            var icons = new GameObject[GameFeatures.MaxLives];
            for (int i = 0; i < icons.Length; i++)
            {
                var heartGo = new GameObject("Heart" + i, typeof(RectTransform));
                heartGo.transform.SetParent(row, false);
                var heart = heartGo.GetComponent<RectTransform>();
                heart.anchorMin = heart.anchorMax = new Vector2(1f, 0.5f);
                heart.sizeDelta = Vector2.zero;
                heart.anchoredPosition = new Vector2(-(spacing * 0.5f + i * spacing), -heartSize * 0.07f);

                AddHeartShape(heart, heartSize * 1.22f, Level1Palette.HeartOutline);
                AddHeartShape(heart, heartSize, Level1Palette.HeartFill);
                icons[i] = heartGo;
            }

            return (rowGo, icons);
        }

        /// <summary>A 45-degree square under two circles.</summary>
        private static void AddHeartShape(RectTransform parent, float size, Color color)
        {
            Image square = UiKit.AddImage(parent, "Point", null, color);
            square.rectTransform.sizeDelta = new Vector2(size, size);
            square.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 45f);

            float lobeOffset = size * 0.354f; // midpoints of the rotated square's two upper edges.
            foreach (float side in new[] { -1f, 1f })
            {
                Image lobe = UiKit.AddImage(parent, "Lobe", UiKit.KnobSprite, color);
                lobe.rectTransform.sizeDelta = new Vector2(size, size);
                lobe.rectTransform.anchoredPosition = new Vector2(side * lobeOffset, lobeOffset);
            }
        }

        /// <summary>
        /// ref.png's altitude meter: a substantial dark rounded track down the left edge filling
        /// yellow from the bottom, the goal altitude in heavy outlined red above it, and the current
        /// altitude in heavy outlined yellow riding beside the top of the fill. Proportions are the
        /// image's relative to screen height, on the taller portrait frame.
        /// </summary>
        private static ProgressBarParts BuildProgressBar(UiKit kit, Transform hudPanel)
        {
            const float barLeft = 40f;
            const float barWidth = 48f;
            const float barBottom = 0.1f;
            const float barTop = 0.8f;

            Image track = UiKit.AddImage(hudPanel, "AltitudeBar", UiKit.UiSprite, Level1Palette.BarTrack);
            RectTransform trackRect = track.rectTransform;
            trackRect.anchorMin = new Vector2(0f, barBottom);
            trackRect.anchorMax = new Vector2(0f, barTop);
            trackRect.pivot = new Vector2(0f, 0.5f);
            trackRect.sizeDelta = new Vector2(barWidth, 0f);
            trackRect.anchoredPosition = new Vector2(barLeft, 0f);

            Image fill = UiKit.AddImage(trackRect, "Fill", UiKit.UiSprite, Level1Palette.BarFill);
            UiKit.Stretch(fill.rectTransform);
            fill.rectTransform.anchorMax = new Vector2(1f, 0f);

            var markerGo = new GameObject("Marker", typeof(RectTransform));
            markerGo.transform.SetParent(trackRect, false);
            var marker = markerGo.GetComponent<RectTransform>();
            marker.anchorMin = marker.anchorMax = new Vector2(1f, 0f);
            marker.sizeDelta = Vector2.zero;

            Text heightLabel = UiKit.AddText(marker, "HeightLabel", "0", 60, TextAnchor.MiddleLeft, new Vector2(10f, 0f),
                pivot: new Vector2(0f, 0.5f), sizeDelta: new Vector2(320f, 90f));
            Level1Ui.StyleHeavyText(kit, heightLabel, Level1Palette.HudYellow);

            Text finishLabel = UiKit.AddText(hudPanel, "FinishLabel", "0", 54, TextAnchor.LowerLeft, new Vector2(barLeft - 22f, 14f),
                anchorMin: new Vector2(0f, barTop), anchorMax: new Vector2(0f, barTop), pivot: Vector2.zero, sizeDelta: new Vector2(360f, 80f));
            Level1Ui.StyleHeavyText(kit, finishLabel, Level1Palette.HudRed);

            return new ProgressBarParts(fill.rectTransform, marker, heightLabel, finishLabel);
        }

        /// <summary>
        /// Three pre-built event-card slots for one polarity, styled on the reference's gift cards.
        /// Positive cards stack on the left edge (blue banner, badge at the left end, text
        /// left-aligned), beside the altitude meter; negative cards mirror them on the right edge
        /// (layered orange flame banner, badge at the right end, text right-aligned). Each column has
        /// its own EventFeedView, which only shifts text, badge and alpha between the slots.
        /// </summary>
        private static void BuildEventFeed(GameObject hudPanel, GameSession session, GameSettings settings, Sprite gloveSprite, BumpPolarity polarity)
        {
            const int slotCount = 3;
            const float slotSpacing = 190f;
            const float cardScale = 0.8f;
            const float anchorY = 0.46f;

            bool left = polarity == BumpPolarity.Positive;
            // +1 lays a card out from its left end, -1 mirrors it from its right end.
            float side = left ? 1f : -1f;
            float edge = left ? 0f : 1f;
            TextAnchor textAnchor = left ? TextAnchor.MiddleLeft : TextAnchor.MiddleRight;
            string column = left ? "Positive" : "Negative";

            var cards = new CanvasGroup[slotCount];
            var senders = new Text[slotCount];
            var details = new Text[slotCount];
            var badges = new Image[slotCount];
            var badgeOutlines = new Image[slotCount];

            for (int i = 0; i < slotCount; i++)
            {
                var cardGo = new GameObject(column + "EventCard" + i, typeof(RectTransform), typeof(CanvasGroup));
                cardGo.transform.SetParent(hudPanel.transform, false);
                var card = cardGo.GetComponent<RectTransform>();
                // The left column starts just right of the altitude meter's track; the right column
                // hugs the right edge. Neither reaches the climber in the centre.
                card.anchorMin = card.anchorMax = new Vector2(edge, anchorY);
                card.pivot = new Vector2(edge, 0.5f);
                card.sizeDelta = new Vector2(420f, 150f);
                card.localScale = Vector3.one * cardScale;
                card.anchoredPosition = new Vector2(left ? 100f : -8f, -i * slotSpacing * cardScale);

                var group = cardGo.GetComponent<CanvasGroup>();
                group.alpha = 0f;
                group.interactable = false;
                group.blocksRaycasts = false;

                if (left)
                {
                    AddCardBar(card, "Banner", side, edge, 60f, 360f, -24f, 70f, Level1Palette.CardBanner);
                    AddCardBar(card, "BannerGlint", side, edge, 84f, 320f, -8f, 24f, Level1Palette.CardBannerGlint);
                }
                else
                {
                    AddCardBar(card, "FlameGlow", side, edge, 30f, 390f, -24f, 88f, Level1Palette.FlameGlow);
                    AddCardBar(card, "Banner", side, edge, 60f, 350f, -24f, 70f, Level1Palette.FlameBanner);
                    AddCardBar(card, "FlameCore", side, edge, 90f, 260f, -32f, 34f, Level1Palette.FlameCore);
                    // Frayed flame tip at the inner end.
                    AddCardKnob(card, "FlameTip0", side, edge, 402f, -24f, 64f, Level1Palette.FlameGlow);
                    AddCardKnob(card, "FlameTip1", side, edge, 418f, -30f, 40f, Level1Palette.FlameBanner);
                }

                badgeOutlines[i] = UiKit.AddImage(card, "BadgeOutline", gloveSprite, Level1Palette.HudStroke);
                PlaceOnEdge(badgeOutlines[i].rectTransform, side, edge, 62f, -6f, 128f, 128f, centred: true);
                badges[i] = UiKit.AddImage(card, "Badge", gloveSprite, new Color(0.9f, 0.08f, 0.08f, 1f));
                PlaceOnEdge(badges[i].rectTransform, side, edge, 62f, -6f, 114f, 114f, centred: true);

                senders[i] = UiKit.AddText(card, "Sender", string.Empty, 42, textAnchor, Vector2.zero);
                PlaceOnEdge(senders[i].rectTransform, side, edge, 132f, 38f, 280f, 56f, centred: false);
                StyleCardText(senders[i]);
                UiKit.AddOutline(senders[i], new Color(0f, 0f, 0f, 0.6f), 2f);

                details[i] = UiKit.AddText(card, "Detail", string.Empty, 36, textAnchor, Vector2.zero);
                PlaceOnEdge(details[i].rectTransform, side, edge, left ? 140f : 130f, -24f, 250f, 60f, centred: false);
                StyleCardText(details[i]);
                UiKit.AddOutline(details[i], new Color(0f, 0f, 0f, 0.55f), 2f);

                cards[i] = group;
            }

            var feedGo = new GameObject(column + "EventFeedView");
            feedGo.transform.SetParent(hudPanel.transform, false);
            EventFeedView feed = feedGo.AddComponent<EventFeedView>();
            SceneBinding.Bind(feed, "session", session);
            SceneBinding.Bind(feed, "settings", settings);
            SceneBinding.Bind(feed, "polarity", polarity);
            SceneBinding.BindArray(feed, "cards", cards);
            SceneBinding.BindArray(feed, "senderTexts", senders);
            SceneBinding.BindArray(feed, "detailTexts", details);
            SceneBinding.BindArray(feed, "badgeIcons", badges);
            SceneBinding.BindArray(feed, "badgeOutlines", badgeOutlines);
        }

        /// <summary>Positions a card child by its distance from the card's icon-side edge; centred children use dx as their centre, others as their near edge.</summary>
        private static void PlaceOnEdge(RectTransform rect, float side, float edge, float dx, float y, float width, float height, bool centred)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(edge, 0.5f);
            rect.pivot = new Vector2(centred ? 0.5f : edge, 0.5f);
            rect.sizeDelta = new Vector2(width, height);
            rect.anchoredPosition = new Vector2(side * dx, y);
        }

        private static void AddCardBar(RectTransform card, string name, float side, float edge, float dx, float width, float y, float height, Color color)
        {
            Image bar = UiKit.AddImage(card, name, UiKit.UiSprite, color);
            PlaceOnEdge(bar.rectTransform, side, edge, dx, y, width, height, centred: false);
        }

        private static void AddCardKnob(RectTransform card, string name, float side, float edge, float dx, float y, float size, Color color)
        {
            Image knob = UiKit.AddImage(card, name, UiKit.KnobSprite, color);
            PlaceOnEdge(knob.rectTransform, side, edge, dx, y, size, size, centred: true);
        }

        /// <summary>
        /// Card lines are sender-controlled: plain text only (no rich-text markup) and always one
        /// line. EventFeedView truncates a value that is wider than its slot with an ellipsis.
        /// </summary>
        private static void StyleCardText(Text text)
        {
            text.supportRichText = false;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
        }

        /// <summary>
        /// A short prompt laid out over ClimbInputSource's exact touch region
        /// (settings.input.touchRegionNormalizedHeight, not a copy of the value). No band graphic:
        /// the reference frame is clean sky. Never a raycast target. Fades out once the player has
        /// climbed a little (see ControlsHintView).
        /// </summary>
        private static void BuildControlsHint(UiKit kit, GameObject hudPanel, GameSession session, GameSettings settings)
        {
            float regionFraction = settings.input.touchRegionNormalizedHeight;

            Text hintText = UiKit.AddText(hudPanel.transform, "ClimbHintText", "Hold below to climb - release to grip", 32, TextAnchor.LowerCenter,
                new Vector2(0f, 20f), anchorMin: new Vector2(0f, 0f), anchorMax: new Vector2(1f, regionFraction), pivot: new Vector2(0.5f, 0f), sizeDelta: new Vector2(0f, 90f));
            kit.UseDisplayFont(hintText);
            UiKit.AddOutline(hintText, new Color(0f, 0f, 0f, 0.5f), 2f);

            var hintViewGo = new GameObject("ControlsHintView");
            hintViewGo.transform.SetParent(hudPanel.transform, false);
            ControlsHintView hintView = hintViewGo.AddComponent<ControlsHintView>();
            SceneBinding.Bind(hintView, "session", session);
            SceneBinding.Bind(hintView, "settings", settings);
            SceneBinding.Bind(hintView, "hintText", hintText);
        }
    }
}
