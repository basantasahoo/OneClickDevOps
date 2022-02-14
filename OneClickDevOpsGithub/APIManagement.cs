using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;
using Newtonsoft.Json;
using OneClickDevOpsGithub.Models;
using OneClickDevOpsGithub.Models1;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Clients.ActiveDirectory;

namespace OneClickDevOpsGithub
{
    public class APIManagement : ControllerBase
    {
        private readonly IConfiguration Configuration;

        public AzParams parms { get; set; }

        public const string Task1 = "Create Resources for the Project";
        public const string Task2 = "Create Economics for the Project";
        public const string Task3 = "Develop application as per Project ";
        public const string Task4 = "Create CI /CD for the Application";
        public const string AZURESOURCE = "Azure";
        public const string AWSSOURCE = "AWS";

        #region Constructor

        public APIManagement(IConfiguration configuration)
        {
            Configuration = configuration;
            parms = new AzParams();
            parms.ClientId = Configuration["clientId"];
            parms.TenantId = Configuration["tenantId"];
            parms.ClientSecret = Configuration["clientSecret"];
            parms.SubscriptionId = Configuration["subscriptionId"];
            parms.HOSTURL = "";
        }

        #endregion

        #region private Methods


        private string GetJsonResult(string path)
        {
            // Read From JSON File/ 
            return  System.IO.File.ReadAllText(Directory.GetCurrentDirectory() + path);
        }

        private List<CarbonFootPrintData> GetDataCenterDetails()
        {
            List<CarbonFootPrintData> cfData = new List<CarbonFootPrintData>();
            string fullapath = string.Empty;
            string jsonResult = string.Empty;
            // Get the Full Path
            fullapath =  @"\wwwroot\JSONData\CarbonFootPrintData.json";

            // Read From JSON File/ 
            jsonResult = System.IO.File.ReadAllText(Directory.GetCurrentDirectory() + fullapath);

            // Deserialize the Response JSON data to ProjectList.
            cfData = JsonConvert.DeserializeObject<List<CarbonFootPrintData>>(jsonResult);
            return cfData;
        }

        private List<AzureProduct> GetAzureResourceDetails()
        {
            List<AzureProduct> cfData = new List<AzureProduct>();
            string fullapath = string.Empty;
            string jsonResult = string.Empty;
            // Get the Full Path
            fullapath =  @"\wwwroot\JSONData\AzureResourceCFs.json" ;

            // Read From JSON File/ 
            jsonResult = System.IO.File.ReadAllText(Directory.GetCurrentDirectory() + fullapath);

            // Deserialize the Response JSON data to ProjectList.
            cfData = JsonConvert.DeserializeObject<List<AzureProduct>>(jsonResult);
            return cfData;
        }

        #endregion

        #region Public Methods

        [HttpGet]
        [Route("api/v1/GetCF")]
        public string GetCarbonFootPrint(string dataCenter, string resourceInputs)
        {
            decimal finalCO2 = default(decimal);

            // Get Data Center Details from Datcenter JSON
            List <CarbonFootPrintData> datacenterDetails = GetDataCenterDetails();
            var d_data = datacenterDetails.Find(d => d.DataCenter == dataCenter);

            // Get Resource Data from Resource JSON
            List<AzureProduct> resourceData = GetAzureResourceDetails();

            //Split Resource Details from query String
            List<string> splitData = resourceInputs.Split(',',StringSplitOptions.RemoveEmptyEntries).ToList();
            List<resourceInstance> resourceList = new List<resourceInstance>();
            List<string> errorData = new List<string>();
            splitData.ForEach(d => {

                //Split data into resourceName and Instance 
                var resource = d.Split('-', StringSplitOptions.RemoveEmptyEntries)[0];
                var instance_data = d.Split('-', StringSplitOptions.RemoveEmptyEntries)[1];

                // Verify Instance is Valid
                int instance = default(int);
                if (int.TryParse(instance_data.Trim(),out instance))
                {
                    //Verify Resource name is valid 
                    AzureProduct r_data = resourceData.Find(x => x.ProductName.ToLower() == resource.Trim().ToLower());
                    if (r_data != null)
                    {
                        decimal cf = r_data.CF;
                        resourceList.Add(new resourceInstance() { ResourceName = resource, Instance = instance, TotalCO2 = d_data.ConversionRate * d_data.PUE * d_data.KWH * cf * instance });
                    }
                    else
                    {
                        errorData.Add("Invalid Resource: " + resource + " in: " + d);
                    }
                }
                else
                {
                    errorData.Add("Invalid Input: " + instance_data + " in: "+ d);
                }
                
            });

            //Calculate Average CF for multiple resourcesz
            int total_instance = resourceList.Sum(d => d.Instance);
            decimal total_cf = resourceList.Sum(d => d.TotalCO2);
            //decimal avg_cf = total_cf / resourceList.Count();

            // Calculate CO2 and return the client.
            finalCO2 = total_cf;

            if (errorData.Any())
            {
                return string.Join("  ~  ", errorData);
            }

            return finalCO2.ToString();
        }

