using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Microsoft.Extensions.Configuration;
using Bgrs.Configuration.Secure;

namespace Bgrs.Jwt.Daemon
{
    public class JwtToken
    {
        public string jwt { get; set; }
    }

    /// <summary>
    /// To automatically run this allocation when the computer starts up:
    /// 1) Locate the .exe on disk, right click on it and select "Send To/Desktop Shortcut".
    ///    Selecting "Create Shortcut" will not work. You have to send the shortcut to the desktop.
    /// 2) Navigate to the windows startup folder (C:\ProgramData\Microsoft\Windows\Start Menu\Programs\StartUp)
    /// 3) Now drag the desktop shortcut into the startup folder.
    /// 4) Next time you reboot, this application will automatically start.
    /// </summary>
    public class MyApplication
    {
        private string EmailSubject => $"JWT Daemon Delivery at {DateTime.Now.ToLongTimeString()}";
        private string EmailSendTo => _Configuration["EmailSendTo"];
        private string EmailFrom => _Configuration["EmailFrom"];

        // We want to wake up every 6 hours to renew JWT
        private const int MinutesBetweenRequests = 6 * 60;
        private readonly IApplicationSetupConfiguration _InitialConfiguration;
        private readonly IApplicationSecrets _ApplicationSecrets;
        private readonly IConfiguration _Configuration;

        /// <summary>
        /// We use constructor dependency injection to the interfaces we need at runtime
        /// </summary>
        /// <param name="InitialConfiguration"></param>
        /// <param name="ApplicationSecrets"></param>
        /// <param name="Configuration"></param>
        public MyApplication(IApplicationSetupConfiguration InitialConfiguration,
            IApplicationSecrets ApplicationSecrets,
            IConfiguration Configuration)
        {
            _InitialConfiguration = InitialConfiguration;
            _ApplicationSecrets = ApplicationSecrets;
            _Configuration = Configuration;
        }

        /// <summary>
        /// This is the application entry point. 
        /// </summary>
        /// <returns></returns>
        internal async Task Run()
        {
            try
            {
                "JWT Daemon starting".TraceInformation();

                // Read the initial JWT we need in order to renew it
                string JWT = _Configuration["InitialJWT"];

                // Read the database connection string to use
                string JwtDataStore = _ApplicationSecrets.ConnectionString("JwtDataStore");

                // If the initial JWT has not yet been installed into the cache, then we can't call to renew it.
                if (!string.IsNullOrEmpty(JWT))
                {
                    $"{DateTime.Now.ToLongTimeString()} Initial JWT: {JWT}".TraceInformation();

                    while (true)
                    {
                        using (HttpClient httpClient = new HttpClient())
                        {
                            var httpRequestMessage = new HttpRequestMessage
                            {
                                Method = HttpMethod.Post,
                                RequestUri = new Uri(_Configuration["RenewJwtUri"]),
                                Headers =
                                {
                                    {HttpRequestHeader.Accept.ToString(), "application/json"},
                                    {"x-onit-token", JWT}
                                }
                            };

                            HttpResponseMessage response = httpClient.SendAsync(httpRequestMessage).Result;
                            if (response != null && response.IsSuccessStatusCode)
                            {
                                // Get the new JWT response
                                JWT = await response.Content.ReadAsStringAsync();

                                // Deserialize the JSON so we can get the actual token
                                JwtToken token = JsonConvert.DeserializeObject<JwtToken>(JWT);
                                JWT = token.jwt;

                                $"{DateTime.Now.ToLongTimeString()} Saving renewed token: {token.jwt}".TraceInformation();

                                // Store the jwt 
                                SaveJwt(JWT, JwtDataStore);

                                // Send an email to everyone to tell them about the new JWT value
                                Email.CreateEmail(token.jwt,EmailFrom,EmailSendTo,EmailSubject);

                                try
                                {
                                    // Tell uptime robot we're alive
                                    var result = await httpClient.GetAsync(new Uri(_Configuration["UptimeRobotUri"]));
                                    $"Uptime Robot Response:{result}".TraceInformation();
                                }
                                catch (Exception err)
                                {
                                    err.Message.TraceError("Failed to communiate with Uptime Robot");
                                }
                            }
                            else
                            {
                                string msg = $"Failed to renew JWT. Status code {response?.StatusCode}";
                                msg.TraceError();
                                Email.CreateEmail($"Failed to renew JWT. Status code {response?.StatusCode}", EmailFrom, EmailSendTo, EmailSubject);
                            }
                        }

                        // Sleep until its time to renew the JWT
                        Thread.Sleep(TimeSpan.FromMinutes(MinutesBetweenRequests));
                    }
                }
                else
                {
                    $"{DateTime.Now.ToLongTimeString()} JWT Value in cache is EMPTY".TraceCritical();
                }
                
            }
            catch (Exception e)
            {
                e.Message.TraceError("Jwt Daemon has stopped working");

                // Inspect error. Send email if we failed to get JWT
                Email.CreateEmail("JWT Exception:" + e.Message, EmailFrom, EmailSendTo, EmailSubject);
            }

        }

        public void SaveJwt(string JWT, string DatabaseConnectionString)
        {

        }

    }
}


