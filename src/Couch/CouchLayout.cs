using System;

namespace EveryonePicks.Couch
{
    /// <summary>A rectangle in normalised screen space: (0,0) top left, (1,1) bottom right.</summary>
    internal struct NRect
    {
        public float x, y, w, h;
        public NRect(float x, float y, float w, float h) { this.x = x; this.y = y; this.w = w; this.h = h; }
        public float CenterX => x + w * 0.5f;
        public float CenterY => y + h * 0.5f;
        public override string ToString() => "(" + x + "," + y + " " + w + "x" + h + ")";
    }

    /// <summary>
    /// Where each local player's lane sits on the couch board, and where its cards go inside it.
    /// Pure arithmetic with no Unity types, so it is unit-tested without the game (tests/CouchTests).
    /// </summary>
    internal static class CouchLayout
    {
        internal const int MaxLanes = 9;

        /// <summary>2 → two full-width rows, 3–4 → 2×2, 5–9 → 3×3. One lane fills the screen.</summary>
        internal static void Grid(int lanes, out int cols, out int rows)
        {
            if (lanes < 1 || lanes > MaxLanes) throw new ArgumentOutOfRangeException(nameof(lanes));
            if (lanes == 1) { cols = 1; rows = 1; }
            else if (lanes == 2) { cols = 1; rows = 2; }
            else if (lanes <= 4) { cols = 2; rows = 2; }
            else { cols = 3; rows = 3; }
        }

        /// <summary>
        /// Lane <paramref name="index"/>'s region, row-major in player order so a player's region
        /// stays put from round to round. <paramref name="gap"/> is the normalised margin between
        /// regions and around the edge.
        /// </summary>
        internal static NRect Region(int index, int lanes, float gap = 0.01f)
        {
            Grid(lanes, out int cols, out int rows);
            if (index < 0 || index >= lanes) throw new ArgumentOutOfRangeException(nameof(index));
            int c = index % cols, r = index / cols;
            float w = (1f - gap * (cols + 1)) / cols;
            float h = (1f - gap * (rows + 1)) / rows;
            return new NRect(gap + c * (w + gap), gap + r * (h + gap), w, h);
        }

        /// <summary>
        /// Card centres for one row of <paramref name="cards"/> cards inside <paramref name="region"/>,
        /// and the scale relative to a full-screen hand. A card is <paramref name="cardAspect"/>
        /// (width/height) and <paramref name="screenAspect"/> converts normalised widths to heights.
        /// The row is centred; a label strip of <paramref name="labelFrac"/> of the region height is
        /// kept free at the top for the player's name.
        /// </summary>
        internal static float CardRow(NRect region, int cards, float screenAspect, float cardAspect,
                                      float[] centersX, out float centerY, float labelFrac = 0.18f,
                                      float spacing = 0.12f)
        {
            if (cards < 1) throw new ArgumentOutOfRangeException(nameof(cards));
            if (centersX == null || centersX.Length < cards) throw new ArgumentException("centersX too short");

            float usableH = region.h * (1f - labelFrac);
            centerY = region.y + region.h * labelFrac + usableH * 0.5f;

            // Card height in normalised screen height, limited by both the free height (90%) and
            // the width all cards plus spacing need; widths in normalised units are h*aspect/screenAspect.
            float hByHeight = usableH * 0.9f;
            float widthPerCardUnit = cardAspect / screenAspect;
            float hByWidth = region.w * 0.95f / (cards * widthPerCardUnit + (cards - 1) * spacing * widthPerCardUnit);
            float cardH = Math.Min(hByHeight, hByWidth);

            float cardW = cardH * widthPerCardUnit;
            float step = cardW * (1f + spacing);
            float first = region.CenterX - step * (cards - 1) * 0.5f;
            for (int i = 0; i < cards; i++) centersX[i] = first + step * i;

            // Relative to the vanilla hand, which this treats as cards of the same count spanning a
            // full screen with no label strip.
            float fullH = Math.Min(0.9f, 0.95f / (cards * widthPerCardUnit + (cards - 1) * spacing * widthPerCardUnit));
            return cardH / fullH;
        }
    }
}
