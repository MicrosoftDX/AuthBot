using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.TeamFoundation.Core.WebApi;
using Microsoft.VisualStudio.Services.Common;
using System.Net.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;

namespace SampleVSOBot.Helpers
{
    public static class VSORestHelper
    {
        public static async Task<String> ListProjects(string token)
        {
            StringBuilder retStr = new StringBuilder();

            retStr.AppendLine("Fetching project list\n");
            Uri uri = new Uri("https://" + AuthBot.Models.AuthSettings.Tenant + ".visualstudio.com/");
            VssBasicCredential credentials = new VssBasicCredential("", token);

            using (ProjectHttpClient projectHttpClient = new ProjectHttpClient(uri, credentials))
            {

                IEnumerable<TeamProjectReference> projects = projectHttpClient.GetProjects().Result;
                retStr.AppendLine(String.Format("Found {0} Projects:\n", projects.Count()));
                foreach (TeamProjectReference project in projects)
                {
                    retStr.AppendLine(String.Format("{0}\n", project.Name));
                }

            }
            return retStr.ToString();
        }

        #region Other sample ways to query TFS
        
        public static async Task<String> GetWorkItems(string token)
        {
            using (var client = new HttpClient())
            {
                var apiVersion = "1.0";
                StringBuilder strResult = new StringBuilder();
                
                var query = "Select [System.Id] From WorkItems Where [System.WorkItemType] = 'Bug' order by [System.CreatedDate] desc";

                client.DefaultRequestHeaders.Accept.Clear();

                //var url = "https://" + AuthBot.Models.AuthSettings.Tenant + ".visualstudio.com/defaultcollection/_apis/wit/";
                //https://www.visualstudio.com/en-us/docs/integrate/get-started/rest/basics
                //VERB https://{account}.VisualStudio.com/DefaultCollection/_apis[/{area}]/{resource}?api-version={version}

                var url = "https://" + AuthBot.Models.AuthSettings.Tenant + ".visualstudio.com/DefaultCollection/_apis/wit/";
                //var url = "https://" + AuthBot.Models.AuthSettings.Tenant + ".visualstudio.com/DefaultCollection/TED%20Consumer/_apis/wit/wiql";

                // Execute a query that returns work item IDs matching the specified criteria
                using (var request = new HttpRequestMessage(HttpMethod.Post, url))
                {
                    request.Headers.Add("Authorization", "Bearer " + token);
                    request.Headers.Add("Accept", "application/json;api-version=" + apiVersion);

                    Dictionary<string, string> body = new Dictionary<string, string>
                    {
                         {
                        "query", query
                        }
                     };

                    request.Content = new StringContent(JsonConvert.SerializeObject(body), Encoding.UTF8, "application/json");

                    using (var response = await client.SendAsync(request))
                    {
                        var content = await response.Content.ReadAsStringAsync();
                        var workItems = JObject.Parse(content)["workItems"] as JArray;

                        string[] ids = workItems.Select<JToken, string>(w => (w["id"] + "")).Take(10).ToArray<string>();
                        string idsString = String.Join(",", ids);

                        // Get details for the last 10
                        using (var detailsRequest = new HttpRequestMessage(HttpMethod.Get, url + "workitems?ids=" + idsString + "&fields=System.Id,System.Title"))
                        {
                            detailsRequest.Headers.Add("Authorization", "Bearer " + token);
                            detailsRequest.Headers.Add("Accept", "application/json;api-version=" + apiVersion);

                            using (var detailsResponse = await client.SendAsync(detailsRequest))
                            {
                                var detailsContent = await detailsResponse.Content.ReadAsStringAsync();
                                var detailsWorkItems = JObject.Parse(detailsContent)["value"] as JArray;

                                foreach (dynamic workItem in detailsWorkItems)
                                {
                                    strResult.AppendFormat("* Work item: {0} ({1})\n",
                                        workItem.fields["System.Id"],
                                        workItem.fields["System.Title"]);
                                }
                            }
                        }
                        return strResult.ToString();
                    }
                }
            }
        }

        
        public static async Task<String> QueryWorkItems_Query(string token)
        {
            string retStr="Nothing to return";
            string _personalAccessToken = token;
            Uri uri = new Uri("https://" + AuthBot.Models.AuthSettings.Tenant + ".visualstudio.com/");
            VssBasicCredential _credentials = new VssBasicCredential("", _personalAccessToken);

            string project = "TED Consumer";
            string query = "Shared Queries/FY17/All Activities";

            using (WorkItemTrackingHttpClient workItemTrackingHttpClient = new WorkItemTrackingHttpClient(uri, _credentials))
            {
                QueryHierarchyItem queryItem;

                try
                {
                    //get the query object based on the query name and project
                    queryItem = workItemTrackingHttpClient.GetQueryAsync(project, query).Result;
                }
                catch (Exception ex)
                {
                    return ex.InnerException.Message;
                }

                //now we have the query id, so lets execute the query and get the results
                WorkItemQueryResult workItemQueryResult = workItemTrackingHttpClient.QueryByIdAsync(queryItem.Id).Result;

                //some error handling                
                if (workItemQueryResult != null)
                {
                    //need to get the list of our work item id's and put them into an array
                    List<int> list = new List<int>();
                    foreach (var item in workItemQueryResult.WorkItems)
                    {
                        list.Add(item.Id);
                    }
                    int[] arr = list.ToArray();

                    //build a list of the fields we want to see
                    string[] fields = new string[3];
                    fields[0] = "System.Id";
                    fields[1] = "System.Title";
                    fields[2] = "System.State";

                    //var workItems = workItemTrackingHttpClient.GetWorkItemsAsync(arr, fields, workItemQueryResult.AsOf).Result;
                    retStr= String.Format("Found {0} items", arr.Length);
                }
            }
            return retStr;
        }
        

