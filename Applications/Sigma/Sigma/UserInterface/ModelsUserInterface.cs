// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Microsoft Research license.

namespace Sigma
{
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.Psi;
    using StereoKit;
    using Renderer = Microsoft.Psi.MixedReality.Applications.Renderer;

    /// <summary>
    /// Implements a StereoKit user interface for displaying a set of models.
    /// </summary>
    public class ModelsUserInterface
    {
        private readonly Dictionary<string, Model> availableModels;
        private readonly Dictionary<string, ModelUserInterfaceCommand> currentModelUserInterfaceCommands = new ();

        /// <summary>
        /// Initializes a new instance of the <see cref="ModelsUserInterface"/> class.
        /// </summary>
        /// <param name="availableModels">The set of available models.</param>
        public ModelsUserInterface(Dictionary<string, Model> availableModels)
            => this.availableModels = availableModels;

        /// <summary>
        /// Gets the state of the displayed models.
        /// </summary>
        public Dictionary<string, ModelUserInterfaceState> State =>
            this.currentModelUserInterfaceCommands
                .Where(mdc => mdc.Value.Visible)
                .ToDictionary(
                    kvp => kvp.Key,
                    kvp => new ModelUserInterfaceState()
                    {
                        ModelName = kvp.Value.ModelName,
                        ModelType = kvp.Value.ModelType,
                        Pose = kvp.Value.Pose,
                        Wireframe = kvp.Value.Wireframe,
                    });

        /// <summary>
        /// Updates the current displayed models.
        /// </summary>
        /// <param name="modelUserInterfaceCommands">A set of model user interface commands.</param>
        /// <exception cref="System.Exception">Throws an exception if the model user interface commands do not provide enough information to resolve all models in world coordinates.</exception>
        public void Update(Dictionary<string, ModelUserInterfaceCommand> modelUserInterfaceCommands)
        {
            // find the list of model names for which we have an actual model (the type is available in the configuration)
            var availableModelNames = modelUserInterfaceCommands.Values.Where(mds => this.availableModels.ContainsKey(mds.ModelType)).Select(mds => mds.ModelName).ToList();

            // Construct the list of new model user interface commands by iterating
            // over the incoming updates list. Throughout this process, if an update arrives for a
            // model that is already displayed (i.e. that is already part of the
            // modelUserInterfaceCommands), we retain the pose of this model as the current one;
            // this in essence implements the _current_ assumption that the model positions can only be
            // changed by the user via drag by hand (the models poses are set programmatically via the
            // update only the first time a model appears, and are then edited only by the users via
            // interaction)
            var newModelUserInterfaceCommands = new Dictionary<string, ModelUserInterfaceCommand>();

            // And for each model name
            foreach (var modelName in availableModelNames)
            {
                // If the model has not already been resolved (included in the new model user interface commands)
                // we try to resolve its position in absolute(world) coordinates.
                if (!newModelUserInterfaceCommands.ContainsKey(modelName))
                {
                    // If we already have a location for this model in the current set of models
                    if (this.currentModelUserInterfaceCommands.TryGetValue(modelName, out ModelUserInterfaceCommand command))
                    {
                        // Then we just adopt that (reflecting the assumption that the latest model positions
                        // (edited by the users) are the correct ones)
                        var absolutePositionModelUserInterfaceCommand = modelUserInterfaceCommands[modelName].DeepClone();
                        absolutePositionModelUserInterfaceCommand.Pose = command.Pose;
                        newModelUserInterfaceCommands.Add(modelName, absolutePositionModelUserInterfaceCommand);
                    }

                    // O/w if the model is already in world coordinates
                    else
                    {
                        // Then add it to the list of models
                        var absolutePositionModelUserInterfaceCommand = modelUserInterfaceCommands[modelName].DeepClone();
                        newModelUserInterfaceCommands.Add(modelName, absolutePositionModelUserInterfaceCommand);
                    }
                }
            }

            // Now update the current list of absolute position model user interface commands based on the
            // newly computed list
            this.currentModelUserInterfaceCommands.Update(
                newModelUserInterfaceCommands.Keys,
                name =>
                {
                    var absolutePositionModelUserInterfaceCommand = newModelUserInterfaceCommands[name];
                    var model = this.availableModels[absolutePositionModelUserInterfaceCommand.ModelType];
                    if (model.Anims.Count != 0)
                    {
                        model.PlayAnim(model.Anims.First(), AnimMode.Loop);
                    }

                    return absolutePositionModelUserInterfaceCommand;
                },
                name => newModelUserInterfaceCommands[name]);
        }

        /// <summary>
        /// Renders the models using the specified renderer.
        /// </summary>
        /// <param name="renderer">The renderer to use when rending the models.</param>
        public void Render(Renderer renderer)
        {
            foreach (var modelName in this.currentModelUserInterfaceCommands.Keys.ToList())
            {
                if (this.currentModelUserInterfaceCommands[modelName].Visible)
                {
                    var model = this.availableModels[this.currentModelUserInterfaceCommands[modelName].ModelType];
                    var pose = this.currentModelUserInterfaceCommands[modelName].Pose;
                    renderer.RenderModel(
                        model,
                        ref pose,
                        this.currentModelUserInterfaceCommands[modelName].CanBeMovedByUser,
                        this.currentModelUserInterfaceCommands[modelName].CanBeScaledByUser,
                        this.currentModelUserInterfaceCommands[modelName].Wireframe,
                        name: modelName);
                    this.currentModelUserInterfaceCommands[modelName].Pose = pose;
                }
            }
        }
    }
}