        [HttpPost]
        [Route("api/v1/TestPostMethod")]
        public string TestPostMethod(string projectName, string pat)
        {
            string result = string.Empty;
            result = projectName + pat;

            return result;
        }

        [HttpGet]
        [Route("api/v1/GetProjectLink")]
        public string GetProjectLink(string projectName)
        {
            var pro = GetAllRepositories("org", "pat", false).value.Find(d => d.name == projectName);
            return pro.webUrl;
        }

        [HttpGet]
        [Route("api/v1/DeleteProject")]
        public string DeleteProject(string projectName, string org, string pat1)
        {
            var pro = GetAllRepositories(org, pat1, false).value.Find(d => d.name == projectName);
            return DeleteProjectAsync(pro.id);
        }


        [HttpGet]
        [Route("api/v1/CreatePipeline")]
        public string CreatePipeline(string projectName, string org, string pat1)
        {
            // Get Repository by ID

            Models.Repository repo = GetAllRepositories(org, pat1, true);
            var newpro = repo.value.Find(d => d.name == projectName);
            // Create Pipeline
            PipeLineRoot piRoot = new PipeLineRoot();
            piRoot.name = "DotnetPipeline";

            piRoot.configuration = new Configuration()
            {
                type = "yaml",
                path = "dontnetpipeline.yaml",
                repository = new Models1.Repository() { id = newpro.id, name = projectName, type = "azureReposGit" }
            };
            PostCreatePipeLine(piRoot, org, pat1, newpro.project.id);

            return newpro.id;
        }

        [HttpGet]
        [Route("api/v1/UploadYaml")]
        public string UploadYaml(string projectName, string org, string pat1)
        {
            // Get Repository by ID

            Models.Repository repo = GetAllRepositories(org, pat1, true);
            var newpro = repo.value.Find(d => d.name == projectName);

            // Upload Yaml File

            // Read yaml file 
            var yamlcontent = System.IO.File.ReadAllText(Environment.CurrentDirectory + @"\BuildPipelines\dotnet.yaml");

            PipelineUploadRoot pipeline = new PipelineUploadRoot();
            pipeline.refUpdates = new List<RefUpdate>();
            pipeline.refUpdates.Add(new RefUpdate() { name = "refs/heads/master", oldObjectId = "0000000000000000000000000000000000000000" });
            pipeline.commits = new List<Commit>();

            // Change
            Change change = new Change()
            {
                changeType = "add",
                item = new Item { path = "/dontnetpipeline.yaml" },
                newContent = new NewContent() { contentType = "rawtext", content = yamlcontent }
            };

            List<Change> list = new List<Change>();
            list.Add(change);

            // Commits
            pipeline.commits.Add(new Commit() { comment = "Added new yaml file into repository", changes = list });
            PostUploadYaml(pipeline, org, pat1, newpro.id);
            return newpro.id;
        }

        [HttpPost]
        [Route("api/v1/CreateProjectPost")]
        public string CreateProjectPOST(string projectName, string description, string org, string pat1, string type, bool isexist, int numberOfTask)
        {
            return CreateProject(projectName, description, org, pat1, type, isexist, numberOfTask);
        }

        [HttpPost]
        [Route("api/v1/CreatePipelinePost")]
        public string CreatePipelinePost(string projectId, string org, string pat1, string type)
        {
            return CreateProject(projectId, "", org, pat1, type, true, 0);
        }

        [HttpPost]
        [Route("api/v1/CreateProjectFromBot")]
        public string CreateProjectFromBot([FromBody] DevOpsProject prop)
        {
            return CreateProject(prop.ProjectName, prop.Description, prop.Org, prop.Pat1, prop.Type, prop.IsExist, prop.NumberOfTask);
        }


