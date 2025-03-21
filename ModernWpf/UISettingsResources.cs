using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Win32;

#if NET5_0_OR_NEWER
using Windows.Foundation.Metadata; // For ApiInformation checks
using Windows.UI.ViewManagement;   // For UISettings
#endif

namespace ModernWpf
{
    internal class UISettingsResources : ResourceDictionary
    {
        private const string AutoHideScrollBarsKey = "AutoHideScrollBars";

#if NET5_0_OR_NEWER
        private const string UniversalApiContractName = "Windows.Foundation.UniversalApiContract";
        private UISettings _uiSettings;
#endif

        private readonly Dispatcher _dispatcher = Dispatcher.CurrentDispatcher;

        public UISettingsResources()
        {
            if (DesignMode.DesignModeEnabled)
            {
                return;
            }

            if (OSVersionHelper.IsWindows10OrGreater)
            {
                Initialize();
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void Initialize()
        {
#if NET5_0_OR_NEWER
            // ---------------------------
            // NEW APPROACH (UISettings-based)
            // ---------------------------
            SystemEvents.UserPreferenceChanged += (sender, args) =>
            {
                if (args.Category == UserPreferenceCategory.General)
                {
                    // This might affect advanced effects or scrollbars as well
                    ApplyAdvancedEffectsEnabled();
                    ApplyAutoHideScrollBars();
                }
            };

            // Instantiate UISettings
            _uiSettings = new UISettings();

            // If you want to hook advanced effects changes
            if (ApiInformation.IsApiContractPresent(UniversalApiContractName, 4))
            {
                InitializeForContract4();
            }

            // If you want to hook auto-hide scrollbars changes
            if (ApiInformation.IsApiContractPresent(UniversalApiContractName, 8))
            {
                InitializeForContract8();
            }

            // Apply initial settings
            ApplyAdvancedEffectsEnabled();
            ApplyAutoHideScrollBars();

#else
            // ---------------------------
            // OLD APPROACH (Registry-based)
            // ---------------------------
            SystemEvents.UserPreferenceChanged += (sender, args) =>
            {
                if (args.Category == UserPreferenceCategory.General)
                {
                    ApplyAdvancedEffectsEnabled();
                    ApplyAutoHideScrollBars();
                }
            };

            ApplyAdvancedEffectsEnabled();
            ApplyAutoHideScrollBars();
#endif
        }

#if NET5_0_OR_NEWER
        // ---------------------------------------------------------------------
        // .NET 5+ or newer code paths that use UISettings (WinRT) functionality
        // ---------------------------------------------------------------------

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void InitializeForContract4()
        {
            // Hook the UISettings event for advanced effects
            _uiSettings.AdvancedEffectsEnabledChanged += (sender, args) =>
            {
                _dispatcher.BeginInvoke(ApplyAdvancedEffectsEnabled);
            };

            // For packaged apps, you might still want to listen to registry changes:
            if (PackagedAppHelper.IsPackagedApp)
            {
                SystemEvents.UserPreferenceChanged += (sender, args) =>
                {
                    if (args.Category == UserPreferenceCategory.General)
                    {
                        ApplyAdvancedEffectsEnabled();
                    }
                };
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void InitializeForContract8()
        {
            // Hook the UISettings event for auto-hide scrollbars
            _uiSettings.AutoHideScrollBarsChanged += (sender, args) =>
            {
                _dispatcher.BeginInvoke(ApplyAutoHideScrollBars);
            };
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void ApplyAdvancedEffectsEnabled()
        {
            var key = SystemParameters.DropShadowKey;
            if (_uiSettings.AdvancedEffectsEnabled)
            {
                Remove(key);
            }
            else
            {
                this[key] = false;
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void ApplyAutoHideScrollBars()
        {
            this[AutoHideScrollBarsKey] = _uiSettings.AutoHideScrollBars;
        }

#else
        // ---------------------------------------------------------------------
        // Older .NET Framework or .NET Core 3.x code paths (registry-based)
        // ---------------------------------------------------------------------

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void ApplyAdvancedEffectsEnabled()
        {
            var key = SystemParameters.DropShadowKey;
            if (SystemEffectsHelper.AreAdvancedEffectsEnabled())
            {
                Remove(key);
            }
            else
            {
                this[key] = false;
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void ApplyAutoHideScrollBars()
        {
            this[AutoHideScrollBarsKey] = SystemScrollBarHelper.AreScrollBarsHidden();
        }

        internal static class SystemEffectsHelper
        {
            private const string DwmRegistryKey = @"HKEY_CURRENT_USER\Software\Microsoft\Windows\DWM";
            private const string EnableBlurBehindValue = "EnableBlurBehind";

            public static bool AreAdvancedEffectsEnabled()
            {
                object registryValue = Registry.GetValue(DwmRegistryKey, EnableBlurBehindValue, 1);
                return registryValue is int intValue && intValue == 1;
            }
        }

        internal static class SystemScrollBarHelper
        {
            private const string AccessibilityRegistryKey = @"HKEY_CURRENT_USER\Control Panel\Accessibility";
            private const string DynamicScrollbarsValue = "DynamicScrollbars";

            public static bool AreScrollBarsHidden()
            {
                object registryValue = Registry.GetValue(AccessibilityRegistryKey, DynamicScrollbarsValue, 0);
                return registryValue is int intValue && intValue == 1;
            }
        }
#endif
    }
}
