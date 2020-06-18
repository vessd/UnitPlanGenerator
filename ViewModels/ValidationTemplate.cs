using FluentValidation;
using FluentValidation.Results;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using UnitPlanGenerator.Common;

namespace UnitPlanGenerator.ViewModels
{
    public class ValidationTemplate : IDataErrorInfo, INotifyDataErrorInfo, INotifyPropertyChanged
    {
        private static readonly ConcurrentDictionary<RuntimeTypeHandle, IValidator> validators;
        private readonly INotifyPropertyChanged target;
        private readonly IValidator validator;
        private ValidationResult validationResult;

        static ValidationTemplate()
        {
            validators = new ConcurrentDictionary<RuntimeTypeHandle, IValidator>();
            ValidatorOptions.CascadeMode = CascadeMode.StopOnFirstFailure;
        }

        public ValidationTemplate(INotifyPropertyChanged target)
        {
            this.target = target;
            validator = GetValidator(target.GetType());
            validationResult = validator.Validate(target);
            target.PropertyChanged += Validate;
        }

        private static IValidator GetValidator(Type modelType)
        {
            if (!validators.TryGetValue(modelType.TypeHandle, out var validator))
            {
                var typeName = string.Format("{0}.{1}Validator", typeof(ValidationTemplate).Namespace, modelType.Name);
                var type = modelType.Assembly.GetType(typeName, true);
                validators[modelType.TypeHandle] = validator = (IValidator)Activator.CreateInstance(type);
            }
            return validator;
        }

        private void Validate(object sender, PropertyChangedEventArgs e)
        {
            validationResult = validator.Validate(target);
            OnPropertyChanged(nameof(HasErrors));
            OnPropertyChanged(nameof(Error));
            foreach (var error in validationResult.Errors)
            {
                RaiseErrorsChanged(error.PropertyName);
            }
        }

        public IEnumerable GetErrors(string propertyName)
        {
            return validationResult.Errors
                                   .Where(x => x.PropertyName == propertyName)
                                   .Select(x => x.ErrorMessage);
        }

        public bool HasErrors => validationResult.Errors.Count > 0;

        public string Error
        {
            get
            {
                var strings = validationResult.Errors
                                              .Select(x => x.ErrorMessage)
                                              .ToArray();
                return string.Join(Environment.NewLine, strings);
            }
        }

        public string this[string propertyName]
        {
            get
            {
                var strings = validationResult.Errors
                                              .Where(x => x.PropertyName == propertyName)
                                              .Select(x => x.ErrorMessage)
                                              .ToArray();
                return string.Join(Environment.NewLine, strings);
            }
        }

        public event EventHandler<DataErrorsChangedEventArgs> ErrorsChanged;

        private void RaiseErrorsChanged(string propertyName) => ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string propertyName = null) =>
            PropertyChanged?.Invoke(this, PropertyChangedEventArgsCache.Instance.Get(propertyName));
    }
}
