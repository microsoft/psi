// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Adapters
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.CodeAnalysis.CSharp.Scripting;
    using Microsoft.CodeAnalysis.Scripting;
    using Microsoft.Psi.Visualization.Data;
    using Microsoft.Psi.Visualization.DataTypes;

    /// <summary>
    /// Implements a stream adapter that adapts the stream values by running a C# script.
    /// </summary>
    /// <typeparam name="TSource">The type of the source stream.</typeparam>
    /// <typeparam name="TResult">The type of the result of the script evaluation.</typeparam>
    public class ScriptAdapter<TSource, TResult> : StreamAdapter<TSource, TResult>
    {
        private readonly Task<Script<TResult>> compileScriptTask;
        private Script<TResult> script;

        /// <summary>
        /// Initializes a new instance of the <see cref="ScriptAdapter{TSource, TResult}"/> class.
        /// </summary>
        /// <param name="scriptCode">The C# script to run.</param>
        /// <param name="usings">The list of usings.</param>
        public ScriptAdapter(string scriptCode, IEnumerable<string> usings)
            : base()
        {
            // Run the script compilation task in the background so it will run in the background and be ready upon first use
            this.compileScriptTask = Task.Run(() =>
            {
                // Load all currently loaded assemblies, but exclude those that are dynamically generated
                var assemblies = AppDomain.CurrentDomain.GetAssemblies().Where(a => !a.IsDynamic && !string.IsNullOrEmpty(a.Location));
                var options = ScriptOptions.Default.WithReferences(assemblies).WithImports(usings);
                var script = CSharpScript.Create<TResult>(scriptCode, options, typeof(ScriptGlobals<TSource>));
                var diagnostics = script.Compile();
                if (!diagnostics.IsEmpty)
                {
                    throw new CompilationErrorException("Script Error", diagnostics);
                }

                // Return the compiled script
                return script;
            });
        }

        /// <inheritdoc/>
        public override TResult GetAdaptedValue(TSource source, Envelope envelope)
        {
            if (this.script == null)
            {
                // cache the script once it has finished compiling
                this.script = this.compileScriptTask.GetAwaiter().GetResult();
            }

            var globals = new ScriptGlobals<TSource>(source, envelope);
            var result = this.script.RunAsync(globals).Result;
            return result.ReturnValue;
        }
    }
}
