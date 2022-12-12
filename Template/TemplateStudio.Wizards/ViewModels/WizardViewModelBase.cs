using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using TemplateStudio.Wizards.ComponentModel;
using TemplateStudio.Wizards.Model;

namespace TemplateStudio.Wizards.ViewModels;

public class WizardViewModelBase : INotifyPropertyChanged
{
	private readonly IDictionary<string, string> _replacements;
	private readonly IReadOnlyDictionary<string, string> _parameters;
	private readonly IDictionary<string, object> _values;

	protected WizardViewModelBase(IDictionary<string, string> replacementsDictionary)
	{
		_replacements = replacementsDictionary;
		_parameters = GetType()
			.GetRuntimeProperties()
			.Where(x => x.GetCustomAttributes<TemplateParameterAttribute>().Any())
			.Select(x => (x.Name, x.GetCustomAttribute<TemplateParameterAttribute>().Name))
			.ToDictionary(x => x.Item1, x => x.Item2);
		_values = new Dictionary<string, object>();
	}

	public event PropertyChangedEventHandler PropertyChanged;

	protected T Get<T>([CallerMemberName]string propertyName = null) =>
		_values.ContainsKey(propertyName) && _values[propertyName] is T value ? value : default;

	protected bool Set<T>(T value, [CallerMemberName]string propertyName = null)
	{
		if(_values.ContainsKey(propertyName) && _values[propertyName] is T source &&
			EqualityComparer<T>.Default.Equals(source, value))
		{
			return false;
		}

		_values[propertyName] = value;
		if(_parameters.ContainsKey(propertyName))
		{
			var key = $"passthrough:{_parameters[propertyName]}";
			if(value is null)
			{
				_replacements.Remove(key);
			}
			else
			{
				var passthrough = value is TemplateChoice choice ? choice.Choice : value.ToString();
				_replacements[key] = passthrough;
			}
		}

		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		return true;
	}
}
