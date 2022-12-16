using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using static TemplateStudio.Wizards.ViewModels.MainViewModelBase;

namespace TemplateStudio.Wizards.ViewModels;

internal abstract class MainViewModelBase : INotifyPropertyChanged
{

	public event PropertyChangedEventHandler PropertyChanged;

	private readonly IDictionary<string, object> _unoCheck;

	protected MainViewModelBase()
	{

	}
	public virtual bool set<T>(ref T field, T newValue, [CallerMemberName] string propertyName = null)
	{
		if (!EqualityComparer<T>.Default.Equals(field, newValue))
		{
			field = newValue;
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
			return true;
		}
		return false;
	}
}
