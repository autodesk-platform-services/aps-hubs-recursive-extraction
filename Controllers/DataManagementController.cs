/////////////////////////////////////////////////////////////////////
// Copyright (c) Autodesk, Inc. All rights reserved
// Written by APS Partner Development
//
// Permission to use, copy, modify, and distribute this software in
// object code form for any purpose and without fee is hereby granted,
// provided that the above copyright notice appears in all copies and
// that both that copyright notice and the limited warranty and
// restricted rights notice below appear in all supporting
// documentation.
//
// AUTODESK PROVIDES THIS PROGRAM "AS IS" AND WITH ALL FAULTS.
// AUTODESK SPECIFICALLY DISCLAIMS ANY IMPLIED WARRANTY OF
// MERCHANTABILITY OR FITNESS FOR A PARTICULAR USE.  AUTODESK, INC.
// DOES NOT WARRANT THAT THE OPERATION OF THE PROGRAM WILL BE
// UNINTERRUPTED OR ERROR FREE.
/////////////////////////////////////////////////////////////////////

using Hangfire;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MongoDB.Driver;
using MongoDB.Bson.Serialization.Attributes;
using Microsoft.AspNetCore.SignalR;
using MongoDB.Bson.Serialization;
using MongoDB.Bson;
using System.Linq;
using Autodesk.DataManagement.Model;


public class DataManagementController : ControllerBase
{
  public readonly IHubContext<ContentsHub> _contentsHub;
  public readonly APSService _apsService;

  public DataManagementController(IHubContext<ContentsHub> contentsHub, APSService apsService)
  {
    _contentsHub = contentsHub;
    _apsService = apsService;
    GC.KeepAlive(_contentsHub);
  }

  /// <summary>
  /// GET TreeNode passing the ID
  /// </summary>
  [HttpGet]
  [Route("api/aps/datamanagement")]
  public async Task<IList<jsTreeNode>> GetTreeNodeAsync(string id)
  {
    Tokens tokens = await OAuthController.PrepareTokens(Request, Response, _apsService);
    if (tokens == null)
    {
      return null;
    }

    IList<jsTreeNode> nodes = new List<jsTreeNode>();

    if (id == "#") // root
      return await GetHubsAsync(tokens);
    else
    {
      string[] idParams = id.Split('/');
      string resource = idParams[idParams.Length - 2];
      switch (resource)
      {
        case "hubs": // hubs node selected/expanded, show projects
          return await GetProjectsAsync(id, tokens);
      }
    }

    return nodes;
  }

  [HttpGet]
  [Route("api/aps/resource/info")]
  public object GetResourceInfo()
  {
    string connectionId = base.Request.Query["connectionId"];
    string hubId = base.Request.Query["hubId"];
    string projectId = base.Request.Query["projectId"];
    string currentFolderId = base.Request.Query["folderId"];
    string dataType = base.Request.Query["dataType"];
    string projectGuid = base.Request.Query["guid"];

    Tokens tokens = OAuthController.PrepareTokens(Request, Response, _apsService).GetAwaiter().GetResult();
    if (tokens == null)
    {
      return null;
    }

    string jobId = BackgroundJob.Enqueue(() =>
      // the API SDK
      GatherData(connectionId, hubId, projectId, currentFolderId, dataType, projectGuid, tokens).GetAwaiter().GetResult()
    );

    return new { Success = true };
  }

  public async Task GatherData(string connectionId, string hubId, string projectId, string currentFolderId, string dataType, string projectGuid, Tokens tokens)
  {
    switch (dataType)
    {
      case "topFolders":
        await GetProjectContents(hubId, projectId, connectionId, dataType, projectGuid, tokens);
        break;
      case "folder":
        await GetFolderContents(projectId, currentFolderId, connectionId, dataType, projectGuid, tokens);
        break;
      default:
        break;
    }
  }

  private async Task<IList<jsTreeNode>> GetHubsAsync(Tokens tokens)
  {
    IList<jsTreeNode> nodes = new List<jsTreeNode>();

    List<HubsData> hubs = (List<HubsData>)await _apsService.GetHubsDataAsync(tokens);
    foreach (HubsData hubData in hubs)
    {
      string nodeType = "hubs";
      switch (hubData.Attributes.Extension.Type)
      {
        case "hubs:autodesk.core:Hub":
          nodeType = "hubs"; // if showing only BIM 360, mark this as 'unsupported'
          break;
        case "hubs:autodesk.a360:PersonalHub":
          nodeType = "personalHub"; // if showing only BIM 360, mark this as 'unsupported'
          break;
        case "hubs:autodesk.bim360:Account":
          nodeType = "bim360Hubs";
          break;
      }

      jsTreeNode hubNode = new jsTreeNode(hubData.Links.Self.Href, hubData.Attributes.Name, nodeType, !(nodeType == "unsupported"));
      nodes.Add(hubNode);
    }
    return nodes;
  }

