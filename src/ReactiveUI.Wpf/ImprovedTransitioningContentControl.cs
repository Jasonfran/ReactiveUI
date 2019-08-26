// Copyright (c) 2019 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace ReactiveUI
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Media.Animation;

    /// <summary>
    /// Transition control.
    /// </summary>
    [TemplatePart(Name = "PART_Container", Type = typeof(FrameworkElement))]
    [TemplatePart(Name = "PART_PreviousContentPresentationSite", Type = typeof(ContentPresenter))]
    [TemplatePart(Name = "PART_CurrentContentPresentationSite", Type = typeof(ContentPresenter))]
    public class ImprovedTransitioningContentControl : ContentControl
    {
        /// <summary>
        /// Set to true so new content appears behind old content to facilitate a certain animation.
        /// </summary>
        public static readonly DependencyProperty AnimateNewContentBehindProperty =
            DependencyProperty.Register(
                                    "AnimateNewContentBehind",
                                    typeof(bool),
                                    typeof(ImprovedTransitioningContentControl),
                                    new PropertyMetadata(default(bool)));

        /// <summary>
        /// Set the transition type.
        /// </summary>
        public static readonly DependencyProperty TransitionProperty =
            DependencyProperty.Register(
                                        "Transition",
                                        typeof(TransitionType),
                                        typeof(ImprovedTransitioningContentControl),
                                        new PropertyMetadata(default(TransitionType)));

        /// <summary>
        /// Used to set a custom transition when using the Custom transition type.
        /// </summary>
        public static readonly DependencyProperty CustomTransitionProperty =
            DependencyProperty.Register(
                                        "CustomTransition",
                                        typeof(Storyboard),
                                        typeof(ImprovedTransitioningContentControl),
                                        new PropertyMetadata(default(Storyboard)));

        private const string PresentationGroup = "PresentationStates";

        private const string NormalState = "Normal";

        private const string CustomState = "Custom";

        private Grid _container;

        private ContentPresenter _previousContentPresentationSite;

        private ContentPresenter _currentContentPresentationSite;

        private Storyboard _selectedTransition;

        /// <summary>
        /// Initializes a new instance of the <see cref="ImprovedTransitioningContentControl"/> class.
        /// </summary>
        public ImprovedTransitioningContentControl()
        {
            DefaultStyleKey = typeof(ImprovedTransitioningContentControl);
        }

        /// <summary>
        /// A type of transition.
        /// </summary>
        public enum TransitionType
        {
            /// <summary>
            /// Used to set a custom transition. You must set the <see cref="ImprovedTransitioningContentControl.CustomTransition"/> property when using this.
            /// </summary>
            Custom
        }

        /// <summary>
        /// Gets or sets a value indicating whether new content should be animated behind old content.
        /// </summary>
        public bool AnimateNewContentBehind
        {
            get { return (bool)GetValue(AnimateNewContentBehindProperty); }
            set { SetValue(AnimateNewContentBehindProperty, value); }
        }

        /// <summary>
        /// Gets or sets the transition to perform.
        /// </summary>
        public TransitionType Transition
        {
            get { return (TransitionType)GetValue(TransitionProperty); }
            set { SetValue(TransitionProperty, value); }
        }

        /// <summary>
        /// Gets or sets the custom transition.
        /// </summary>
        public Storyboard CustomTransition
        {
            get { return (Storyboard)GetValue(CustomTransitionProperty); }
            set { SetValue(CustomTransitionProperty, value); }
        }

        /// <inheritdoc/>
        public override void OnApplyTemplate()
        {
            // Wire up all of the various control parts.
            _container = (Grid)GetTemplateChild("PART_Container");
            if (_container == null)
            {
                throw new ArgumentException("PART_Container not found.");
            }

            _currentContentPresentationSite =
                (ContentPresenter)GetTemplateChild("PART_CurrentContentPresentationSite");

            if (_currentContentPresentationSite == null)
            {
                throw new ArgumentException("PART_CurrentContentPresentationSite not found.");
            }

            _previousContentPresentationSite =
                (ContentPresenter)GetTemplateChild("PART_PreviousContentPresentationSite");

            // Move storyboard into Custom visual state if needed
            if (Transition == TransitionType.Custom && CustomTransition != null)
            {
                var customVisualState = GetVisualStateByName(CustomState);
                customVisualState.Storyboard = CustomTransition;
            }
        }

        /// <summary>
        /// Handled when content has changed.
        /// </summary>
        /// <param name="oldContent">Old content.</param>
        /// <param name="newContent">New content.</param>
        protected override void OnContentChanged(object oldContent, object newContent)
        {
            base.OnContentChanged(oldContent, newContent);
            DoTransition(oldContent, newContent);
        }

        private void DoTransition(object oldContent, object newContent)
        {
            if (_previousContentPresentationSite != null && _currentContentPresentationSite != null)
            {
                _previousContentPresentationSite.Content = oldContent;
                _currentContentPresentationSite.Content = newContent;

                if (AnimateNewContentBehind)
                {
                    Panel.SetZIndex(_currentContentPresentationSite, 0);
                    Panel.SetZIndex(_previousContentPresentationSite, 1);
                }
                else
                {
                    Panel.SetZIndex(_currentContentPresentationSite, 1);
                    Panel.SetZIndex(_previousContentPresentationSite, 0);
                }

                var transitionName = Transition.ToString();
                _selectedTransition = GetTransitionStoryboardByName(transitionName);
                _selectedTransition.Completed += OnTransitionComplete;
                _selectedTransition.Begin(_container);
            }
        }

        private void OnTransitionComplete(object sender, EventArgs e)
        {
            _selectedTransition.Completed -= OnTransitionComplete;

            // VisualStateManager.GoToState(this, NormalState, false);
        }

        private VisualState GetVisualStateByName(string transitionName)
        {
            var presentationGroup =
                ((IEnumerable<VisualStateGroup>)VisualStateManager.GetVisualStateGroups(_container))
                .FirstOrDefault(o => o.Name == PresentationGroup);

            if (presentationGroup == null)
            {
                throw new ArgumentException("Invalid VisualStateGroup.");
            }

            var visualState = ((IEnumerable<VisualState>)presentationGroup.States).FirstOrDefault(o => o.Name == transitionName);

            if (visualState == null)
            {
                throw new ArgumentException("Invalid VisualState name.");
            }

            return visualState;
        }

        private Storyboard GetTransitionStoryboardByName(string transitionName)
        {
            var presentationGroup =
                ((IEnumerable<VisualStateGroup>)VisualStateManager.GetVisualStateGroups(_container))
                .FirstOrDefault(o => o.Name == PresentationGroup);

            if (presentationGroup == null)
            {
                throw new ArgumentException("Invalid VisualStateGroup.");
            }

            var visualState = GetVisualStateByName(transitionName);

            var transition = visualState.Storyboard;

            if (transition == null)
            {
                throw new ArgumentException("Invalid transition");
            }

            return transition;
        }
    }
}
