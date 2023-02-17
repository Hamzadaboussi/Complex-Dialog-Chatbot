// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Bson.Serialization.Attributes;
using System;
using Microsoft.Bot.Schema;
using Microsoft.Identity.Client;

namespace Microsoft.BotBuilderSamples
{
    public class TopLevelDialog : ComponentDialog
    {
        // Define value names for values tracked inside the dialogs.
        private const string UserInfo = "value-userInfo";
        public int c = 0 ;
        
        
        

        public TopLevelDialog()
            : base(nameof(TopLevelDialog))
        {
           
            

            AddDialog(new TextPrompt(nameof(TextPrompt)));
            AddDialog(new NumberPrompt<int>(nameof(NumberPrompt<int>)));
            AddDialog(new ReviewSelectionDialog());

            
            
            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[]
                {
                    NameStepAsync,
                    AgeStepAsync,
                    StartSelectionStepAsync,
                    AcknowledgementStepAsync,
                }));

            InitialDialogId = nameof(WaterfallDialog);
        }

        private static async Task<DialogTurnResult> NameStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            //var userProfile = (UserProfile)stepContext.Values[UserInfo];
            var mongoInstance = MongoDBSingleton.Instance;
            var client = mongoInstance.Client;
            var database = client.GetDatabase("Test");
            var collection = database.GetCollection<BsonDocument>("Test");
            var filter = Builders<BsonDocument>.Filter.Eq("Id", stepContext.Context.Activity.From.Id);
            var result = await collection.Find(filter).FirstOrDefaultAsync();
            stepContext.Values[UserInfo] = new UserProfile();
            if(string.IsNullOrEmpty((string)result["name"] ))
            {         
                var promptOptions = new PromptOptions { Prompt = MessageFactory.Text("Please enter your name.") };
                return await stepContext.PromptAsync(nameof(TextPrompt), promptOptions, cancellationToken);

            }
            else { 
                 //userProfile.Name = (string)result["name"];
                 return await stepContext.NextAsync();
            }
            
            
        }

        private async Task<DialogTurnResult> AgeStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            // Set the user's name to what they entered in response to the name prompt.
            var userProfile = (UserProfile)stepContext.Values[UserInfo];

            var mongoInstance = MongoDBSingleton.Instance;
            var client = mongoInstance.Client;
            var database = client.GetDatabase("Test");

            var collection = database.GetCollection<BsonDocument>("Test");
            var filter = Builders<BsonDocument>.Filter.Eq("Id", stepContext.Context.Activity.From.Id);
            var result = await collection.Find(filter).FirstOrDefaultAsync();
            if  (string.IsNullOrEmpty((string)result["name"])) { 
                userProfile.Name = (string)stepContext.Result;
                var update = Builders<BsonDocument>.Update.Set("name", (string)stepContext.Result);
                var resulte = collection.UpdateOne(filter, update);
                var promptOptions = new PromptOptions { Prompt = MessageFactory.Text("Please enter your age.") };
                
                // Ask the user to enter their age.
                return await stepContext.PromptAsync(nameof(NumberPrompt<int>), promptOptions, cancellationToken);
            }
            else
            { userProfile.Name = (string)result["name"];
               
                if ((int)result["age"] == 0)
                {
                var promptOptions = new PromptOptions { Prompt = MessageFactory.Text("Please enter your age.") };

                // Ask the user to enter their age.
                return await stepContext.PromptAsync(nameof(NumberPrompt<int>), promptOptions, cancellationToken);
                }
                else {
                    return await stepContext.NextAsync(); 
                      }
            }
            
            
        }

        private async Task<DialogTurnResult> StartSelectionStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            // Set the user's age to what they entered in response to the age prompt.
            var userProfile = (UserProfile)stepContext.Values[UserInfo];
            var mongoInstance = MongoDBSingleton.Instance;
            var client = mongoInstance.Client;
            var database = client.GetDatabase("Test");

            var collection = database.GetCollection<BsonDocument>("Test");
            var filter = Builders<BsonDocument>.Filter.Eq("Id", stepContext.Context.Activity.From.Id);
            var result = await collection.Find(filter).FirstOrDefaultAsync();


            if (result["age"] == 0) { 
                 userProfile.Age = (int)stepContext.Result;
                 var update = Builders<BsonDocument>.Update.Set("age", (int)stepContext.Result);
                 var resulte = collection.UpdateOne(filter, update);
             
            }
            else
            {
                userProfile.Age=(int)result["age"];
            }
            
            
            if (userProfile.Age < 25)
                {
                    await stepContext.Context.SendActivityAsync(
                        MessageFactory.Text("You must be 25 or older to participatee."),
                        cancellationToken);
                    return await stepContext.NextAsync(new List<string>(), cancellationToken);
                }

            if ((string.IsNullOrEmpty((string)result["choix1"]))||(string.IsNullOrEmpty((string)result["choix2"]))){

                 
                 
                    // Otherwise, start the review selection dialog.
                    return await stepContext.BeginDialogAsync(nameof(ReviewSelectionDialog), null, cancellationToken);
                
            }
            else
            {
               return await stepContext.NextAsync(); 
            }
        }

        private async Task<DialogTurnResult> AcknowledgementStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            // Set the user's company selection to what they entered in the review-selection dialog.
            var userProfile = (UserProfile)stepContext.Values[UserInfo];
            userProfile.CompaniesToReview = stepContext.Result as List<string> ?? new List<string>();
            
            // Thank them for participating.
            await stepContext.Context.SendActivityAsync(
                MessageFactory.Text($"Thanks for participating, {((UserProfile)stepContext.Values[UserInfo]).Name}."),
                cancellationToken);

            // Exit the dialog, returning the collected user information.
            return await stepContext.EndDialogAsync(stepContext.Values[UserInfo], cancellationToken);
        }
    }
}

public sealed class MongoDBSingleton
{
    private static readonly MongoDBSingleton instance = new MongoDBSingleton();
    private static MongoClient client;

    private MongoDBSingleton()
    {
        string connectionString = "mongodb+srv://Hamza_Daboussi:hamza123@test.nfjkagp.mongodb.net/test";
        client = new MongoClient(connectionString);
    }

    public static MongoDBSingleton Instance
    {
        get
        {
            return instance;
        }
    }

    public MongoClient Client
    {
        get
        {
            return client;
        }
    }
}
