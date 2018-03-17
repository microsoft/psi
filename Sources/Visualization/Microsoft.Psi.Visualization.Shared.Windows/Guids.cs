// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

#if COM_SERVER
namespace Microsoft.Psi.Visualization.Server
#elif COM_CLIENT
namespace Microsoft.Psi.Visualization.Client
#endif
{
    using System;

    /// <summary>
    /// Class that contains IID and CLSID strings and GUIDs.
    /// </summary>
    public static class Guids
    {
        /// <summary>
        /// INotifyRemoteConfigurationChanged IID string.
        /// </summary>
        public const string INotifyRemoteConfigurationChangedIIDString = "8ccd4945-1e8a-4e31-b025-b7a2e2a23748";

        /// <summary>
        /// IRemoteNavigator IID string.
        /// </summary>
        public const string IRemoteNavigatorIIDString = "b7d21bd3-27fb-42c8-bbe4-cfda5310d3fb";

        /// <summary>
        /// IRemoteNavigatorRange IID string.
        /// </summary>
        public const string IRemoteNavigatorRangeIIDString = "bcda8cac-5318-4f61-913e-b65d56d3a1ec";

        /// <summary>
        /// IRemoteStreamVisualizationObject IID string.
        /// </summary>
        public const string IRemoteStreamVisualizationObjectIIDString = "f55b9214-35b2-4b30-8bb1-4472a0764a1d";

        /// <summary>
        /// IRemoteVisualizationContainer IID string.
        /// </summary>
        public const string IRemoteVisualizationContainerIIDString = "0a3cb4c9-22b5-4f77-8630-d857f2f56c6a";

        /// <summary>
        /// IRemoteVisualizationObject IID string.
        /// </summary>
        public const string IRemoteVisualizationObjectIIDString = "a525117f-1f55-4d7b-8e7a-85fad63a9940";

        /// <summary>
        /// IRemoteVisualizationPanel IID string.
        /// </summary>
        public const string IRemoteVisualizationPanelIIDString = "4427610a-5a70-435e-a0c9-a0f9239dc2f9";

        /// <summary>
        /// IRemoteVisualizationService IID string.
        /// </summary>
        public const string IRemoteVisualizationServiceIIDString = "2c106527-c445-4fa3-9b27-04b17059ea03";

        /// <summary>
        /// RemoteNavigator CLSID string.
        /// </summary>
        public const string RemoteNavigatorCLSIDString = "5c326e01-7348-4a71-9190-b2e205abf585";

        /// <summary>
        /// RemoteNavigatorRange CLSID string.
        /// </summary>
        public const string RemoteNavigatorRangeCLSIDString = "e2c630cb-b2fc-482e-bb18-11d7a7d14a25";

        /// <summary>
        /// RemoteStreamVisualizationObject CLSID string.
        /// </summary>
        public const string RemoteStreamVisualizationObjectCLSIDString = "43ce8015-e2bb-427d-ae74-4546dde042f8";

        /// <summary>
        /// RemoteVisualizationContainer CLSID string.
        /// </summary>
        public const string RemoteVisualizationContainerCLSIDString = "05fcfd66-26a1-4bb8-967c-6f1a64105e0e";

        /// <summary>
        /// RemoteVisualizationObject CLSID string.
        /// </summary>
        public const string RemoteVisualizationObjectCLSIDString = "fc837d10-71c7-4c8c-8a2f-c610d2ccb202";

        /// <summary>
        /// RemoteVisualizationPanel CLSID string.
        /// </summary>
        public const string RemoteVisualizationPanelCLSIDString = "14e94ae6-6c61-4aa8-b1bb-b868b1a226cf";

        /// <summary>
        /// RemoteVisualizationService CLSID string.
        /// </summary>
        public const string RemoteVisualizationServiceCLSIDString = "8f2d3209-e9cf-49a0-bccb-18264ac9f676";

        /// <summary>
        /// INotifyRemoteConfigurationChanged IID Guid.
        /// </summary>
        public static readonly Guid INotifyRemoteConfigurationChangedIID = new Guid(INotifyRemoteConfigurationChangedIIDString);

        /// <summary>
        /// IRemoteNavigator IID Guid.
        /// </summary>
        public static readonly Guid IRemoteNavigatorIID = new Guid(IRemoteNavigatorIIDString);

        /// <summary>
        /// IRemoteNavigatorRange IID Guid.
        /// </summary>
        public static readonly Guid IRemoteNavigatorRangeIID = new Guid(IRemoteNavigatorRangeIIDString);

        /// <summary>
        /// IRemoteStreamVisualizationObject IID Guid.
        /// </summary>
        public static readonly Guid IRemoteStreamVisualizationObjectIID = new Guid(IRemoteStreamVisualizationObjectIIDString);

        /// <summary>
        /// IRemoteVisualizationContainer IID Guid.
        /// </summary>
        public static readonly Guid IRemoteVisualizationContainerIID = new Guid(IRemoteVisualizationContainerIIDString);

        /// <summary>
        /// IRemoteVisualizationObject IID Guid.
        /// </summary>
        public static readonly Guid IRemoteVisualizationObjectIID = new Guid(IRemoteVisualizationObjectIIDString);

        /// <summary>
        /// IRemoteVisualizationPanel IID Guid.
        /// </summary>
        public static readonly Guid IRemoteVisualizationPanelIID = new Guid(IRemoteVisualizationPanelIIDString);

        /// <summary>
        /// IRemoteVisualizationService IID Guid.
        /// </summary>
        public static readonly Guid IRemoteVisualizationServiceIID = new Guid(IRemoteVisualizationServiceIIDString);

        /// <summary>
        /// RemoteNavigator CLSID Guid.
        /// </summary>
        public static readonly Guid RemoteNavigatorCLSID = new Guid(RemoteNavigatorCLSIDString);

        /// <summary>
        /// RemoteNavigatorRange CLSID Guid.
        /// </summary>
        public static readonly Guid RemoteNavigatorRangeCLSID = new Guid(RemoteNavigatorRangeCLSIDString);

        /// <summary>
        /// RemoteStreamVisualizationObject CLSID Guid.
        /// </summary>
        public static readonly Guid RemoteStreamVisualizationObjectCLSID = new Guid(RemoteStreamVisualizationObjectCLSIDString);

        /// <summary>
        /// RemoteVisualizationContainer CLSID Guid.
        /// </summary>
        public static readonly Guid RemoteVisualizationContainerCLSID = new Guid(RemoteVisualizationContainerCLSIDString);

        /// <summary>
        /// RemoteVisualizationObject CLSID Guid.
        /// </summary>
        public static readonly Guid RemoteVisualizationObjectCLSID = new Guid(RemoteVisualizationObjectCLSIDString);

        /// <summary>
        /// RemoteVisualizationPanel CLSID Guid.
        /// </summary>
        public static readonly Guid RemoteVisualizationPanelCLSID = new Guid(RemoteVisualizationPanelCLSIDString);

        /// <summary>
        /// RemoteVisualizationService CLSID Guid.
        /// </summary>
        public static readonly Guid RemoteVisualizationServiceCLSID = new Guid(RemoteVisualizationServiceCLSIDString);
    }
}