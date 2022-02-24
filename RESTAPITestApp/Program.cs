using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;

namespace RESTAPITestApp
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");

            HttpClient client = new HttpClient();
            client.BaseAddress = new Uri("https://localhost:44346/");
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));

            List<DevOpsProject> list = new List<DevOpsProject>() {
            new DevOpsProject(){ ProjectName="aaa",Type="0" },
            new DevOpsProject(){ ProjectName="bbb",Type="0" },
            new DevOpsProject(){ ProjectName="ccc",Type="0" },
            new DevOpsProject(){ ProjectName="ddd",Type="0" },
            };

            var szData = JsonConvert.SerializeObject(list);
            var content = new StringContent(szData, Encoding.UTF8, "application/json");
            using (HttpResponseMessage response = client.PostAsync(string.Format("api/v1/CreateBulkProjects?org={0}&pat1={1}","org","pat1"), content).Result)
            {
                if (response.IsSuccessStatusCode)
                {
                    var testdata = response.Content.ReadAsStringAsync().Result;
                    // baseDriverInfo = await response.Content.ReadAsAsync<IEnumerable<BaseDriverInfo>>();
                    Console.WriteLine(testdata);
                }
                else
                {
                   // baseDriverInfo = new[] { new BaseDriverInfo() };
                }
            }
        }
    }

    public class DevOpsProject
    {
        // string projectName, string description, string org, string pat1, string type, bool isexist, int numberOfTask
        public string ProjectName { get; set; }
        public string Description { get; set; }
        public string Org { get; set; }
        public string Pat1 { get; set; }
        public string Type { get; set; }
        public bool IsExist { get; set; }
        public int NumberOfTask { get; set; }

    }
}
