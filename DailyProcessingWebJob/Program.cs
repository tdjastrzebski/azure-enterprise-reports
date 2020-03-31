/*
 * Copyright © Tomasz Jastrzębski 2019-2020
 */
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Newtonsoft.Json;
using System;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;
using Tab = Microsoft.AnalysisServices.Tabular;

public class Program
{
    private readonly ILogger _logger;
    private readonly IConfiguration _config;

    // mandatory params
    private readonly string SqlServer;
    private readonly string SqlDatabase;
    private readonly string SqlUser;
    private readonly string SqlPassword;

    private readonly string EaEnrollmentNumber;
    private readonly string EaAccessKey;

    private readonly string AppTenantId;
    private readonly string AppClientId;
    private readonly string AppClientSecret;

    private readonly string AasService;
    private readonly string AasServer;
    private readonly string AasDatabase;

    // optional params
    private readonly int MaxAttemptCount;
    private readonly int HttpRequestTimeout;
    private readonly int BatchCopyTimeout;
    private readonly int BatchSize;
    private readonly int PeriodCommitTimeout;
    private readonly int MonthsToLookBack;
    private readonly int IndexDefragTimeout;
    private readonly bool TrackMaxLenghts;
    private readonly bool BufferStream;

    private DateTime _batchStartTime;
    
    // do not modify this class constructor, it is used for dependency injection
    public Program(ILogger<Program> logger, IConfiguration config)
    {
        _logger = logger;
        _config = config;

        //SqlConnection = _config.GetValue<string>("SqlConnection");
        SqlServer = _config.GetValue<string>("SqlServer");
        SqlDatabase = _config.GetValue<string>("SqlDatabase");
        SqlUser = _config.GetValue<string>("SqlUser");
        SqlPassword = _config.GetValue<string>("SqlPassword");
        EaEnrollmentNumber = _config.GetValue<string>("EaEnrollmentNumber");
        EaAccessKey = _config.GetValue<string>("EaAccessKey");

        AppTenantId = _config.GetValue<string>("AppTenantId");
        AppClientId = _config.GetValue<string>("AppClientId");
        AppClientSecret = _config.GetValue<string>("AppClientSecret");

        AasService = _config.GetValue<string>("AasService");
        AasServer = _config.GetValue<string>("AasServer");
        AasDatabase = _config.GetValue<string>("AasDatabase");

        MaxAttemptCount = _config.GetValue("MaxAttemptCount", 3);
        HttpRequestTimeout = _config.GetValue("HttpRequestTimeout", 1000 * 60 * 5); // 5 min
        BatchCopyTimeout = _config.GetValue("BatchCopyTimeout", 2 * 60); // 2 min
        BatchSize = _config.GetValue("BatchSize", 10000);
        PeriodCommitTimeout = _config.GetValue("PeriodCommitTimeout", 3 * 60 * 60); // 3h
        MonthsToLookBack = _config.GetValue("MonthsToLookBack", 0); // number of months to look back during initial data load, it seems 9 months is max value
        IndexDefragTimeout = _config.GetValue("IndexDefragTimeout", 2 * 60 * 60); // 2h
        TrackMaxLenghts = _config.GetValue("TrackMaxLenghts", false);
        BufferStream = _config.GetValue("BufferStream", false);
    }

    public static void Main(string[] args)
    {
        var builder = new HostBuilder()
            .ConfigureWebJobs(b => {
                b.AddAzureStorageCoreServices();
                b.AddTimers();
            })
            .ConfigureLogging((context, b) => {
                b.SetMinimumLevel(LogLevel.Trace);
                b.AddConsole();
                // If this key exists in any config, use it to enable App Insights
                string appInsightsKey = context.Configuration["APPINSIGHTS_INSTRUMENTATIONKEY"];

                if (!string.IsNullOrEmpty(appInsightsKey)) {
                    b.AddApplicationInsightsWebJobs(o => o.InstrumentationKey = appInsightsKey);
                }
            })
            .UseConsoleLifetime();

        var host = builder.Build();

        using (host) host.Run();
    }

