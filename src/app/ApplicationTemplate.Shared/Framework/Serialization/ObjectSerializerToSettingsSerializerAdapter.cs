using System;
using System.Collections.Generic;
using System.Text;
using GeneratedSerializers;
using Nventive.Persistence;

namespace ApplicationTemplate
{
	public class ObjectSerializerToSettingsSerializerAdapter : ISettingsSerializer
	{
		private readonly IObjectSerializer _objectSerializer;

		public ObjectSerializerToSettingsSerializerAdapter(IObjectSerializer objectSerializer)
		{
			_objectSerializer = objectSerializer ?? throw new ArgumentNullException(nameof(objectSerializer));
		}

		public object FromString(string source, Type targetType)
		{
			return _objectSerializer.FromString(source, targetType);
		}

		public string ToString(object value, Type valueType)
		{
			return _objectSerializer.ToString(value, valueType);
		}
	}
}
