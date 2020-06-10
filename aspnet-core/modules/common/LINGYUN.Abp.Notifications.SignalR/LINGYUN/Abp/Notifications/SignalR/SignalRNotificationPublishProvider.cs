﻿using LINGYUN.Abp.Notifications.SignalR.Hubs;
using LINGYUN.Abp.RealTime.Client;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LINGYUN.Abp.Notifications.SignalR
{
    public class SignalRNotificationPublishProvider : NotificationPublishProvider
    {
        public ILogger<SignalRNotificationPublishProvider> Logger { protected get; set; }

        public override string Name => "SignalR";

        private readonly IOnlineClientManager _onlineClientManager;

        private readonly IHubContext<NotificationsHub> _hubContext;

        public SignalRNotificationPublishProvider(
           IOnlineClientManager onlineClientManager,
           IHubContext<NotificationsHub> hubContext,
           IServiceProvider serviceProvider)
            : base(serviceProvider)
        {
            _hubContext = hubContext;
            _onlineClientManager = onlineClientManager;

            Logger = NullLogger<SignalRNotificationPublishProvider>.Instance;
        }

        public override async Task PublishAsync(NotificationInfo notification, IEnumerable<UserIdentifier> identifiers)
        {
            foreach(var identifier in identifiers)
            {
                var onlineClientContext = new OnlineClientContext(notification.TenantId, identifier.UserId);
                var onlineClients = _onlineClientManager.GetAllByContext(onlineClientContext);
                foreach (var onlineClient in onlineClients)
                {
                    try
                    {
                        var signalRClient = _hubContext.Clients.Client(onlineClient.ConnectionId);
                        if (signalRClient == null)
                        {
                            Logger.LogDebug("Can not get user " + onlineClientContext.UserId + " with connectionId " + onlineClient.ConnectionId + " from SignalR hub!");
                            continue;
                        }

                        await signalRClient.SendAsync("getNotification", notification);
                    }
                    catch (Exception ex)
                    {
                        Logger.LogWarning("Could not send notifications to user: {0}", identifier.UserId);
                        Logger.LogWarning("Send to user notifications error: {0}", ex.Message);
                    }
                }
            }
        }
    }
}