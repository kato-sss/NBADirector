# NBA 選手名鑑 - ASP.NET Core MVC

NBAの全30チームの選手情報をリアルタイムAPIで取得・表示するWebアプリです。

## 機能

- 全30チームをイースタン/ウェスタン・カンファレンス + ディビジョン別に表示
- チームをクリックすると選手一覧ページへ遷移
- 選手一覧はテーブル表示とカード表示に切り替え可能
- 選手名・ポジション・国籍でフィルタリング検索
- JSON APIエンドポイント (`/api/PlayersApi/teams`, `/api/PlayersApi/teams/{id}/players`)
- APIが利用できない場合はフォールバックデータを表示

## 使用技術

- **ASP.NET Core 8.0 MVC**
- **Bootstrap 5.3**
- **BallDontLie API v1** (https://www.balldontlie.io/)
- **Bebas Neue / Barlow Condensed** フォント

## セットアップ

### 1. .NET SDK インストール

```bash
# .NET 8.0 SDK が必要です
dotnet --version
```

### 2. APIキーの取得（無料）

1. https://www.balldontlie.io/ にアクセス
2. 無料アカウントを作成してAPIキーを取得
3. `Services/NbaApiService.cs` の `ApiKey` 定数にキーを設定:

```csharp
private const string ApiKey = "YOUR_API_KEY_HERE";
```

または `appsettings.json` で管理する場合:

```json
{
  "NbaApi": {
    "ApiKey": "YOUR_API_KEY_HERE"
  }
}
```

### 3. 起動

```bash
cd NBADirectory
dotnet restore
dotnet run
```

ブラウザで `https://localhost:5001` を開く。

## APIエンドポイント

| メソッド | URL | 説明 |
|--------|-----|------|
| GET | `/api/PlayersApi/teams` | 全チーム一覧 (JSON) |
| GET | `/api/PlayersApi/teams/{id}/players` | チームの選手一覧 (JSON) |
| GET | `/api/PlayersApi/teams/{id}/players?search=xxx` | 選手フィルタリング |

## ディレクトリ構成

```
NBADirectory/
├── Controllers/
│   ├── HomeController.cs     # トップページ（チーム一覧）
│   └── TeamController.cs     # チーム詳細 + API Controller
├── Models/
│   └── NbaModels.cs          # Team / Player / ViewModel等
├── Services/
│   └── NbaApiService.cs      # BallDontLie API クライアント
├── Views/
│   ├── Home/Index.cshtml     # チーム一覧ページ
│   ├── Team/Index.cshtml     # 選手一覧ページ
│   └── Shared/_Layout.cshtml # レイアウト
└── wwwroot/
    ├── css/site.css          # カスタムスタイル
    └── js/site.js            # アニメーション等
```

## フォールバック機能

APIキー未設定やネットワーク障害時は、ハードコードされたスタティックデータを表示します（Lakers・Celticsは選手データあり、その他チームはサンプル1名）。
