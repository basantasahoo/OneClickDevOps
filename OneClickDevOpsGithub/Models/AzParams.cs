using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OneClickDevOpsGithub.Models
{
    public class AzParams
    {
        public string Organisation { get; set; }
        public string ProjectId { get; set; }
        public string ProjectURL { get; set; }
        public string ClientId { get; set; }
        public string TenantId { get; set; }
        public string ClientSecret { get; set; }
        public string SubscriptionId { get; set; }
        public string HOSTURL { get; set; }

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
