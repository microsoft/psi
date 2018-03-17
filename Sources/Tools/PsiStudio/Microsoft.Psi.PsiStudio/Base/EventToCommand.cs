// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

#pragma warning disable SA1118 // The parameter spans multiple lines.

namespace Microsoft.Psi.Visualization.Base
{
    using System;
    using System.Windows;
    using System.Windows.Input;
    using System.Windows.Interactivity;

    /// <summary>
    /// This <see cref="T:System.Windows.Interactivity.TriggerAction`1" /> can be used to bind any event on any FrameworkElement to an <see cref="ICommand" />.
    /// Typically, this element is used in XAML to connect the attached element to a command located in a ViewModel. This trigger can only be attached
    /// to a FrameworkElement or a class deriving from FrameworkElement.
    /// <para>To access the EventArgs of the fired event, use a RelayCommand&lt;EventArgs&gt;
    /// and leave the CommandParameter and CommandParameterValue empty!</para>
    /// </summary>
    public class EventToCommand : TriggerAction<DependencyObject>
    {
        /// <summary>
        /// Identifies the <see cref="AlwaysInvokeCommand" /> dependency property.
        /// </summary>
        public static readonly DependencyProperty AlwaysInvokeCommandProperty =
            DependencyProperty.Register(
                nameof(EventToCommand.AlwaysInvokeCommand),
                typeof(bool),
                typeof(EventToCommand),
                new PropertyMetadata(false));

        /// <summary>
        /// Identifies the <see cref="Command" /> dependency property
        /// </summary>
        public static readonly DependencyProperty CommandProperty =
            DependencyProperty.Register(
                nameof(EventToCommand.Command),
                typeof(ICommand),
                typeof(EventToCommand),
                new PropertyMetadata(null, (s, e) => OnCommandChanged(s as EventToCommand, e)));

        /// <summary>
        /// Identifies the <see cref="CommandParameter" /> dependency property
        /// </summary>
        public static readonly DependencyProperty CommandParameterProperty =
            DependencyProperty.Register(
                nameof(EventToCommand.CommandParameter),
                typeof(object),
                typeof(EventToCommand),
                new PropertyMetadata(
                    null,
                    (s, e) =>
                    {
                        var sender = s as EventToCommand;
                        if (sender == null || sender.AssociatedObject == null)
                        {
                            return;
                        }

                        sender.EnableDisableElement();
                    }));

        /// <summary>
        /// Identifies the <see cref="EventArgsConverterParameter" /> dependency property.
        /// </summary>
        public static readonly DependencyProperty EventArgsConverterParameterProperty =
            DependencyProperty.Register(
                nameof(EventToCommand.EventArgsConverterParameter),
                typeof(object),
                typeof(EventToCommand),
                new PropertyMetadata(null));

        /// <summary>
        /// Identifies the <see cref="MustToggleIsEnabled" /> dependency property.
        /// </summary>
        public static readonly DependencyProperty MustToggleIsEnabledProperty =
            DependencyProperty.Register(
                nameof(EventToCommand.MustToggleIsEnabled),
                typeof(bool),
                typeof(EventToCommand),
                new PropertyMetadata(
                    false,
                    (s, e) =>
                    {
                        var sender = s as EventToCommand;
                        if (sender == null || sender.AssociatedObject == null)
                        {
                            return;
                        }

                        sender.EnableDisableElement();
                    }));

        private object commandParameterValue;
        private bool? mustToggleValue;

        /// <summary>
        /// Gets or sets a value indicating whether the command should be invoked even
        /// if the attached control is disabled. This is a dependency property.
        /// </summary>
        public bool AlwaysInvokeCommand
        {
            get { return (bool)this.GetValue(AlwaysInvokeCommandProperty); }
            set { this.SetValue(AlwaysInvokeCommandProperty, value); }
        }

        /// <summary>
        /// Gets or sets the ICommand that this trigger is bound to. This
        /// is a DependencyProperty.
        /// </summary>
        public ICommand Command
        {
            get { return (ICommand)this.GetValue(CommandProperty); }
            set { this.SetValue(CommandProperty, value); }
        }

        /// <summary>
        /// Gets or sets an object that will be passed to the <see cref="Command" />
        /// attached to this trigger. This is a DependencyProperty.
        /// </summary>
        public object CommandParameter
        {
            get { return this.GetValue(CommandParameterProperty); }
            set { this.SetValue(CommandParameterProperty, value); }
        }

        /// <summary>
        /// Gets or sets an object that will be passed to the <see cref="Command" />
        /// attached to this trigger. This property is here for compatibility
        /// with the Silverlight version. This is NOT a DependencyProperty.
        /// For databinding, use the <see cref="CommandParameter" /> property.
        /// </summary>
        public object CommandParameterValue
        {
            get=> this.commandParameterValue ?? this.CommandParameter;
            set
            {
                this.commandParameterValue = value;
                this.EnableDisableElement();
            }
        }

