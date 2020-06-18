using UnitPlanGenerator.Views;
using Prism.Ioc;
using System.Windows;
using UnitPlanGenerator.Services.Interfaces;
using UnitPlanGenerator.Services;
using UnitPlanGenerator.ViewModels;

namespace UnitPlanGenerator
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App
    {
        private const string AutoHideScrollBarsKey = "AutoHideScrollBars";

        protected override Window CreateShell()
        {
            Current.Resources[AutoHideScrollBarsKey] = true;
            return Container.Resolve<MainWindow>();
        }

        protected override void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.Register<IUserCredentialService, UserCredentialService>();
            containerRegistry.Register<IPasswordService, PasswordService>();
            containerRegistry.Register<IPasswordHasher, PasswordHasher>();
            containerRegistry.Register<ICurriculumImportService, CurriculumImportService>();
            containerRegistry.Register<IDialogService, DialogService>();
            containerRegistry.RegisterSingleton<IDatabaseService, DatabaseService>();
            containerRegistry.RegisterSingleton<IIdentityService, IdentityService>();

            containerRegistry.RegisterSingleton<MainWindowViewModel>();
            containerRegistry.RegisterSingleton<SettingsPageViewModel>();
            containerRegistry.RegisterSingleton<UserManagerPageViewModel>();
            containerRegistry.RegisterSingleton<CurriculumPageViewModel>();
            containerRegistry.RegisterSingleton<ImportCurriculumPageViewModel>();
            containerRegistry.RegisterSingleton<AddSemesterPageViewModel>();
        }
    }
}
