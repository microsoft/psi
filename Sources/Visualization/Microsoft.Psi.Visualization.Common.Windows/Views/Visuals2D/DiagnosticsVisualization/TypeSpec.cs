// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.PsiStudio.TypeSpec
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    /// <summary>
    /// Class representing type specification.
    /// </summary>
    public class TypeSpec
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TypeSpec"/> class.
        /// </summary>
        /// <param name="typeName">Fully qualified type name.</param>
        public TypeSpec(string typeName)
            : this(Parse(Lex(typeName).ToArray()))
        {
        }

        private TypeSpec(string name, string modifier, List<TypeSpec> typeParams)
        {
            this.Name = name;
            this.Modifier = modifier;
            this.TypeParams = typeParams;
        }

        private TypeSpec(string name, string modifier)
            : this(name, modifier, new List<TypeSpec>())
        {
        }

        private TypeSpec(TypeSpec spec)
            : this(spec.Name, spec.Modifier, spec.TypeParams)
        {
        }

        private enum TokenKind
        {
            None,
            Name,
            Separator, // . , `
            Open, // [
            Close, // ]
        }

        /// <summary>
        /// Gets type name.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Gets type modifier (e.g. array "[]").
        /// </summary>
        public string Modifier { get; private set; }

        /// <summary>
        /// Gets generic type parameters.
        /// </summary>
        public List<TypeSpec> TypeParams { get; private set; }

        /// <summary>
        /// Simplify fully qualified type name.
        /// </summary>
        /// <param name="typeName">Fully qualified type name.</param>
        /// <returns>Simplified name.</returns>
        public static string Simplify(string typeName)
        {
            return new TypeSpec(typeName).ToString();
        }

        /// <inheritdoc />
        public override string ToString()
        {
            var builder = new StringBuilder();
            this.BuildString(builder);
            return builder.ToString();
        }

        /// <summary>
        /// Tokenize string into lexical pieces.
        /// </summary>
        /// <param name="value">Full or partial typename.</param>
        /// <returns>Sequence of tokens.</returns>
        private static IEnumerable<Token> Lex(string value)
        {
            // see https://docs.microsoft.com/en-us/dotnet/framework/reflection-and-codedom/specifying-fully-qualified-type-names
            var j = 0;
            for (var i = 0; i < value.Length; i++)
            {
                var c = value[i];
                var kind = TokenKind.None;
                switch (c)
                {
                    case '\\':
                        i++; // include escaped char (even brackets, commas, etc.)
                        continue;
                    case '.':
                    case ',':
                    case '`':
                        kind = TokenKind.Separator;
                        break;
                    case '[':
                        kind = TokenKind.Open;
                        break;
                    case ']':
                        kind = TokenKind.Close;
                        break;
                }

                if (kind != TokenKind.None)
                {
                    if (i > j)
                    {
                        yield return new Token(TokenKind.Name, value.Substring(j, i - j)); // preceeding name, if any
                    }

                    j = i + 1;
                    yield return new Token(kind, value.Substring(i, 1));
                }
            }

            if (j < value.Length)
            {
                yield return new Token(TokenKind.Name, value.Substring(j)); // final
            }
        }

        /// <summary>
        /// Parse tokens into recursive TypeSpec structure.
        /// </summary>
        /// <param name="tokens">Lexical tokens.</param>
        /// <returns>Type specification.</returns>
        private static TypeSpec Parse(Token[] tokens)
        {
            StringBuilder name = new StringBuilder();
            StringBuilder modifier = new StringBuilder();
            for (var i = 0; i < tokens.Length; i++)
            {
                var t = tokens[i];
                switch (t.Kind)
                {
                    case TokenKind.Name:
                        name.Clear();
                        name.Append(t.Value);
                        break;
                    case TokenKind.Open:
                        modifier.Append('['); // before ` braces represent arrays
                        break;
                    case TokenKind.Close:
                        modifier.Append(']'); // before ` braces represent arrays
                        break;
                    case TokenKind.Separator:
                        switch (t.Value)
                        {
                            case ",":
                                return new TypeSpec(name.ToString(), modifier.ToString()); // e.g. [Foo.Bar[], ... -> Bar []
                            case "`":
                                // generic type specification
                                var spec = new TypeSpec(name.ToString(), modifier.ToString());
                                if (i + 3 >= tokens.Length || tokens[i + 2].Kind != TokenKind.Open || tokens[i + 3].Kind != TokenKind.Open)
                                {
                                    return spec; // some types appear to be generic, but are _not_ followed by type params
                                }

                                // parse type params in the form [[...<param0>...],[...<param1>...],...] by matching braces and recursing
                                int match = 0;
                                for (var j = i + 3; j < tokens.Length; j++)
                                {
                                    switch (tokens[j].Kind)
                                    {
                                        case TokenKind.Open:
                                            match++;
                                            break;
                                        case TokenKind.Close:
                                            match--;
                                            break;
                                    }

                                    // sub [...<param>...] part
                                    if (match == 0 && i + 4 < tokens.Length)
                                    {
                                        var len = j - i - 5;
                                        var childTokens = new Token[len];
                                        Array.Copy(tokens, i + 4, childTokens, 0, len);
                                        spec.TypeParams.Add(Parse(childTokens)); // recurse
                                        if (j + 1 < tokens.Length && tokens[j + 1].Kind == TokenKind.Close)
                                        {
                                            return spec; // final
                                        }
                                        else
                                        {
                                            j++;
                                            i = j;
                                            if (j + 1 >= tokens.Length || tokens[j].Kind != TokenKind.Separator || tokens[j].Value != "," || tokens[j + 1].Kind != TokenKind.Open)
                                            {
                                                throw new ArgumentException("Malformed generic syntax. Expected \"Foo`123[[...],[...\"");
                                            }
                                        }
                                    }
                                }

                                break;
                        }

                        break;
                }
            }

            return new TypeSpec(name.ToString(), modifier.ToString()); // undelimited single type
        }

        /// <summary>
        /// Convert type specification to simplified description.
        /// </summary>
        /// <param name="builder">String builder recursively accumulating description.</param>
        private void BuildString(StringBuilder builder)
        {
            var tuple = false;
            switch (this.Name)
            {
                // see https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/built-in-types-table
                case "Boolean":
                    builder.Append("bool");
                    break;
                case "Single":
                    builder.Append("float");
                    break;
                case "Int16":
                    builder.Append("short");
                    break;
                case "UInt16":
                    builder.Append("ushort");
                    break;
                case "Int32":
                    builder.Append("int");
                    break;
                case "UInt32":
                    builder.Append("uint");
                    break;
                case "Int64":
                    builder.Append("long");
                    break;
                case "UInt64":
                    builder.Append("ulong");
                    break;
                case "Byte":
                case "SByte":
                case "Char":
                case "Decimal":
                case "Double":
                case "Object":
                case "String":
                    builder.Append(this.Name.ToLower());
                    break;
                case "Tuple":
                case "ValueTuple":
                    tuple = true;
                    break;
                default:
                    builder.Append(this.Name);
                    break;
            }

            if (this.TypeParams.Count > 0)
            {
                builder.Append(tuple ? '(' : '<');
                for (var i = 0; i < this.TypeParams.Count; i++)
                {
                    if (i > 0)
                    {
                        builder.Append(", ");
                    }

                    this.TypeParams[i].BuildString(builder);
                }

                builder.Append(tuple ? ')' : '>');
            }

            builder.Append(this.Modifier);
        }

        private class Token
        {
            public Token(TokenKind kind, string value)
            {
                this.Kind = kind;
                this.Value = value;
            }

            public TokenKind Kind { get; private set; }

            public string Value { get; private set; }
        }
    }
}