  private async Task<IList<jsTreeNode>> GetProjectsAsync(string href, Tokens tokens)
  {
    IList<jsTreeNode> nodes = new List<jsTreeNode>();

    // extract the hubId from the href
    string[] idParams = href.Split('/');
    string hubId = idParams[idParams.Length - 1];

    List<ProjectsData> projects = (List<ProjectsData>)await _apsService.GetProjectsDatasAsync(hubId, tokens);
    foreach (ProjectsData project in projects)
    {
      // check the type of the project to show an icon
      string nodeType = "projects";
      switch (project.Attributes.Extension.Type)
      {
        case "projects:autodesk.core:Project":
          nodeType = "a360projects";
          break;
        case "projects:autodesk.bim360:Project":
          if (project.Attributes.Extension.Data.ProjectType == "ACC")
          {
            nodeType = "accprojects";
          }
          else
          {
            nodeType = "bim360projects";
          }
          break;
      }

      jsTreeNode projectNode = new jsTreeNode(project.Links.Self.Href, project.Attributes.Name, nodeType, false);
      nodes.Add(projectNode);
    }

    return nodes;
  }

  public async Task GetProjectContents(string hubId, string projectId, string connectionId, string dataType, string projectGuid, Tokens tokens)
  {
    List<dynamic> topfolders = new List<dynamic>();

    List<TopFoldersData> folderContentsDatas = (List<TopFoldersData>)await _apsService.GetTopFoldersDatasAsync(hubId, projectId, tokens);
    foreach (TopFoldersData folderContentsData in folderContentsDatas)
    {
      dynamic dynamicFolder = getNewObject(folderContentsData);
      topfolders.Add(dynamicFolder);
    }

    string jobGuid = Guid.NewGuid().ToString();
    await AddItemsToDb(jobGuid, topfolders);
    await ContentsHub.SendContents(_contentsHub, connectionId, jobGuid, dataType, projectGuid, null);
  }

  public async Task AddItemsToDb(string jobGuid, List<dynamic> items)
  {
    MongoClient client = new MongoClient(Environment.GetEnvironmentVariable("MONGO_CONNECTOR"));

    IMongoDatabase database = client.GetDatabase(Environment.GetEnvironmentVariable("ITEMS_DB"));
    var itemsCollection = database.GetCollection<ItemsCollection>(Environment.GetEnvironmentVariable("ITEMS_COLLECTION"));
    foreach (var item in items)
    {
      ItemsCollection newItem = new ItemsCollection()
      {
        jobGuid = jobGuid,
        item = item
      };
      itemsCollection.InsertOne(newItem);
    }
  }

  [HttpGet]
  [Route("api/aps/resource/items")]
  public List<dynamic> GetItemsFromDb()
  {
    string jobGuid = base.Request.Query["jobGuid"];

    try
    {
      BsonClassMap.RegisterClassMap<ItemsCollection>();
    }
    catch (Exception)
    {

    }

    MongoClient client = new MongoClient(Environment.GetEnvironmentVariable("MONGO_CONNECTOR"));

    IMongoDatabase database = client.GetDatabase(Environment.GetEnvironmentVariable("ITEMS_DB"));

    var items_collection = database.GetCollection<ItemsCollection>(Environment.GetEnvironmentVariable("ITEMS_COLLECTION"));

    List<ItemsCollection> retornados = items_collection.Find(colitem => colitem.jobGuid == jobGuid).ToList();

    List<dynamic> items = retornados.Select(o => o.item).ToList();

    var bson_collection = database.GetCollection<BsonDocument>(Environment.GetEnvironmentVariable("ITEMS_COLLECTION"));
    var filter = Builders<BsonDocument>.Filter.Eq("jobGuid", jobGuid);
    bson_collection.DeleteManyAsync(filter);

    return items;
  }

  public async Task GetFolderContents(string projectId, string folderId, string connectionId, string dataType, string projectGuid, Tokens tokens)
  {
    await GetAllFolderContents(projectId, folderId, connectionId, dataType, projectGuid, tokens);
  }

  public async Task GetAllFolderContents(string projectId, string folderId, string connectionId, string dataType, string projectGuid, Tokens tokens)
  {
    FolderContents folderContents = await _apsService.GetFolderContentsDatasAsync(projectId, folderId, tokens);

    List<dynamic> items = AddItems(folderContents);

    int pageNumber = 0;
    try
    {
      while (folderContents.Links.Next != null)
      {
        pageNumber++;
        folderContents = await _apsService.GetFolderContentsDatasAsync(projectId, folderId, pageNumber, tokens);

        List<dynamic> newItems = AddItems(folderContents);
        items.AddRange(newItems);
      }
    }
    catch (Exception)
    { }

    string jobGuid = Guid.NewGuid().ToString();

    await AddItemsToDb(jobGuid, items);

    await ContentsHub.SendContents(_contentsHub, connectionId, jobGuid, dataType, projectGuid, folderId);
  }

