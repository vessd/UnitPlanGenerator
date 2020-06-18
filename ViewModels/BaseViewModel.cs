using System.ComponentModel;
using System.Runtime.CompilerServices;
using UnitPlanGenerator.Common;
using Validar;

namespace UnitPlanGenerator.ViewModels
{
    [InjectValidation]
    public abstract class BaseViewModel : INotifyPropertyChanged
    {
        protected readonly ValidationTemplate _validationTemplate;

        public event PropertyChangedEventHandler PropertyChanged;

        public BaseViewModel()
        {
            _validationTemplate = new ValidationTemplate(this);
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null) =>
            PropertyChanged?.Invoke(this, PropertyChangedEventArgsCache.Instance.Get(propertyName));
    }
}
