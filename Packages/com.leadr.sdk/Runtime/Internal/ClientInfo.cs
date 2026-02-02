using UnityEngine;

namespace Leadr.Internal
{
    internal static class ClientInfo
    {
        internal const string SdkVersion = "0.1.2";

        private const string ClientId = "sdk-unity";

        private static string _leadrClientHeader;
        private static string _userAgentHeader;

        public static string LeadrClientHeader
        {
            get
            {
                if (_leadrClientHeader == null)
                    Build();
                return _leadrClientHeader;
            }
        }

        public static string UserAgentHeader
        {
            get
            {
                if (_userAgentHeader == null)
                    Build();
                return _userAgentHeader;
            }
        }

        private static void Build()
        {
            var runtime = $"unity-{Application.unityVersion}";
            var platform = MapPlatform(Application.platform);
            var arch = GetArch();
            var scriptingBackend = GetScriptingBackend();

            _leadrClientHeader = $"{ClientId}; v={SdkVersion}; runtime={runtime}; platform={platform}; arch={arch}";
            if (_leadrClientHeader.Length > 256)
                _leadrClientHeader = _leadrClientHeader.Substring(0, 256);

            _userAgentHeader = $"LEADR-SDK-Unity/{SdkVersion} (Unity {Application.unityVersion}; {scriptingBackend}; {platform})";
        }

        private static string MapPlatform(RuntimePlatform runtimePlatform)
        {
            switch (runtimePlatform)
            {
                case RuntimePlatform.WindowsPlayer:
                case RuntimePlatform.WindowsEditor:
                    return "windows";
                case RuntimePlatform.OSXPlayer:
                case RuntimePlatform.OSXEditor:
                    return "darwin";
                case RuntimePlatform.LinuxPlayer:
                case RuntimePlatform.LinuxEditor:
                    return "linux";
                case RuntimePlatform.Android:
                    return "android";
                case RuntimePlatform.IPhonePlayer:
                    return "ios";
                case RuntimePlatform.WebGLPlayer:
                    return "webgl";
                case RuntimePlatform.PS4:
                    return "ps4";
                case RuntimePlatform.PS5:
                    return "ps5";
                case RuntimePlatform.Switch:
                    return "switch";
                case RuntimePlatform.XboxOne:
                    return "xboxone";
                default:
                    return runtimePlatform.ToString().ToLowerInvariant();
            }
        }

        private static string GetArch()
        {
            var processorType = SystemInfo.processorType.ToLowerInvariant();
            bool is64Bit = System.Environment.Is64BitProcess;

            if (processorType.Contains("arm") || processorType.Contains("aarch"))
                return is64Bit ? "arm64" : "arm";

            return is64Bit ? "x64" : "x86";
        }

        private static string GetScriptingBackend()
        {
#if ENABLE_IL2CPP
            return "IL2CPP";
#else
            return "Mono";
#endif
        }
    }
}
