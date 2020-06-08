﻿using System;
using System.Diagnostics;
using System.Threading.Tasks;
using FourRoads.TelligentCommunity.MigratorFramework.Entities;
using FourRoads.TelligentCommunity.MigratorFramework.Sql;

namespace FourRoads.TelligentCommunity.MigratorFramework.Interfaces
{
    public interface IMigrationRepository
    {
        IPagedList<MigratedData> List(int pageSize, int pageIndex);
        MigrationContext CreateUpdate(MigratedData migratedData, double processingTimeTotal);
        void Install(Version lastInstalledVersion);
        void SetTotalRecords(int totalProcessing);
        void CreateUrlRedirect(string source, string destination);
        MigratedData GetMigratedData(string objectType, string sourceKey);
        void SetState(MigrationState state);
        void CreateNewContext();
        void ResetJob();
        void FailedItem(string objectType, string key, string error);
        MigrationContext GetMigrationContext();
        void CreateLogEntry(string message, EventLogEntryType type);
        IPagedList<MigrationLog> ListLog(int pageSize, int pageIndex);
    }
}