        [HttpGet]
        [Route("api/v1/CreateProject")]
        public string CreateProject(string projectName, string description, string org, string pat1, string type, bool isexist, int numberOfTask)
        {
            var message = string.Empty;
            try
            {
                if (!isexist)
                {
                    // Create content to send POST
                    ProjectRoot project = new ProjectRoot();
                    project.name = projectName;
                    project.description = description;
                    project.capabilities = new Capabilities();
                    project.capabilities.versioncontrol = new Versioncontrol() { sourceControlType = "Git" };
                    project.capabilities.processTemplate = new ProcessTemplate() { templateTypeId = "6b724908-ef14-45cf-84f8-768b5384da45" };
                    PostCreateProject(project, org, pat1);
                    Thread.Sleep(5000);
                    message += "Project Created.";
                }
                // Get Repository by ID
                Models.Repository repo = GetAllRepositories(org, pat1, true);
                Value newpro = null;
                if (isexist)
                {
                    newpro = repo.value.Find(d => d.project.id == projectName);
                }
                else
                {
                    newpro = repo.value.Find(d => d.name == projectName);
                }

                // Upload Yaml File

                // Read yaml file 
                string ypath = @"dotnet.yaml";
                if (type == "1")
                {
                    ypath = @"javapipeline.yml";
                }
                if (type == "2")
                {
                    ypath = @"pythonpipeline.yml";
                }

                string fullapath = @"\wwwroot\BuildPipelines\" + ypath;

                var yamlcontent = System.IO.File.ReadAllText(Directory.GetCurrentDirectory() + fullapath);

                PipelineUploadRoot pipeline = new PipelineUploadRoot();
                pipeline.refUpdates = new List<RefUpdate>();
                pipeline.refUpdates.Add(new RefUpdate() { name = "refs/heads/master", oldObjectId = "0000000000000000000000000000000000000000" });
                pipeline.commits = new List<Commit>();

                // Change
                Change change = new Change()
                {
                    changeType = "add",
                    item = new Item { path = ypath },
                    newContent = new NewContent() { contentType = "rawtext", content = yamlcontent }
                };

                List<Change> list = new List<Change>();
                list.Add(change);

                // Commits
                pipeline.commits.Add(new Commit() { comment = "Added new yaml file into repository", changes = list });
                PostUploadYaml(pipeline, org, pat1, newpro.id);

                message += " Yaml Uploaded";
                // Create Pipeline
                PipeLineRoot piRoot = new PipeLineRoot();
                piRoot.name = "Pipeline";

                piRoot.configuration = new Configuration()
                {
                    type = "yaml",
                    path = ypath,
                    repository = new Models1.Repository() { id = newpro.id, name = newpro.project.name, type = "azureReposGit" }
                };
                PostCreatePipeLine(piRoot, org, pat1, newpro.project.id);
                message += " Pipeline Created";

                if (!isexist)
                {
                    List<string> taskToCreate = new List<string>() { Task1, Task2, Task3, Task4 };
                    taskToCreate = taskToCreate.Take(numberOfTask).ToList();

                    taskToCreate.ForEach(d =>
                    {
                        List<TaskRoot> taskList = new List<TaskRoot>();

                        TaskRoot taskroot = new TaskRoot() { op = "add", path = "/fields/System.Title", value = d, from = null };
                        taskList.Add(taskroot);
                        // Create Task
                        PostCreateTask(taskList, org, pat1, newpro.project.id);
                        Thread.Sleep(2000);
                    });
                }

                return newpro.webUrl;
            }
            catch (Exception ex)
            {

                return "Error: " + message + " - " + ex.Message;
            }

        }

        [HttpGet]
        [Route("api/v1/GetWorkItemList")]
        public List<WorkItemValue> GetWorkItemList(string org, string pat1, string projectName)
        {
            List<WorkItemValue> workItemLit = GetWorkItemListFromAPI2(org, pat1);
            return workItemLit.Where(x => x.Fields.TeamProject == projectName).ToList();
        }

        [HttpGet]
        [Route("api/v1/CreateWorkItem")]
        public string CreateWorkItem(string org, string pat, string projectName, string taskTitle, string Desc)
        {
            Desc = Desc.Replace("~*", "\r\n");
            string result = string.Empty;

            List<TaskRoot> taskList = new List<TaskRoot>();

            taskList.Add(new TaskRoot() { op = "add", path = "/fields/System.Title", value = taskTitle, from = null });
            taskList.Add(new TaskRoot() { op = "add", path = "/fields/System.Description", value = Desc, from = null });

            // Create Task
            result = PostCreateTask(taskList, org, pat, projectName);
            Thread.Sleep(2000);

            return result;
        }

        [HttpGet]
        [Route("api/v1/GetArtifact")]
        public RootArtifact GetProjectArtifact(string org, string pat, string projectName)
        {
            RootArtifact data = new RootArtifact();
            // Create Task
            data = GetArtifacts( org, pat, projectName);

            return data;
        }

        [HttpGet]
        [Route("api/v1/GetProjectList")]
        public AzRepository GetProjectList(string org, string pat1, bool isRepository = false)
        {
            Models.Repository list = GetAllRepositories(org, pat1, isRepository);
                 

            AzRepository repo = new AzRepository();
            repo.azprojectList = new List<AzProject>();
            // List<WorkItemValue> workItemLit = GetWorkItemListFromAPI2(org, pat1);

            list?.value?.ForEach(d => {
                AzProject pro = new AzProject() { id = d.id, name = d.name, url = d.url };
                pro.azPipelineList = GetPipelineList(org, pat1, d.id).value;
                pro.azPipelineList = pro.azPipelineList ?? new List<Value>();
                //  pro.WorkItemList = workItemLit.Where(x => x.Fields.TeamProject == d.name).ToList(); 
                repo.azprojectList.Add(pro);
            });
            return repo;
        }

        [HttpPost]
        [Route("api/v1/ProjectList")]
        public AzRepository ProjectList(string org, string pat1)
        {
            return GetProjectList(org, pat1);
        }

