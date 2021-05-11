using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Linq;
using System.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace ApplicationTemplate
{
    public partial class DataValidationView : ContentControl
    {
        private bool _isDefaultState = true;

        public DataValidationView()
        {
            DefaultStyleKey = typeof(DataValidationView);

            IsTabStop = false;
        }

        public INotifyDataErrorInfo Model
        {
            get => (INotifyDataErrorInfo)GetValue(ModelProperty);
            set => SetValue(ModelProperty, value);
        }

        public static readonly DependencyProperty ModelProperty =
            DependencyProperty.Register("Model", typeof(INotifyDataErrorInfo), typeof(DataValidationView), new PropertyMetadata(default(INotifyDataErrorInfo), (d, e) => ((DataValidationView)d).OnModelChanged((INotifyDataErrorInfo)e.OldValue, (INotifyDataErrorInfo)e.NewValue)));

        public string PropertyName
        {
            get => (string)GetValue(PropertyNameProperty);
            set => SetValue(PropertyNameProperty, value);
        }

        public static readonly DependencyProperty PropertyNameProperty =
            DependencyProperty.Register("PropertyName", typeof(string), typeof(DataValidationView), new PropertyMetadata(default(string), (d, e) => ((DataValidationView)d).OnPropertyNamedChanged()));

        public DataValidationState State
        {
            get => (DataValidationState)GetValue(FieldValidationStateProperty);
            set => SetValue(FieldValidationStateProperty, value);
        }

        public static readonly DependencyProperty FieldValidationStateProperty =
            DependencyProperty.Register("State", typeof(DataValidationState), typeof(DataValidationView), new PropertyMetadata(default(DataValidationState)));

        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            Update();
        }

        private void OnModelChanged(INotifyDataErrorInfo oldModel, INotifyDataErrorInfo newModel)
        {
            _isDefaultState = true;

            if (oldModel != null)
            {
                oldModel.ErrorsChanged -= OnErrorsChanged;
            }

            if (newModel != null)
            {
                newModel.ErrorsChanged += OnErrorsChanged;
            }

            Update();
        }

        private void OnPropertyNamedChanged()
        {
            _isDefaultState = true;

            Update();
        }

        private void OnErrorsChanged(object sender, DataErrorsChangedEventArgs e)
        {
            //_ = Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, ErrorsChangedUI);

            void ErrorsChangedUI()
            {
                // Errors have changed but not for that property; don't update.
                if (PropertyName != null && e.PropertyName != null && PropertyName != e.PropertyName)
                {
                    return;
                }

                // It should no longer be in the default state.
                _isDefaultState = false;

                Update();
            }
        }

        private void Update()
        {
            State = GetDataValidationState();

            VisualStateManager.GoToState(this, State.StateType.ToString(), true);
        }

        private DataValidationState GetDataValidationState()
        {
            if (Model == null || _isDefaultState)
            {
                return new DataValidationState(DataValidationStateType.Default);
            }

            var state = new DataValidationState(DataValidationStateType.Valid);

            if (Model.HasErrors)
            {
                var errors = Model
                    .GetErrors(PropertyName)
                    .Cast<object>()
                    .ToImmutableList();

                if (errors.Any())
                {
                    state = new DataValidationState(DataValidationStateType.Error, errors);
                }
                else
                {
                    // The errors are not related to that property; it's valid.
                }
            }

            return state;
        }
    }
}
