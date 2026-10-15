using System;

namespace EveryonePicks.Couch
{
    /// <summary>
    /// One lane's card selection, driven by that player's horizontal stick/keys. A push past the
    /// deadzone moves once at once, then repeats after <see cref="FirstRepeat"/> every
    /// <see cref="Repeat"/> seconds while held; returning to centre re-arms it. Wraps at the ends,
    /// like a carousel, so a row of five is never more than two moves from any card.
    /// </summary>
    internal sealed class LaneCursor
    {
        internal const float Deadzone = 0.5f;
        internal const float FirstRepeat = 0.35f;
        internal const float Repeat = 0.12f;

        private int count;
        private int heldDir;
        private float heldFor;
        private float nextRepeatAt;

        internal int Selected { get; private set; }

        internal LaneCursor(int count, int start = -1)
        {
            Reset(count, start);
        }

        /// <summary>New hand (a fresh row after an extra pick): selection starts in the middle.</summary>
        internal void Reset(int count, int start = -1)
        {
            if (count < 1) throw new ArgumentOutOfRangeException(nameof(count));
            this.count = count;
            Selected = start >= 0 && start < count ? start : count / 2;
            heldDir = 0;
            heldFor = 0f;
        }

        /// <summary>Feed one frame of input. Returns true if the selection changed.</summary>
        internal bool Update(float x, float dt)
        {
            int dir = x > Deadzone ? 1 : x < -Deadzone ? -1 : 0;
            if (dir == 0) { heldDir = 0; return false; }

            if (dir != heldDir)
            {
                heldDir = dir;
                heldFor = 0f;
                nextRepeatAt = FirstRepeat;
                return Step(dir);
            }

            heldFor += dt;
            bool moved = false;
            // A long frame (hitch) may owe several repeats; deliver them all so holding feels the
            // same at any frame rate.
            while (heldFor >= nextRepeatAt)
            {
                nextRepeatAt += Repeat;
                moved |= Step(dir);
            }
            return moved;
        }

        private bool Step(int dir)
        {
            if (count < 2) return false;
            Selected = ((Selected + dir) % count + count) % count;
            return true;
        }
    }
}
