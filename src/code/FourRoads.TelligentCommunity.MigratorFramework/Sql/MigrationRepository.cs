﻿using System;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;
using FourRoads.TelligentCommunity.MigratorFramework.Entities;
using FourRoads.TelligentCommunity.MigratorFramework.Interfaces;
using Telligent.Evolution.Extensibility;
using Telligent.Evolution.Extensibility.Api.Version1;

namespace FourRoads.TelligentCommunity.MigratorFramework.Sql
{
    public class MigrationRepository : IMigrationRepository
    {
        
        private SqlConnection GetSqlConnection()
        {
            return Apis.Get<IDatabaseConnections>().GetConnection("SiteSqlServer");
        }

        public void Install(Version lastInstalledVersion)
        {
            string table = @"IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[fr_MigrationProcessed]') AND type in (N'U'))
                                BEGIN

                                CREATE TABLE [dbo].[fr_MigrationProcessed](
	                                [Id] [int] NOT NULL IDENTITY(1,1),
                                    [Created] DateTime2 NOT NULL,
	                                [ObjectType] [nvarchar](20) NOT NULL,
	                                [SourceKey] [nvarchar](50) NOT NULL,
                                    [ResultKey] [nvarchar](50) NOT NULL,
                                 CONSTRAINT [PK_fr_MigrationProcessed] PRIMARY KEY CLUSTERED 
                                (
	                                [Id] ASC
                                )WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
                                ) ON [PRIMARY]

                                END";

            string failedTable = @"IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[fr_MigrationFailed]') AND type in (N'U'))
                                BEGIN

                                CREATE TABLE [dbo].[fr_MigrationFailed](
	                                [Id] [int] NOT NULL IDENTITY(1,1),
                                    [Created] DateTime2 NOT NULL,
	                                [ObjectType] [nvarchar](20) NOT NULL,
	                                [SourceKey] [nvarchar](50) NOT NULL,
                                    [Error] [nvarchar](max) NOT NULL,
                                 CONSTRAINT [PK_fr_MigrationFailed] PRIMARY KEY CLUSTERED 
                                (
	                                [Id] ASC
                                )WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
                                ) ON [PRIMARY]

                                END";

            string redirectTable = @"IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[fr_MigrationRedirector]') AND type in (N'U'))
                                BEGIN

                                CREATE TABLE [dbo].[fr_MigrationRedirector](
	                                [Id] [int] NOT NULL  IDENTITY(1,1),
	                                [SourceUrl] [nvarchar](max) NOT NULL,
                                    [ResultUrl] [nvarchar](max) NOT NULL,
                                 CONSTRAINT [PK_fr_MigrationRedirector] PRIMARY KEY CLUSTERED 
                                (
	                                [Id] ASC
                                )WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
                                ) ON [PRIMARY]

                                END";

            string contextTable = @"IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[fr_MigrationContext]') AND type in (N'U'))
                                BEGIN

                                CREATE TABLE [dbo].[fr_MigrationContext](
	                                [Id] [int] NOT NULL  IDENTITY(1,1),
                                    [TotalRows] [int] NOT NULL,
                                    [ProcessedRows] [int] NOT NULL,
                                    [State] [int] NOT NULL,
                                    [RowsProcessingTimeAvg] [decimal] NOT NULL,
                                    [CurrentObjectType] [nvarchar](20) NOT NULL,
                                    [Started] [DateTime2] NOT NULL,
                                    [LastUpdated] [DateTime2] NOT NULL,
                                 CONSTRAINT [PK_fr_MigrationContext] PRIMARY KEY CLUSTERED 
                                (
	                                [Id] ASC
                                )WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
                                ) ON [PRIMARY]

                                END
            ";

            using (var connection = GetSqlConnection())
            {
                connection.Open();

                using (var command = new SqlCommand(table, connection))
                {
                    command.ExecuteNonQuery();
                }

                using (var command = new SqlCommand(contextTable, connection))
                {
                    command.ExecuteNonQuery();
                }

                using (var command = new SqlCommand(redirectTable, connection))
                {
                    command.ExecuteNonQuery();
                }

                using (var command = new SqlCommand(failedTable, connection))
                {
                    command.ExecuteNonQuery();
                }
            }
        }

