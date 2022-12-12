using System;

namespace TemplateStudio.Wizards.ComponentModel;

[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
internal class TemplateParameterAttribute : Attribute
{
	public TemplateParameterAttribute(string name)
	{
		Name = name;
	}

	public string Name { get; }
}
