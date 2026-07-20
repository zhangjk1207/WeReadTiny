using System;
using System.Drawing;
using System.Globalization;

namespace WeReadStealth
{
    internal static class WebViewPageBridge
    {
        private const string BackgroundPrefix = "wrs-bg:#";

        internal const string BackgroundWatcherScript = @"(() => {
    let lastMessage = '';
    let updateTimer = 0;

    const parseColor = (value) => {
        const match = value && value.match(/rgba?\(([^)]+)\)/i);
        if (!match) return null;
        const parts = match[1].split(',').map((part) => Number.parseFloat(part.trim()));
        if (parts.length < 3 || parts.slice(0, 3).some(Number.isNaN)) return null;
        if (parts.length > 3 && parts[3] < 0.1) return null;
        return parts.slice(0, 3).map((part) => Math.max(0, Math.min(255, Math.round(part))));
    };

    const findBackgroundAt = (y) => {
        let element = document.elementFromPoint(
            Math.max(1, window.innerWidth - 40),
            Math.max(1, Math.min(window.innerHeight - 1, Math.floor(y))));
        while (element) {
            const color = parseColor(getComputedStyle(element).backgroundColor);
            if (color) return color;
            element = element.parentElement;
        }
        for (const fallback of [document.body, document.documentElement]) {
            if (fallback) {
                const color = parseColor(getComputedStyle(fallback).backgroundColor);
                if (color) return color;
            }
        }
        return null;
    };

    const findBackground = () => {
        const samples = [0.15, 0.5, 0.85]
            .map((ratio) => findBackgroundAt(window.innerHeight * ratio))
            .filter(Boolean);
        if (!samples.length) {
            return matchMedia('(prefers-color-scheme: dark)').matches ? [30, 30, 30] : [255, 255, 255];
        }

        samples.sort((left, right) => {
            const leftLuminance = left[0] * 299 + left[1] * 587 + left[2] * 114;
            const rightLuminance = right[0] * 299 + right[1] * 587 + right[2] * 114;
            return leftLuminance - rightLuminance;
        });
        return samples[Math.floor(samples.length / 2)];
    };

    const sendBackground = () => {
        updateTimer = 0;
        const color = findBackground();
        const hex = color.map((part) => part.toString(16).padStart(2, '0')).join('');
        const message = 'wrs-bg:#' + hex;
        if (message !== lastMessage) {
            lastMessage = message;
            chrome.webview.postMessage(message);
        }
    };

    const scheduleUpdate = () => {
        clearTimeout(updateTimer);
        updateTimer = setTimeout(sendBackground, 80);
    };

    const start = () => {
        new MutationObserver(scheduleUpdate).observe(document.documentElement, {
            attributes: true,
            subtree: true,
            attributeFilter: ['class', 'style']
        });
        addEventListener('resize', scheduleUpdate, { passive: true });
        addEventListener('mousemove', scheduleUpdate, { passive: true });
        sendBackground();
    };

    if (document.readyState === 'loading') {
        addEventListener('DOMContentLoaded', start, { once: true });
    } else {
        start();
    }
})();";

        internal static bool TryParseBackgroundMessage(string message, out Color color)
        {
            color = Color.Empty;
            if (message == null || message.Length != BackgroundPrefix.Length + 6 || !message.StartsWith(BackgroundPrefix, StringComparison.Ordinal))
            {
                return false;
            }

            if (!int.TryParse(message.Substring(BackgroundPrefix.Length), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int rgb))
            {
                return false;
            }

            color = Color.FromArgb((rgb >> 16) & 0xff, (rgb >> 8) & 0xff, rgb & 0xff);
            return true;
        }
    }
}