        public async Task<IPagedList<MigratedData>> List(int pageSize, int pageIndex)
        {
            int startRow = pageIndex * pageSize;
            int endRow = startRow + pageSize;

            PagedList<MigratedData> results = new PagedList<MigratedData>();

            string query = $@"  SELECT  *
                            FROM    ( SELECT  * , ROW_NUMBER() OVER ( ORDER BY Id ) AS RowNum 
                                      FROM      fr_MigrationProcessed
                                    ) AS RowConstrainedResult
                            WHERE   RowNum >= {startRow}
                                AND RowNum <= {endRow}
                            ORDER BY RowNum
            ";

            string count = @" SELECT Count(Id) FROM fr_MigrationProcessed";

            using (var connection = GetSqlConnection())
            {
                connection.Open();

                using (var command = new SqlCommand(query, connection))
                {
                    using (var r = await command.ExecuteReaderAsync())
                    {
                        while (r.Read())
                        {
                            results.Add(new MigratedData()
                            {
                                ObjectType = Convert.ToString(r["ObjectType"]),
                                SourceKey = Convert.ToString(r["SourceKey"]),
                                ResultKey = Convert.ToString(r["ResultKey"]),
                            });
                        }
                    }
                }

                using (var command = new SqlCommand(count, connection))
                {
                    results.Total =Convert.ToInt32( command.ExecuteScalar());
                }
            }

            return results;
        }

        public async Task<MigrationContext> CreateUpdate(MigratedData migratedData, double processingTimeTotal)
        {
            string create = @"
                    INSERT INTO fr_MigrationProcessed (ObjectType, SourceKey, ResultKey, Created)
                    VALUES (@ObjectType,@SourceKey,@ResultKey,@Created);

                    UPDATE fr_MigrationContext 
                            SET ProcessedRows = ProcessedRows + 1, 
                                    RowsProcessingTimeAvg=@RowsProcessingTimeAvg ,
                                    CurrentObjectType=@ObjectType,
                                    LastUpdated=GetDate()
                                    WHERE ID = (SELECT Top 1 Id FROM fr_MigrationContext);

                    SELECT TOP 1 * FROM fr_MigrationContext;
            ";

            using (var connection = GetSqlConnection())
            {
                connection.Open();

                using (var command = new SqlCommand(create, connection))
                {
                    command.Parameters.Add("@ObjectType", SqlDbType.NText, 20).Value = migratedData.ObjectType;
                    command.Parameters.Add("@SourceKey", SqlDbType.NText, 50).Value = migratedData.SourceKey;
                    command.Parameters.Add("@ResultKey", SqlDbType.NText, 50).Value = migratedData.ResultKey;
                    command.Parameters.Add("@Created", SqlDbType.DateTime2).Value =DateTime.Now;
                    command.Parameters.Add("@RowsProcessingTimeAvg", SqlDbType.Decimal).Value = processingTimeTotal;

                    using (var r = await command.ExecuteReaderAsync())
                    {
                        if (r.Read())
                        {
                            return new MigrationContext()
                            {
                                ProcessedRows = Convert.ToInt32(r["ProcessedRows"]),
                                RowsProcessingTimeAvg = Convert.ToDecimal(r["RowsProcessingTimeAvg"]),
                                State = (MigrationState) Convert.ToInt32(r["State"]),
                                TotalRows = Convert.ToInt32(r["TotalRows"]),
                                CurrentObjectType = Convert.ToString(r["CurrentObjectType"]),
                                Started = Convert.ToDateTime(r["Started"]),
                                LastUpdated = Convert.ToDateTime(r["LastUpdated"]),
                            };
                        }
                    }
                }
            }

            return null;
        }

        public async Task<MigratedData> GetMigratedData(string objectType, string sourceKey)
        {
            string create = @"
                   SELECT * FROM  fr_MigrationProcessed WHERE ObjectType = @ObjectType AND SourceKey= @SourceKey
            ";

            using (var connection = GetSqlConnection())
            {
                connection.Open();

                using (var command = new SqlCommand(create, connection))
                {
                    command.Parameters.Add("@ObjectType", SqlDbType.NVarChar, 20).Value = objectType;
                    command.Parameters.Add("@SourceKey", SqlDbType.NVarChar, 50).Value = sourceKey;
                    using (var r = await command.ExecuteReaderAsync())
                    {
                        if (r.NextResult())
                        {
                            if (r.Read())
                            {
                                return new MigratedData()
                                {
                                    ObjectType = Convert.ToString(r["ObjectType"]),
                                    SourceKey = Convert.ToString(r["SourceKey"]),
                                    ResultKey = Convert.ToString(r["ResultKey"]),
                                };
                            }
                        }
                    }
                }
            }

            return null;
        }

