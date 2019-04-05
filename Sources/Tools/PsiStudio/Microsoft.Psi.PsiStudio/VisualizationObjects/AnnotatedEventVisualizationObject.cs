// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.VisualizationObjects
{
    using System;
    using System.ComponentModel;
    using System.Linq;
    using System.Runtime.Serialization;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Input;
    using System.Windows.Media;
    using GalaSoft.MvvmLight.CommandWpf;
    using Microsoft.Psi.Data.Annotations;
    using Microsoft.Psi.Data.Json;
    using Microsoft.Psi.Visualization.Annotations;
    using Microsoft.Psi.Visualization.Config;
    using Microsoft.Psi.Visualization.Data;
    using Microsoft.Psi.Visualization.Extensions;
    using Microsoft.Psi.Visualization.Helpers;
    using Microsoft.Psi.Visualization.Input;
    using Microsoft.Psi.Visualization.Views.Visuals2D;
    using Microsoft.Psi.Visualization.Windows;

    /// <summary>
    /// Class implements a annotated event visualization object.
    /// </summary>
    [DataContract(Namespace = "http://www.microsoft.com/psi")]
    public class AnnotatedEventVisualizationObject : TimelineVisualizationObject<AnnotatedEvent, AnnotatedEventVisualizationObjectConfiguration>
    {
        private RelayCommand nextAnnotatedEventCommand;
        private RelayCommand previousAnnotatedEventCommand;
        private RelayCommand startAnnotatedEventCommand;
        private RelayCommand endAnnotatedEventCommand;
        private RelayCommand<AnnotationSchemaValue> addAnnotatedEventCommand;
        private RelayCommand addPointEventCommand;
        private RelayCommand addSchemaCommand;
        private RelayCommand<Message<AnnotatedEvent>> deleteEventCommand;
        private RelayCommand<AnnotatedEvent> setValueCommand;
        private RelayCommand<ContextMenuEventArgs> dynamicCanvsContextMenuOpeningCommand;
        private RelayCommand saveAnnotationsCommand;
        private RelayCommand<object[]> setSchemaValueCommand;
        private JsonStreamMetadata metadata;
        private AnnotatedEventDefinition definition;
        private bool isDirty;
        private Message<AnnotatedEvent> currentContinuousAnnotatedEvent = default(Message<AnnotatedEvent>);

        /// <inheritdoc/>
        [IgnoreDataMember]
        public override Color LegendColor => this.Configuration.TextColor;

        /// <summary>
        /// Gets the annotated event definition.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public AnnotatedEventDefinition Definition => this.definition;

        /// <summary>
        /// Gets or sets a value indicating whether the underlying annotation store is dirty.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public bool IsDirty
        {
            get { return this.isDirty; }
            set { this.Set(nameof(this.IsDirty), ref this.isDirty, value); }
        }

        /// <summary>
        /// Gets the next annotated event command.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public RelayCommand NextAnnotatedEventCommand
        {
            get
            {
                if (this.nextAnnotatedEventCommand == null)
                {
                    this.nextAnnotatedEventCommand = new RelayCommand(
                        () =>
                        {
                            var annotatedEvent = this.Data.Where(m => m.Data.StartTime >= this.Navigator.SelectionRange.EndTime).OrderBy(m => m.Data.StartTime).FirstOrDefault();
                            if (annotatedEvent == default(Message<AnnotatedEvent>))
                            {
                                annotatedEvent = this.Data.OrderBy(m => m.Data.StartTime).FirstOrDefault();
                            }

                            if (annotatedEvent != default(Message<AnnotatedEvent>))
                            {
                                this.Navigator.SelectionRange.SetRange(annotatedEvent.Data.StartTime, annotatedEvent.Data.EndTime);
                            }
                        });
                }

                return this.nextAnnotatedEventCommand;
            }
        }

        /// <summary>
        /// Gets the previous annotated event command.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public RelayCommand PreviousAnnotatedEventCommand
        {
            get
            {
                if (this.previousAnnotatedEventCommand == null)
                {
                    this.previousAnnotatedEventCommand = new RelayCommand(
                        () =>
                        {
                            var annotatedEvent = this.Data.Where(m => m.Data.EndTime <= this.Navigator.SelectionRange.StartTime).OrderBy(m => m.Data.EndTime).LastOrDefault();
                            if (annotatedEvent == default(Message<AnnotatedEvent>))
                            {
                                annotatedEvent = this.Data.OrderBy(m => m.Data.EndTime).LastOrDefault();
                            }

                            if (annotatedEvent != default(Message<AnnotatedEvent>))
                            {
                                this.Navigator.SelectionRange.SetRange(annotatedEvent.Data.StartTime, annotatedEvent.Data.EndTime);
                            }
                        });
                }

                return this.previousAnnotatedEventCommand;
            }
        }

        /// <summary>
        /// Gets the start annotated event command.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public RelayCommand StartAnnotatedEventCommand
        {
            get
            {
                if (this.startAnnotatedEventCommand == null)
                {
                    this.startAnnotatedEventCommand = new RelayCommand(
                        () =>
                        {
                            if (this.currentContinuousAnnotatedEvent == default(Message<AnnotatedEvent>))
                            {
                                var annotatedEvent = this.Definition.CreateAnnotatedEvent(this.Navigator.Cursor, this.Navigator.Cursor);

                                this.currentContinuousAnnotatedEvent = Message.Create(annotatedEvent, annotatedEvent.StartTime, annotatedEvent.StartTime, this.metadata.Id, this.metadata.MessageCount++);
                                this.Data.Add(this.currentContinuousAnnotatedEvent);
                                this.IsDirty = true;
                            }
                        });
                }

                return this.startAnnotatedEventCommand;
            }
        }

        /// <summary>
        /// Gets the end annotated event command.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public RelayCommand EndAnnotatedEventCommand
        {
            get
            {
                if (this.endAnnotatedEventCommand == null)
                {
                    this.endAnnotatedEventCommand = new RelayCommand(
                        () =>
                        {
                            // remove the event
                            this.Data.Remove(this.currentContinuousAnnotatedEvent);

                            // set the end time
                            this.currentContinuousAnnotatedEvent.Data.EndTime = this.Navigator.Cursor;

                            // re-add the event
                            this.Data.Add(this.currentContinuousAnnotatedEvent);

                            // then null the event out
                            this.currentContinuousAnnotatedEvent = default(Message<AnnotatedEvent>);
                            this.IsDirty = true;
                        });
                }

                return this.endAnnotatedEventCommand;
            }
        }

        /// <summary>
        /// Gets the add annotated event command.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public RelayCommand<AnnotationSchemaValue> AddAnnotatedEventCommand
        {
            get
            {
                if (this.addAnnotatedEventCommand == null)
                {
                    this.addAnnotatedEventCommand = new RelayCommand<AnnotationSchemaValue>(
                        schemaValue =>
                        {
                            var annotatedEvent = this.Definition.CreateAnnotatedEvent(this.Navigator.SelectionRange.StartTime, this.Navigator.SelectionRange.EndTime);
                            annotatedEvent.SetAnnotation(0, schemaValue.Value, this.Definition.Schemas[0]);

                            var sequenceId = this.Data.OrderBy(ae => ae.SequenceId).LastOrDefault().SequenceId + 1;
                            var message = new Message<AnnotatedEvent>(annotatedEvent, annotatedEvent.StartTime, annotatedEvent.StartTime, this.metadata.Id, sequenceId);
                            this.Data.Add(message);
                            this.IsDirty = true;
                        },
                        _ =>
                        {
                            // selection range must be valid
                            if (this.Navigator.SelectionRange.Duration.Ticks <= 0)
                            {
                                return false;
                            }

                            // selection range must not overlap with any existing annotated events
                            return this.Data.All(m => this.Navigator.SelectionRange.EndTime < m.Data.StartTime || this.Navigator.SelectionRange.StartTime > m.Data.EndTime);
                        });
                }

                return this.addAnnotatedEventCommand;
            }
        }

        /// <summary>
        /// Gets the add point event command.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public RelayCommand AddPointEventCommand
        {
            get
            {
                if (this.addPointEventCommand == null)
                {
                    this.addPointEventCommand = new RelayCommand(() => { }, () => false);
                }

                return this.addPointEventCommand;
            }
        }

        /// <summary>
        /// Gets the add schema command.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public RelayCommand AddSchemaCommand
        {
            get
            {
                if (this.addSchemaCommand == null)
                {
                    this.addSchemaCommand = new RelayCommand(
                        () =>
                        {
                            AddAnnotationWindow dlg = new AddAnnotationWindow(AnnotationSchemaRegistryViewModel.Default.Schemas, false);
                            var result = dlg.ShowDialog();
                            if (result.HasValue && result.Value)
                            {
                                // add new schema to the annotated event template
                                this.Definition.AddSchema(dlg.AnnotationSchema);

                                // add new schema with default schema value to all existing annotated events
                                foreach (var message in this.Data)
                                {
                                    message.Data.AddAnnotation(null, dlg.AnnotationSchema);
                                }

                                this.IsDirty = true;
                            }
                        },
                        () => false);
                }

                return this.addSchemaCommand;
            }
        }

        /// <summary>
        /// Gets the delete event command.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public RelayCommand<Message<AnnotatedEvent>> DeleteEventCommand
        {
            get
            {
                if (this.deleteEventCommand == null)
                {
                    this.deleteEventCommand = new RelayCommand<Message<AnnotatedEvent>>(
                        annotatedEvent =>
                        {
                            this.Data.Remove(annotatedEvent);
                            this.IsDirty = true;
                        });
                }

                return this.deleteEventCommand;
            }
        }

        /// <summary>
        /// Gets the set value command.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public RelayCommand<AnnotatedEvent> SetValueCommand
        {
            get
            {
                if (this.setValueCommand == null)
                {
                    this.setValueCommand = new RelayCommand<AnnotatedEvent>(
                        annotatedEvent =>
                        {
                            SetAnnotationValueWindow dlg = new SetAnnotationValueWindow
                            {
                                // Currently, only support one annotation
                                AnnotationValue = annotatedEvent.Annotations[0]
                            };
                            var result = dlg.ShowDialog();
                            if (result.HasValue && result.Value)
                            {
                                // Currently, only support one annotation
                                annotatedEvent.SetAnnotation(0, dlg.AnnotationValue, this.Definition.Schemas[0]);
                            }

                            this.IsDirty = true;
                        });
                }

                return this.setValueCommand;
            }
        }

        /// <summary>
        /// Gets the dyanmic canvas context menu opening command.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public RelayCommand<ContextMenuEventArgs> DynamicCanvasContextMenuOpeningCommand
        {
            get
            {
                if (this.dynamicCanvsContextMenuOpeningCommand == null)
                {
                    this.dynamicCanvsContextMenuOpeningCommand = new RelayCommand<ContextMenuEventArgs>(
                        contextMenuEventArgs =>
                        {
                            if ((Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift)
                            {
                                // eat context menu opening, when shift key is pressed (dropping end selection marker)
                                contextMenuEventArgs.Handled = true;
                            }
                        });
                }

                return this.dynamicCanvsContextMenuOpeningCommand;
            }
        }

        /// <summary>
        /// Gets the save annotations command.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public RelayCommand SaveAnnotationsCommand
        {
            get
            {
                if (this.saveAnnotationsCommand == null)
                {
                    this.saveAnnotationsCommand = new RelayCommand(() => this.Save(), () => this.IsDirty);
                }

                return this.saveAnnotationsCommand;
            }
        }

        /// <summary>
        /// Gets set schema value command.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public RelayCommand<object[]> SetSchemaValueCommand
        {
            get
            {
                if (this.setSchemaValueCommand == null)
                {
                    this.setSchemaValueCommand = new RelayCommand<object[]>(
                        parameters =>
                        {
                            var schemaValue = parameters[0] as AnnotationSchemaValue;
                            var path = parameters[1] as System.Windows.Shapes.Path;
                            var text = path.Parent.FindChild<TextBlock>("Value");
                            path.Tag = schemaValue.Value;
                            path.Fill = schemaValue.Color.ToMediaBrush();
                            text.Text = schemaValue.Value;
                            this.IsDirty = true;
                        });
                }

                return this.setSchemaValueCommand;
            }
        }

        /// <inheritdoc />
        [Browsable(false)]
        [IgnoreDataMember]
        public override DataTemplate DefaultViewTemplate => XamlHelper.CreateTemplate(this.GetType(), typeof(AnnotatedEventVisualizationObjectView));

        /// <summary>
        /// Save the underlying annotation store, if dirty.
        /// </summary>
        public void Save()
        {
            using (var waitCursor = new WaitCursor())
            {
                if (this.IsDirty)
                {
                    // reorder messages according to startTime - requires generating new Message<T>s
                    int sequenceId = 0;
                    var messages = this.Data.OrderBy(m => m.Data.StartTime).Select(m => Message.Create(m.Data, m.Data.StartTime, m.Data.StartTime, this.metadata.Id, sequenceId++));

                    // persist the data
                    using (var writer = new AnnotationSimpleWriter(this.Definition))
                    {
                        var streamBinding = this.Configuration.StreamBinding;
                        writer.CreateStore(streamBinding.StoreName, streamBinding.StorePath);
                        writer.CreateStream<AnnotatedEvent>(this.metadata, messages);
                        writer.WriteAll(ReplayDescriptor.ReplayAll);
                    }

                    this.IsDirty = false;
                }
            }
        }

        /// <inheritdoc />
        protected override void InitNew()
        {
            base.InitNew();
            this.Configuration.Height = 20;
            this.isDirty = false;
        }

        /// <inheritdoc />
        protected override void OnStreamBound()
        {
            base.OnStreamBound();

            this.Data = DataManager.Instance.ReadStream<AnnotatedEvent>(this.Configuration.StreamBinding, DateTime.MinValue, DateTime.MaxValue);
            using (var reader = DataManager.Instance.GetReader(this.Configuration.StreamBinding))
            {
                this.metadata = reader.AvailableStreams.First(s => s.Name == this.Configuration.StreamBinding.StreamName) as JsonStreamMetadata;
                this.RaisePropertyChanging(nameof(this.Definition));
                this.definition = (reader as AnnotationSimpleReader).Definition;
                this.RaisePropertyChanged(nameof(this.Definition));
            }
        }

        /// <inheritdoc />
        protected override void OnDisconnect()
        {
            this.Save();
        }
    }
}
