﻿//
// Copyright 2023 Google LLC
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


using Google.Solutions.IapDesktop.Application.Diagnostics;
using Google.Solutions.IapDesktop.Application.Host;
using Google.Solutions.IapDesktop.Application.Profile;
using Google.Solutions.IapDesktop.Application.ToolWindows.Update;
using Google.Solutions.IapDesktop.Application.Windows;
using Google.Solutions.IapDesktop.Application.Windows.Dialog;
using Google.Solutions.Mvvm.Controls;
using Google.Solutions.Platform.Net;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TaskbarClock;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TrackBar;

namespace Google.Solutions.IapDesktop.Application.Test.ToolWindows.Update
{
    [TestFixture]
    public class TestCheckForUpdateCommand
    {
        private static IUpdatePolicyFactory CreateUpdatePolicyFactory(
            bool adviseAllUpdates,
            ReleaseTrack followedTrack)
        {
            var policy = new Mock<IUpdatePolicy>();
            policy.SetupGet(p => p.FollowedTrack).Returns(followedTrack);
            policy
                .Setup(p => p.IsUpdateAdvised(It.IsAny<IRelease>()))
                .Returns(adviseAllUpdates);

            var policyFactory = new Mock<IUpdatePolicyFactory>();
            policyFactory
                .Setup(f => f.GetPolicy())
                .Returns(policy.Object);

            return policyFactory.Object;
        }

        private static ITaskDialog CreateDialog(string commandLinkToClick)
        {
            var dialog = new Mock<ITaskDialog>();
            dialog
                .Setup(d => d.ShowDialog(
                    It.IsAny<IWin32Window>(),
                    It.IsAny<TaskDialogParameters>()))
                .Callback<IWin32Window, TaskDialogParameters>((w, p) =>
                {
                    p.Buttons
                        .OfType<TaskDialogCommandLinkButton>()
                        .First(b => b.Text == commandLinkToClick)
                        .PerformClick();
                })
                .Returns(DialogResult.OK);

            return dialog.Object;
        }

        private static ITaskDialog CreateCancelledDialog()
        {
            var dialog = new Mock<ITaskDialog>();
            dialog
                .Setup(d => d.ShowDialog(
                    It.IsAny<IWin32Window>(),
                    It.IsAny<TaskDialogParameters>()))
                .Returns(DialogResult.Cancel);

            return dialog.Object;
        }

        //---------------------------------------------------------------------
        // IsAutomatedCheckDue.
        //---------------------------------------------------------------------

        [Test]
        public void IsAutomatedCheckDue()
        {
            var policy = new Mock<IUpdatePolicy>();
            policy
                .Setup(p => p.IsUpdateCheckDue(It.IsAny<DateTime>()))
                .Returns(true);

            var policyFactory = new Mock<IUpdatePolicyFactory>();
            policyFactory
                .Setup(f => f.GetPolicy())
                .Returns(policy.Object);

            var command = new CheckForUpdateCommand<IMainWindow>(
                new Mock<IWin32Window>().Object,
                new Mock<IInstall>().Object,
                policyFactory.Object,
                new Mock<IReleaseFeed>().Object,
                new Mock<ITaskDialog>().Object,
                new Mock<IBrowser>().Object);

            var time = DateTime.Now;
            Assert.IsTrue(command.IsAutomatedCheckDue(time));

            policy.Verify(p => p.IsUpdateCheckDue(time), Times.Once);
        }

        //---------------------------------------------------------------------
        // PromptForDownload.
        //---------------------------------------------------------------------

        [Test]
        public void WhenReleaseIsNull_ThenPromptForDownloadReturns()
        {
            var policy = new Mock<IUpdatePolicy>();
            var policyFactory = new Mock<IUpdatePolicyFactory>();
            policyFactory
                .Setup(f => f.GetPolicy())
                .Returns(policy.Object);

            var command = new CheckForUpdateCommand<IMainWindow>(
                new Mock<IWin32Window>().Object,
                new Mock<IInstall>().Object,
                policyFactory.Object,
                new Mock<IReleaseFeed>().Object,
                new Mock<ITaskDialog>().Object,
                new Mock<IBrowser>().Object);

            command.PromptForDownload(null);

            policy.Verify(p => p.IsUpdateAdvised(It.IsAny<IRelease>()), Times.Never);
        }

        [Test]
        public void WhenPolicyDoesNotAdviseUpdate_ThenPromptForDownloadReturns()
        {
            var policy = new Mock<IUpdatePolicy>();
            var policyFactory = new Mock<IUpdatePolicyFactory>();
            policyFactory
                .Setup(f => f.GetPolicy())
                .Returns(policy.Object);

            var command = new CheckForUpdateCommand<IMainWindow>(
                new Mock<IWin32Window>().Object,
                new Mock<IInstall>().Object,
                policyFactory.Object,
                new Mock<IReleaseFeed>().Object,
                new Mock<ITaskDialog>().Object,
                new Mock<IBrowser>().Object);

            command.PromptForDownload(new Mock<IRelease>().Object);

            policy.Verify(p => p.IsUpdateAdvised(It.IsAny<IRelease>()), Times.Once);
        }

