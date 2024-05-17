using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Autodesk.Authentication;
using Autodesk.Authentication.Model;
using Autodesk.DataManagement;
using Autodesk.SDKManager;
using Autodesk.DataManagement.Model;

public class Tokens
{
    public string InternalToken;
    public string PublicToken;
    public string RefreshToken;
    public DateTime ExpiresAt;
}

public partial class APSService
{
    private readonly string _clientId;
    private readonly string _clientSecret;
    private readonly string _callbackUri;
    private readonly AuthenticationClient _authClient;
    private readonly DataManagementClient _dataManagementClient;
    private readonly List<Scopes> InternalTokenScopes = new List<Scopes> { Scopes.DataRead, Scopes.ViewablesRead };
    private readonly List<Scopes> PublicTokenScopes = new List<Scopes> { Scopes.DataRead, Scopes.ViewablesRead };

    public APSService(string clientId, string clientSecret, string callbackUri)
    {
        _clientId = clientId;
        _clientSecret = clientSecret;
        _callbackUri = callbackUri;
        SDKManager sdkManager = SdkManagerBuilder
                .Create() // Creates SDK Manager Builder itself.
                .Build();

        _authClient = new AuthenticationClient(sdkManager);
        _dataManagementClient = new DataManagementClient(sdkManager);

    }

    public string GetAuthorizationURL()
    {
        return _authClient.Authorize(_clientId, ResponseType.Code, _callbackUri, InternalTokenScopes);
    }

    public async Task<Tokens> GenerateTokens(string code)
    {
        ThreeLeggedToken internalAuth = await _authClient.GetThreeLeggedTokenAsync(_clientId, _clientSecret, code, _callbackUri);
        RefreshToken publicAuth = await _authClient.GetRefreshTokenAsync(_clientId, _clientSecret, internalAuth.RefreshToken, PublicTokenScopes);
        return new Tokens
        {
            PublicToken = publicAuth.AccessToken,
            InternalToken = internalAuth.AccessToken,
            RefreshToken = publicAuth._RefreshToken,
            ExpiresAt = DateTime.Now.ToUniversalTime().AddSeconds((double)internalAuth.ExpiresIn)
        };
    }

    public async Task<Tokens> RefreshTokens(Tokens tokens)
    {
        RefreshToken internalAuth = await _authClient.GetRefreshTokenAsync(_clientId, _clientSecret, tokens.RefreshToken, InternalTokenScopes);
        RefreshToken publicAuth = await _authClient.GetRefreshTokenAsync(_clientId, _clientSecret, internalAuth._RefreshToken, PublicTokenScopes);
        return new Tokens
        {
            PublicToken = publicAuth.AccessToken,
            InternalToken = internalAuth.AccessToken,
            RefreshToken = publicAuth._RefreshToken,
            ExpiresAt = DateTime.Now.ToUniversalTime().AddSeconds((double)internalAuth.ExpiresIn).AddSeconds(-1700)
        };
    }

    public async Task<UserInfo> GetUserProfile(Tokens tokens)
    {
        UserInfo userInfo = await _authClient.GetUserInfoAsync(tokens.InternalToken);
        return userInfo;
    }

    public async Task<IEnumerable<dynamic>> GetVersions(string projectId, string itemId, Tokens tokens)
    {
        Versions versions = await _dataManagementClient.GetItemVersionsAsync(projectId, itemId);
        return versions.Data;
    }

    public async Task<IEnumerable<HubsData>> GetHubsDataAsync(Tokens tokens)
    {
        Hubs hubs = await _dataManagementClient.GetHubsAsync(accessToken: tokens.InternalToken);
        return hubs.Data;
    }

    public async Task<IEnumerable<ProjectsData>> GetProjectsDatasAsync(string hubId, Tokens tokens)
    {
        Projects projects = await _dataManagementClient.GetHubProjectsAsync(hubId, accessToken: tokens.InternalToken);
        return projects.Data;
    }

    public async Task<IEnumerable<TopFoldersData>> GetTopFoldersDatasAsync(string hubId, string projectId, Tokens tokens)
    {
        TopFolders topFolders = await _dataManagementClient.GetProjectTopFoldersAsync(hubId, projectId, accessToken: tokens.InternalToken);
        return topFolders.Data;
    }

    public async Task<FolderContents> GetFolderContentsDatasAsync(string projectId, string folderUrn, Tokens tokens)
    {
        FolderContents folderContents = await _dataManagementClient.GetFolderContentsAsync(projectId, folderUrn, accessToken: tokens.InternalToken);
        return folderContents;
    }

    internal async Task<FolderContents> GetFolderContentsDatasAsync(string projectId, string folderId, int pageNumber, Tokens tokens)
    {
        FolderContents folderContents = await _dataManagementClient.GetFolderContentsAsync(projectId, folderId, pageNumber: pageNumber, accessToken: tokens.InternalToken);
        return folderContents;
    }

    internal async Task<string> GetClientId()
    {
        return _clientId;
    }
}