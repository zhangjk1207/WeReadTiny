using System;

namespace WeReadStealth
{
    internal static class ToolbarVisibilityPolicy
    {
        internal const int RevealEdgeHeight = 8;
        internal const int InteractionHeight = 44;
        internal static readonly TimeSpan HideDelay = TimeSpan.FromMilliseconds(1200);

        internal static bool ShouldShow(
            bool autoHide,
            bool currentlyVisible,
            bool cursorInsideWindow,
            int cursorY,
            TimeSpan timeSinceInteraction)
        {
            if (!autoHide)
            {
                return true;
            }

            if (cursorInsideWindow && cursorY >= 0 && cursorY <= RevealEdgeHeight)
            {
                return true;
            }

            if (currentlyVisible && cursorInsideWindow && cursorY >= 0 && cursorY <= InteractionHeight)
            {
                return true;
            }

            return timeSinceInteraction < HideDelay;
        }
    }
}
