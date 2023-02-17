// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using MongoDB.Driver;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;

namespace Microsoft.BotBuilderSamples
{
    public class ReviewSelectionDialog : ComponentDialog
    {
        // Define a "done" response for the company selection prompt.
        private const string DoneOption = "done";
        
        // Define value names for values tracked inside the dialogs.
        private const string CompaniesSelected = "value-companiesSelected";

        // Define the company choices for the company selection prompt.
        private readonly string[] _companyOptions = new string[]
        {
            "Adatum Corporation", "Contoso Suites", "Graphic Design Institute", "Wide World Importers",
        };

        public ReviewSelectionDialog()
            : base(nameof(ReviewSelectionDialog))
        {
            AddDialog(new ChoicePrompt(nameof(ChoicePrompt)));

            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[]
                {
                    SelectionStepAsync,
                    LoopStepAsync,
                }));

            InitialDialogId = nameof(WaterfallDialog);
        }

        private async Task<DialogTurnResult> SelectionStepAsync(
            WaterfallStepContext stepContext,
            CancellationToken cancellationToken)
        {
            // Continue using the same selection list, if any, from the previous iteration of this dialog.
            var list = stepContext.Options as List<string> ?? new List<string>();
            stepContext.Values[CompaniesSelected] = list;
            var mongoInstance = MongoDBSingleton.Instance;
            var client = mongoInstance.Client;
            var database = client.GetDatabase("Test");
            
            var collection = database.GetCollection<BsonDocument>("Test");
            var filter = Builders<BsonDocument>.Filter.Eq("Id", stepContext.Context.Activity.From.Id);
            var result = await collection.Find(filter).FirstOrDefaultAsync();
            if (!string.IsNullOrEmpty(((string)result["choix1"])))
            {
                list.Add((string)result["choix1"]);
            }
            // Create a prompt message.
            string message;
            if ((list.Count is 0) ) 
            {
                message = $"Please choose a company to review, or `{DoneOption}` to finish.";
            }
            else
            {
                message = $"You have selected **{list[0]}**. You can review an additional company, " +
                    $"or choose `{DoneOption}` to finish.";
            }

            // Create the list of options to choose from.
            var options = _companyOptions.ToList();
            options.Add(DoneOption);
            if (list.Count > 0)
            {
                options.Remove(list[0]);
            }

            var promptOptions = new PromptOptions
            {
                Prompt = MessageFactory.Text(message),
                RetryPrompt = MessageFactory.Text("Please choose an option from the list."),
                Choices = ChoiceFactory.ToChoices(options),
            };

            // Prompt the user for a choice.
            return await stepContext.PromptAsync(nameof(ChoicePrompt), promptOptions, cancellationToken);
        }

        private async Task<DialogTurnResult> LoopStepAsync(
            WaterfallStepContext stepContext,
            CancellationToken cancellationToken)
        {
            // Retrieve their selection list, the choice they made, and whether they chose to finish.
            var list = stepContext.Values[CompaniesSelected] as List<string>;
            var choice = (FoundChoice)stepContext.Result;
            var done = choice.Value == DoneOption;

            var mongoInstance = MongoDBSingleton.Instance;
            var client = mongoInstance.Client;
            var database = client.GetDatabase("Test");

            var collection = database.GetCollection<BsonDocument>("Test");
            var filter = Builders<BsonDocument>.Filter.Eq("Id", stepContext.Context.Activity.From.Id);
            var result = await collection.Find(filter).FirstOrDefaultAsync();

            
            if (list.Count < 2)
            {
                // If they chose a company, add it to the list.
                list.Add(choice.Value);
                if (string.IsNullOrEmpty((string)result["choix1"]))
                {
                    
                    var update = Builders<BsonDocument>.Update.Set("choix1", choice.Value);
                    var resulte = collection.UpdateOne(filter, update);
                }
                if (!string.IsNullOrEmpty((string)result["choix1"]) && ((string.IsNullOrEmpty((string)result["choix2"]))))
                {
                    var update = Builders<BsonDocument>.Update.Set("choix2", choice.Value);
                    var resulte = collection.UpdateOne(filter, update);
                }

                   
            }

            if (( list.Count >= 2) )
            {
                // If they're done, exit and return their list.
                return await stepContext.EndDialogAsync(list, cancellationToken);
            }
            else
            {
                // Otherwise, repeat this dialog, passing in the list from this iteration.
                return await stepContext.ReplaceDialogAsync(nameof(ReviewSelectionDialog), list, cancellationToken);
            }
        }
    }
}
