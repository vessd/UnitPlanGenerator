using System;
using System.ComponentModel;
using System.Threading.Tasks;

namespace UnitPlanGenerator.Common
{
    public sealed class NotifyTask : INotifyPropertyChanged
    {
        public Task Task { get; private set; }

        public Task TaskCompleted { get; private set; }

        public TaskStatus Status => Task.Status;

        public bool IsCompleted => Task.IsCompleted;

        public bool IsNotCompleted => !Task.IsCompleted;

        public bool IsSuccessfullyCompleted => Task.Status == TaskStatus.RanToCompletion;

        public bool IsCanceled => Task.IsCanceled;

        public bool IsFaulted => Task.IsFaulted;

        public AggregateException Exception => Task.Exception;

        public Exception InnerException => Exception?.InnerException;

        public string ErrorMessage => InnerException?.Message;

        public event PropertyChangedEventHandler PropertyChanged;

        private NotifyTask(Task task)
        {
            Task = task;
            TaskCompleted = MonitorTaskAsync(task);
        }

        private async Task MonitorTaskAsync(Task task)
        {
            try
            {
                await task;
            }
            catch
            { 
            }
            finally
            {
                NotifyProperties(task);
            }
        }

        private void NotifyProperties(Task task)
        {
            var propertyChanged = PropertyChanged;
            if (propertyChanged == null)
                return;

            if (task.IsCanceled)
            {
                propertyChanged(this, PropertyChangedEventArgsCache.Instance.Get(nameof(Status)));
                propertyChanged(this, PropertyChangedEventArgsCache.Instance.Get(nameof(IsCanceled)));
            }
            else if (task.IsFaulted)
            {
                propertyChanged(this, PropertyChangedEventArgsCache.Instance.Get(nameof(Exception)));
                propertyChanged(this, PropertyChangedEventArgsCache.Instance.Get(nameof(InnerException)));
                propertyChanged(this, PropertyChangedEventArgsCache.Instance.Get(nameof(ErrorMessage)));
                propertyChanged(this, PropertyChangedEventArgsCache.Instance.Get(nameof(Status)));
                propertyChanged(this, PropertyChangedEventArgsCache.Instance.Get(nameof(IsFaulted)));
            }
            else
            {
                propertyChanged(this, PropertyChangedEventArgsCache.Instance.Get(nameof(Status)));
                propertyChanged(this, PropertyChangedEventArgsCache.Instance.Get(nameof(IsSuccessfullyCompleted)));
            }
            propertyChanged(this, PropertyChangedEventArgsCache.Instance.Get(nameof(IsCompleted)));
            propertyChanged(this, PropertyChangedEventArgsCache.Instance.Get(nameof(IsNotCompleted)));
        }

        public static NotifyTask Crate(Task task)
        {
            return new NotifyTask(task);
        }

        public static NotifyTask<TResult> Crate<TResult>(Task<TResult> task, TResult defaultResult = default)
        {
            return new NotifyTask<TResult>(task, defaultResult);
        }

        public static NotifyTask Crate(Func<Task> asyncAction)
        {
            return Crate(asyncAction());
        }

        public static NotifyTask<TResult> Crate<TResult>(Func<Task<TResult>> asyncAction, TResult defaultResult = default)
        {
            return Crate(asyncAction(), defaultResult);
        }
    }

    public sealed class NotifyTask<TResult> : INotifyPropertyChanged
    {
        private readonly TResult _defaultResult;

        public Task<TResult> Task { get; private set; }

        public Task TaskCompleted { get; private set; }

        public TResult Result => IsSuccessfullyCompleted ? Task.Result : _defaultResult;

        public TaskStatus Status => Task.Status;

        public bool IsCompleted => Task.IsCompleted;

        public bool IsNotCompleted => !Task.IsCompleted;

        public bool IsSuccessfullyCompleted => Task.Status == TaskStatus.RanToCompletion;

        public bool IsCanceled => Task.IsCanceled;

        public bool IsFaulted => Task.IsFaulted;

        public AggregateException Exception => Task.Exception;

        public Exception InnerException => Exception?.InnerException;

        public string ErrorMessage => InnerException?.Message;

        public event PropertyChangedEventHandler PropertyChanged;

        public NotifyTask(Task<TResult> task, TResult defaultResult)
        {
            _defaultResult = defaultResult;
            Task = task;
            TaskCompleted = MonitorTaskAsync(task);
        }

        private async Task MonitorTaskAsync(Task task)
        {
            try
            {
                await task;
            }
            catch
            {
            }
            finally
            {
                NotifyProperties(task);
            }
        }

        private void NotifyProperties(Task task)
        {
            var propertyChanged = PropertyChanged;
            if (propertyChanged == null)
                return;

            if (task.IsCanceled)
            {
                propertyChanged(this, PropertyChangedEventArgsCache.Instance.Get(nameof(Status)));
                propertyChanged(this, PropertyChangedEventArgsCache.Instance.Get(nameof(IsCanceled)));
            }
            else if (task.IsFaulted)
            {
                propertyChanged(this, PropertyChangedEventArgsCache.Instance.Get(nameof(Exception)));
                propertyChanged(this, PropertyChangedEventArgsCache.Instance.Get(nameof(InnerException)));
                propertyChanged(this, PropertyChangedEventArgsCache.Instance.Get(nameof(ErrorMessage)));
                propertyChanged(this, PropertyChangedEventArgsCache.Instance.Get(nameof(Status)));
                propertyChanged(this, PropertyChangedEventArgsCache.Instance.Get(nameof(IsFaulted)));
            }
            else
            {
                propertyChanged(this, PropertyChangedEventArgsCache.Instance.Get(nameof(Result)));
                propertyChanged(this, PropertyChangedEventArgsCache.Instance.Get(nameof(Status)));
                propertyChanged(this, PropertyChangedEventArgsCache.Instance.Get(nameof(IsSuccessfullyCompleted)));
            }
            propertyChanged(this, PropertyChangedEventArgsCache.Instance.Get(nameof(IsCompleted)));
            propertyChanged(this, PropertyChangedEventArgsCache.Instance.Get(nameof(IsNotCompleted)));
        }
    }
}