        public static async Task<String> QueryWorkItems_Wiql(string token)
        {
            string _personalAccessToken = token;
            Uri uri = new Uri("https://" + AuthBot.Models.AuthSettings.Tenant + ".visualstudio.com/");
            VssBasicCredential _credentials = new VssBasicCredential("", _personalAccessToken);

            //needed to scope our query to the project
            string project = "TED Consumer";

            //var query = "Select [System.Id] From WorkItems Where [System.WorkItemType] = 'Bug' order by [System.CreatedDate] desc";
            //create a wiql object and build our query
            Wiql wiql = new Wiql()
            {
                Query = "Select [State], [Title] " +
                        "From WorkItems " +
                        "Where [Work Item Type] = 'Bug' " +
                        "And [System.TeamProject] = '" + project + "' " +
                        "And [System.State] = 'New' " +
                        "Order By [State] Asc, [Changed Date] Desc"
            };

            //create instance of work item tracking http client
            using (WorkItemTrackingHttpClient workItemTrackingHttpClient = new WorkItemTrackingHttpClient(uri, _credentials))
            {
                //execute the query to get the list of work items in teh results
                WorkItemQueryResult workItemQueryResult = workItemTrackingHttpClient.QueryByWiqlAsync(wiql).Result;

                //some error handling                
                if (workItemQueryResult != null)
                {
                    //need to get the list of our work item id's and put them into an array
                    List<int> list = new List<int>();
                    foreach (var item in workItemQueryResult.WorkItems)
                    {
                        list.Add(item.Id);
                    }
                    int[] arr = list.ToArray();

                    //build a list of the fields we want to see
                    string[] fields = new string[3];
                    fields[0] = "System.Id";
                    fields[1] = "System.Title";
                    fields[2] = "System.State";

                    var workItems = workItemTrackingHttpClient.GetWorkItemsAsync(arr, fields, workItemQueryResult.AsOf).Result;
                }
            }
            return "done";
        }

        #endregion  
    }

}