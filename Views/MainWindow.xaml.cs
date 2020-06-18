using ModernWpf.Controls;
using ModernWpf.Navigation;
using System;
using System.Linq;
using System.Windows;
using UnitPlanGenerator.Services;

namespace UnitPlanGenerator.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            NavigationService.Frame = ContentFrame;
            NavigationService.Navigated += NavigationService_Navigated;
        }

        private void NavigationService_Navigated(object sender, System.Windows.Navigation.NavigationEventArgs e)
        {
            if (e.SourcePageType() == typeof(SettingsPage))
            {
                NavView.SelectedItem = NavView.SettingsItem;
            }
            else
            {
                NavView.SelectedItem = NavView.MenuItems.OfType<NavigationViewItem>().FirstOrDefault(x => GetPageType(x) == e.SourcePageType());
            }
        }

        private void NavView_BackRequested(NavigationView sender, NavigationViewBackRequestedEventArgs args)
        {
            NavigationService.GoBack();
        }

        private void NavView_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
        {
            if (args.IsSettingsInvoked)
            {
                NavigationService.Navigate(typeof(SettingsPage));
            }
            else
            {
                if (args.InvokedItemContainer is NavigationViewItem menuItem)
                {
                    var pageType = GetPageType(menuItem);
                    NavigationService.Navigate(pageType);
                }
            }
        }

        private Type GetPageType(NavigationViewItem item)
        {
            return item.Tag as Type;
        }
    }
}
