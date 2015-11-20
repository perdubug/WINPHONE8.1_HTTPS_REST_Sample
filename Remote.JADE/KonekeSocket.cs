using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using Newtonsoft.Json;
using Remote.JADE.Resources;
using System.IO;
using Windows.Storage;
using System.Net.Http;

namespace Remote.JADE
{
    // e.g. "{\"result\":0,\"des\":\"success\",\"datalist\":[{\"device_name\":\"智能插座\",\"device_type\":\"2\",\"user_id\":\"1178223763\",
    //       \"kid\":\"fadbdee8-ce0e-4a77-bbb7-df747e0455ef\"},{\"device_name\":\"客厅插座\",\"device_type\":\"2\",\"user_id\":\"1178223763\",
    //       \"kid\":\"26c1dbd5-5bd3-4c8c-8c2e-0c5a094ccccf\"}]}"
    public class ReturnJSON_REST_DeviceInfo
    {
        public string device_name { get; set; }
        public string device_type { get; set; }
        public string user_id { get; set; }
        public string kid { get; set; }
    }

    class GetKList_Result
    {
        public bool successful { get; set; }
        public IList<ReturnJSON_REST_DeviceInfo> kIDs { get; set; }
        public string strErrorMessage { get; set; }
    }

    class KonekeSocket
    {
        private const string baseSSLUrl = "https://kk.bigk2.com:8443/KOAuthDemeter/Alley/authorize";
        private const string baseUrl = "http://kk.bigk2.com:8080/KOAuthDemeter";
        private const string userName = "13911782237";
        private const string userPwd = "microsoft123";
        private const string clientId = "510qx65acv3nrx5d";
        private const string clientSecret = "1bBeVWHF7Jr339nj";
        private const string serverCertificateName = "*.bigk2.com";
        private const string machineName = "kk.bigk2.com";
        private const string redirect_uri = " ";

        public sealed class ReturnJSON_AuthCode
        {
            public string result { get; set; }
            public string des { get; set; }
            public string code { get; set; }
        }

        // e.g. "{\"result\":\"0\",\"des\":\"success\",\"expires_in\":3600,\"refresh_token\":\"c1c4b5908767980bbaa714112e93e221\",\"access_token\":\"779302f08dfb700ebfd6673c60672eda\"}"
        public sealed class ReturnJSON_AccessToken
        {
            public string result { get; set; }
            public string des { get; set; }
            public string expires_in { get; set; }
            public string refresh_token { get; set; }
            public string access_token { get; set; }
        }

        public sealed class ReturnJSON_UserID
        {
            public string username { get; set; }
            public string userid { get; set; }
        }     

        public sealed class ReturnJSON_REST_doSwitchK
        {
            public string result { get; set; }
            public string des { get; set; }
        }       

        public sealed class ReturnJSON_REST_getKList
        {
            public string result { get; set; }
            public string des { get; set; }
            public IList<ReturnJSON_REST_DeviceInfo> datalist { get; set; }
        }

        public sealed class PostJSON_REST_getKList
        {
            public string userid { get; set; }
        }

        public sealed class PostJSON_REST_switchKLight
        {
            public string userid { get; set; }
            public string kid { get; set; }
            public string key { get; set; }
        }

        //
        // Call GetAuthCode method in WPSSL(a Windows Runtime Component),which issue SSL/HTTPS(OpenSSL for Windows version) request with client certificate
        //
        public string getAuthCode()
        {
            String projectName = Assembly.GetExecutingAssembly().GetName().Name.ToString();

            Stream fcap = Assembly.GetExecutingAssembly().GetManifestResourceStream(projectName + ".Assets.microsoftca.pem");
            Stream fclp = Assembly.GetExecutingAssembly().GetManifestResourceStream(projectName + ".Assets.microsoft.pem");

            String path = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().GetName().CodeBase);

            string capempath = Path.GetTempPath() + "microsoftca.pem";
            string clpempath = Path.GetTempPath() + "microsoft.pem";

            if (fcap != null)
            {
                using (Stream input = fcap)
                {
                    using (Stream output = File.OpenWrite(capempath))
                    {
                        input.CopyTo(output);
                        input.Close();
                    }
                }
            }

            if (fclp != null)
            {
                using (Stream input = fclp)
                {
                    using (Stream output = File.OpenWrite(clpempath))
                    {
                        input.CopyTo(output);
                        input.Close();
                    }
                }
            }

            WPSSL.WPSSLImpl sccMain = new WPSSL.WPSSLImpl();
            String httpsResponse = sccMain.GetAuthCode(capempath, clpempath);

            httpsResponse = httpsResponse.Substring(httpsResponse.LastIndexOf("\r\n\r\n") + 4);
            ReturnJSON_AuthCode rjs = JsonConvert.DeserializeObject<ReturnJSON_AuthCode>(httpsResponse);

            if (!rjs.result.Equals("0"))
            {
                Console.WriteLine("Get AuthCode error:{0}", rjs.des);
                throw new FileNotFoundException("Wrong AuthCode!");
            }

            return rjs.code;
        }

