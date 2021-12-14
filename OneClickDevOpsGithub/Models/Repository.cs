using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OneClickDevOpsGithub.Models
{

    public class WorkItemResponse
    {
        public List<WorkItemValue> value { get; set; }
        public int count { get; set; }
    }

    public class WorkItemValue
    {
        public int Id { get; set; }

        public string Url { get; set; }

        public WorkItemFields Fields { get; set; }
    }

    public class WorkItemFields
    {

        [JsonProperty("System.TeamProject")]
        public string TeamProject { get; set; }

        [JsonProperty("System.WorkItemType")]
        public string WorkItemType { get; set; }

        [JsonProperty("System.State")]
        public string State { get; set; }

        [JsonProperty("System.Reason")]
        public string Reason { get; set; }

        [JsonProperty("System.Title")]
        public string Title { get; set; }

    }

    public class Repository
    {
        public List<Value> value { get; set; }
        public int count { get; set; }
    }
    public class Project
    {
        public string id { get; set; }
        public string name { get; set; }
        public string url { get; set; }
        public string state { get; set; }
        public int revision { get; set; }
        public string visibility { get; set; }
        public DateTime lastUpdateTime { get; set; }
    }

    public class Value
    {
        public string id { get; set; }
        public string name { get; set; }
        public string url { get; set; }
        public Project project { get; set; }
        public string defaultBranch { get; set; }
        public int size { get; set; }
        public string remoteUrl { get; set; }
        public string sshUrl { get; set; }
        public string webUrl { get; set; }
    }

    public class Versioncontrol
    {
        public string sourceControlType { get; set; }
    }

    public class ProcessTemplate
    {
        public string templateTypeId { get; set; }
    }

    public class Capabilities
    {
        public Versioncontrol versioncontrol { get; set; }
        public ProcessTemplate processTemplate { get; set; }
    }

    public class ProjectRoot
    {
        public string name { get; set; }
        public string description { get; set; }
        public Capabilities capabilities { get; set; }
    }




    public class RefUpdate
    {
        public string name { get; set; }
        public string oldObjectId { get; set; }
    }

    public class Item
    {
        public string path { get; set; }
    }

    public class NewContent
    {
        public string content { get; set; }
        public string contentType { get; set; }
    }

    public class Change
    {
        public string changeType { get; set; }
        public Item item { get; set; }
        public NewContent newContent { get; set; }
    }

    public class Commit
    {
        public string comment { get; set; }
        public List<Change> changes { get; set; }
    }

    public class PipelineUploadRoot
    {
        public List<RefUpdate> refUpdates { get; set; }
        public List<Commit> commits { get; set; }
    }

    public class AzRepository
    {
        public List<AzProject> azprojectList { get; set; }
    }
    public class AzProject
    {
        public string id { get; set; }
        public string name { get; set; }
        public string url { get; set; }
        public List<Value> azPipelineList { get; set; }

        public List<WorkItemValue> WorkItemList { get; set; }
    }
}
namespace OneClickDevOpsGithub.Models1
{ 
    public class Repository
    {
        public string id { get; set; }
        public string name { get; set; }
        public string type { get; set; }
    }

    public class Configuration
    {
        public string type { get; set; }
        public string path { get; set; }
        public Repository repository { get; set; }
    }

    public class PipeLineRoot
    {
        public string name { get; set; }
        public Configuration configuration { get; set; }
    }

    public class TaskRoot
    {
        public string from { get; set; }
        public string op { get; set; }
        public string path { get; set; }
        public string value { get; set; }

    }

    public class WorkItem
    {
        public string Title { get; set; }
        public string Type { get; set; }
    }


    public class ArtifactProperties
    {
        public string localpath { get; set; }
        public string artifactsize { get; set; }
    }

    public class ArtifactResource
    {
        public string type { get; set; }
        public string data { get; set; }
        public ArtifactProperties properties { get; set; }
        public string url { get; set; }
        public string downloadUrl { get; set; }
    }

    public class ArtifactValue
    {
        public int id { get; set; }
        public string name { get; set; }
        public string source { get; set; }
        public ArtifactResource resource { get; set; }
    }

    public class RootArtifact
    {
        public int count { get; set; }
        public List<ArtifactValue> value { get; set; }
    }


}
