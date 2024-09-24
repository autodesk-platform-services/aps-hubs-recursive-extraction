using Microsoft.AspNetCore.SignalR;
using System.Collections.Generic;
using System.Dynamic;
using System.Threading.Tasks;

public class ContentsHub : Microsoft.AspNetCore.SignalR.Hub
{
  public async static Task SendData(IHubContext<ContentsHub> hub, string connectionId, string dataType, string folderData, string parentFolderId)
  {
    await hub.Clients.Client(connectionId).SendAsync("ReceiveData", dataType, folderData, parentFolderId);
  }
  public async static Task SendUpdate(IHubContext<ContentsHub> hub, string connectionId, int completedJobs, int pendingJobs)
  {
    await hub.Clients.Client(connectionId).SendAsync("ReceiveUpdate", completedJobs, pendingJobs);
  }
}