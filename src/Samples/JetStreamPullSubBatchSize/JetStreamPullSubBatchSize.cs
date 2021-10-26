﻿// Copyright 2021 The NATS Authors
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using NATS.Client;
using NATS.Client.JetStream;

namespace NATSExamples
{
    /// <summary>
    /// This example will demonstrate basic use of a pull subscription of:
    /// batch size only pull: <c>Pull(int batchSize)</c>
    /// </summary>
    internal static class JetStreamPullSubBatchSize
    {
        private const string Usage = 
            "Usage: JetStreamPullSubBatchSize [-url url] [-creds file] [-stream stream] " +
            "[-subject subject] [-durable durable] [-count count]" +
            "\n\nDefault Values:" +
            "\n   [-stream]  pull-stream" +
            "\n   [-subject] pull-subject" +
            "\n   [-durable] pull-durable" +
            "\n   [-count]   15";

        public static void Main(string[] args)
        {
            ArgumentHelper helper = new ArgumentHelperBuilder("Pull Subscription using primitive Batch Size", args, Usage)
                .DefaultStream("pull-stream")
                .DefaultSubject("pull-subject")
                .DefaultDurable("pull-durable")
                .DefaultCount(15)
                .Build();

            try
            {
                using (IConnection c = new ConnectionFactory().CreateConnection(helper.MakeOptions()))
                {
                    // Create a JetStreamManagement context.
                    IJetStreamManagement jsm = c.CreateJetStreamManagementContext();
                    
                    // Use the utility to create a stream stored in memory.
                    JsUtils.CreateStreamExitWhenExists(jsm, helper.Stream, helper.Subject);

                    // Create our JetStream context.
                    IJetStream js = c.CreateJetStreamContext();

                    // Start publishing the messages, don't wait for them to finish, simulating an outside producer.
                    JsUtils.PublishInBackground(js, helper.Subject, "pull-message", helper.Count);

                    // Build our subscription options. Durable is REQUIRED for pull based subscriptions
                    PullSubscribeOptions pullOptions = PullSubscribeOptions.Builder()
                        .WithDurable(helper.Durable) // required
                        .Build();

                    // subscribe
                    IJetStreamPullSubscription sub = js.PullSubscribe(helper.Subject, pullOptions);
                    c.Flush(1000);

                    bool keepGoing = true;
                    int red = 0;
                    while (keepGoing && red < helper.Count) {
                        sub.Pull(10);
                        int round = 0;
                        while (keepGoing && round < 10)
                        {
                            try
                            {
                                Msg m = sub.NextMessage(1000); // first message
                                Console.WriteLine($"{++red}. Message: {m}");
                                m.Ack();
                                round++;
                            }
                            catch (NATSTimeoutException) // timeout means there are no messages available
                            {
                                keepGoing = false;
                            }
                        }
                    }

                    // delete the stream since we are done with it.
                    jsm.DeleteStream(helper.Stream);
                }
            }
            catch (Exception ex)
            {
                helper.ReportException(ex);
            }
        }
    }
}
