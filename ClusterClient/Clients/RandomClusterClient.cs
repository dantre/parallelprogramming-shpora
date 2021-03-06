﻿using System;
using System.Threading.Tasks;
using log4net;

namespace ClusterClient.Clients
{
    public class RandomClusterClient : ClusterClientBase
    {
        
        public RandomClusterClient(string[] replicaAddresses)
            : base(replicaAddresses)
        {
        }

        public async override Task<string> ProcessRequestAsync(string query, TimeSpan timeout)
        {
            var randomUri = GetReplicaAddress();
            var randomWebRequest = CreateRequest(randomUri + "?query=" + query);
            
            Log.InfoFormat("Processing {0}", randomWebRequest.RequestUri);

            var resultTask = ProcessRequestInternalAsync(randomWebRequest);
            await Task.WhenAny(resultTask, Task.Delay(timeout));
            if (!resultTask.IsCompleted)
            {
                grayList.Dict.TryAdd(randomUri,  DateTime.Now.Add(GrayListWaitTime));
                throw new TimeoutException();
            }
            return resultTask.Result;
        }

        private string GetReplicaAddress()
        {
            string replica;
            grayList.UpdateGrayTable();

            var rnd = new Random();
            do
            {
                replica = ReplicaAddresses[rnd.Next(ReplicaAddresses.Length)];
            } while (grayList.Dict.ContainsKey(replica));
            return replica;
        }

        protected override ILog Log => LogManager.GetLogger(typeof(RandomClusterClient));
    }
}