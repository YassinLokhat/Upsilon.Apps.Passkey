using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using Upsilon.Apps.Passkey.GUI.Helper;
using Upsilon.Apps.Passkey.Interfaces.Enums;
using Upsilon.Apps.Passkey.Interfaces.Models;

namespace Upsilon.Apps.Passkey.GUI.ViewModels.Controls
{
   internal class LogViewModel(ILog log) : INotifyPropertyChanged
   {
      private readonly ILog _log = log;
      public string DateTime => _log.DateTime.ToString("yyyy-MM-dd HH:mm");
      public string EventType => _log.EventType.ToReadableString();
      public string Message => _log.Message;
      public bool NeedsReview
      {
         get => _log.NeedsReview;
         set
         {
            if (_log.NeedsReview != value)
            {
               _log.NeedsReview = value;
               OnPropertyChanged(nameof(NeedsReview));
               OnPropertyChanged(nameof(NeedsReviewString));
            }
         }
      }
      public string NeedsReviewString => NeedsReview ? "Needs review" : "Reviewed";

      public event PropertyChangedEventHandler? PropertyChanged;

      protected virtual void OnPropertyChanged(string propertyName)
      {
         PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
      }

      public bool MeetsConditions(DateTime fromDateFilter, DateTime toDateFilter, LogEventType eventType, string message, bool needsReview)
      {
         if (fromDateFilter <= System.DateTime.Now.Date
            && _log.DateTime.Date < fromDateFilter)
         {
            return false;
         }

         if (toDateFilter <= System.DateTime.Now.Date
            && _log.DateTime.Date > toDateFilter)
         {
            return false;
         }

         if (eventType != LogEventType.None
            && _log.EventType != eventType)
         {
            return false;
         }

         if (needsReview
            && !_log.NeedsReview)
         {
            return false;
         }

         return _log.Message.Contains(message, StringComparison.CurrentCultureIgnoreCase);
      }
   }
}