        [HttpGet]
        [Route("api/v1/GetProductList")]
        public List<AzureProduct> GetProjectList(string source)
        {
            List<AzureProduct> productLit = new List<AzureProduct>();
            string fullapath = string.Empty;
            if (source == AZURESOURCE)
            {
                fullapath = @"\wwwroot\JSONData\Azureproducts.json";
            }
            if (source == AWSSOURCE)
            {
                fullapath = @"\wwwroot\JSONData\AWSProducts.json";
            }

            // Read From JSON File/ 
            var jsonResult = System.IO.File.ReadAllText(Directory.GetCurrentDirectory() + fullapath);

            // Deserialize the Response JSON data to ProjectList.
            productLit = JsonConvert.DeserializeObject<List<AzureProduct>>(jsonResult);
            return productLit;
        }

        [HttpGet]
        [Route("api/v1/CheckAppServiceResponse")]
        public async Task<bool> ChecAppService()
        {
            bool status = default(bool);

            HttpClient client = new HttpClient();
            string url = "https://cloudaitestapp.azurewebsites.net";
            var checkingResponse = await client.GetAsync(url);
            if (checkingResponse.StatusCode == HttpStatusCode.OK)
            {
                return true;

            }
            return status;
        }

        private static async Task<string> GetAccessToken()
        {
            Console.WriteLine("Begin GetAccessToken");
            string tenantId = "a23d489f-d887-4e80-a67a-725ef29238e7";
            string clientId = "f360863a-17c6-464e-a0b1-deaa43ff3c79";

            string clientSecreate = "IkR7Q~Spj5OMVbmX9mJuMqFGoZQU9iPJLwYbN";

            string authContextURL = "https://login.windows.net/" + tenantId;
            var authenticationContext = new AuthenticationContext(authContextURL);
            var credential = new ClientCredential(clientId, clientSecreate);
            var result = await authenticationContext.AcquireTokenAsync("https://management.azure.com/", credential);
            if (result == null)
            {
                throw new InvalidOperationException("Failed to obtain the JWT token");
            }
            string token = result.AccessToken;
            return token;
        }

        [HttpGet]
        [Route("api/v1/StartAppService")]
        public async Task<bool> StartAppService()
        {
            string accesstoken = GetAccessToken().Result;
            bool status = default(bool);

            var url = "https://management.azure.com/subscriptions/924c87d5-d151-484b-8b4a-d7bda28bbe83/resourceGroups/AITestApp/providers/Microsoft.Web/sites/cloudAITestApp/start?api-version=2021-02-01";
            // var accessToken = "eyJ0eXAiOiJKV1QiLCJhbGciOiJSUzI1NiIsIng1dCI6Imwzc1EtNTBjQ0g0eEJWWkxIVEd3blNSNzY4MCIsImtpZCI6Imwzc1EtNTBjQ0g0eEJWWkxIVEd3blNSNzY4MCJ9.eyJhdWQiOiJodHRwczovL21hbmFnZW1lbnQuY29yZS53aW5kb3dzLm5ldC8iLCJpc3MiOiJodHRwczovL3N0cy53aW5kb3dzLm5ldC9hMjNkNDg5Zi1kODg3LTRlODAtYTY3YS03MjVlZjI5MjM4ZTcvIiwiaWF0IjoxNjM4OTUzOTQ3LCJuYmYiOjE2Mzg5NTM5NDcsImV4cCI6MTYzODk1ODEzMiwiYWNyIjoiMSIsImFpbyI6IkFXUUFtLzhUQUFBQW9VeHZiaHloeXlOdUZFMmFiS3I4NVZXVkJ5T21qSW95cmxFdUJpc21oNEFGdFZlRk5BbnQrNDVHZjNEb1JXRFhQNTJCNVVJZnhyV01ZM2lGeXJlVjJBU0NrL0lPWndBbmlWaWtISVpqV2hXVG1jVlZxSnVndGR6SGpsbzhnNlhjIiwiYWx0c2VjaWQiOiIxOmxpdmUuY29tOjAwMDY3RkZFOTM4MDlCNjgiLCJhbXIiOlsicHdkIiwibWZhIl0sImFwcGlkIjoiN2Y1OWE3NzMtMmVhZi00MjljLWEwNTktNTBmYzViYjI4YjQ0IiwiYXBwaWRhY3IiOiIyIiwiZW1haWwiOiJCQVNBTlRBX1NBSE9PQGhvdG1haWwuY29tIiwiZmFtaWx5X25hbWUiOiJTYWhvbyIsImdpdmVuX25hbWUiOiJCYXNhbnRhIiwiZ3JvdXBzIjpbIjMwZTc1Y2YzLWE1YTItNDU0ZS04MWRkLWJjODVjNjc4ODkxMyJdLCJpZHAiOiJsaXZlLmNvbSIsImlwYWRkciI6IjIwLjE4Ny44MS4zMyIsIm5hbWUiOiJCYXNhbnRhIFNhaG9vIiwib2lkIjoiN2Q1NjM1MDItM2E1NS00ZjFmLTk2N2UtOGYzYzk4MTAxODUyIiwicHVpZCI6IjEwMDMyMDAxQTg0RTY5MzAiLCJyaCI6IjAuQVZVQW4wZzlvb2ZZZ0U2bWVuSmU4cEk0NTNPbldYLXZMcHhDb0ZsUV9GdXlpMFNJQUt3LiIsInNjcCI6InVzZXJfaW1wZXJzb25hdGlvbiIsInN1YiI6ImdWeVVGeFFtUkZ1enJ5S3ZKb3JWYTZCWHI2aFNzMGN1UHg1SHVxU1VXcFEiLCJ0aWQiOiJhMjNkNDg5Zi1kODg3LTRlODAtYTY3YS03MjVlZjI5MjM4ZTciLCJ1bmlxdWVfbmFtZSI6ImxpdmUuY29tI0JBU0FOVEFfU0FIT09AaG90bWFpbC5jb20iLCJ1dGkiOiJDWVZWX1BWOEFrLTdNNzlpSTZwaEFBIiwidmVyIjoiMS4wIiwid2lkcyI6WyI2MmU5MDM5NC02OWY1LTQyMzctOTE5MC0wMTIxNzcxNDVlMTAiLCJiNzlmYmY0ZC0zZWY5LTQ2ODktODE0My03NmIxOTRlODU1MDkiXSwieG1zX3RjZHQiOjE2MzcwMzYxNzB9.kaW14pZC3UO8I9z18buSnp1H3w - nosl4L29PvIlqPbpnY - noqxRiAJ399S5sHy6D7beM7ks4TIbMh32dkrvypLOip - Nt7UfOQENby8uLelAC7YbSZF3qs - QUiYJFX538R3PvCWCxo2yDOZXcLSLogJocC69EzL - 9F - _5xDLIsJK8to2soTopNU956LzGkKhi5fieLCsIAvv7J7iX6IWfjRYZXMqwj0DpNORB8jxikCV52ieGIM1aS6BkPQv7nWP3Adv1EEqb_yw75XUclTCus5pfA6qzOSdd1_QNzGDzuIQ26bSEdtEqpZnTbr4WR42OS6veTaAExGIF0APB0gYV_w";
            // POST https://management.azure.com/subscriptions/924c87d5-d151-484b-8b4a-d7bda28bbe83/resourceGroups/AITestApp/providers/Microsoft.Web/sites/cloudAITestApp/start?api-version=2021-02-01

            var httpClient = new HttpClient
            {
                BaseAddress = new Uri(url)
            };

            httpClient.DefaultRequestHeaders.Remove("Authorization");
            httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer " + accesstoken);
            HttpResponseMessage response = await httpClient.PostAsync(url, null);

            var HttpsResponse = await response.Content.ReadAsStringAsync();
            var JSONObject = JsonConvert.DeserializeObject<object>(HttpsResponse);

            Console.WriteLine(JSONObject);
           // var JSONObj = JsonConvert.Deserialize<Dictionary<string, string>>(JSONObject);
           // return response.StatusCode.ToString();

            return status;
        }

