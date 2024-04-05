// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Microsoft Research license.

namespace Sigma
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using MathNet.Spatial.Euclidean;
    using Microsoft.Psi.Interop.Serialization;
    using StereoKit;
    using Renderer = Microsoft.Psi.MixedReality.Applications.Renderer;

    /// <summary>
    /// Modes for the task panel.
    /// </summary>
    public enum TaskPanelMode
    {
        /// <summary>
        /// No task panel is displayed.
        /// </summary>
        None,

        /// <summary>
        /// The task panel displays the selected task.
        /// </summary>
        Task,

        /// <summary>
        /// The task panel displays the task list.
        /// </summary>
        TaskList,
    }

    /// <summary>
    /// Implements the StereoKit user interface for displaying the task panel.
    /// </summary>
    /// <typeparam name="TTask">The type of the task.</typeparam>
    public class TaskPanelUserInterface<TTask> : Rectangle3DUserInterface
        where TTask : Task, IInteropSerializable, new()
    {
        private readonly TaskPanelUserInterfaceConfiguration configuration;
        private readonly Paragraph taskNameParagraph = default;
        private readonly Dictionary<int, StepPanel> stepPanels = new ();
        private readonly Dictionary<int, Paragraph> taskListParagraphs = new ();

        private readonly Dictionary<float, Mesh> selectionMeshes = new ();
        private Material selectionMeshMaterial;

        private List<Point3D> panelCorners;
        private Mesh panelMesh;
        private Material panelMaterial;

        private bool showTaskPanel;
        private TaskPanelMode mode;
        private int? selectedStepIndex;
        private int? selectedSubStepIndex;
        private bool showOnlySelectedStep;

        private int topStepPanelIndex = 0;

        private List<string> taskList;

        /// <summary>
        /// Initializes a new instance of the <see cref="TaskPanelUserInterface{TTask}"/> class.
        /// </summary>
        /// <param name="configuration">The configuration.</param>
        /// <param name="name">An optional name for the task panel.</param>
        public TaskPanelUserInterface(TaskPanelUserInterfaceConfiguration configuration, string name = nameof(TaskPanelUserInterface<TTask>))
            : base(name)
        {
            this.configuration = configuration;
            this.taskNameParagraph = new Paragraph($"{this.Name}.TaskNameParagraph");

            this.Width = this.configuration.Width;
            this.Height = this.configuration.Height;
        }

        /// <summary>
        /// Initializes the task panel.
        /// </summary>
        /// <param name="taskLibrary">The task library.</param>
        public void Initialize(TaskLibrary<TTask> taskLibrary)
        {
            this.taskList = taskLibrary.Tasks?.Select(t => t.Name)?.ToList();
        }

        /// <summary>
        /// Updates the task panel.
        /// </summary>
        /// <param name="taskPanelCommand">The task panel command.</param>
        public void Update(TaskPanelUserInterfaceCommand taskPanelCommand)
        {
            if (taskPanelCommand.Mode == TaskPanelMode.None)
            {
                this.showTaskPanel = false;
            }
            else if (taskPanelCommand.Mode == TaskPanelMode.TaskList)
            {
                this.showTaskPanel = true;
                this.mode = TaskPanelMode.TaskList;

                // Update the task list paragraphs
                this.taskListParagraphs.Update(
                    Enumerable.Range(0, this.taskList.Count),
                    createKey: k => new Paragraph($"{this.Name}.TaskListParagraph[{k}]"));

                // Update each paragraph
                for (int i = 0; i < this.taskList.Count; i++)
                {
                    this.taskListParagraphs[i].Update(
                        this.taskList[i],
                        this.configuration.TaskListTextStyle,
                        this.configuration.Width,
                        topMargin: this.configuration.Padding,
                        bottomMargin: 0,
                        rightMargin: this.configuration.Padding,
                        leftMargin: this.configuration.Padding);
                }
            }
            else
            {
                this.showTaskPanel = true;
                this.mode = TaskPanelMode.Task;

                // Update the selected step indices
                this.showOnlySelectedStep = taskPanelCommand.ShowOnlySelectedStep;
                this.selectedStepIndex = taskPanelCommand.SelectedStepIndex;
                this.selectedSubStepIndex = taskPanelCommand.SelectedSubStepIndex;

                // Update the task name paragraph
                this.taskNameParagraph.Update(
                    taskPanelCommand.Task.Name,
                    this.configuration.TaskNameTextStyle,
                    this.configuration.Width,
                    rightMargin: this.configuration.Padding,
                    leftMargin: this.configuration.Padding,
                    topMargin: this.configuration.Padding,
                    bottomMargin: this.configuration.Padding,
                    centered: true);

                // Update the collection of step panels
                this.stepPanels.Update(
                    taskPanelCommand.ShowOnlySelectedStep ? new int[] { taskPanelCommand.SelectedStepIndex.Value } : Enumerable.Range(0, taskPanelCommand.Task.Steps.Count),
                    createKey: i => taskPanelCommand.Task.Steps[i].UpdateStepPanel(
                        null,
                        taskPanelCommand,
                        this.configuration,
                        this.configuration.Height - this.taskNameParagraph.Height - this.configuration.Padding,
                        $"{this.Name}.Step[{i}]"));

                // Now perform an update on each of the panels
                foreach (var i in this.stepPanels.Keys.ToList())
                {
                    this.stepPanels[i] = taskPanelCommand.Task.Steps[i].UpdateStepPanel(
                        this.stepPanels[i],
                        taskPanelCommand,
                        this.configuration,
                        this.configuration.Height - this.taskNameParagraph.Height - this.configuration.Padding,
                        $"{this.Name}.Step[{i}]");
                }
            }
        }

        /// <inheritdoc/>
        public override List<Rectangle3DUserInterfaceState> Render(Renderer renderer, CoordinateSystem pose)
        {
            if (!this.showTaskPanel)
            {
                return new ();
            }

            // Draw the panel
            this.panelMesh ??= this.CreatePanelMesh(renderer);
            renderer.RenderMesh(pose, this.panelMesh, this.panelMaterial);
            renderer.RenderPolygon(pose, this.panelCorners, 0.001f, System.Drawing.Color.White);

            if (this.mode == TaskPanelMode.Task)
            {
                // Draw the task name (title)
                this.taskNameParagraph.Render(renderer, pose);
                var offsetV = this.taskNameParagraph.Height;

                // Underline the title
                renderer.RenderLine(
                    pose,
                    new Point3D(0, this.configuration.Padding, -offsetV),
                    new Point3D(0, this.configuration.Width - this.configuration.Padding, -offsetV),
                    0.0005f,
                    System.Drawing.Color.White);

                // Draw the steps
                offsetV += this.configuration.Padding;
                var hasSelection = false;
                var selectionOffsetV = 0f;
                var selectionHeight = 0f;

                // If we are showing only the selected step then no need to compute the
                if (this.showOnlySelectedStep)
                {
                    // Finally, draw the steps
                    var selectedStep = this.stepPanels.First();

                    // Draw the step
                    selectedStep.Value.Render(renderer, pose.ApplyUV(0, offsetV));
                    offsetV += selectedStep.Value.Height;

                    // If this is the selected step
                    if (!this.selectedSubStepIndex.HasValue && this.selectedStepIndex.HasValue && this.selectedStepIndex.Value == selectedStep.Key)
                    {
                        hasSelection = true;
                        selectionOffsetV = offsetV - selectedStep.Value.Height;
                        selectionHeight = selectedStep.Value.Height;
                    }
                }

                // O/w if we are showing all steps
                else
                {
                    // If we have a selection, recompute the topStepIndex so that the selected step
                    // and the next step are in view.
                    if (this.selectedStepIndex.HasValue)
                    {
                        if (this.topStepPanelIndex > this.selectedStepIndex.Value)
                        {
                            this.topStepPanelIndex = this.selectedStepIndex.Value;
                        }
                        else
                        {
                            // Compute the vertical offsets for all panels starting with the top panel
                            // index
                            var verticalEndPoint = new Dictionary<int, double>();
                            var v = offsetV;
                            for (int i = this.topStepPanelIndex; i < this.stepPanels.Count; i++)
                            {
                                v += this.stepPanels[i].Height;
                                verticalEndPoint[i] = v;
                            }

                            var selectedStepVerticalEndPoint = verticalEndPoint[this.selectedStepIndex.Value];
                            var nextStepVerticalEndPoint = this.selectedStepIndex.Value < this.stepPanels.Count - 1 ? verticalEndPoint[this.selectedStepIndex.Value + 1] : 0;
                            if (selectedStepVerticalEndPoint > this.configuration.Height || nextStepVerticalEndPoint > this.configuration.Height)
                            {
                                this.topStepPanelIndex = this.selectedStepIndex.Value;
                            }
                        }
                    }

                    // Finally, draw the steps. Draw at least one step and draw as long as we're not beyond the panel
                    for (int i = this.topStepPanelIndex;
                        (i < this.stepPanels.Count && i == this.topStepPanelIndex) ||
                        (i < this.stepPanels.Count && offsetV + this.stepPanels[i].Height <= this.configuration.Height);
                        i++)
                    {
                        // Draw the step
                        this.stepPanels[i].Render(renderer, pose.ApplyUV(0, offsetV));
                        offsetV += this.stepPanels[i].Height;

                        // If this is the selected step
                        if (!this.selectedSubStepIndex.HasValue && this.selectedStepIndex.HasValue && this.selectedStepIndex.Value == i)
                        {
                            hasSelection = true;
                            selectionOffsetV = offsetV - this.stepPanels[i].Height;
                            selectionHeight = this.stepPanels[i].Height;
                        }
                    }
                }

                // Now if we have a selection to render
                if (hasSelection)
                {
                    var selectionMeshPose = new CoordinateSystem(new Point3D(0, 0, -selectionOffsetV), UnitVector3D.XAxis, UnitVector3D.YAxis, UnitVector3D.ZAxis).TransformBy(pose);
                    var selectionMesh = this.GetOrCreateSelectionMesh(renderer, selectionHeight);
                    renderer.RenderMesh(selectionMeshPose, selectionMesh, this.selectionMeshMaterial);
                }
            }
            else if (this.mode == TaskPanelMode.TaskList)
            {
                var offsetV = 0f;
                for (int i = 0; i < this.taskListParagraphs.Count; i++)
                {
                    this.taskListParagraphs[i].Render(renderer, pose.ApplyUV(0, offsetV));
                    offsetV += this.taskListParagraphs[i].Height;
                }
            }
            else
            {
                throw new NotImplementedException("Unexpected task panel mode.");
            }

            return this.GetUserInterfaceState(pose);
        }

        private Mesh CreatePanelMesh(Renderer renderer)
        {
            // The slate mesh forms the background of the task window, and is rendered in the
            // YZ plane of a coordinate system (X points forward, towards the user, who is
            // looking at the slate). The structure of the slate mesh is shown below.
            //
            //   z
            //   ^
            //   |
            //   ---->y
            //
            //    0--3
            //    |  |
            //    1--2
            this.panelCorners = new List<Point3D>()
            {
                new Point3D(-0.003f, 0, 0),
                new Point3D(-0.003f, 0, -this.configuration.Height),
                new Point3D(-0.003f, this.configuration.Width, -this.configuration.Height),
                new Point3D(-0.003f, this.configuration.Width, 0),
            };

            var indices = new List<uint>() { 0, 1, 2, 0, 2, 3 };

            this.panelMesh = renderer.CreateMesh(this.panelCorners, indices);
            this.panelMaterial = renderer.GetOrCreateMaterial(this.configuration.Color);

            return this.panelMesh;
        }

        private Mesh GetOrCreateSelectionMesh(Renderer renderer, float height)
        {
            if (!this.selectionMeshes.ContainsKey(height))
            {
                // Create the selection mesh
                var selectionMeshCorners = new List<Point3D>()
                {
                    new Point3D(-0.002f, 0, 0),
                    new Point3D(-0.002f, 0, -height),
                    new Point3D(-0.002f, this.configuration.Width, -height),
                    new Point3D(-0.002f, this.configuration.Width, 0),
                };
                var indices = new List<uint>() { 0, 1, 2, 0, 2, 3 };
                var selectionMesh = renderer.CreateMesh(selectionMeshCorners, indices);
                this.selectionMeshes.Add(height, selectionMesh);
            }

            this.selectionMeshMaterial ??= renderer.GetOrCreateMaterial(this.configuration.SelectionColor);

            return this.selectionMeshes[height];
        }
    }
}
