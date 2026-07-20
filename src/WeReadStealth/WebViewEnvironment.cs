using Microsoft.Web.WebView2.Core;

namespace WeReadStealth
{
    internal static class WebViewEnvironment
    {
        internal static CoreWebView2EnvironmentOptions CreateOptions()
        {
            CoreWebView2EnvironmentOptions options = new CoreWebView2EnvironmentOptions
            {
                AdditionalBrowserArguments = "--hide-scrollbars",
                ScrollBarStyle = CoreWebView2ScrollbarStyle.Default
            };
            return options;
        }
    }
}
