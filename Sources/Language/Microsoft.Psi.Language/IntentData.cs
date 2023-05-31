// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Language
{
    using System;
    using System.Runtime.Serialization;
    using System.Runtime.Serialization.Json;
    using System.Text;

    /// <summary>
    /// Represents a collection of intents and entities.
    /// </summary>
    /// <remarks>
    /// This class may be used by language understanding components to represent the detected
    /// intents and extracted entities from spoken or textual input, or utterances.
    /// </remarks>
    [DataContract(Namespace = "http://www.microsoft.com/psi")]
    public class IntentData : IFormattable
    {
        /// <summary>
        /// A static instance of the serializer used to deserialize the JSON object.
        /// </summary>
        private static readonly DataContractJsonSerializer IntentDataSerializer =
            new DataContractJsonSerializer(typeof(IntentData));

        /// <summary>
        /// Gets or sets the original query from which the current intent data was extracted.
        /// </summary>
        [DataMember]
        public string Query { get; set; }

        /// <summary>
        /// Gets or sets the list of intents detected.
        /// </summary>
        [DataMember]
        public Intent[] Intents { get; set; }

        /// <summary>
        /// Gets or sets the list of entities extracted.
        /// </summary>
        [DataMember]
        public Entity[] Entities { get; set; }

        /// <inheritdoc/>
        public override string ToString()
        {
            return this.ToString("G", null);
        }

        /// <inheritdoc/>
        public string ToString(string format, IFormatProvider formatProvider)
        {
            StringBuilder sb = new StringBuilder();
            if (format == "G")
            {
                sb.Append("[");
                if (this.Intents != null)
                {
                    for (int i = 0; i < this.Intents.Length; i++)
                    {
                        if (i > 0)
                        {
                            sb.Append(" ");
                        }

                        sb.Append(this.Intents[i].Value);
                    }
                }

                if (this.Entities != null)
                {
                    for (int i = 0; i < this.Entities.Length; i++)
                    {
                        sb.Append($" ({this.Entities[i].Type} {this.Entities[i].Value})");
                    }
                }

                sb.Append("]");
            }
            else if (format == "L")
            {
                if (this.Intents != null)
                {
                    sb.AppendLine("********* Intents *********");
                    for (int i = 0; i < this.Intents.Length; i++)
                    {
                        sb.AppendFormat(
                            "[{0}] Intent={1} ({2})",
                            i,
                            this.Intents[i].Value,
                            this.Intents[i].Score);
                        sb.AppendLine();
                    }
                }

                if (this.Entities != null)
                {
                    sb.AppendLine("********* Entities *********");
                    for (int i = 0; i < this.Entities.Length; i++)
                    {
                        sb.AppendFormat(
                            "[{0}] {1} = {2} ({3})",
                            i,
                            this.Entities[i].Type,
                            this.Entities[i].Value,
                            this.Entities[i].Score);
                        sb.AppendLine();
                    }
                }
            }

            return sb.ToString();
        }
    }
}