        public async Task<string> GetAccessToken(string authcode)
        {
            string strResult;
            string strResponse;
            FormUrlEncodedContent requestContent;

            ReturnJSON_AccessToken ratf = null;

            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri(baseUrl);
                client.DefaultRequestHeaders.Accept.Clear();
                //client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                Dictionary<string, string> postData = new Dictionary<string, string>();
                postData.Add("grant_type", "authorization_code");
                postData.Add("client_id", clientId);
                postData.Add("client_secret", clientSecret);
                postData.Add("redirect_uri", redirect_uri);
                postData.Add("code", authcode);

                requestContent = new FormUrlEncodedContent(postData);
                HttpResponseMessage response = await client.PostAsync(string.Format("{0}/accessToken", baseUrl), requestContent);
                var s = response.Content.ReadAsStringAsync();
                strResponse = s.Result;
                if (response.IsSuccessStatusCode == false || (int)response.StatusCode != 200)
                {
                    strResult = "Error:10011, wrong http response";
                    return strResult;
                }
            }

            ratf = JsonConvert.DeserializeObject<ReturnJSON_AccessToken>(strResponse);
            if (ratf == null || !ratf.result.Equals("0"))
            {
                strResult = "Error:10012, wrong JSON return";
                return strResult;
            }

            return ratf.access_token;
        }

        public async Task<string> GetUserId(string strAccessToken)
        {
            string strResult;
            string strResponse;
            ReturnJSON_UserID ruio = null;

            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri(baseUrl);
                client.DefaultRequestHeaders.Accept.Clear();                

                client.DefaultRequestHeaders.Add("Authorization", string.Format("Bearer {0}", strAccessToken));
                HttpResponseMessage response = await client.PostAsync(string.Format("{0}/UserInfo", baseUrl), null);

                var s = response.Content.ReadAsStringAsync();
                strResponse = s.Result;
                ruio = JsonConvert.DeserializeObject<ReturnJSON_UserID>(strResponse);

                if (response.IsSuccessStatusCode == false || ruio == null || (int)response.StatusCode != 200)
                {
                    strResult = "Error:10021, wrong http response";
                    return strResult;
                }
            }

            //Console.WriteLine("UserName:{0}, UserID:{1}", ruio.username, ruio.userid);

            return ruio.userid;
        }

        public async Task<GetKList_Result> GetKList(string accessToken, string userId)
        {
            string strResult = "OK";
            string strResponse;
            string strJson;
            ReturnJSON_REST_getKList rrgl = null;
            GetKList_Result gr = new GetKList_Result();

            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri(baseUrl);
                client.DefaultRequestHeaders.Accept.Clear();
                //client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                client.DefaultRequestHeaders.Add("Authorization", string.Format("Bearer {0}", accessToken));

                PostJSON_REST_getKList prgl = new PostJSON_REST_getKList();
                prgl.userid = userId;

                strJson = JsonConvert.SerializeObject(prgl);

                HttpResponseMessage response = await client.PostAsync(string.Format("{0}/User/getKList", baseUrl), new StringContent(strJson, Encoding.UTF8, "application/json"));
                var s = response.Content.ReadAsStringAsync();
                strResponse = s.Result;

                if (!response.IsSuccessStatusCode || (int)response.StatusCode != 200)
                {
                    gr.successful = false;
                    strResult = "Error:10031, wrong http response";
                }

                rrgl = JsonConvert.DeserializeObject<ReturnJSON_REST_getKList>(strResponse);
                if (rrgl == null || !rrgl.result.Equals("0"))
                {
                    gr.successful = false;
                    strResult = "Error:10032, wrong JSON response";
                }

                gr.kIDs = rrgl.datalist;
                gr.successful = true;
                gr.strErrorMessage = strResult;
            }

            return gr;
        }

        public async Task<string> SwitchKLight(string accessToken, string userId, string kid, string device_name, string action)
        {
            string strJson;
            string strResult;
            ReturnJSON_REST_doSwitchK dsk = null;
            string strRet;

            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri(baseUrl);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Add("Authorization", string.Format("Bearer {0}", accessToken));

                PostJSON_REST_switchKLight prsk = new PostJSON_REST_switchKLight();
                prsk.userid = userId;
                prsk.kid = kid;
                prsk.key = action;

                strJson = JsonConvert.SerializeObject(prsk);

                HttpResponseMessage response = await client.PostAsync(string.Format("{0}/User/switchKLight", baseUrl), new StringContent(strJson, Encoding.UTF8, "application/json"));

                var s = response.Content.ReadAsStringAsync();
                strResult = s.Result;

                if (!response.IsSuccessStatusCode || (int)response.StatusCode != 200)
                {
                    strRet = "Error:10010, wrong http response";
                }

                dsk = JsonConvert.DeserializeObject<ReturnJSON_REST_doSwitchK>(strResult);
                if (dsk == null || !dsk.result.Equals("0"))
                {
                    strRet = action + " " + device_name + ":Unknown" + "\r\n" + "    maybe the device is offline:" + kid + "\r\n";
                }
                else
                {
                    strRet = action + " " + device_name + ":successed:" + kid + "\r\n";
                }
            }

            return strRet;
        }

    }

}
