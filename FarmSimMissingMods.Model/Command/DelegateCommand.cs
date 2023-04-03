using System.Windows.Input;

namespace FarmSimMissingMods.Model.Command
{
    public class DelegateCommand : ICommand
    {
        private readonly Predicate<object> m_CanExecute;
        private readonly Action<object> m_Execute;

        public event EventHandler CanExecuteChanged;

        public DelegateCommand(Action<object> execute, Predicate<object> canExecute = null)
        {
            m_Execute = execute;
            m_CanExecute = canExecute;
        }

        public bool CanExecute(object parameter)
        {
            if (m_CanExecute == null)
            {
                return true;
            }

            return m_CanExecute(parameter);
        }

        public void Execute(object parameter)
        {
            m_Execute(parameter);
        }

        public void RaiseCanExecuteChanged()
        {
            if (CanExecuteChanged != null)
            {
                CanExecuteChanged(this, EventArgs.Empty);
            }
        }
    }

    public class DelegateCommand<T> : ICommand
    {
        private readonly Action<T> m_ExecuteMethod;
        private readonly Func<T, bool> m_CanExecuteMethod;

        public DelegateCommand(Action<T> executeMethod, Func<T, bool> canExecuteMethod = null)
        {
            if (executeMethod == null)
            {
                throw new ArgumentNullException(nameof(executeMethod));
            }

            m_ExecuteMethod = executeMethod;
            m_CanExecuteMethod = canExecuteMethod;
        }

        public bool CanExecute(T parameter)
        {
            if (m_CanExecuteMethod != null)
            {
                return m_CanExecuteMethod(parameter);
            }

            return true;
        }

        public void Execute(T parameter)
        {
            if (m_ExecuteMethod != null)
            {
                m_ExecuteMethod(parameter);
            }
        }
        
        bool ICommand.CanExecute(object parameter)
        {
            // if T is of value type and the parameter is not
            // set yet, then return false if CanExecute delegate
            // exists, else return true
            if (parameter == null && typeof(T).IsValueType)
            {
                return m_CanExecuteMethod == null;
            }

            return CanExecute((T) parameter);
        }

        void ICommand.Execute(object parameter)
        {
            Execute((T) parameter);
        }

        public event EventHandler CanExecuteChanged;
    }
}