    // Note: do not modify this method signature or attibutes, do not simplify "default(CancellationToken)", do not remove unused "timer" parameter
    [Singleton]
    [FunctionName("TimerJob")]
    public async Task TimerJob([TimerTrigger("%JobDailySchedule%", RunOnStartup = false)] TimerInfo timer, CancellationToken token = default(CancellationToken))
    {
        _logger.LogInformation("OS version: " + Environment.OSVersion);
        _logger.LogInformation("user interactive: " + Environment.UserInteractive);

        // test only:
        //await Run(DateTime.Parse("2018-01-01"), DateTime.Parse("2018-12-31"), token).ConfigureAwait(false);
        //await Run(@"c:\data.csv", DateTime.Parse("2018-01-01"), DateTime.Parse("2018-12-31"), token).ConfigureAwait(false);

        var startDate = DateTime.UtcNow.Date.AddDays(-4); // by default start 4 days ago
        var endDate = DateTime.UtcNow.Date.AddDays(-1); // and end on previous day
        DateTime? lastDate;

        using (var connection = GetSqlConnection()) {
            try {
                await connection.OpenAsync(token).ConfigureAwait(false);
            } catch (SqlException e) {
                _logger.LogError($"Error number {e.Number}, class {e.Class} while opening database connection: {e.Message}");
                return;
            }
            // check the last record date
            var cmd = new SqlCommand("select max([Date]) from dbo.AzureUsageRecords", connection);
            lastDate = await cmd.ExecuteScalarAsync(token).ConfigureAwait(false) as DateTime?;
            lastDate = lastDate?.AddDays(-2); // start 2 days earlier - just in case previous data import was incomplete
        }

        if (lastDate == null) {
            // no records yet, start from 1st of this month
            lastDate = new DateTime(DateTime.UtcNow.Date.Year, DateTime.UtcNow.Date.Month, 1).AddMonths(-MonthsToLookBack);
        }
        if (lastDate < startDate) startDate = lastDate.Value;

        await Run(startDate, endDate, token).ConfigureAwait(false);
    }

