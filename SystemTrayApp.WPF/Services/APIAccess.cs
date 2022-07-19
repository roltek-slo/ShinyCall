using ShinyCall.Mappings;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace ShinyCall.Services
{
    public static class APIAccess
    {
        
        public static string url = ConfigurationManager.AppSettings["APIaddress"];
        public static string user = ConfigurationManager.AppSettings["SIPPhoneNumber"];
        public static string call = ConfigurationManager.AppSettings["CallData"];


        public static async Task<RoltecResponse> GetPageAsync(string uuid, string clientPhoneNumber, string userId, string internalPhoneNumber)
        {
            string url_final = string.Empty;
            url_final = url + $"/roltek-api/popup/api/getCallPopUp?id={uuid}&clientPhoneNumber={clientPhoneNumber}&userID={userId}&internalPhoneNumber={internalPhoneNumber}";

            using(HttpResponseMessage response = await APIHelper.ApiClient.GetAsync(url_final))
            {
                if(response.IsSuccessStatusCode)
                {
                    RoltecResponse roltecResponse = await response.Content.ReadAsAsync<RoltecResponse>();

                    return roltecResponse;
                } else
                {
                    throw new Exception(response.ReasonPhrase);
                }
            }

        }
    }
}