        /// <summary>
        /// Gets or sets a converter used to convert the EventArgs when using
        /// <see cref="PassEventArgsToCommand"/>. If PassEventArgsToCommand is false,
        /// this property is never used.
        /// </summary>
        public IEventArgsConverter EventArgsConverter { get; set; }

        /// <summary>
        /// Gets or sets a parameters for the converter used to convert the EventArgs when using
        /// <see cref="PassEventArgsToCommand"/>. If PassEventArgsToCommand is false,
        /// this property is never used. This is a dependency property.
        /// </summary>
        public object EventArgsConverterParameter
        {
            get { return this.GetValue(EventArgsConverterParameterProperty); }
            set { this.SetValue(EventArgsConverterParameterProperty, value); }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the attached element must be
        /// disabled when the <see cref="Command" /> property's CanExecuteChanged
        /// event fires. If this property is true, and the command's CanExecute
        /// method returns false, the element will be disabled. If this property
        /// is false, the element will not be disabled when the command's
        /// CanExecute method changes. This is a DependencyProperty.
        /// </summary>
        public bool MustToggleIsEnabled
        {
            get { return (bool)this.GetValue(MustToggleIsEnabledProperty); }
            set { this.SetValue(MustToggleIsEnabledProperty, value); }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the attached element must be
        /// disabled when the <see cref="Command" /> property's CanExecuteChanged
        /// event fires. If this property is true, and the command's CanExecute
        /// method returns false, the element will be disabled. This property is here for
        /// compatibility with the Silverlight version. This is NOT a DependencyProperty.
        /// For databinding, use the <see cref="MustToggleIsEnabled" /> property.
        /// </summary>
        public bool MustToggleIsEnabledValue
        {
            get => this.mustToggleValue == null ? this.MustToggleIsEnabled : this.mustToggleValue.Value;
            set
            {
                this.mustToggleValue = value;
                this.EnableDisableElement();
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the EventArgs of the event that triggered this
        /// action should be passed to the bound RelayCommand. If this is true, the command should
        /// accept arguments of the corresponding type (for example RelayCommand&lt;MouseButtonEventArgs&gt;).
        /// </summary>
        public bool PassEventArgsToCommand { get; set; }

        /// <summary>
        /// Provides a simple way to invoke this trigger programatically
        /// without any EventArgs.
        /// </summary>
        public void Invoke()
        {
            this.Invoke(null);
        }

        /// <summary>
        /// Called when this trigger is attached to a FrameworkElement.
        /// </summary>
        protected override void OnAttached()
        {
            base.OnAttached();
            this.EnableDisableElement();
        }

        /// <summary>
        /// Executes the trigger.
        /// <para>To access the EventArgs of the fired event, use a RelayCommand&lt;EventArgs&gt;
        /// and leave the CommandParameter and CommandParameterValue empty!</para>
        /// </summary>
        /// <param name="parameter">The EventArgs of the fired event.</param>
        protected override void Invoke(object parameter)
        {
            if (this.AssociatedElementIsDisabled() && !this.AlwaysInvokeCommand)
            {
                return;
            }

            var command = this.GetCommand();
            var commandParameter = this.CommandParameterValue;
            if (commandParameter == null && this.PassEventArgsToCommand)
            {
                commandParameter = this.EventArgsConverter == null ? parameter : this.EventArgsConverter.Convert(parameter, this.EventArgsConverterParameter);
            }

            if (command != null && command.CanExecute(commandParameter))
            {
                command.Execute(commandParameter);
            }
        }

        private static void OnCommandChanged(EventToCommand element, DependencyPropertyChangedEventArgs e)
        {
            if (element == null)
            {
                return;
            }

            if (e.OldValue != null)
            {
                ((ICommand)e.OldValue).CanExecuteChanged -= element.OnCommandCanExecuteChanged;
            }

            var command = (ICommand)e.NewValue;
            if (command != null)
            {
                command.CanExecuteChanged += element.OnCommandCanExecuteChanged;
            }

            element.EnableDisableElement();
        }

        /// <summary>
        /// This method is here for compatibility with the Silverlight version.
        /// </summary>
        /// <returns>The FrameworkElement to which this trigger is attached.</returns>
        private FrameworkElement GetAssociatedObject()
        {
            return this.AssociatedObject as FrameworkElement;
        }

        /// <summary>
        /// This method is here for compatibility with the Silverlight 3 version.
        /// </summary>
        /// <returns>The command that must be executed when this trigger is invoked.</returns>
        private ICommand GetCommand()
        {
            return this.Command;
        }

        private bool AssociatedElementIsDisabled()
        {
            var element = this.GetAssociatedObject();
            return this.AssociatedObject == null || (element != null && !element.IsEnabled);
        }

        private void EnableDisableElement()
        {
            var element = this.GetAssociatedObject();
            if (element == null)
            {
                return;
            }

            var command = this.GetCommand();
            if (this.MustToggleIsEnabledValue && command != null)
            {
                element.IsEnabled = command.CanExecute(this.CommandParameterValue);
            }
        }

        private void OnCommandCanExecuteChanged(object sender, EventArgs e)
        {
            this.EnableDisableElement();
        }
    }
}