        [Test]
        public void WhenUserCancels_ThenPromptForDownloadReturns()
        {
            var command = new CheckForUpdateCommand<IMainWindow>(
                new Mock<IWin32Window>().Object,
                new Mock<IInstall>().Object,
                CreateUpdatePolicyFactory(true, ReleaseTrack.Normal),
                new Mock<IReleaseFeed>().Object,
                CreateCancelledDialog(),
                new Mock<IBrowser>().Object);

            command.PromptForDownload(new Mock<IRelease>().Object);
        }

        [Test]
        public void WhenUserSelectsDownload_ThenPromptForDownloadOpensDownload()
        {
            var browser = new Mock<IBrowser>();

            var downloadUrl = "http://example.com/download";
            var release = new Mock<IRelease>();
            release.SetupGet(r => r.DownloadUrl).Returns(downloadUrl);

            var command = new CheckForUpdateCommand<IMainWindow>(
                new Mock<IWin32Window>().Object,
                new Mock<IInstall>().Object,
                CreateUpdatePolicyFactory(true, ReleaseTrack.Normal),
                new Mock<IReleaseFeed>().Object,
                CreateDialog("Yes, download now"),
                browser.Object);

            command.PromptForDownload(release.Object);

            browser.Verify(b => b.Navigate(downloadUrl), Times.Once);
        }

        [Test]
        public void WhenDownloadUrlNotFound_ThenPromptForDownloadOpensDetails()
        {
            var browser = new Mock<IBrowser>();

            var detailsUrl = "http://example.com/details";
            var release = new Mock<IRelease>();
            release.SetupGet(r => r.DetailsUrl).Returns(detailsUrl);

            var command = new CheckForUpdateCommand<IMainWindow>(
                new Mock<IWin32Window>().Object,
                new Mock<IInstall>().Object,
                CreateUpdatePolicyFactory(true, ReleaseTrack.Normal),
                new Mock<IReleaseFeed>().Object,
                CreateDialog("Yes, download now"),
                browser.Object);

            command.PromptForDownload(release.Object);

            browser.Verify(b => b.Navigate(detailsUrl), Times.Once);
        }

        [Test]
        public void WhenUserSelectsMoreDetails_ThenPromptForDownloadOpensDetails()
        {
            var browser = new Mock<IBrowser>();

            var downloadUrl = "http://example.com/download";
            var detailsUrl = "http://example.com/details";
            var release = new Mock<IRelease>();
            release.SetupGet(r => r.DownloadUrl).Returns(downloadUrl);
            release.SetupGet(r => r.DetailsUrl).Returns(detailsUrl);

            var command = new CheckForUpdateCommand<IMainWindow>(
                new Mock<IWin32Window>().Object,
                new Mock<IInstall>().Object,
                CreateUpdatePolicyFactory(true, ReleaseTrack.Normal),
                new Mock<IReleaseFeed>().Object,
                CreateDialog("Show release notes"),
                browser.Object);

            command.PromptForDownload(release.Object);

            browser.Verify(b => b.Navigate(detailsUrl), Times.Once);
        }

        [Test]
        public void WhenUserSelectsLater_ThenPromptForDownloadReturns()
        {
            var browser = new Mock<IBrowser>();

            var command = new CheckForUpdateCommand<IMainWindow>(
                new Mock<IWin32Window>().Object,
                new Mock<IInstall>().Object,
                CreateUpdatePolicyFactory(true, ReleaseTrack.Normal),
                new Mock<IReleaseFeed>().Object,
                CreateDialog("No, download later"),
                browser.Object);

            command.PromptForDownload(new Mock<IRelease>().Object);

            browser.Verify(b => b.Navigate(It.IsAny<string>()), Times.Never);
        }

        //---------------------------------------------------------------------
        // Execute.
        //---------------------------------------------------------------------

        [Test]
        public void WhenPolicyUsesNormalOrCriticalTrack_ThenExecuteReadsCanaryFeed(
            [Values(ReleaseTrack.Normal, ReleaseTrack.Critical)] ReleaseTrack track)
        {
            var feed = new Mock<IReleaseFeed>();
            var command = new CheckForUpdateCommand<IMainWindow>(
                new Mock<IWin32Window>().Object,
                new Mock<IInstall>().Object,
                CreateUpdatePolicyFactory(false, track),
                feed.Object,
                CreateCancelledDialog(),
                new Mock<IBrowser>().Object);

            command.Execute(null, CancellationToken.None);

            feed.Verify(f => f.FindLatestReleaseAsync(
                ReleaseFeedOptions.None, 
                CancellationToken.None), 
                Times.Once);
        }

        [Test]
        public void WhenPolicyUsesCanaryTrack_ThenExecuteReadsCanaryFeed()
        {
            var feed = new Mock<IReleaseFeed>();
            var command = new CheckForUpdateCommand<IMainWindow>(
                new Mock<IWin32Window>().Object,
                new Mock<IInstall>().Object,
                CreateUpdatePolicyFactory(false, ReleaseTrack.Canary),
                feed.Object,
                CreateCancelledDialog(),
                new Mock<IBrowser>().Object);

            command.Execute(null, CancellationToken.None);

            feed.Verify(f => f.FindLatestReleaseAsync(
                ReleaseFeedOptions.IncludeCanaryReleases,
                CancellationToken.None),
                Times.Once);
        }
    }
}