    private async Task Run(DateTime startDate, DateTime endDate, CancellationToken token = default(CancellationToken))
    {
        int attemptCount;

        for (int year = startDate.Year; year <= endDate.Year; year++) {
            int startMonth = (year == startDate.Year ? startDate.Month : 1);
            int endMonth = (year == endDate.Year ? endDate.Month : 12);

            for (int month = startMonth; month <= endMonth; month++) {
                DateTime dateFrom = new DateTime(year, month, 1);
                DateTime dateTo = new DateTime(year, month, 1).AddMonths(1).AddDays(-1);

                if (dateFrom < startDate) dateFrom = startDate;
                if (dateTo > endDate) dateTo = endDate;

                int readCount = 0;
                int commitCount = 0;
                attemptCount = 0;

                await TruncateStageRecords(token).ConfigureAwait(false);

                // download and stage records
                while (attemptCount < MaxAttemptCount) {
                    if (attemptCount > 0) {
                        // remove only last date from Stage and retry
                        DateTime? lastDate = await ClearLastStagedDate(token).ConfigureAwait(false);
                        if (lastDate.HasValue) dateFrom = lastDate.Value;
                    }

                    _logger.LogInformation($"processing records in date range {dateFrom:d} - {dateTo:d}, attempt {attemptCount + 1}");

                    try {
                        readCount = await UploadFromWebService(dateFrom, dateTo, token).ConfigureAwait(false);
                        break;
                    } catch (WebException ex) {
                        var response = ex.Response as HttpWebResponse;

                        if (response?.StatusCode == HttpStatusCode.NotFound) {
                            _logger.LogWarning($"no records returned for {dateFrom:d} - {dateTo:d} range");
                            break;
                        } else if (response?.StatusCode == HttpStatusCode.Unauthorized) {
                            _logger.LogError($"API access is unauthorized. Verify API access key is valid.");
                            return;
                        } else if (response?.StatusCode == HttpStatusCode.BadRequest) {
                            _logger.LogError($"Bad request. Verify API call parameters, particularly date range.");
                            return;
                        } else {
                            LogException(ex);
                        }
                    } catch (Exception ex) {
                        LogException(ex);
                    }

                    attemptCount++;
                }

                if (attemptCount == MaxAttemptCount) {
                    _logger.LogError("maximum number of attempts exceeded - terminating");
                    return;
                }

                attemptCount = 0;

                while (attemptCount < MaxAttemptCount && readCount > 0) {
                    _logger.LogInformation($"committing records, attempt {attemptCount + 1}");

                    try {
                        commitCount = await CommitRecords(dateFrom, dateTo, token).ConfigureAwait(false);
                        break;
                    } catch (Exception ex) {
                        LogException(ex);
                        attemptCount++;
                    }
                }

                if (attemptCount == MaxAttemptCount) {
                    _logger.LogError("maximum number of attempts exceeded - terminating");
                    return;
                }

                if (commitCount != readCount) {
                    _logger.LogWarning($"number of records read and commited does not match");
                }
            }
        }

        attemptCount = 0;

        while (attemptCount < MaxAttemptCount) {
            _logger.LogInformation($"defragmenting SQL Server indexes, attempt {attemptCount + 1}");

            try {
                var result = await DefragmentSqlDatabaseIndexes(token).ConfigureAwait(false);
                break;
            } catch (Exception ex) {
                LogException(ex);
                attemptCount++;
            }
        }

        attemptCount = 0;

        while (attemptCount < MaxAttemptCount) {
            _logger.LogInformation($"processing Analysis Services database, attempt {attemptCount + 1}");

            try {
                ProcessAnalysisServicesDatabase();
                break;
            } catch (Exception ex) {
                LogException(ex);
                attemptCount++;
            }
        }

        if (attemptCount == MaxAttemptCount) {
            _logger.LogError("maximum number of attempts exceeded - terminating");
            return;
        }
    }

    private async Task<int> UploadFromWebService(DateTime dateFrom, DateTime dateTo, CancellationToken token)
    {
        // call details: https://docs.microsoft.com/en-us/rest/api/billing/enterprise/billing-enterprise-api-usage-detail
        var request = WebRequest.CreateHttp($"https://consumption.azure.com/v3/enrollments/{EaEnrollmentNumber}/usagedetails/download?startTime={dateFrom:yyyy-MM-dd}&endTime={dateTo:yyyy-MM-dd}");
        request.Headers.Add(HttpRequestHeader.Authorization, "Bearer " + EaAccessKey);
        request.ContinueTimeout = HttpRequestTimeout;
        var startTime = DateTime.UtcNow;

        using (var response = (HttpWebResponse)await request.GetResponseAsync().ConfigureAwait(false)) {
            if (response.StatusCode != HttpStatusCode.OK || response.ContentType != "application/octet-stream") {
                throw new Exception($"Unexpected response or status code: {response.StatusCode} ({response.StatusDescription}), {response.ContentType}.");
            }

            using (var stream = response.GetResponseStream()) {
                var waitTime = DateTime.UtcNow.Subtract(startTime);
                _logger.LogInformation($"response received in {waitTime.TotalSeconds:n1} s");

                if (BufferStream) {
                    using (var buffer = new MemoryStream()) {
                        startTime = DateTime.UtcNow;
                        await stream.CopyToAsync(buffer, 8 * 1024, token).ConfigureAwait(false);
                        waitTime = DateTime.UtcNow.Subtract(startTime);
                        _logger.LogInformation($"{buffer.Length:n0} bytes received in {waitTime.TotalSeconds:n1} s");
                        buffer.Seek(0, SeekOrigin.Begin);
                        int recordCount = await UploadFromStream(buffer, dateFrom, dateTo, token).ConfigureAwait(false);
                        return recordCount;
                    }
                } else {
                    int recordCount = await UploadFromStream(stream, dateFrom, dateTo, token).ConfigureAwait(false);
                    return recordCount;
                }
            }
        }
    }

