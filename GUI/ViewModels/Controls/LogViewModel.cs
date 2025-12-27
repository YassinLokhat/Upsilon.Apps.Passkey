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
      public readonly ILog Log = log;
      public string DateTime => Log.DateTime.ToString("yyyy-MM-dd HH:mm");
      public string EventType => Log.EventType.ToReadableString();
      public string Message => Log.Message;
      public bool NeedsReview
      {
         get => Log.NeedsReview;
         set
         {
            if (Log.NeedsReview != value)
            {
               Log.NeedsReview = value;
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
            && Log.DateTime.Date < fromDateFilter)
         {
            return false;
         }

         if (toDateFilter <= System.DateTime.Now.Date
            && Log.DateTime.Date > toDateFilter)
         {
            return false;
         }

         if (eventType != LogEventType.None
            && Log.EventType != eventType)
         {
            return false;
         }

         if (needsReview
            && !Log.NeedsReview)
         {
            return false;
         }

         return Log.Message.Contains(message, StringComparison.CurrentCultureIgnoreCase);
      }
   }
}