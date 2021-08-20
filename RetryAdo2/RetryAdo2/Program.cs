// ADO.Netの再試行ロジックの学習用プログラム
// Microsoft Docs「手順 4:ADO.NET で SQL に弾性的に接続する」のサンプルコードを基に変更。
// ・Microsoft Docs「手順 4:ADO.NET で SQL に弾性的に接続する」
// 　https://docs.microsoft.com/ja-jp/sql/connect/ado-net/step-4-connect-resiliently-sql-ado-net

using System;  // C#
using CG = System.Collections.Generic;
using QC = Microsoft.Data.SqlClient;
using TD = System.Threading;
using System.CommandLine; // System.CommandLine (https://github.com/dotnet/command-line-api)
using System.CommandLine.Invocation;
using System.IO;

namespace RetryAdo2
{
    public class Program
    {
        static public int Main(string[] args)
        {
            var rootCommand = new RootCommand
            {
                new Option<int>(
                    new string[] {"--max-retry", "-m" },
                    getDefaultValue: () => 4,
                    description: "An option whose argument is max retry count"),
                new Option<bool>(
                    new string[] {"--force", "-f" },
                    getDefaultValue: () => false,
                    description: "An option whose argument is force retry"),
                new Option<int>(
                    new string[] {"--interval","-i" },
                    getDefaultValue: () => 10,
                    description: "An option whose argument is retry interval seconds"),
                new Option<string>(
                    new string[] { "--data-source", "-d" },
                    getDefaultValue: () => "tcp:myazuresqldbserver.database.windows.net,1433", //["Server"]
                    description: "An option whose argument is connection data source"),
                new Option<string>(
                    new string[] { "--initial-catalog", "-c" },
                    getDefaultValue: () => "MyDatabase", //["Database"]
                    description: "An option whose argument is connection initial catalog"),
                new Option<string>(
                    new string[] { "--user", "-u" },
                    getDefaultValue: () => "MyLogin", // "@yourservername"  as suffix sometimes. 
                    description: "An option whose argument is connection user id"),
                new Option<string>(
                    new string[] { "--password", "-p" },
                    getDefaultValue: () => "MyPassword",
                    description: "An option whose argument is connection user password"),
                new Option<int>(
                    new string[] { "--cn-max-retry", "-cm" },
                    getDefaultValue: () => 3,
                    description: "An option whose argument is connection max retry count"),
                new Option<int>(
                    new string[] { "--cn--interval", "-ci" },
                    getDefaultValue: () => 10,
                    description: "An option whose argument is retry connection interval seconds"),
                new Option<int>(
                    new string[] { "--cn--timeout", "-ct" },
                    getDefaultValue: () => 30,
                    description: "An option whose argument is retry connection timeout seconds"),
                new Option<int>(
                    new string[] { "--com--timeout", "-comt" },
                    getDefaultValue: () => 30,
                    description: "An option whose argument is retry command timeout seconds")

            };
            rootCommand.Description = "ADO.Net Retry sample app";

            // Note that the parameters of the handler method are matched according to the names of the options
            rootCommand.Handler = CommandHandler.Create<int, bool, int, string, string, string, string, int, int, int, int>((maxRetry, force, interval, dataSource, initialCatalog, user, password, cnMaxRetry, cnInterval, cnTimeout, comTimeout) =>
            {
                bool succeeded = false;
                int totalNumberOfTimesToTry = maxRetry;
                int retryIntervalSeconds = interval;

                Console.WriteLine("Start Retry sample:Max retry count = {0} :Force retry = {1}", totalNumberOfTimesToTry, force);

                for (int tries = 1;
                  tries <= totalNumberOfTimesToTry;
                  tries++)
                {
                    try
                    {
                        if (tries > 1)
                        {
                            string message = succeeded && force ? "Force retry." : "Transient error encountered.";
                            Console.WriteLine
                              ("{0} Will begin attempt number {1} of {2} max...",
                              message,
                              tries, totalNumberOfTimesToTry
                              );
                            TD.Thread.Sleep(1000 * retryIntervalSeconds);
                            retryIntervalSeconds = Convert.ToInt32
                              (retryIntervalSeconds * 1.5);
                        }
                        Console.WriteLine("Start access of databas");
                        AccessDatabase(dataSource, initialCatalog, user, password, cnMaxRetry, cnInterval, cnTimeout, comTimeout);
                        succeeded = true;
                        if (force)
                        {
                            Console.WriteLine("SUCCESS: next access the database!");
                            continue;
                        }
                        break;
                    }

                    catch (QC.SqlException sqlExc)
                    {
                        Console.WriteLine("SqlException: Number {0} / Message: {1}", sqlExc.Number, sqlExc.Message);

                        if (TransientErrorNumbers.Contains
                          (sqlExc.Number) == true)
                        {
                            Console.WriteLine("{0}: transient occurred.", sqlExc.Number);
                            continue;
                        }
                        else
                        {
                            Console.WriteLine(sqlExc);
                            succeeded = false;
                            if (force)
                            {
                                Console.WriteLine("ERROR: next access the database!");
                                continue;
                            }
                            break;
                        }
                    }

                    catch (TestSqlException sqlExc)
                    {
                        if (TransientErrorNumbers.Contains
                          (sqlExc.Number) == true)
                        {
                            Console.WriteLine("{0}: transient occurred. (TESTING.)", sqlExc.Number);
                            continue;
                        }
                        else
                        {
                            Console.WriteLine(sqlExc);
                            succeeded = false;
                            if (force)
                            {
                                Console.WriteLine("ERROR: next access the database!");
                                continue;
                            }
                            break;
                        }
                    }

                    catch (Exception Exc)
                    {
                        Console.WriteLine(Exc);
                        succeeded = false;
                        if (force)
                        {
                            Console.WriteLine("ERROR: next access the database!");
                            continue;
                        }
                        break;
                    }

                    finally
                    {
                        Console.WriteLine("End access of databas");
                    }
                }

                if (succeeded == true)
                {
                    Console.WriteLine("SUCCESS: eable to access the database!");
                    return 0;
                }
                else
                {
                    Console.WriteLine("ERROR: Unable to access the database!");
                    return 1;
                }
            });

            // Parse the incoming args and invoke the handler
            return rootCommand.InvokeAsync(args).Result;

        }

