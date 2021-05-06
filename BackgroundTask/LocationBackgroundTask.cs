//*********************************************************
//
// Copyright (c) Microsoft. All rights reserved.
// THIS CODE IS PROVIDED *AS IS* WITHOUT WARRANTY OF
// ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING ANY
// IMPLIED WARRANTIES OF FITNESS FOR A PARTICULAR
// PURPOSE, MERCHANTABILITY, OR NON-INFRINGEMENT.
//
//*********************************************************

using System;
using System.Threading;
using Windows.ApplicationModel.Background;
using Windows.Storage;
using Windows.Devices.Geolocation;
using Serilog;
using System.IO;

namespace BackgroundTask
{
    public sealed class LocationBackgroundTask : IBackgroundTask
    {
        private CancellationTokenSource _cts = null;
        private static string rootFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

        async void IBackgroundTask.Run(IBackgroundTaskInstance taskInstance)
        {
            BackgroundTaskDeferral deferral = taskInstance.GetDeferral();

            try
            {

               // Windows.Storage.StorageFolder localFolder = Windows.Storage.ApplicationData.Current.LocalFolder;
                string filePath = Path.Combine(rootFolderPath , "locationLogs.txt");
                Log.Logger = new LoggerConfiguration()
                    .MinimumLevel.Debug()
                    .WriteTo.File(filePath, rollingInterval: RollingInterval.Day)
                    .CreateLogger();
                //Log.Information("started background");
                //System.Diagnostics.Debug.WriteLine("LogFilePath:" + filePath);
                // Associate a cancellation handler with the background task.
                taskInstance.Canceled += OnCanceled;

                // Get cancellation token
                if (_cts == null)
                {
                    _cts = new CancellationTokenSource();
                }
                CancellationToken token = _cts.Token;

                // Create geolocator object
                Geolocator geolocator = new Geolocator();

                // Make the request for the current position
                Geoposition pos = await geolocator.GetGeopositionAsync().AsTask(token);

                DateTime currentTime = DateTime.Now;

                WriteStatusToAppData("Time: " + currentTime.ToString());
                WriteGeolocToAppData(pos);
            }
            catch (UnauthorizedAccessException)
            {
                WriteStatusToAppData("Disabled");
                WipeGeolocDataFromAppData();
            }
            catch (Exception ex)
            {
                WriteStatusToAppData(ex.ToString());
                WipeGeolocDataFromAppData();
            }
            finally
            {
                _cts = null;
                deferral.Complete();
            }
        }

        private void WriteGeolocToAppData(Geoposition pos)
        {
            var settings = ApplicationData.Current.LocalSettings;
            settings.Values["Latitude"] = pos.Coordinate.Point.Position.Latitude.ToString();
            settings.Values["Longitude"] = pos.Coordinate.Point.Position.Longitude.ToString();
            settings.Values["Accuracy"] = pos.Coordinate.Accuracy.ToString();

            string msg = String.Format("Latitude: {0}, Longitude: {1}, Accuracy: {2}, Souce: {3}", settings.Values["Latitude"], settings.Values["Longitude"], settings.Values["Accuracy"], pos.Coordinate.PositionSource.ToString());
            LogToFile(msg);
        }

        private void WipeGeolocDataFromAppData()
        {
            var settings = ApplicationData.Current.LocalSettings;
            settings.Values["Latitude"] = "";
            settings.Values["Longitude"] = "";
            settings.Values["Accuracy"] = "";
        }

        private void WriteStatusToAppData(string status)
        {
            var settings = ApplicationData.Current.LocalSettings;
            settings.Values["Status"] = status;
            //LogToFile(status);
        }

        private void OnCanceled(IBackgroundTaskInstance sender, BackgroundTaskCancellationReason reason)
        {
            if (_cts != null)
            {
                _cts.Cancel();
                _cts = null;
            }
        }

        private void LogToFile(string msg)
        {
           Log.Information(msg);
        }
    }
}
