using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Bgrs.Configuration.Secure;

namespace Bgrs.Jwt.Daemon
{
    class Program
    {
        /// <summary>
        /// NOTE:!!!!!!!!!!!!!!!!! Even though this is a console application, we set the output type in the project
        ///                        to be a "windows application". This prevents the console window from appearing.
        ///                        If we want to output messages to the console window for debugging, then change the
        ///                        output type back to "console application". 
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        static async Task Main(string[] args)
        {
            Console.WriteLine("Ensure that you are not running in a VPN or that appropriate ports are open to access Azure Resources.");

            ConfigurationResults<MyApplication> _configuredApplication = HostBuilderHelper.CreateApp<MyApplication>(args, ConfigureLocalServices);
            await _configuredApplication.myService.Run();
        }

        /// <summary>
        /// If the default factory method "ConsoleHostBuilderHelper.CreateApp" does not support all the services
        /// you need at runtime, then you can add them here. "CreateApp" calls this method before any other services
        /// are added to the IServiceCollection.
        /// </summary>
        /// <param name="hostingContext">HostBuilderContext</param>
        /// <param name="services">IServiceCollection</param>
        /// <param name="InitialConfiguration">IApplicationSetupConfiguration</param>
        public static void ConfigureLocalServices(HostBuilderContext hostingContext, IServiceCollection services, IApplicationSetupConfiguration InitialConfiguration)
        {
            // Register the appropriate interfaces depending on the environment
            bool useKeyVaultKey = !string.IsNullOrEmpty(InitialConfiguration.KeyVaultKey);

            // .... Any custom configuration you need can be done here ..........
        }
    }
}


