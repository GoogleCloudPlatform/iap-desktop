//
// Copyright 2024 Google LLC
//
// Licensed to the Apache Software Foundation (ASF) under one
// or more contributor license agreements.  See the NOTICE file
// distributed with this work for additional information
// regarding copyright ownership.  The ASF licenses this file
// to you under the Apache License, Version 2.0 (the
// "License"); you may not use this file except in compliance
// with the License.  You may obtain a copy of the License at
// 
//   http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing,
// software distributed under the License is distributed on an
// "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
// KIND, either express or implied.  See the License for the
// specific language governing permissions and limitations
// under the License.
//

using Google.Solutions.IapDesktop.Application.ToolWindows.ProjectExplorer;
using Google.Solutions.IapDesktop.Core.ProjectModel;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Application.Test.ToolWindows.ProjectExplorer
{
    [TestFixture]
    public class TestProjectExplorerViewModelNodes
    {
        private class SampleNode : ProjectExplorerViewModel.ViewModelNode
        {
            private readonly IList<SampleNode> children;

            public SampleNode(
                string text,
                params SampleNode[] children)
                : base(null, null, text, true, 0)
            {
                this.ModelNode = new Mock<IProjectModelNode>().Object;
                this.CanReload = false;
                this.children = children;
            }

            public override IProjectModelNode ModelNode { get; }

            protected override ProjectExplorerViewModel ViewModel
            {
                get => throw new NotImplementedException();
            }

            internal override bool CanReload { get; }

            protected override Task<IEnumerable<ProjectExplorerViewModel.ViewModelNode>> LoadChildrenAsync(
                bool forceReload, 
                CancellationToken token)
            {
                return Task.FromResult(
                    this.children.Cast<ProjectExplorerViewModel.ViewModelNode>());
            }
        }

        //----------------------------------------------------------------------
        // LoadedChildren.
        //----------------------------------------------------------------------

        [Test]
        public void LoadedChildren_WhenNotLoaded()
        {
            var node = new SampleNode("root");
            CollectionAssert.IsEmpty(node.LoadedChildren);
        }

        //----------------------------------------------------------------------
        // LoadedDescendents.
        //----------------------------------------------------------------------

        [Test]
        public void LoadedDescendents_WhenNotLoaded()
        {
            var node = new SampleNode("root");
            CollectionAssert.IsEmpty(node.LoadedDescendents);
        }
    }
}
