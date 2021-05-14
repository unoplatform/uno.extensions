using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ApplicationTemplate.Hosting
{
    public interface IServiceConfigurer
   {
       IConfiguration Configuration { get; set; }
       void ConfigureServices(IServiceCollection services);
   }
}