        public void SetTotalRecords(int totalProcessing)
        {
            string udpate = @"
                    UPDATE fr_MigrationContext SET TotalRows = @TotalRows , LastUpdated=GetDate() WHERE ID = (SELECT Top 1 Id FROM fr_MigrationContext)";

            using (var connection = GetSqlConnection())
            {
                connection.Open();

                using (var command = new SqlCommand(udpate, connection))
                {
                    command.Parameters.Add("@TotalRows", SqlDbType.Int).Value = totalProcessing;

                    command.ExecuteNonQuery();
                }
            }
        }

        public void CreateUrlRedirect(string source, string destination)
        {
            string create = @"
                    INSERT INTO fr_MigrationRedirector (SourceUrl, ResultUrl)
                    VALUES (@SourceUrl,@ResultUrl)
            ";

            using (var connection = GetSqlConnection())
            {
                connection.Open();

                using (var command = new SqlCommand(create, connection))
                {
                    command.Parameters.Add("@SourceUrl", SqlDbType.NText, int.MaxValue).Value = source;
                    command.Parameters.Add("@ResultUrl", SqlDbType.NText, int.MaxValue).Value = destination;

                    command.ExecuteNonQuery();
                }
            }
        }

        public void SetState(MigrationState state)
        {
            string create = @" UPDATE fr_MigrationContext SET State = @State, LastUpdated=GetDate() WHERE ID = (SELECT Top 1 Id FROM fr_MigrationContext)";

            using (var connection = GetSqlConnection())
            {
                connection.Open();

                using (var command = new SqlCommand(create, connection))
                {
                    command.Parameters.Add("@State", SqlDbType.Int).Value = (int)state;

                    command.ExecuteNonQuery();
                }
            }

        }

        public void CreateNewContext()
        {
            string create = @"
                    INSERT INTO fr_MigrationContext (TotalRows, ProcessedRows, State, RowsProcessingTimeAvg, Started, LastUpdated,CurrentObjectType)
                    VALUES (0, 0, @State, 0, GetDate(), GetDate(),'')
            ";

            using (var connection = GetSqlConnection())
            {
                connection.Open();

                using (var command = new SqlCommand(create, connection))
                {
                    command.Parameters.Add("@State", SqlDbType.Int).Value = MigrationState.Pending;
                    
                    command.ExecuteNonQuery();
                }
            }
        }

        public void ResetJob()
        {
            string create = @"
                    DELETE fr_MigrationContext";

            using (var connection = GetSqlConnection())
            {
                connection.Open();

                using (var command = new SqlCommand(create, connection))
                {
                    command.ExecuteNonQuery();
                }
            }
        }

        public void FailedItem(string objectType , string key, string error)
        {
            string create = @"
                    INSERT INTO fr_MigrationFailed (ObjectType, SourceKey, Error, Created)
                    VALUES (@ObjectType,@SourceKey,@Error,GetDate());
            ";

            using (var connection = GetSqlConnection())
            {
                connection.Open();

                using (var command = new SqlCommand(create, connection))
                {
                    command.Parameters.Add("@ObjectType", SqlDbType.NText, 20).Value = objectType;
                    command.Parameters.Add("@SourceKey", SqlDbType.NText, 50).Value = key;
                    command.Parameters.Add("@Error", SqlDbType.NText, int.MaxValue).Value = error;

                    command.ExecuteNonQuery();
                }
            }
        }

        public async Task<MigrationContext> GetMigrationContext()
        {
            string create = @"
                    SELECT TOP 1 * FROM fr_MigrationContext;
            ";

            using (var connection = GetSqlConnection())
            {
                connection.Open();

                using (var command = new SqlCommand(create, connection))
                {
                    using (var r = await command.ExecuteReaderAsync())
                    {
                        if (r.Read())
                        {
                            return new MigrationContext()
                            {
                                ProcessedRows = Convert.ToInt32(r["ProcessedRows"]),
                                RowsProcessingTimeAvg = Convert.ToDecimal(r["RowsProcessingTimeAvg"]),
                                State = (MigrationState)Convert.ToInt32(r["State"]),
                                TotalRows = Convert.ToInt32(r["TotalRows"]),
                                CurrentObjectType = Convert.ToString(r["CurrentObjectType"]),
                                Started = Convert.ToDateTime(r["Started"]),
                                LastUpdated = Convert.ToDateTime(r["LastUpdated"]),
                            };
                        }
                    }
                }
            }

            return new MigrationContext();
        }
    }
}
