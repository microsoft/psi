// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Serialization
{
    using System.Reflection;

    /// <summary>
    /// The type member schema information.
    /// </summary>
    public sealed class TypeMemberSchema
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TypeMemberSchema"/> class.
        /// </summary>
        /// <param name="name">The member name.</param>
        /// <param name="type">The type name, in contract form (either data contract name or assembly-qualified name).</param>
        /// <param name="isRequired">True if the member is required.</param>
        /// <param name="memberInfo">A fieldInfo or PropertyInfo object for this member. Optional.</param>
        public TypeMemberSchema(string name, string type, bool isRequired, MemberInfo memberInfo = null)
        {
            this.Name = name;
            this.Type = type;
            this.IsRequired = isRequired;
            this.MemberInfo = memberInfo;
        }

        /// <summary>
        /// Gets the name of the member.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Gets the type of the member, in contract form (either data contract name or assembly-qualified name).
        /// </summary>
        public string Type { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the member is required.
        /// </summary>
        public bool IsRequired { get; private set; }

        /// <summary>
        /// Gets the PropertyInfo or FieldInfo specification for this member.
        /// </summary>
        public MemberInfo MemberInfo { get; private set; }

        /// <inheritdoc/>
        public override string ToString()
        {
            return this.Name;
        }
    }
}
