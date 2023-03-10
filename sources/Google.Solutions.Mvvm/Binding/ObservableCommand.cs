using Google.Solutions.Mvvm.Commands;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Google.Solutions.Mvvm.Binding
{
    /// <summary>
    /// A command that is associated witha fixed context,
    /// typically surfaced as a button.
    /// </summary>
    public interface IObservableCommand : ICommand
    {
        /// <summary>
        /// Check if command can be executed.
        /// </summary>
        IObservableProperty<bool> CanExecute { get; }

        /// <summary>
        /// Executes the command.
        /// </summary>
        Task ExecuteAsync();
    }

    public class ObservableCommand : IObservableCommand
    {
        private string activityText;
        private readonly Func<Task> executeFunc;

        private ObservableCommand(
            string text,
            Func<Task> executeFunc,
            IObservableProperty<bool> canExecute)
        {
            this.Text = text;
            this.CanExecute = canExecute;
            this.executeFunc = executeFunc;
        }

        public string Text { get; }
        public Image Image { get; set; }
        public Keys ShortcutKeys { get; set; }
        public bool IsDefault { get; set; }

        public string ActivityText
        {
            get => this.activityText ?? this.Text.Replace("&", string.Empty);
            set
            {
                Debug.Assert(
                    value.Contains("ing"),
                    "Action name should be formatted like 'Doing something'");

                this.activityText = value;
            }
        }

        public IObservableProperty<bool> CanExecute { get; }

        public Task ExecuteAsync()
        {
            return this.executeFunc();
        }

        //---------------------------------------------------------------------
        // Builder methods.
        //---------------------------------------------------------------------

        public static ObservableCommand Build(
            string text,
            Func<Task> executeFunc,
            IObservableProperty<bool> canExecute = null)
        {
            return new ObservableCommand(
                text,
                executeFunc,
                canExecute ?? ObservableProperty.Build(true));
        }
    }
}
