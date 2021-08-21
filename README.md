# ADO.Netの再試行ロジックの学習用プログラム
Microsoft Docs「手順 4:ADO.NET で SQL に弾性的に接続する」のサンプルコードを基に変更。
- Microsoft Docs「手順 4:ADO.NET で SQL に弾性的に接続する」
    - https://docs.microsoft.com/ja-jp/sql/connect/ado-net/step-4-connect-resiliently-sql-ado-net

## 使用するデータベース
基にしたサンプルと同様、AdventureWorks スキーを使用。
- Microsoft Docs「ステップ 2: ADO.NET 開発用の SQL データベースを作成する」を参照
  - https://docs.microsoft.com/ja-jp/sql/connect/ado-net/step-2-create-sql-database-ado-net-development?view=sql-server-ver15

## ポイントと注意点
- .Net Framework 4.8 以降で動作。
- 再試行の回数や待ち時間、DB接続情報などコマンドの引数で変更可能。
- 初回の接続で成功し、再試行が不要でも強制的に再試行するオプション有。
- コマンド引数の詳細は「-?」「-h」「--help」オプションで確認可能。
- クエリを論理CPU数を取得する内容に変更し、クエリを実行したSQL Databaseの仮想マシンが、スケーリング前かスケーリング後か確認可能。
- ネットワーク切断のテストの代わりにホスト名誤りのテストを実施可能。
- コマンドのタイムアウトも再試行する。
- 接続情報がソースに直書きされており、接続先に合わせてソース修正が必要。

## コマンド引数（「--help」オプションの実行結果）

```
C:\{Your Path}>RetryAdo2.exe --help
RetryAdo2
  ADO.Net Retry sample app

Usage:
  RetryAdo2 [options]

Options:
  -m, --max-retry <max-retry>              An option whose argument is max retry count [default: 4]
  -f, --force                              An option whose argument is force retry [default: False]
  -i, --interval <interval>                An option whose argument is retry interval seconds [default: 10]
  -d, --data-source <data-source>          An option whose argument is connection data source [default:
                                           tcp:myazuresqldbserver.database.windows.net,1433]
  -c, --initial-catalog <initial-catalog>  An option whose argument is connection initial catalog [default: MyDatabase]
  -u, --user <user>                        An option whose argument is connection user id [default: MyLogin]
  -p, --password <password>                An option whose argument is connection user password [default: MyPassword]
  -cm, --cn-max-retry <cn-max-retry>       An option whose argument is connection max retry count [default: 3]
  -ci, --cn--interval <cn--interval>       An option whose argument is retry connection interval seconds [default: 10]
  -ct, --cn--timeout <cn--timeout>         An option whose argument is retry connection timeout seconds [default: 30]
  -comt, --com--timeout <com--timeout>     An option whose argument is retry command timeout seconds [default: 30]
  --version                                Show version information
  -?, -h, --help                           Show help and usage information
```
