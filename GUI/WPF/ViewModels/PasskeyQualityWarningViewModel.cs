using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using Upsilon.Apps.Passkey.GUI.WPF.Helper;
using Upsilon.Apps.Passkey.GUI.WPF.Localization;
using Upsilon.Apps.Passkey.GUI.WPF.Services;
using Upsilon.Apps.Passkey.Interfaces.Models;

namespace Upsilon.Apps.Passkey.GUI.WPF.ViewModels
{
   internal sealed class PasskeyQualityWarningViewModel : INotifyPropertyChanged, ILanguageAware, IDisposable
   {
      [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Instance property so WPF can refresh Title on language change.")]
      public string Title => Strings.Format(nameof(Strings.Title_PasskeyQualityWarningsWindow), AppInfo.Title);

      public PasskeyQualityIssueItemViewModel[] Issues { get; private set; }

      public event PropertyChangedEventHandler? PropertyChanged;

      public PasskeyQualityWarningViewModel()
      {
         AppServices.Session.Warnings.NotifiedWarningsChanged += _warnings_NotifiedWarningsChanged;
         Issues = _loadIssues();
      }

      public void OnLanguageChanged()
         => _reloadIssues(alsoTitle: true);

      public void Dispose()
      {
         AppServices.Session.Warnings.NotifiedWarningsChanged -= _warnings_NotifiedWarningsChanged;
      }

      private void _warnings_NotifiedWarningsChanged(object? sender, EventArgs e)
         => _reloadIssues(alsoTitle: false);

      private void _reloadIssues(bool alsoTitle)
      {
         Issues = _loadIssues();
         PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Issues)));
         if (alsoTitle)
         {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Title)));
         }
      }

      private static PasskeyQualityIssueItemViewModel[] _loadIssues()
      {
         List<PasskeyQualityIssueItemViewModel> items = [];

         foreach (IInsufficientPasskeysWarning warning in AppServices.Session.Warnings
            .GetNotifiedWarnings(WarningKinds.InsufficientPasskeys)
            .OfType<IInsufficientPasskeysWarning>())
         {
            items.Add(new(
               Strings.Label_NotifyInsufficientPasskeys,
               Strings.Format(nameof(Strings.Msg_PasskeyQuality_InsufficientPasskeys), warning.Count, warning.RecommendedMinimum)));
         }

         foreach (IWeakPasskeyWarning warning in AppServices.Session.Warnings
            .GetNotifiedWarnings(WarningKinds.WeakPasskey)
            .OfType<IWeakPasskeyWarning>())
         {
            string indexes = string.Join(", ", warning.PasskeyIndexes.Select(i => i + 1));
            items.Add(new(
               Strings.Label_NotifyWeakPasskey,
               Strings.Format(nameof(Strings.Msg_PasskeyQuality_WeakPasskey), indexes)));
         }

         foreach (IPasskeyLeakedWarning warning in AppServices.Session.Warnings
            .GetNotifiedWarnings(WarningKinds.PasskeyLeaked)
            .OfType<IPasskeyLeakedWarning>())
         {
            string indexes = string.Join(", ", warning.PasskeyIndexes.Select(i => i + 1));
            items.Add(new(
               Strings.Label_NotifyPasskeyLeaked,
               Strings.Format(nameof(Strings.Msg_PasskeyQuality_PasskeyLeaked), indexes)));
         }

         return [.. items];
      }
   }

   internal sealed class PasskeyQualityIssueItemViewModel(string title, string description)
   {
      public string Title { get; } = title;
      public string Description { get; } = description;
   }
}
