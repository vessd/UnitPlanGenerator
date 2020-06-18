using ModernWpf.Controls;
using ModernWpf.Media.Animation;
using System;
using System.Windows.Navigation;

namespace UnitPlanGenerator.Services
{
    public static class NavigationService
    {
        public static event NavigatedEventHandler Navigated;

        public static event NavigationFailedEventHandler NavigationFailed;

        private static bool _clearJournal;

        private static Frame _frame;

        public static Frame Frame
        {
            get
            {
                if (_frame == null)
                {
                    _frame = new Frame();
                    RegisterFrameEvents();
                }

                return _frame;
            }

            set
            {
                UnregisterFrameEvents();
                _frame = value;
                RegisterFrameEvents();
            }
        }

        public static bool CanGoBack => Frame.CanGoBack;

        public static bool CanGoForward => Frame.CanGoForward;

        public static bool GoBack()
        {
            if (CanGoBack)
            {
                Frame.GoBack();
                return true;
            }

            return false;
        }

        public static void GoForward() => Frame.GoForward();

        public static bool Navigate(Type pageType, bool clearJournal = false)
        {
            return Frame.Dispatcher.Invoke(() =>
            {
                if (Frame.Content?.GetType() != pageType)
                {
                    _clearJournal = clearJournal;
                    return Frame.Navigate(pageType);
                }
                else if (clearJournal)
                {
                    ClearJournal();
                }

                return false;
            });
        }

        public static bool Navigate<T>() where T : Page => Navigate(typeof(T));

        private static void RegisterFrameEvents()
        {
            if (_frame != null)
            {
                _frame.Navigated += Frame_Navigated;
                _frame.NavigationFailed += Frame_NavigationFailed;
            }
        }

        private static void UnregisterFrameEvents()
        {
            if (_frame != null)
            {
                _frame.Navigated -= Frame_Navigated;
                _frame.NavigationFailed -= Frame_NavigationFailed;
            }
        }

        private static void Frame_NavigationFailed(object sender, NavigationFailedEventArgs e) => NavigationFailed?.Invoke(sender, e);

        private static void Frame_Navigated(object sender, NavigationEventArgs e)
        {
            if (_clearJournal)
            {
                ClearJournal();
            }
            Navigated?.Invoke(sender, e);
        }

        private static void ClearJournal()
        {
            Frame.Dispatcher.Invoke(() =>
            {
                _clearJournal = false;
                var entry = Frame.RemoveBackEntry();
                while (entry != null)
                {
                    entry = Frame.RemoveBackEntry();
                }
            });
        }
    }
}
