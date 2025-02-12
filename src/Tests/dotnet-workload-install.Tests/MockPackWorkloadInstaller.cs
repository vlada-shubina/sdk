// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Microsoft.DotNet.Workloads.Workload.Install;
using static Microsoft.NET.Sdk.WorkloadManifestReader.WorkloadResolver;
using Microsoft.DotNet.Workloads.Workload.Install.InstallRecord;
using Microsoft.Extensions.EnvironmentAbstractions;

namespace Microsoft.DotNet.Cli.Workload.Install.Tests
{
    internal class MockPackWorkloadInstaller : IWorkloadPackInstaller
    {
        public IList<PackInfo> InstalledPacks;
        public IList<PackInfo> RolledBackPacks = new List<PackInfo>();
        public IList<(ManifestId manifestId, ManifestVersion manifestVersion, SdkFeatureBand sdkFeatureBand, DirectoryPath? offlineCache)> InstalledManifests = 
            new List<(ManifestId, ManifestVersion, SdkFeatureBand, DirectoryPath?)>();
        public IList<PackInfo> CachedPacks = new List<PackInfo>();
        public string CachePath;
        public bool GarbageCollectionCalled = false;
        public MockInstallationRecordRepository InstallationRecordRepository;
        public bool FailingRollback;

        public MockPackWorkloadInstaller(string failingWorkload = null, bool failingRollback = false, IList<WorkloadId> installedWorkloads = null, IList<PackInfo> installedPacks = null)
        {
            InstallationRecordRepository = new MockInstallationRecordRepository(failingWorkload, installedWorkloads);
            FailingRollback = failingRollback;
            InstalledPacks = installedPacks ?? new List<PackInfo>();
        }

        public void InstallWorkloadPack(PackInfo packInfo, SdkFeatureBand sdkFeatureBand, DirectoryPath? offlineCache = null)
        {
            InstalledPacks.Add(packInfo);
            CachePath = offlineCache?.Value;
        }

        public void RollBackWorkloadPackInstall(PackInfo packInfo, SdkFeatureBand sdkFeatureBand)
        {
            if (FailingRollback)
            {
                throw new Exception("Rollback failure");
            }
            RolledBackPacks.Add(packInfo);
        }

        public void GarbageCollectInstalledWorkloadPacks()
        {
            GarbageCollectionCalled = true;
        }

        public InstallationUnit GetInstallationUnit()
        {
            return InstallationUnit.Packs;
        }

        public IWorkloadPackInstaller GetPackInstaller()
        {
            return this;
        }

        public IWorkloadInstallationRecordRepository GetWorkloadInstallationRecordRepository()
        {
            return InstallationRecordRepository;
        }

        public void InstallWorkloadManifest(ManifestId manifestId, ManifestVersion manifestVersion, SdkFeatureBand sdkFeatureBand, DirectoryPath? offlineCache = null)
        {
            InstalledManifests.Add((manifestId, manifestVersion, sdkFeatureBand, offlineCache));
        }

        public void DownloadToOfflineCache(PackInfo pack, DirectoryPath cachePath, bool includePreviews)
        {
            CachedPacks.Add(pack);
            CachePath = cachePath.Value;
        }

        public IEnumerable<PackInfo> GetInstalledPacks(SdkFeatureBand sdkFeatureBand)
        {
            return InstalledPacks;
        }

        public IWorkloadInstaller GetWorkloadInstaller() => throw new NotImplementedException();
    }

    internal class MockInstallationRecordRepository : IWorkloadInstallationRecordRepository
    {
        public IList<WorkloadId> WorkloadInstallRecord = new List<WorkloadId>();
        private readonly string FailingWorkload;
        public IList<WorkloadId> InstalledWorkloads;

        public MockInstallationRecordRepository(string failingWorkload = null, IList<WorkloadId> installedWorkloads = null)
        {
            FailingWorkload = failingWorkload;
            InstalledWorkloads = installedWorkloads ?? new List<WorkloadId>();
        }

        public void WriteWorkloadInstallationRecord(WorkloadId workloadId, SdkFeatureBand sdkFeatureBand)
        {
            WorkloadInstallRecord.Add(workloadId);
            if (workloadId.ToString().Equals(FailingWorkload))
            {
                throw new Exception($"Failing workload: {workloadId}");
            }
        }

        public void DeleteWorkloadInstallationRecord(WorkloadId workloadId, SdkFeatureBand sdkFeatureBand)
        {
            WorkloadInstallRecord.Remove(workloadId);
        }
        public IEnumerable<WorkloadId> GetInstalledWorkloads(SdkFeatureBand sdkFeatureBand)
        {
            return InstalledWorkloads;
        }

        public IEnumerable<SdkFeatureBand> GetFeatureBandsWithInstallationRecords() => throw new NotImplementedException();
    }
}
