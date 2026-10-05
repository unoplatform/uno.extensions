global using System;
global using System.Collections.Concurrent;
global using System.Collections.Generic;
global using System.ComponentModel;
global using System.ComponentModel.DataAnnotations;
global using System.Globalization;
global using System.Linq;
global using System.Threading;
global using System.Threading.Tasks;
global using Microsoft.Extensions.DependencyInjection;
global using Microsoft.Extensions.DependencyInjection.Extensions;
global using Microsoft.Extensions.Configuration;
global using Microsoft.Extensions.Hosting;
global using Microsoft.Extensions.Localization;
global using Microsoft.Extensions.Options;
global using Uno.Extensions.Hosting;
global using Uno.Extensions.Validation;
// Avoids naming clash with System.ComponentModel.DataAnnotations.Validator
global using Validator = Uno.Extensions.Validation.Validator;