    /// <remarks>this method allows to load data from the csv file</remarks>
    private async Task<int> Run(string filePath, DateTime dateFrom, DateTime dateTo, CancellationToken token)
    {
        try {
            using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read)) {
                await TruncateStageRecords(token).ConfigureAwait(false);
                return await UploadFromStream(stream, dateFrom, dateTo, token).ConfigureAwait(false);
            }
        } catch (Exception ex) {
            LogException(ex);
            throw;
        }
    }

    private async Task TruncateStageRecords(CancellationToken token)
    {
        var startTime = DateTime.UtcNow;

        using (var connection = GetSqlConnection()) {
            await connection.OpenAsync(token).ConfigureAwait(false);
            var cmd = new SqlCommand("dbo.TruncateAzureUsageRecords_Stage", connection);
            cmd.CommandType = CommandType.StoredProcedure;
            await cmd.ExecuteNonQueryAsync(token).ConfigureAwait(false);
        }

        TimeSpan processingTime = DateTime.UtcNow.Subtract(startTime);
        _logger.LogInformation($"stage upload prepared in {processingTime.TotalSeconds:n1} s");
    }

    private async Task<DateTime?> ClearLastStagedDate(CancellationToken token)
    {
        var startTime = DateTime.UtcNow;
        DateTime? lastDate;

        using (var connection = GetSqlConnection()) {
            await connection.OpenAsync(token).ConfigureAwait(false);
            var cmd = new SqlCommand("select max([Date]) from dbo.AzureUsageRecords_Stage", connection);
            lastDate = await cmd.ExecuteScalarAsync(token).ConfigureAwait(false) as DateTime?;

            if (lastDate.HasValue) {
                cmd = new SqlCommand("delete from dbo.AzureUsageRecords_Stage with(tablock) where [Date] >= @date", connection);
                cmd.Parameters.Add("date", SqlDbType.DateTime).Value = lastDate;
                await cmd.ExecuteNonQueryAsync(token).ConfigureAwait(false);
            }
        }

        TimeSpan processingTime = DateTime.UtcNow.Subtract(startTime);
        _logger.LogInformation($"restart prepared in {processingTime.TotalSeconds:n1} s");

        if (lastDate.HasValue) {
            _logger.LogInformation($"restarting at {lastDate:d}");
        } else {
            _logger.LogInformation($"restarting at the beginning");
        }

        return lastDate;
    }

    private async Task<int> UploadFromStream(Stream stream, DateTime dateFrom, DateTime dateTo, CancellationToken token)
    {
        int recordCount;
        var startTime = DateTime.UtcNow;
        TimeSpan processingTime;

        using (var reader = new StreamReader(stream, Encoding.UTF8))
        using (var connection = GetSqlConnection()) {
            await connection.OpenAsync(token).ConfigureAwait(false);
            var bulkCopy = new SqlBulkCopy(connection, SqlBulkCopyOptions.FireTriggers | SqlBulkCopyOptions.TableLock, null);
            bulkCopy.DestinationTableName = "dbo.AzureUsageRecords_Stage";
            bulkCopy.BatchSize = BatchSize;
            bulkCopy.NotifyAfter = BatchSize;
            bulkCopy.BulkCopyTimeout = BatchCopyTimeout;
            bulkCopy.SqlRowsCopied += BulkCopy_SqlRowsCopied;

            startTime = DateTime.UtcNow;
            string line1 = await reader.ReadLineAsync().ConfigureAwait(false); // skip the first line - contains billing period

            using (var recReader = new CsvDataReader<DetailedUsage>(reader, x => { return Sink(x, dateFrom, dateTo); }, TrackMaxLenghts)) {
                _batchStartTime = DateTime.UtcNow;

                // note: by default SqlBulkCopy relies on column ordinal only - create mappings
                for (int sourceColumnOrdinal = 0; sourceColumnOrdinal < recReader.FieldCount; sourceColumnOrdinal++) {
                    string destinationColumnName = recReader.GetName(sourceColumnOrdinal);
                    bulkCopy.ColumnMappings.Add(new SqlBulkCopyColumnMapping(sourceColumnOrdinal, destinationColumnName));
                }

                try {
                    await bulkCopy.WriteToServerAsync(recReader, token).ConfigureAwait(false);
                } catch (SqlException ex) {
                    // Note: error 40197 with code 4815 indicates some text column length is too short
                    if (TrackMaxLenghts && ex.Number == 40197) {
                        string dataLengths = "Max field lengths registered:";
                        foreach (var field in recReader.MaxLenghts) {
                            dataLengths += $"\n{field.Key} : {field.Value}";
                        }
                        _logger.LogDebug(dataLengths);
                    }
                    throw;
                } catch (Exception ex) {
                    throw;
                }

                recordCount = recReader.RecordsAffected;
            }
        }

        processingTime = DateTime.UtcNow.Subtract(startTime);

        if (recordCount != 0 && processingTime.TotalSeconds != 0) {
            _logger.LogInformation($"total {recordCount:n0} records uploaded in {processingTime.TotalSeconds:n1} s ({recordCount / processingTime.TotalSeconds:n1} rec/s)");
        } else {
            _logger.LogInformation($"total {recordCount:n0} records uploaded in {processingTime.TotalSeconds:n1} s");
        }

        return recordCount;
    }

    /// <returns>returns TRUE if record should be stored, FALSE if record should be skipped</returns>
    private bool Sink(DetailedUsage record, DateTime dateFrom, DateTime dateTo)
    {
        if (record.Date < dateFrom || record.Date > dateTo) return false; // skip this record

        record.ResourceGuid = record.ResourceGuid ?? Guid.Empty;

        if (String.IsNullOrEmpty(record.ConsumedService)) {
            if (record.InstanceId.StartsWith("/subscriptions/", StringComparison.InvariantCultureIgnoreCase)) {
                // ConsumedService is missing - extract ConsumedService from InstanceId
                // typical InstanceId: /subscriptions/fcb03b08-822b-4a90-8361-f71493f54bd5/resourceGroups/mystorageaccounts/providers/Microsoft.Storage/storageAccounts/files0001
                int ind6 = "/subscriptions/".Length;
                ind6 = record.InstanceId.IndexOf('/', ind6 + 1);
                ind6 = record.InstanceId.IndexOf('/', ind6 + 1);
                ind6 = record.InstanceId.IndexOf('/', ind6 + 1);
                ind6 = record.InstanceId.IndexOf('/', ind6 + 1);
                int ind7 = record.InstanceId.IndexOf('/', ind6 + 1);
                record.ConsumedService = record.InstanceId.Substring(ind6 + 1, ind7 - ind6 - 1);
            }
        }

        if (!String.IsNullOrEmpty(record.Tags)) {
            record.Tags = TransformJsonTags(record.Tags);
        }

        return true;
    }

    private string TransformJsonTags(string json)
    {
        // converts single object attributes to array of objects with single attribute
        var textBuilder = new StringBuilder();

        using (var textWriter = new StringWriter(textBuilder)) {
            var writer = new JsonTextWriter(textWriter);

            using (var stream = new MemoryStream(Encoding.Unicode.GetBytes(json)))
            using (var textReader = new StreamReader(stream, Encoding.Unicode))
            using (var reader = new JsonTextReader(textReader)) {
                bool objectOpen = false;
                bool arrayOpen = false;

                while (reader.Read()) {
                    switch (reader.TokenType) {
                        case JsonToken.StartObject:
                            if (arrayOpen) throw new InvalidOperationException("Invalid writer state.");
                            writer.WriteStartArray();
                            arrayOpen = true;
                            break;
                        case JsonToken.PropertyName:
                            if (objectOpen) throw new InvalidOperationException("Invalid writer state.");
                            writer.WriteStartObject();
                            writer.WritePropertyName("name");
                            writer.WriteValue(reader.Value);
                            objectOpen = true;
                            break;
                        case JsonToken.String:
                        case JsonToken.Date:
                        case JsonToken.Integer:
                        case JsonToken.Float:
                        case JsonToken.Boolean:
                        case JsonToken.Null:
                            writer.WritePropertyName("value");
                            writer.WriteValue(reader.Value ?? "");
                            writer.WriteEndObject();
                            objectOpen = false;
                            break;
                        case JsonToken.EndObject:
                            writer.WriteEndArray();
                            arrayOpen = false;
                            break;
                        default:
                            throw new ArgumentException($"Unsupported {nameof(reader.TokenType)}: {reader.TokenType}.");
                    }
                }
            }

            return textBuilder.ToString();
        }
    }

    private void BulkCopy_SqlRowsCopied(object sender, SqlRowsCopiedEventArgs e)
    {
        var batchTime = DateTime.UtcNow - _batchStartTime;
        _logger.LogInformation($"{BatchSize:n0} records uploaded in {batchTime.TotalSeconds:n1} s ({BatchSize / batchTime.TotalSeconds:n1} rec/s), total {e.RowsCopied:n0} records");
        _batchStartTime = DateTime.UtcNow;
    }

    private async Task<int> CommitRecords(DateTime dateFrom, DateTime dateTo, CancellationToken token)
    {
        using (var connection = GetSqlConnection()) {
            var cmd = new SqlCommand("dbo.CommitUsageRecords", connection);
            cmd.CommandTimeout = PeriodCommitTimeout;
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.Add("@DateFrom", SqlDbType.Date).Value = dateFrom;
            cmd.Parameters.Add("@DateTo", SqlDbType.Date).Value = dateTo;

            var startTime = DateTime.UtcNow;
            await connection.OpenAsync(token).ConfigureAwait(false);
            int recordCount = await cmd.ExecuteNonQueryAsync(token).ConfigureAwait(false);
            var processTime = DateTime.UtcNow.Subtract(startTime);
            _logger.LogInformation($"{recordCount:n0} records commited in {processTime.TotalSeconds:n1} s ({recordCount / processTime.TotalSeconds:n1} rec/s)");

            return recordCount;
        }
    }

    private async Task<(int indexCount, int defragCount)> DefragmentSqlDatabaseIndexes(CancellationToken token)
    {
        using (var connection = GetSqlConnection()) {
            connection.InfoMessage += delegate (object sender, SqlInfoMessageEventArgs e) {
                string[] lines = e.Message.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
                foreach (string line in lines) _logger.LogInformation(line);
            };

            var cmd = new SqlCommand("dbo.DefragmentIndexes", connection);
            cmd.CommandTimeout = IndexDefragTimeout;
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.Add("@Table", SqlDbType.NVarChar);
            cmd.Parameters.Add("@IndexCount", SqlDbType.Int).Direction = ParameterDirection.Output;
            cmd.Parameters.Add("@DefragCount", SqlDbType.Int).Direction = ParameterDirection.Output;

            await connection.OpenAsync(token).ConfigureAwait(false);
            var startTime = DateTime.UtcNow;

            cmd.Parameters["@Table"].Value = "dbo.AzureUsageRecords";
            await cmd.ExecuteNonQueryAsync(token).ConfigureAwait(false);
            int indexCount = (int)cmd.Parameters["@IndexCount"].Value;
            int defragCount = (int)cmd.Parameters["@DefragCount"].Value;

            cmd.Parameters["@Table"].Value = "dbo.AzureUsageTags";
            await cmd.ExecuteNonQueryAsync(token).ConfigureAwait(false);
            indexCount += (int)cmd.Parameters["@IndexCount"].Value;
            defragCount += (int)cmd.Parameters["@DefragCount"].Value;
            
            connection.Close(); // close connection to purge pending InfoMessages

            var processTime = DateTime.UtcNow.Subtract(startTime);
            _logger.LogInformation($"{defragCount}/{indexCount} indexes defragmented in {processTime.TotalSeconds:n1} s");

            return (indexCount, defragCount);
        }
    }

    private SqlConnection GetSqlConnection(bool useServicePrincipal = false)
    {
        var builder = new SqlConnectionStringBuilder();

        builder.DataSource = SqlServer;
        builder.InitialCatalog = SqlDatabase;
        builder.ConnectTimeout = 30;
        builder.PersistSecurityInfo = false;
        builder.TrustServerCertificate = false;
        builder.Encrypt = true;
        builder.MultipleActiveResultSets = false;
        builder.NetworkLibrary = "dbmssocn";

        if (!useServicePrincipal) {
            builder.UserID = SqlUser;
            builder.Password = SqlPassword;
        }

        var con = new SqlConnection(builder.ToString());

        if (useServicePrincipal) {
            con.AccessToken = GetAccessToken("https://database.windows.net/");
        }

        return con;
    }

    private string GetAccessToken(string resource)
    {
        var authContext = new AuthenticationContext("https://login.microsoftonline.com/" + AppTenantId);
        var credential = new ClientCredential(AppClientId, AppClientSecret);
        var startTime = DateTime.UtcNow;
        var token = authContext.AcquireTokenAsync(resource, credential).Result;
        var processTime = DateTime.UtcNow.Subtract(startTime);
        _logger.LogInformation($"access token acquired in {processTime.TotalSeconds:n1} s");
        string accessToken = token.AccessToken;

        return accessToken;
    }

    private void ProcessAnalysisServicesDatabase()
    {
        var accessToken = GetAccessToken("https://" + AasService);

        var startTime = DateTime.UtcNow;

        using (Tab.Server aas = new Tab.Server()) {
            string connStr = $"Provider=MSOLAP;Data Source=asazure://{AasService}/{AasServer};Password={accessToken};Persist Security Info=True;Impersonation Level=Delegate;";
            aas.Connect(connStr);

            Tab.Database db = null;

            if (!aas.Databases.ContainsName(AasDatabase)) {
                // note: in case database is not found, verify service principal has *admin* rights - read/process rights are insufficent
                throw new ApplicationException($"Database '{AasDatabase}' not found. Make sure service principal has database Admin rights.");
            }

            db = aas.Databases[AasDatabase];
            db.Model.RequestRefresh(Tab.RefreshType.Full);
            db.Model.SaveChanges(); // commit executes the refresh
            aas.Disconnect();
        }

        var processTime = DateTime.UtcNow.Subtract(startTime);
        _logger.LogInformation($"database processed in {processTime.TotalSeconds:n1} s");
    }

    private void LogException(Exception e, [CallerLineNumber] int line = 0, [CallerMemberName] string caller = null, [CallerFilePath] string file = null)
    {
        file = file == null ? null : Path.GetFileName(file);

        if (e is SqlException) {
            _logger.LogError($"{nameof(SqlException)} number: {((SqlException)e).Number}, code: {((SqlException)e).ErrorCode}, message: {e.Message} function: {caller}(), line: {line}, file: {file}.\n{e.StackTrace}");
        } else {
            _logger.LogError($"{e.GetType().Name}, message: {e.Message}, function: {caller}(), line: {line}, file: {file}.\n{e.StackTrace}");
        }
    }
}