        /// <summary>  
        /// Connects to the database, reads,  
        /// prints results to the console.  
        /// </summary>  
        static public void AccessDatabase(string dataSource, string initialCatalog, string user, string password, int connectMaxRetry, int connectInterval, int connectTimeout, int commandTimeout)
        {
            //throw new TestSqlException(4060); //(7654321);  // Uncomment for testing.  

            using (var sqlConnection = new QC.SqlConnection
                (GetSqlConnectionString(dataSource, initialCatalog, user, password, connectMaxRetry, connectInterval, connectTimeout)))
            {
                using (var dbCommand = sqlConnection.CreateCommand())
                {
                    // コマンドタイムアウトを設定する（既定値は 30 秒）
                    dbCommand.CommandTimeout = commandTimeout;

                    dbCommand.CommandText = @"
-- データ更新とスケーリングが分かるようsys.dm_os_sys_infoから論理CPU数を取得するクエリに変更
-- また、長時間処理の代わりとして、ウェイトと10回ループを追加

-- トランザクション開始
BEGIN TRAN

-- 変数宣言
DECLARE @index INTEGER

-- ループ用変数を初期化
SET @index = 0

WHILE @index < 10
BEGIN
    -- ループ用変数をインクリメント
    SET @index = @index + 1

    PRINT @index

    -- 1秒間のウェイト
    WAITFOR DELAY '00:00:01';

	-- 元と同じ値で更新
	UPDATE SalesLT.ProductDescription
	SET Description = 'Chromoly steel.'
	WHERE ProductDescriptionID = 3

	-- 論理CPU数を取得
    SELECT 'cpu_count', CONVERT(NVARCHAR, cpu_count)  from sys.dm_os_sys_info
END

-- トランザクション終了（コミット）
COMMIT;";
/* "
    SELECT TOP 3  
        ob.name,  
        CAST(ob.object_id as nvarchar(32)) as [object_id]  
      FROM sys.objects as ob  
      WHERE ob.type='IT'  
      ORDER BY ob.name"
*/
                    sqlConnection.Open();
                    var dataReader = dbCommand.ExecuteReader();

                    while (dataReader.Read())
                    {
                        Console.WriteLine("{0}\t{1}",
                          dataReader.GetString(0),
                          dataReader.GetString(1));
                    }
                }
            }
        }

        /// <summary>  
        /// You must edit the four 'my' string values.  
        /// </summary>  
        /// <returns>An ADO.NET connection string.</returns>  
        static private string GetSqlConnectionString(string dataSource, string initialCatalog, string user, string password, int connectMaxRetry, int connectInterval, int connectTimeout)
        {
            // Prepare the connection string to Azure SQL Database.  
            var sqlConnectionSB = new QC.SqlConnectionStringBuilder();

            // Change these values to your values.  
            sqlConnectionSB.DataSource = dataSource; //["Server"]  
            sqlConnectionSB.InitialCatalog = initialCatalog; //["Database"]  

            sqlConnectionSB.UserID = user;  // "@yourservername"  as suffix sometimes.  
            sqlConnectionSB.Password = password;
            sqlConnectionSB.IntegratedSecurity = false;

            // Adjust these values if you like. (ADO.NET 4.5.1 or later.)  
            sqlConnectionSB.ConnectRetryCount = connectMaxRetry;
            sqlConnectionSB.ConnectRetryInterval = connectInterval;  // Seconds.  

            // Leave these values as they are.  
            sqlConnectionSB.IntegratedSecurity = false;
            sqlConnectionSB.Encrypt = true;
            sqlConnectionSB.ConnectTimeout = connectTimeout;

            return sqlConnectionSB.ToString();
        }

        static public CG.List<int> TransientErrorNumbers =
          new CG.List<int> { 4060, 40197, 40501, 40613,
      49918, 49919, 49920, 11001
      , 53 /* ネットワーク切断テスト用（11001の代わり。RDP利用時にネットワークの切断はNGのため） */
      ,18456 /* 接続ユーザー誤りのテスト用 */
      , -2 /* 実行タイムアウトのテスト用 */ };
    }

    /// <summary>  
    /// For testing retry logic, you can have method  
    /// AccessDatabase start by throwing a new  
    /// TestSqlException with a Number that does  
    /// or does not match a transient error number  
    /// present in TransientErrorNumbers.  
    /// </summary>  
    internal class TestSqlException : ApplicationException
    {
        internal TestSqlException(int testErrorNumber)
        { this.Number = testErrorNumber; }

        internal int Number
        { get; set; }
    }
}
