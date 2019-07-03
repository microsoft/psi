// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Speech
{
    using System.Collections.Generic;
    using System.Globalization;
    using System.Speech.Recognition;
    using Microsoft.Psi.Language;

    /// <summary>
    /// Static helper methods.
    /// </summary>
    public static class SystemSpeech
    {
        /// <summary>
        /// Method to construct the IntentData (intents and entities) from
        /// a SemanticValue.
        /// </summary>
        /// <param name="semanticValue">The SemanticValue object.</param>
        /// <returns>An IntentData object containing the intents and entities.</returns>
        public static IntentData BuildIntentData(SemanticValue semanticValue)
        {
            List<Intent> intentList = new List<Intent>();

            if (semanticValue.Value != null)
            {
                intentList.Add(new Intent()
                {
                    Value = semanticValue.Value.ToString(),
                    Score = semanticValue.Confidence,
                });
            }

            // Consider top-level semantics to be intents.
            foreach (var entry in semanticValue)
            {
                intentList.Add(new Intent()
                {
                    Value = entry.Key,
                    Score = entry.Value.Confidence,
                });
            }

            List<Entity> entityList = ExtractEntities(semanticValue);

            return new IntentData()
            {
                Intents = intentList.ToArray(),
                Entities = entityList.ToArray(),
            };
        }

        /// <summary>
        /// Creates a new speech recognition engine.
        /// </summary>
        /// <param name="language">The language for the recognition engine.</param>
        /// <param name="grammars">The grammars to load.</param>
        /// <returns>A new speech recognition engine object.</returns>
        internal static SpeechRecognitionEngine CreateSpeechRecognitionEngine(string language, GrammarInfo[] grammars)
        {
            var recognizer = new SpeechRecognitionEngine(new CultureInfo(language));
            if (grammars == null)
            {
                recognizer.LoadGrammar(new DictationGrammar());
            }
            else
            {
                foreach (GrammarInfo grammarInfo in grammars)
                {
                    Grammar grammar = new Grammar(grammarInfo.FileName)
                    {
                        Name = grammarInfo.Name,
                    };
                    recognizer.LoadGrammar(grammar);
                }
            }

            return recognizer;
        }

        /// <summary>
        /// Method to extract all entities contained within a SemanticValue.
        /// </summary>
        /// <param name="semanticValue">The SemanticValue object.</param>
        /// <returns>The list of extracted entities.</returns>
        private static List<Entity> ExtractEntities(SemanticValue semanticValue)
        {
            List<Entity> entityList = new List<Entity>();
            foreach (var entry in semanticValue)
            {
                // We currently only consider leaf nodes (whose underlying
                // value is of type string) as entities.
                if (entry.Value.Value is string)
                {
                    // Extract the entity's type (key), value and confidence score.
                    entityList.Add(new Entity()
                    {
                        Type = entry.Key,
                        Value = (string)entry.Value.Value,
                        Score = entry.Value.Confidence,
                    });
                }
                else
                {
                    // Keep looking for leaf nodes.
                    entityList.AddRange(ExtractEntities(entry.Value));
                }
            }

            return entityList;
        }
    }
}
