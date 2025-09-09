using AppFunctions.Common;
using AppFunctions.models;
using Extensions.Config;
using Newtonsoft.Json;
using System.Net.Http.Headers;

namespace AppFunctions.services
{
    public class GeneralService
    {
        public static async Task<string> CreateNotificationAsync(Notification jObjectStructure)
        {
            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri(Constants.ModNotifications.Get());

                var myContent = JsonConvert.SerializeObject(jObjectStructure);

                var buffer = System.Text.Encoding.UTF8.GetBytes(myContent);
                var byteContent = new ByteArrayContent(buffer);

                byteContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");

                var response = client.PostAsync(@"api/Notifications", byteContent).Result;
                var contents = await response.Content.ReadAsStringAsync();

                return "OK";
            }
        }

        public static async Task<string> CreateTemplateAsync(Template template)
        {
            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri(Constants.ModNotifications.Get());

                var myContent = JsonConvert.SerializeObject(template);

                var buffer = System.Text.Encoding.UTF8.GetBytes(myContent);
                var byteContent = new ByteArrayContent(buffer);

                byteContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");

                var response = client.PostAsync(@"api/templates", byteContent).Result;
                var contents = await response.Content.ReadAsStringAsync();

                return "OK";
            }
        }

        public static async Task<string> CreateConfigNotificationsAsync(ConfigNotifications config)
        {
            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri(Constants.ModNotifications.Get());

                var myContent = JsonConvert.SerializeObject(config);

                var buffer = System.Text.Encoding.UTF8.GetBytes(myContent);
                var byteContent = new ByteArrayContent(buffer);

                byteContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");

                var response = client.PostAsync(@"api/configNotifications", byteContent).Result;
                var contents = await response.Content.ReadAsStringAsync();

                return "OK";
            }
        }

        public static async Task<string> CreateTemplateMapAsync(TemplateMap config)
        {
            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri(Constants.ModNotifications.Get());

                var myContent = JsonConvert.SerializeObject(config);

                var buffer = System.Text.Encoding.UTF8.GetBytes(myContent);
                var byteContent = new ByteArrayContent(buffer);

                byteContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");

                var response = client.PostAsync(@"api/templateMaps", byteContent).Result;
                var contents = await response.Content.ReadAsStringAsync();

                return "OK";
            }
        }
    }
}