  public static List<dynamic> AddItems(FolderContents folderContents)
  {
    List<dynamic> items = new List<dynamic>();

    // let's start iterating the FOLDER DATA
    foreach (FolderContentsData folderContentsData in folderContents.Data)
    {
      dynamic newItem = getNewObject(folderContentsData);
      if (newItem.type == "file")
      {
        try
        {
          dynamic itemLastVersion = getFileVersion(folderContents.Included, folderContentsData.Id);
          newItem.version = itemLastVersion.version;
          newItem.size = itemLastVersion.size;
        }
        catch (Exception)
        {

        }
      }
      items.Add(newItem);
    }
    return items;
  }

  public static dynamic getNewObject(TopFoldersData folderContentItem)
  {
    dynamic newItem = new System.Dynamic.ExpandoObject();
    newItem.createTime = folderContentItem.Attributes.CreateTime;
    newItem.createUserId = folderContentItem.Attributes.CreateUserId;
    newItem.createUserName = folderContentItem.Attributes.CreateUserName;
    newItem.lastModifiedTime = folderContentItem.Attributes.LastModifiedTime;
    newItem.lastModifiedUserId = folderContentItem.Attributes.LastModifiedUserId;
    newItem.lastModifiedUserName = folderContentItem.Attributes.LastModifiedUserName;
    newItem.hidden = folderContentItem.Attributes.Hidden;
    newItem.id = folderContentItem.Id;
    newItem.timestamp = DateTime.UtcNow.ToLongDateString();

    string extension = folderContentItem.Attributes.Extension.Type;
    switch (extension)
    {
      case "folders:autodesk.bim360:Folder":
        newItem.name = folderContentItem.Attributes.Name;
        newItem.filesInside = 0;
        newItem.foldersInside = 0;
        newItem.type = "folder";
        break;
      case "items:autodesk.bim360:File":
        newItem.name = folderContentItem.Attributes.DisplayName;
        newItem.type = "file";
        break;
      case "items:autodesk.bim360:Document":
        newItem.name = folderContentItem.Attributes.DisplayName;
        newItem.type = "file";
        break;
      default:
        newItem.name = folderContentItem.Attributes.DisplayName;
        newItem.type = "file";
        break;
    }

    return newItem;
  }

  public static dynamic getNewObject(FolderContentsData folderContentItem)
  {
    dynamic newItem = new System.Dynamic.ExpandoObject();
    newItem.createTime = folderContentItem.Attributes.CreateTime;
    newItem.createUserId = folderContentItem.Attributes.CreateUserId;
    newItem.createUserName = folderContentItem.Attributes.CreateUserName;
    newItem.lastModifiedTime = folderContentItem.Attributes.LastModifiedTime;
    newItem.lastModifiedUserId = folderContentItem.Attributes.LastModifiedUserId;
    newItem.lastModifiedUserName = folderContentItem.Attributes.LastModifiedUserName;
    newItem.hidden = folderContentItem.Attributes.Hidden;
    newItem.id = folderContentItem.Id;
    newItem.timestamp = DateTime.UtcNow.ToLongDateString();

    string extension = folderContentItem.Attributes.Extension.Type;
    switch (extension)
    {
      case "folders:autodesk.bim360:Folder":
        newItem.name = folderContentItem.Attributes.Name;
        newItem.filesInside = 0;
        newItem.foldersInside = 0;
        newItem.type = "folder";
        break;
      case "items:autodesk.bim360:File":
        newItem.name = folderContentItem.Attributes.DisplayName;
        newItem.type = "file";
        break;
      case "items:autodesk.bim360:Document":
        newItem.name = folderContentItem.Attributes.DisplayName;
        newItem.type = "file";
        break;
      default:
        newItem.name = folderContentItem.Attributes.DisplayName;
        newItem.type = "file";
        break;
    }

    return newItem;
  }

  public static dynamic getFileVersion(List<FolderContentsIncluded> included, string itemId)
  {
    List<FolderContentsIncluded> folderIncluded = included.Where(x => x.Relationships.Item.Data.Id == itemId).ToList();
    FolderContentsIncluded folderIncludedMaxVersion = folderIncluded.MaxBy(x => x.Attributes.VersionNumber);
    try
    {
      dynamic dynamicAux = new System.Dynamic.ExpandoObject();
      dynamicAux.version = folderIncludedMaxVersion.Attributes.VersionNumber;
      dynamicAux.size = folderIncludedMaxVersion.Attributes.StorageSize;
      return dynamicAux;
    }
    catch (Exception ex)
    {
      dynamic dynamicAuxMissing = new System.Dynamic.ExpandoObject();
      dynamicAuxMissing.version = "";
      dynamicAuxMissing.size = "";
      return dynamicAuxMissing;
    }
  }


  [BsonIgnoreExtraElements]
  public class ItemsCollection
  {
    public string jobGuid { get; set; }
    public dynamic item { get; set; }
  }

  public class jsTreeNode
  {
    public jsTreeNode(string id, string text, string type, bool children)
    {
      this.id = id;
      this.text = text;
      this.type = type;
      this.children = children;
    }

    public string id { get; set; }
    public string text { get; set; }
    public string type { get; set; }
    public bool children { get; set; }
  }

}

