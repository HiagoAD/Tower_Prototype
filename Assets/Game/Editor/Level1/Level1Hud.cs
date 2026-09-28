using Game.Core;
using Game.Gameplay;
using Game.Presentation;
using Game.Webhook;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Editor
{
    /// <summary>Level 1's in-game HUD: altitude meter, controls hint and the two event-card columns.</summary>
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
        /// controls hint, both event-card columns and the HudView that drives them. The panel is
        /// shrunk to Screen.safeArea so every top/bottom-anchored child clears a camera cutout.
        /// </summary>
        public static GameObject Build(UiKit kit, RectTransform canvasRect, GameSession session, Sprite gloveSprite)
        {
            GameObject hudPanel = UiKit.BuildPanel(canvasRect, "HudPanel");
            hudPanel.SetActive(false);
            // The test phone has a camera cutout; shrink the whole HUD to Screen.safeArea so every
            // top/bottom-anchored child below clears it instead of drawing under the notch.
            hudPanel.AddComponent<SafeAreaFitter>();

            ProgressBarParts bar = BuildProgressBar(kit, hudPanel.transform);

            BuildControlsHint(kit, hudPanel, session);
            BuildEventFeed(hudPanel, session, gloveSprite, BumpPolarity.Positive);
            BuildEventFeed(hudPanel, session, gloveSprite, BumpPolarity.Negative);

            var hudViewGo = new GameObject("HudView");
            hudViewGo.transform.SetParent(hudPanel.transform, false);
            HudView hudView = hudViewGo.AddComponent<HudView>();
            SceneBinding.Bind(hudView, "session", session);
            SceneBinding.Bind(hudView, "progressFill", bar.Fill);
            SceneBinding.Bind(hudView, "progressMarker", bar.Marker);
            SceneBinding.Bind(hudView, "heightLabel", bar.HeightLabel);
            SceneBinding.Bind(hudView, "finishLabel", bar.FinishLabel);
            return hudPanel;
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
        private static void BuildEventFeed(GameObject hudPanel, GameSession session, Sprite gloveSprite, BumpPolarity polarity)
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
        /// (ClimbInputSource.TouchRegionNormalizedHeight, not a copy of the value). No band graphic:
        /// the reference frame is clean sky. Never a raycast target. Fades out once the player has
        /// climbed a little (see ControlsHintView).
        /// </summary>
        private static void BuildControlsHint(UiKit kit, GameObject hudPanel, GameSession session)
        {
            float regionFraction = ClimbInputSource.TouchRegionNormalizedHeight;

            Text hintText = UiKit.AddText(hudPanel.transform, "ClimbHintText", "Hold below to climb - release to grip", 32, TextAnchor.LowerCenter,
                new Vector2(0f, 20f), anchorMin: new Vector2(0f, 0f), anchorMax: new Vector2(1f, regionFraction), pivot: new Vector2(0.5f, 0f), sizeDelta: new Vector2(0f, 90f));
            kit.UseDisplayFont(hintText);
            UiKit.AddOutline(hintText, new Color(0f, 0f, 0f, 0.5f), 2f);

            var hintViewGo = new GameObject("ControlsHintView");
            hintViewGo.transform.SetParent(hudPanel.transform, false);
            ControlsHintView hintView = hintViewGo.AddComponent<ControlsHintView>();
            SceneBinding.Bind(hintView, "session", session);
            SceneBinding.Bind(hintView, "hintText", hintText);
        }
    }
}
