## Serialization
_[TBD - Review and update this guidance]_

We use [GeneratedSerializers](https://github.com/nventive/GeneratedSerializers) for faster runtime (de)serialization.

- The serialization settings are configured inside the [SerializationConfiguration.cs](../src/app/ApplicationTemplate.Shared/Configuration/SerializationConfiguration.cs) file.

  - This is where you will add your serializable types.
  - You can simply resolve a `ISerializer` or `IObjectSerializer`.
  - You can add serialization adapters, this removes the need to add a dependency between libraries (e.g. `ObjectSerializerToSettingsSerializerAdapter` adapts serialization for settings and objects).