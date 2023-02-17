// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Bson.Serialization.Attributes;
using System;

using Microsoft.Identity.Client;

namespace Microsoft.BotBuilderSamples
{
    public class DialogAndWelcomeBot<T> : DialogBot<T> where T : Dialog
    {
        public DialogAndWelcomeBot(ConversationState conversationState, UserState userState, T dialog, ILogger<DialogBot<T>> logger)
            : base(conversationState, userState, dialog, logger)
        {
        }

        protected override async Task OnMembersAddedAsync(
    IList<ChannelAccount> membersAdded,
    ITurnContext<IConversationUpdateActivity> turnContext,
    CancellationToken cancellationToken)
{
    foreach (var member in membersAdded)
    {
        if (member.Id != turnContext.Activity.Recipient.Id)
        {
            var mongoInstance = MongoDBSingleton.Instance;
            var client = mongoInstance.Client;
            var database = client.GetDatabase("Test");
            var collection = database.GetCollection<BsonDocument>("Test");
            

            var filter = Builders<BsonDocument>.Filter.Eq("Id", turnContext.Activity.From.Id);
            var result = await collection.Find(filter).FirstOrDefaultAsync();

            if (result == null)
            {
                var reply = MessageFactory.Text($"Welcome to Complex Dialog Bot {member.Name}. " +
                    "This bot provides a complex conversation, with multiple dialogs. " +
                    "Type anything to get started.");
                
                        var document = new BsonDocument
            {
                { "Id", turnContext.Activity.From.Id },
                { "name", "" },
                { "age", 0 },
                { "choix1", "" },
                { "choix2", "" },
                
            };

            collection.InsertOne(document);
            await turnContext.SendActivityAsync(reply, cancellationToken);
            }
            else
            {
                await turnContext.SendActivityAsync(MessageFactory.Text("Tab Any Key to complete the conversation"), cancellationToken);
               //continue;
            }
        }
    }
}

    }
}