        #endregion

        #region GetTestMethod

        [HttpGet]
        [Route("api/v1/DoGet")]
        public string DoGet(String accessToken)
        {
            var response = "";
            using (var client = new HttpClient())
            {
                string subscriptionId = Configuration["subscriptionId"];
                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + accessToken);
                var baseUrl = new Uri($"https://management.azure.com/");
                var requestURl = baseUrl +
                                @"subscriptions/" + subscriptionId + "/resourcegroups?api-version=2019-05-01";
                response = client.GetAsync(requestURl).Result.Content.ReadAsStringAsync().Result;
            }
            return response;
        }

        #endregion

        #region DevOps Actions

        public string PostCreateProject(ProjectRoot project, string org, string pat1)
        {
            string result = string.Empty;
            string organization = org;
            string pat = pat1;
            var cokiestoUse = new CookieContainer();
            using (var handler = new HttpClientHandler { CookieContainer = cokiestoUse, Credentials = new NetworkCredential("", "") })
            using (var client = new HttpClient(handler))
            {
                client.BaseAddress = new Uri($"https://dev.azure.com/{organization}/_apis/projects?api-version=6.0");
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Add("Cookie", "VstsSession=%7B%22PersistentSessionId%22%3A%224890eb27-901e-44bc-8b9f-17ca8c8cf349%22%2C%22PendingAuthenticationSessionId%22%3A%2200000000-0000-0000-0000-000000000000%22%2C%22CurrentAuthenticationSessionId%22%3A%2200000000-0000-0000-0000-000000000000%22%2C%22SignInState%22%3A%7B%7D%7D");
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(
                   System.Text.ASCIIEncoding.ASCII.GetBytes(string.Format("{0}:{1}", "", pat))));

                // Create HttpContent for POST
                var content = JsonConvert.SerializeObject(project);
                var httpContent = new StringContent(content, Encoding.UTF8, "application/json");

                using (var response = client.PostAsync(client.BaseAddress, httpContent).Result)
                {
                    if (response.Content != null)
                    {
                        // Get response data 
                        result = response.Content.ReadAsStringAsync().Result;

                        return result;
                    }
                    else if (response.StatusCode == HttpStatusCode.BadRequest)
                    {
                        // Log for the bad request.
                        var message = response.Content.ReadAsStringAsync().Result;
                        // Log Message
                    }
                }

            }
            return "";
        }

        public string PostUploadYaml(PipelineUploadRoot pipeline, string org, string pat1, string repositoryId)
        {
            string result = string.Empty;

            string organization = org;
            string pat = pat1;

            var cokiestoUse = new CookieContainer();
            using (var handler = new HttpClientHandler { CookieContainer = cokiestoUse, Credentials = new NetworkCredential("", "") })
            using (var client = new HttpClient(handler))
            {
                client.BaseAddress = new Uri($"https://dev.azure.com/{organization}/_apis/git/repositories/{repositoryId}/pushes?api-version=6.0");
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Add("Cookie", "VstsSession=%7B%22PersistentSessionId%22%3A%224890eb27-901e-44bc-8b9f-17ca8c8cf349%22%2C%22PendingAuthenticationSessionId%22%3A%2200000000-0000-0000-0000-000000000000%22%2C%22CurrentAuthenticationSessionId%22%3A%2200000000-0000-0000-0000-000000000000%22%2C%22SignInState%22%3A%7B%7D%7D");
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(
                   System.Text.ASCIIEncoding.ASCII.GetBytes(string.Format("{0}:{1}", "", pat))));

                // Create HttpContent for POST
                var content = JsonConvert.SerializeObject(pipeline);
                var httpContent = new StringContent(content, Encoding.UTF8, "application/json");

                using (var response = client.PostAsync(client.BaseAddress, httpContent).Result)
                {
                    if (response.Content != null)
                    {
                        // Get response data 
                        result = response.Content.ReadAsStringAsync().Result;

                        return result;
                    }
                    else if (response.StatusCode == HttpStatusCode.BadRequest)
                    {
                        // Log for the bad request.
                        var message = response.Content.ReadAsStringAsync().Result;
                        // Log Message
                    }
                }

            }
            return "";
        }

        public string DeleteProjectAsync(string projectId)
        {
            string result = string.Empty;
            string organization = Configuration["organisationId"];
            string pat = Configuration["pat"];

            var cokiestoUse = new CookieContainer();
            using (var handler = new HttpClientHandler { CookieContainer = cokiestoUse, Credentials = new NetworkCredential("", "") })
            using (var client = new HttpClient(handler))
            {
                client.BaseAddress = new Uri($"https://dev.azure.com/{organization}/_apis/projects/{projectId}?api-version=6.0");
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Add("Cookie", "VstsSession=%7B%22PersistentSessionId%22%3A%224890eb27-901e-44bc-8b9f-17ca8c8cf349%22%2C%22PendingAuthenticationSessionId%22%3A%2200000000-0000-0000-0000-000000000000%22%2C%22CurrentAuthenticationSessionId%22%3A%2200000000-0000-0000-0000-000000000000%22%2C%22SignInState%22%3A%7B%7D%7D");
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(
                   System.Text.ASCIIEncoding.ASCII.GetBytes(string.Format("{0}:{1}", "", pat))));

                var response = client.DeleteAsync(client.BaseAddress).Result;
                if (response.Content != null)
                {
                    // Get response data 
                    result = response.Content.ReadAsStringAsync().Result;
                    return "Delete success";
                }
                else if (response.StatusCode == HttpStatusCode.BadRequest)
                {
                    // Log for the bad request.
                    var message = response.Content.ReadAsStringAsync().Result;
                    // Log Message
                }
            }
            return "";
        }

        public string PostCreatePipeLine(PipeLineRoot piroot, string org, string pat1, string projectname)
        {
            string result = string.Empty;
            string organization = org;
            string pat = pat1;

            var cokiestoUse = new CookieContainer();
            using (var handler = new HttpClientHandler { CookieContainer = cokiestoUse, Credentials = new NetworkCredential("", "") })
            using (var client = new HttpClient(handler))
            {
                client.BaseAddress = new Uri($"https://dev.azure.com/{organization}/{projectname}/_apis/pipelines?api-version=6.0-preview.1");
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Add("Cookie", "VstsSession=%7B%22PersistentSessionId%22%3A%224890eb27-901e-44bc-8b9f-17ca8c8cf349%22%2C%22PendingAuthenticationSessionId%22%3A%2200000000-0000-0000-0000-000000000000%22%2C%22CurrentAuthenticationSessionId%22%3A%2200000000-0000-0000-0000-000000000000%22%2C%22SignInState%22%3A%7B%7D%7D");
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(
                   System.Text.ASCIIEncoding.ASCII.GetBytes(string.Format("{0}:{1}", "", pat))));

                // Create HttpContent for POST
                var content = JsonConvert.SerializeObject(piroot);
                var httpContent = new StringContent(content, Encoding.UTF8, "application/json");

                var response = client.PostAsync(client.BaseAddress, httpContent).Result;
                if (response.Content != null)
                {
                    // Get response data 
                    result = response.Content.ReadAsStringAsync().Result;

                    return result;
                }
                else if (response.StatusCode == HttpStatusCode.BadRequest)
                {
                    // Log for the bad request.
                    var message = response.Content.ReadAsStringAsync().Result;
                    // Log Message
                }
            }
            return "";
        }

        public string PostCreateTask(List<TaskRoot> tasklist, string org, string pat1, string projectname)
        {
            string result = string.Empty;
            string organization = org;
            string pat = pat1;
            string type = "task";

            var cokiestoUse = new CookieContainer();
            using (var handler = new HttpClientHandler { CookieContainer = cokiestoUse, Credentials = new NetworkCredential("", "") })
            using (var client = new HttpClient(handler))
            {
                client.BaseAddress = new Uri($"https://dev.azure.com/{organization}/{projectname}/_apis/wit/workitems/${type}?api-version=6.0");
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Add("Cookie", "VstsSession=%7B%22PersistentSessionId%22%3A%224890eb27-901e-44bc-8b9f-17ca8c8cf349%22%2C%22PendingAuthenticationSessionId%22%3A%2200000000-0000-0000-0000-000000000000%22%2C%22CurrentAuthenticationSessionId%22%3A%2200000000-0000-0000-0000-000000000000%22%2C%22SignInState%22%3A%7B%7D%7D");
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(
                   System.Text.ASCIIEncoding.ASCII.GetBytes(string.Format("{0}:{1}", "", pat))));

                // Create HttpContent for POST
                var content = JsonConvert.SerializeObject(tasklist);
                var httpContent = new StringContent(content, Encoding.UTF8, "application/json-patch+json");

                var response = client.PostAsync(client.BaseAddress, httpContent).Result;
                if (response.Content != null)
                {
                    // Get response data 
                    result = response.Content.ReadAsStringAsync().Result;

                    return result;
                }
                else if (response.StatusCode == HttpStatusCode.BadRequest)
                {
                    // Log for the bad request.
                    var message = response.Content.ReadAsStringAsync().Result;
                    // Log Message
                }
            }
            return "";
        }

        public Models.Repository GetAllRepositories(string org, string pat1, bool isRepository = false)
        {
            string result = string.Empty;
            Models.Repository repoList = new Models.Repository();

            string organization = org;
            string pat = pat1;

            string url = isRepository ? $"https://dev.azure.com/{organization}/_apis/git/repositories?api-version=4.1" : $"https://dev.azure.com/{organization}/_apis/projects?api-version=6.0";

            var cokiestoUse = new CookieContainer();
            using (var handler = new HttpClientHandler { CookieContainer = cokiestoUse, Credentials = new NetworkCredential("", "") })
            using (var client = new HttpClient(handler))
            {
                //client.BaseAddress = new Uri();
                client.BaseAddress = new Uri(url);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Add("Cookie", "VstsSession=%7B%22PersistentSessionId%22%3A%224890eb27-901e-44bc-8b9f-17ca8c8cf349%22%2C%22PendingAuthenticationSessionId%22%3A%2200000000-0000-0000-0000-000000000000%22%2C%22CurrentAuthenticationSessionId%22%3A%2200000000-0000-0000-0000-000000000000%22%2C%22SignInState%22%3A%7B%7D%7D");
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(
                   System.Text.ASCIIEncoding.ASCII.GetBytes(string.Format("{0}:{1}", "", pat))));

                using (var response = client.GetAsync(client.BaseAddress).Result)
                {
                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        // Get response data 
                        result = response.Content.ReadAsStringAsync().Result;

                        // Deserialize the Response JSON data to ProjectList.
                        repoList = JsonConvert.DeserializeObject<Models.Repository>(result);
                    }
                    else if (response.StatusCode == HttpStatusCode.BadRequest)
                    {
                        // Log for the bad request.
                        var message = response.Content.ReadAsStringAsync().Result;
                        // Log Message
                    }
                }
            }
            return repoList;
        }

        public Models.Repository GetPipelineList(string org, string pat1, string projectname)
        {
            string result = string.Empty;
            Models.Repository repoList = new Models.Repository();

            string organization = org;
            string pat = pat1;

            string url = $"https://dev.azure.com/{organization}/{projectname}/_apis/pipelines?api-version=6.0-preview.1";

            var cokiestoUse = new CookieContainer();
            using (var handler = new HttpClientHandler { CookieContainer = cokiestoUse, Credentials = new NetworkCredential("", "") })
            using (var client = new HttpClient(handler))
            {
                //client.BaseAddress = new Uri();
                client.BaseAddress = new Uri(url);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Add("Cookie", "VstsSession=%7B%22PersistentSessionId%22%3A%224890eb27-901e-44bc-8b9f-17ca8c8cf349%22%2C%22PendingAuthenticationSessionId%22%3A%2200000000-0000-0000-0000-000000000000%22%2C%22CurrentAuthenticationSessionId%22%3A%2200000000-0000-0000-0000-000000000000%22%2C%22SignInState%22%3A%7B%7D%7D");
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(
                   System.Text.ASCIIEncoding.ASCII.GetBytes(string.Format("{0}:{1}", "", pat))));

                using (var response = client.GetAsync(client.BaseAddress).Result)
                {
                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        // Get response data 
                        result = response.Content.ReadAsStringAsync().Result;

                        // Deserialize the Response JSON data to ProjectList.
                        repoList = JsonConvert.DeserializeObject<Models.Repository>(result);
                    }
                    else if (response.StatusCode == HttpStatusCode.BadRequest)
                    {
                        // Log for the bad request.
                        var message = response.Content.ReadAsStringAsync().Result;
                        // Log Message
                    }
                }
            }
            return repoList;
        }

        /// <summary>
        /// Get All work Item for Organisation
        /// </summary>
        /// <param name="org"></param>
        /// <param name="pat1"></param>
        /// <returns></returns>
        public List<WorkItemValue> GetWorkItemListFromAPI2(string org, string pat1)
        {
            string result = string.Empty;
            WorkItemResponse repoList = new WorkItemResponse();

            string organization = org;
            string pat = pat1;

            Uri orgUrl = new Uri($"https://dev.azure.com/{organization}/");
            string personalAccessToken = $"{pat}";
            VssConnection connection = new VssConnection(orgUrl, new VssBasicCredential(string.Empty, personalAccessToken));

            WorkItemTrackingHttpClient witClient = connection.GetClient<WorkItemTrackingHttpClient>();

            Wiql wiq = new Wiql();
            wiq.Query = "Select [System.Id], [System.Title], [System.State] From WorkItems Where [System.WorkItemType] = 'Task' AND [State] <> 'Closed' AND [State] <> 'Removed' order by [Microsoft.VSTS.Common.Priority] asc, [System.CreatedDate] desc";

            WorkItemQueryResult tasks = witClient.QueryByWiqlAsync(wiq).Result;
            IEnumerable<WorkItemReference> tasksRefs;
            tasksRefs = tasks.WorkItems.OrderBy(x => x.Id);
            List<WorkItemValue> workItemFinalList = new List<WorkItemValue>();

            if (tasksRefs.Count() > 0)
            {
                List<Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models.WorkItem> tasksList = witClient.GetWorkItemsAsync(tasksRefs.Select(wir => wir.Id)).Result;

                foreach (var task in tasksList)
                {
                    workItemFinalList.Add(new WorkItemValue()
                    {
                        Id = task.Id.Value,
                        Url = task.Url,
                        Fields = new WorkItemFields()
                        {
                            Title = task.Fields["System.Title"].ToString(),
                            Reason = task.Fields["System.Reason"].ToString(),
                            State = task.Fields["System.State"].ToString(),
                            TeamProject = task.Fields["System.TeamProject"].ToString(),
                            WorkItemType = task.Fields["System.WorkItemType"].ToString()
                        }
                    });
                }
            }

            return workItemFinalList;
        }


        public RootArtifact GetArtifacts(string org, string pat1, string projectname)
        {
            string result = string.Empty;
            RootArtifact artifactItem = new RootArtifact();

            string organization = org;
            string pat = pat1;
            string buildId = "46";

            string url = $"https://dev.azure.com/{organization}/{projectname}/_apis/build/builds/{buildId}/artifacts?api-version=6.0";

            var cokiestoUse = new CookieContainer();
            using (var handler = new HttpClientHandler { CookieContainer = cokiestoUse, Credentials = new NetworkCredential("", "") })
            using (var client = new HttpClient(handler))
            {
                //client.BaseAddress = new Uri();
                client.BaseAddress = new Uri(url);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Add("Cookie", "VstsSession=%7B%22PersistentSessionId%22%3A%224890eb27-901e-44bc-8b9f-17ca8c8cf349%22%2C%22PendingAuthenticationSessionId%22%3A%2200000000-0000-0000-0000-000000000000%22%2C%22CurrentAuthenticationSessionId%22%3A%2200000000-0000-0000-0000-000000000000%22%2C%22SignInState%22%3A%7B%7D%7D");
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(
                   System.Text.ASCIIEncoding.ASCII.GetBytes(string.Format("{0}:{1}", "", pat))));

                using (var response = client.GetAsync(client.BaseAddress).Result)
                {
                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        // Get response data 
                        result = response.Content.ReadAsStringAsync().Result;

                        // Deserialize the Response JSON data to ProjectList.
                        artifactItem = JsonConvert.DeserializeObject<RootArtifact>(result);
                    }
                    else if (response.StatusCode == HttpStatusCode.BadRequest)
                    {
                        // Log for the bad request.
                        var message = response.Content.ReadAsStringAsync().Result;
                        // Log Message
                    }
                }
            }
            return artifactItem;
        }

        #endregion
    }
}
